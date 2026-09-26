using System.Text;
using System.Text.Json;
using ServiceLib.Discovery.Models;
using ServiceLib.Discovery.Services;
using ServiceLib.Helper;
using ServiceLib.Models.Entities;

namespace ServiceLib.FaultHarness;

internal static class Program
{
    private static readonly string Root = AppContext.BaseDirectory;
    private static readonly string CatalogPath = Path.Combine(Root, "catalog.json");
    private static readonly string MarkerPath = Path.Combine(Root, "fault-checkpoint.marker");

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.Error.WriteLine("Usage: ServiceLib.FaultHarness <init|worker|verify> [checkpoint]");
            return 2;
        }

        try
        {
            EnsureTables();
            switch (args[0])
            {
                case "init":
                    await InitializeAsync();
                    return 0;
                case "worker":
                    if (args.Length != 2)
                    {
                        throw new ArgumentException("worker requires a checkpoint");
                    }
                    await WorkerAsync(args[1]);
                    return 3;
                case "verify":
                    if (args.Length != 2)
                    {
                        throw new ArgumentException("verify requires a checkpoint");
                    }
                    await VerifyAsync(args[1]);
                    return 0;
                default:
                    throw new ArgumentException($"Unknown mode '{args[0]}'.");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
        finally
        {
            if (args[0] != "worker")
            {
                await SQLiteHelper.Instance.DisposeDbConnectionAsync();
            }
        }
    }

    private static void EnsureTables()
    {
        SQLiteHelper.Instance.CreateTable<ProviderAsnCatalogRegistryItem>();
        SQLiteHelper.Instance.CreateTable<ProviderAsnCatalogRevisionItem>();
    }

    private static async Task InitializeAsync()
    {
        var store = new SqliteProviderAsnCatalogRegistryStore();
        var existing = await store.ListAsync(new ProviderAsnCatalogRegistryQuery
        {
            IncludeDisabled = true,
            IncludeUnregistered = true,
            MaxItems = 1000,
        });
        if (existing.Count != 0)
        {
            throw new InvalidOperationException("Fault harness init requires an empty isolated database.");
        }

        await File.WriteAllBytesAsync(CatalogPath, Catalog("v1", "203.0.113.10"));
        var service = new ProviderAsnCatalogRegistryService(store);
        var registered = await service.RegisterAsync(CatalogPath);

        Console.WriteLine(JsonSerializer.Serialize(new
        {
            mode = "init",
            database = SQLiteHelper.Instance.DatabasePath,
            catalog = CatalogPath,
            registryId = registered.Id,
            catalogVersion = registered.CatalogVersion,
        }));
    }

    private static async Task WorkerAsync(string checkpoint)
    {
        ValidateCheckpoint(checkpoint);
        if (File.Exists(MarkerPath))
        {
            File.Delete(MarkerPath);
        }

        var store = new SqliteProviderAsnCatalogRegistryStore();
        var rows = await store.ListAsync(new ProviderAsnCatalogRegistryQuery
        {
            IncludeDisabled = true,
            IncludeUnregistered = true,
            MaxItems = 10,
        });
        var row = rows.Single();
        var service = new ProviderAsnCatalogRegistryService(store);
        var plan = await service.PrepareUpdateAsync(row.Id, Catalog("v2", "203.0.113.20"));

        if (checkpoint.StartsWith("after-rollback-", StringComparison.Ordinal))
        {
            var applied = await service.ApplyUpdateAsync(row.Id, plan);

            ProviderAsnCatalogFaultInjection.Handler = hit =>
            {
                if (!string.Equals(hit, checkpoint, StringComparison.Ordinal))
                {
                    return;
                }

                WriteDurableMarker(hit);
                Thread.Sleep(Timeout.Infinite);
            };

            await service.RollbackRevisionAsync(applied.Id);
        }
        else
        {
            ProviderAsnCatalogFaultInjection.Handler = hit =>
            {
                if (!string.Equals(hit, checkpoint, StringComparison.Ordinal))
                {
                    return;
                }

                WriteDurableMarker(hit);
                Thread.Sleep(Timeout.Infinite);
            };

            await service.ApplyUpdateAsync(row.Id, plan);
        }

        throw new InvalidOperationException($"Fault checkpoint '{checkpoint}' was not reached.");
    }

    private static async Task VerifyAsync(string checkpoint)
    {
        ValidateCheckpoint(checkpoint);

        var store = new SqliteProviderAsnCatalogRegistryStore();
        var service = new ProviderAsnCatalogRegistryService(store);
        var rows = await service.ListAsync(new ProviderAsnCatalogRegistryQuery
        {
            IncludeDisabled = true,
            IncludeUnregistered = true,
            MaxItems = 10,
        });
        var registry = rows.Single();

        var catalog = await JsonProviderAsnEndpointCatalog.LoadAsync(CatalogPath);
        var revisions = await store.ListRevisionsAsync(new ProviderAsnCatalogRevisionQuery
        {
            RegistryId = registry.Id,
            IncludeRolledBack = true,
            MaxItems = 10,
        });

        var rollbackCheckpoint = checkpoint.StartsWith("after-rollback-", StringComparison.Ordinal);
        var expectedCommitted = string.Equals(checkpoint, "after-registry-upsert", StringComparison.Ordinal);
        var expectedVersion = expectedCommitted ? "v2" : "v1";
        if (!string.Equals(catalog.Document.Version, expectedVersion, StringComparison.Ordinal)
            || !string.Equals(registry.CatalogVersion, expectedVersion, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Recovered file/registry version mismatch: file={catalog.Document.Version}, registry={registry.CatalogVersion}, expected={expectedVersion}.");
        }
        if (!string.Equals(catalog.Sha256, registry.Sha256, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Recovered catalog SHA does not match persisted registry SHA.");
        }

        if (expectedCommitted)
        {
            if (string.IsNullOrWhiteSpace(registry.ActiveRevisionId)
                || revisions.Count(x => x.RolledBackAtUnixMs is null) != 1
                || revisions.All(x => x.Id != registry.ActiveRevisionId || x.RolledBackAtUnixMs is not null))
            {
                throw new InvalidOperationException("Fully committed crash checkpoint did not preserve one active revision.");
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(registry.ActiveRevisionId)
                || revisions.Any(x => x.RolledBackAtUnixMs is null))
            {
                throw new InvalidOperationException(
                    rollbackCheckpoint
                        ? "Rollback recovery left an active catalog revision."
                        : "Partial apply recovery left an active catalog revision.");
            }
            if (rollbackCheckpoint && revisions.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Rollback recovery expected one rolled-back revision, found {revisions.Count}.");
            }
        }

        var integrityBefore = await IntegrityCheckAsync();
        if (!integrityBefore.All(x => string.Equals(x.integrity_check, "ok", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("SQLite integrity_check failed before WAL checkpoint.");
        }
        await SQLiteCompactionService.CheckpointWalAsync();
        var integrityAfter = await IntegrityCheckAsync();
        if (!integrityAfter.All(x => string.Equals(x.integrity_check, "ok", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException("SQLite integrity_check failed after WAL checkpoint.");
        }

        var journal = ProviderAsnCatalogApplyRecovery.JournalPath(CatalogPath);
        var rollbackJournal = ProviderAsnCatalogApplyRecovery.RollbackJournalPath(CatalogPath);
        if (File.Exists(journal) || File.Exists(rollbackJournal))
        {
            throw new InvalidOperationException("Crash-recovery journal remained after reconciliation.");
        }
        var orphanTemps = Directory.EnumerateFiles(
                Path.GetDirectoryName(CatalogPath)!,
                "." + Path.GetFileName(CatalogPath) + ".tmp-*")
            .ToArray();
        if (orphanTemps.Length != 0)
        {
            throw new InvalidOperationException("Durable atomic-file temporary files remained after recovery.");
        }

        Console.WriteLine(JsonSerializer.Serialize(new
        {
            mode = "verify",
            checkpoint,
            database = SQLiteHelper.Instance.DatabasePath,
            catalogVersion = catalog.Document.Version,
            registryVersion = registry.CatalogVersion,
            registrySha256 = registry.Sha256,
            activeRevisionId = registry.ActiveRevisionId,
            revisionCount = revisions.Count,
            rolledBackRevisions = revisions.Count(x => x.RolledBackAtUnixMs is not null),
            integrityBefore = integrityBefore.Select(x => x.integrity_check).ToArray(),
            integrityAfter = integrityAfter.Select(x => x.integrity_check).ToArray(),
            journalPresent = false,
            rollbackJournalPresent = false,
            orphanTempFiles = 0,
        }, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static Task<List<IntegrityRow>> IntegrityCheckAsync()
        => SQLiteHelper.Instance.QueryAsync<IntegrityRow>("PRAGMA integrity_check;");

    private static void WriteDurableMarker(string checkpoint)
    {
        var raw = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(new
            {
                checkpoint,
                pid = Environment.ProcessId,
                at = DateTimeOffset.UtcNow,
            }) + Environment.NewLine);

        using var stream = new FileStream(
            MarkerPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.Read,
            4096,
            FileOptions.WriteThrough);
        stream.Write(raw);
        stream.Flush(flushToDisk: true);
    }

    private static void ValidateCheckpoint(string checkpoint)
    {
        if (checkpoint is not (
                "after-file-apply"
                or "after-revision-insert"
                or "after-registry-upsert"
                or "after-rollback-file"
                or "after-rollback-registry"
                or "after-rollback-revision"))
        {
            throw new ArgumentException($"Unsupported fault checkpoint '{checkpoint}'.");
        }
    }

    private static byte[] Catalog(string version, string address)
        => Encoding.UTF8.GetBytes(
            $$"""
            {
              "schemaVersion":1,
              "id":"fault-harness-catalog",
              "version":"{{version}}",
              "source":"abrupt-loss-harness",
              "updatedAt":"{{DateTimeOffset.UtcNow:O}}",
              "entries":[
                {
                  "address":"{{address}}",
                  "sourceId":"edge-1",
                  "logicalHosts":["front.example"]
                }
              ]
            }
            """);

    public sealed class IntegrityRow
    {
        public string integrity_check { get; set; } = string.Empty;
    }
}
