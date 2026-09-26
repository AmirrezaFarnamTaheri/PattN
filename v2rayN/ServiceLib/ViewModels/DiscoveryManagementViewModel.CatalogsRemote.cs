using ServiceLib.Discovery.Models;
using ServiceLib.Discovery.Services;
using ServiceLib.Reviver.Models;
using ServiceLib.Reviver.Promotion;
using ServiceLib.Reviver.Services;

namespace ServiceLib.ViewModels;

/// <summary>Catalog, remote-trust, archive, and provenance command handlers.</summary>
public partial class DiscoveryManagementViewModel
{
    private async Task RegisterCatalogAsync()
    {
        var path = await BrowseCatalogFileInteraction.HandleSafe(RxVoid.Default);
        if (path.IsNullOrEmpty())
        {
            return;
        }

        await RunBusyAsync("Registering provider catalog...", async () =>
        {
            var registered = await _catalogs.RegisterAsync(path);
            await RefreshCatalogsCoreAsync();
            SelectedCatalog = Catalogs.FirstOrDefault(x => x.Id == registered.Id);
            StatusMessage = $"Registered catalog '{registered.DisplayName}'.";
        });
    }

    private async Task RefreshSelectedCatalogAsync()
    {
        if (SelectedCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectCatalog;
            return;
        }

        var id = SelectedCatalog.Id;
        await RunBusyAsync("Refreshing catalog audit...", async () =>
        {
            await _catalogs.RefreshAsync(id);
            await RefreshCatalogsCoreAsync();
            SelectedCatalog = Catalogs.FirstOrDefault(x => x.Id == id);
            StatusMessage = "Catalog metadata refreshed.";
        });
    }

    private async Task ToggleCatalogEnabledAsync()
    {
        if (SelectedCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectCatalog;
            return;
        }

        var id = SelectedCatalog.Id;
        var enabled = !SelectedCatalog.Enabled;
        await RunBusyAsync(enabled ? "Enabling catalog..." : "Disabling catalog...", async () =>
        {
            await _catalogs.SetEnabledAsync(id, enabled);
            await RefreshCatalogsCoreAsync();
            SelectedCatalog = Catalogs.FirstOrDefault(x => x.Id == id);
            StatusMessage = enabled ? "Catalog enabled." : "Catalog disabled.";
        });
    }

    private async Task UnregisterCatalogAsync()
    {
        if (Busy)
        {
            return;
        }
        if (SelectedCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectCatalog;
            return;
        }
        var id = SelectedCatalog.Id;
        if (!await ConfirmInteraction.HandleSafe(ResUI.TbConfirmCatalogUnregister))
        {
            return;
        }
        if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal))
        {
            StatusMessage = "Catalog selection changed; unregister cancelled.";
            return;
        }

        await RunBusyAsync("Unregistering catalog...", async () =>
        {
            var receipt = await _catalogs.UnregisterAsync(id);
            _pendingCatalogUpdate = null;
            HasCatalogUpdatePreview = false;
            CatalogUpdatePreview = "Preview an update to review changes before applying.";
            await RefreshCatalogsCoreAsync();
            await RefreshCatalogRevisionsAsync();
            StatusMessage =
                $"Catalog unregistered. File retained. {receipt.RevisionCount} revision record(s) preserved.";
        });
    }

    private async Task RefreshRetiredCatalogsAsync()
        => await RunBusyAsync("Refreshing retired catalogs...", async () =>
        {
            await RefreshRetiredCatalogsCoreAsync();
            StatusMessage = "Retired catalog archive refreshed.";
        });

    private async Task ReRegisterRetiredCatalogAsync()
    {
        if (SelectedRetiredCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectRetiredCatalog;
            return;
        }

        var retired = SelectedRetiredCatalog;
        await RunBusyAsync("Re-registering retired catalog...", async () =>
        {
            var restored = await _catalogs.RegisterAsync(
                retired.FilePath,
                new ProviderAsnCatalogRegistrationOptions
                {
                    DisplayName = retired.DisplayName,
                    Enabled = false,
                });
            await RefreshCatalogsCoreAsync();
            await RefreshRetiredCatalogsCoreAsync();
            SelectedCatalog = Catalogs.FirstOrDefault(x => x.Id == restored.Id);
            StatusMessage = "Catalog re-registered in disabled state. Review it before enabling.";
        });
    }

    private async Task DiscardRetiredCatalogHistoryAsync()
    {
        if (Busy)
        {
            return;
        }
        if (SelectedRetiredCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectRetiredCatalog;
            return;
        }
        var id = SelectedRetiredCatalog.Id;
        if (!await ConfirmInteraction.HandleSafe(ResUI.TbConfirmDiscardRetiredCatalogHistory))
        {
            return;
        }
        if (!string.Equals(SelectedRetiredCatalog?.Id, id, StringComparison.Ordinal))
        {
            StatusMessage = "Retired catalog selection changed; discard cancelled.";
            return;
        }

        await RunBusyAsync("Discarding retired catalog revision history...", async () =>
        {
            var receipt = await _catalogs.DiscardRetiredRevisionHistoryAsync(id);
            await RefreshRetiredCatalogsCoreAsync();
            SelectedRetiredCatalog = RetiredCatalogs.FirstOrDefault(x => x.Id == id);
            await RefreshRetiredCatalogRevisionsAsync();
            StatusMessage =
                $"Discarded {receipt.RevisionCount} persisted revision record(s). Catalog file was not deleted.";
        });
    }

    private async Task ExportRetiredCatalogArchiveAsync()
    {
        if (SelectedRetiredCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectRetiredCatalog;
            return;
        }

        var id = SelectedRetiredCatalog.Id;
        var destination = await SaveCatalogArchiveInteraction.HandleSafe(RxVoid.Default);
        if (destination.IsNullOrEmpty())
        {
            return;
        }
        if (!string.Equals(SelectedRetiredCatalog?.Id, id, StringComparison.Ordinal))
        {
            StatusMessage = "Retired catalog selection changed; export cancelled.";
            return;
        }

        await RunBusyAsync("Exporting retired catalog archive...", async () =>
        {
            var bundle = await _catalogArchive.PrepareAsync(id);
            await _catalogArchive.SaveAsync(bundle, destination);
            StatusMessage = $"Retired catalog archive exported to {destination}.";
        });
    }

    private async Task RenameCatalogAsync()
    {
        if (SelectedCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectCatalog;
            return;
        }

        var id = SelectedCatalog.Id;
        await RunBusyAsync("Renaming catalog...", async () =>
        {
            await _catalogs.RenameAsync(id, CatalogDisplayNameEdit);
            await RefreshCatalogsCoreAsync();
            SelectedCatalog = Catalogs.FirstOrDefault(x => x.Id == id);
            StatusMessage = "Catalog label updated.";
        });
    }

    private async Task ConfigureRemoteCatalogSourceAsync()
    {
        if (SelectedCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectCatalog;
            return;
        }
        if (RemoteCatalogUri.IsNullOrEmpty())
        {
            StatusMessage = "Enter an HTTPS catalog URL first.";
            return;
        }

        var id = SelectedCatalog.Id;
        IReadOnlyList<string> proposedPins;
        try
        {
            proposedPins = ProviderAsnCatalogTransportPinning.ParseEditorText(RemoteTlsSpkiPinsText);
        }
        catch (Exception ex)
        {
            RemoteTlsPinStatus = $"Invalid pin set: {ex.Message}";
            StatusMessage = "Fix the HTTPS SPKI pin set before saving.";
            return;
        }

        var draft = new ProviderAsnCatalogRemoteSourceConfig
        {
            Uri = RemoteCatalogUri,
            SignatureUri = RemoteSignatureUri,
            SignaturePolicy = RemoteSignaturePolicy,
            TrustedKeyId = RemoteTrustedKeyId,
            TrustedPublicKeySpkiBase64 = RemoteTrustedPublicKeySpkiBase64,
            TlsSpkiPinsSha256 = proposedPins,
        };
        var pinDiff = ProviderAsnCatalogTransportPinning.Diff(_savedRemoteTlsSpkiPins, proposedPins);
        if (pinDiff.Removed.Count > 0
            && !await ConfirmInteraction.HandleSafe(
                string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    ResUI.TbConfirmRemoveTlsPins,
                    pinDiff.Removed.Count)))
        {
            StatusMessage = "Remote source save cancelled; existing transport trust remains unchanged.";
            return;
        }
        if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal))
        {
            StatusMessage = "Catalog selection changed; remote source save cancelled.";
            return;
        }

        await RunBusyAsync("Saving remote catalog source...", async () =>
        {
            await _remoteCatalogs.ConfigureAsync(id, draft);
            _pendingRemoteCatalogFetch = null;
            HasRemoteCatalogPreview = false;
            RemoteCatalogFetchPreview = ResUI.TbDiscoveryFetchPreview;
            await RefreshRemoteCatalogSourceAsync();
            StatusMessage = "Remote catalog source saved. No catalog bytes were changed.";
        });
    }

    private async Task RemoveRemoteCatalogSourceAsync()
    {
        if (SelectedCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectCatalog;
            return;
        }
        var id = SelectedCatalog.Id;
        if (!await ConfirmInteraction.HandleSafe(ResUI.TbConfirmRemoveRemoteCatalogSource))
        {
            return;
        }
        if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal))
        {
            StatusMessage = "Catalog selection changed; remote source removal cancelled.";
            return;
        }

        await RunBusyAsync("Removing remote catalog source...", async () =>
        {
            await _remoteCatalogs.RemoveAsync(id);
            _pendingRemoteCatalogFetch = null;
            HasRemoteCatalogPreview = false;
            ClearRemoteCatalogEditor();
            RemoteCatalogFetchPreview = ResUI.TbDiscoveryFetchPreview;
            RemoteCatalogSourceSummary = ResUI.TbDiscoveryNoRemoteSource;
            StatusMessage = "Remote source configuration and cache metadata removed. Catalog file unchanged.";
        });
    }

    private async Task FetchRemoteCatalogPreviewAsync()
    {
        if (SelectedCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectCatalog;
            return;
        }

        var id = SelectedCatalog.Id;
        _pendingRemoteCatalogFetch = null;
        HasRemoteCatalogPreview = false;
        RemoteCatalogFetchPreview = ResUI.TbDiscoveryFetchPreview;
        await RunBusyAsync("Fetching remote catalog preview from saved source...", async () =>
        {
            var preview = await _remoteCatalogs.FetchPreviewAsync(id);
            if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal))
            {
                StatusMessage = "Catalog selection changed while fetching; stale remote preview discarded.";
                return;
            }

            _pendingRemoteCatalogFetch = preview;
            HasRemoteCatalogPreview = preview.HasUpdate;
            RemoteCatalogFetchPreview = FormatRemoteCatalogFetch(preview);
            await RefreshRemoteCatalogSourceAsync(updateEditor: false);
            StatusMessage = preview.HasUpdate
                ? "Remote update preview prepared. Review signature and diff before Apply."
                : "Remote source checked; local catalog already matches remote content.";
        });
    }

    private async Task ApplyRemoteCatalogPreviewAsync()
    {
        if (Busy)
        {
            return;
        }
        if (SelectedCatalog is null || _pendingRemoteCatalogFetch is null)
        {
            StatusMessage = "Fetch a remote catalog preview first.";
            return;
        }
        var id = SelectedCatalog.Id;
        var preview = _pendingRemoteCatalogFetch;
        if (!preview.HasUpdate)
        {
            StatusMessage = "The current remote preview contains no catalog update.";
            return;
        }
        if (!string.Equals(preview.RegistryId, id, StringComparison.Ordinal))
        {
            _pendingRemoteCatalogFetch = null;
            HasRemoteCatalogPreview = false;
            RemoteCatalogFetchPreview = ResUI.TbDiscoveryFetchPreview;
            StatusMessage = "The remote preview belongs to another catalog; fetch a fresh preview.";
            return;
        }
        if (!await ConfirmInteraction.HandleSafe(ResUI.TbConfirmCatalogUpdate))
        {
            return;
        }
        if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal)
            || !ReferenceEquals(_pendingRemoteCatalogFetch, preview))
        {
            StatusMessage = "Catalog selection or remote preview changed; apply cancelled.";
            return;
        }

        await RunBusyAsync("Applying remote catalog update...", async () =>
        {
            var revision = await _remoteCatalogs.ApplyAsync(preview);
            _pendingRemoteCatalogFetch = null;
            HasRemoteCatalogPreview = false;
            RemoteCatalogFetchPreview = $"Applied remote revision {revision.Id}.";
            await RefreshCatalogsCoreAsync();
            SelectedCatalog = Catalogs.FirstOrDefault(x => x.Id == id);
            await RefreshCatalogRevisionsAsync();
            SelectedCatalogRevision = CatalogRevisions.FirstOrDefault(x => x.Id == revision.Id)
                                      ?? SelectedCatalogRevision;
            await RefreshRemoteCatalogSourceAsync(updateEditor: false);
            StatusMessage = "Remote catalog update applied through the revision pipeline.";
        });
    }

    private async Task RefreshRemoteCatalogSourceAsync(bool updateEditor = true)
    {
        var selectedId = SelectedCatalog?.Id;
        if (selectedId.IsNullOrEmpty())
        {
            ClearRemoteCatalogEditor();
            RemoteCatalogSourceSummary = ResUI.TbDiscoveryNoRemoteSource;
            return;
        }

        try
        {
            var source = await _remoteCatalogs.GetAsync(selectedId);
            if (!string.Equals(SelectedCatalog?.Id, selectedId, StringComparison.Ordinal))
            {
                return;
            }

            if (source is null)
            {
                ClearRemoteCatalogEditor();
                RemoteCatalogSourceSummary = ResUI.TbDiscoveryNoRemoteSource;
                return;
            }

            _savedRemoteTlsSpkiPins = source.TlsSpkiPinsSha256;
            if (updateEditor)
            {
                RemoteCatalogUri = source.Uri;
                RemoteSignatureUri = source.SignatureUri;
                RemoteSignaturePolicy = source.SignaturePolicy;
                RemoteTrustedKeyId = source.TrustedKeyId;
                RemoteTrustedPublicKeySpkiBase64 = source.TrustedPublicKeySpkiBase64;
                RemoteTlsSpkiPinsText = ProviderAsnCatalogTransportPinning.FormatEditorText(source.TlsSpkiPinsSha256);
            }
            UpdateRemoteTlsPinStatus();
            RemoteCatalogSourceSummary = FormatRemoteCatalogSource(source);
        }
        catch (Exception ex)
        {
            if (string.Equals(SelectedCatalog?.Id, selectedId, StringComparison.Ordinal))
            {
                RemoteCatalogSourceSummary = $"Remote source status unavailable: {ex.Message}";
            }
            Logging.SaveLog($"Remote catalog source refresh failed: {ex}");
        }
    }

    private async Task RefreshSelectedCatalogRevisionDetailsAsync(ProviderAsnCatalogRevisionView? revision)
    {
        var local = FormatCatalogRevision(revision);
        if (revision is null)
        {
            CatalogRevisionDetails = local;
            return;
        }

        var revisionId = revision.Id;
        try
        {
            var provenance = await _remoteCatalogs.GetRevisionProvenanceAsync(revisionId);
            if (!string.Equals(SelectedCatalogRevision?.Id, revisionId, StringComparison.Ordinal))
            {
                return;
            }

            CatalogRevisionDetails = provenance is null
                ? local + Environment.NewLine + "Origin: local/manual catalog update."
                : local + Environment.NewLine + FormatRemoteRevisionProvenance(provenance);
        }
        catch (Exception ex)
        {
            if (string.Equals(SelectedCatalogRevision?.Id, revisionId, StringComparison.Ordinal))
            {
                CatalogRevisionDetails = local + Environment.NewLine + $"Remote provenance unavailable: {ex.Message}";
            }
            Logging.SaveLog($"Remote catalog revision provenance refresh failed: {ex}");
        }
    }

    private void ClearRemoteCatalogEditor()
    {
        RemoteCatalogUri = string.Empty;
        RemoteSignatureUri = string.Empty;
        RemoteSignaturePolicy = ProviderAsnCatalogSignaturePolicy.None;
        RemoteTrustedKeyId = string.Empty;
        RemoteTrustedPublicKeySpkiBase64 = string.Empty;
        _savedRemoteTlsSpkiPins = [];
        RemoteTlsSpkiPinsText = string.Empty;
        RemoteTlsPinStatus = ResUI.TbDiscoveryNoExtraTlsPins;
        _observedRemoteTls = null;
        RemoteObservedTlsSpki = string.Empty;
        RemoteObservedTlsDetails = ResUI.TbDiscoveryInspectSpki;
        _pendingRemoteSourceImport = null;
            HasRemoteSourceImportPreview = false;
        RemoteSourceImportPreview = ResUI.TbDiscoveryImportPreview;
    }

    private void UpdateRemoteTlsPinStatus()
    {
        try
        {
            var proposed = ProviderAsnCatalogTransportPinning.ParseEditorText(RemoteTlsSpkiPinsText);
            var diff = ProviderAsnCatalogTransportPinning.Diff(_savedRemoteTlsSpkiPins, proposed);

            if (proposed.Count == 0)
            {
                RemoteTlsPinStatus = _savedRemoteTlsSpkiPins.Count == 0
                    ? "No extra TLS pins. Standard certificate validation is active."
                    : $"Valid pending change: saving removes {_savedRemoteTlsSpkiPins.Count} configured pin(s).";
                return;
            }

            var rotation = diff.Added.Count > 0 && diff.Unchanged.Count > 0
                ? " · overlap rotation ready (old and new pins coexist until Save)"
                : string.Empty;
            RemoteTlsPinStatus =
                $"{proposed.Count}/{ProviderAsnCatalogTransportPinning.MaximumPins} valid pin(s) · " +
                $"+{diff.Added.Count} -{diff.Removed.Count} unchanged={diff.Unchanged.Count}{rotation}";
        }
        catch (Exception ex)
        {
            RemoteTlsPinStatus = $"Invalid pin set: {ex.Message}";
        }
    }

    private async Task InspectRemoteTlsSpkiAsync()
    {
        if (RemoteCatalogUri.IsNullOrEmpty())
        {
            StatusMessage = "Enter an HTTPS catalog URL first.";
            return;
        }

        var sourceUri = RemoteCatalogUri;
        await RunBusyAsync("Inspecting ordinary HTTPS server key...", async () =>
        {
            var observation = await _tlsObservation.ObserveAsync(sourceUri);
            if (!string.Equals(RemoteCatalogUri, sourceUri, StringComparison.Ordinal))
            {
                StatusMessage = "Catalog URL changed while inspecting TLS; stale observation discarded.";
                return;
            }

            _observedRemoteTls = observation;
            RemoteObservedTlsSpki = observation.SpkiSha256;
            RemoteObservedTlsDetails = FormatTlsObservation(observation);
            StatusMessage =
                "Observed the server SPKI through ordinary validated HTTPS. It has not been trusted or saved.";
        });
    }

    private async Task AddObservedRemoteTlsPinAsync()
    {
        if (_observedRemoteTls is null || RemoteObservedTlsSpki.IsNullOrEmpty())
        {
            StatusMessage = "Inspect the current HTTPS server key first.";
            return;
        }

        if (!ProviderAsnCatalogTlsObservationService.CanUseObservedPinForSource(
                _observedRemoteTls,
                RemoteCatalogUri,
                out var incompatibility))
        {
            StatusMessage = incompatibility;
            return;
        }

        IReadOnlyList<string> proposed;
        try
        {
            proposed = ProviderAsnCatalogTransportPinning.ParseEditorText(RemoteTlsSpkiPinsText);
        }
        catch (Exception ex)
        {
            RemoteTlsPinStatus = $"Invalid pin set: {ex.Message}";
            StatusMessage = "Fix the existing pin editor before adding the observed key.";
            return;
        }

        var merged = ProviderAsnCatalogTransportPinning.NormalizePins(
            proposed.Append(_observedRemoteTls.SpkiSha256));
        RemoteTlsSpkiPinsText = ProviderAsnCatalogTransportPinning.FormatEditorText(merged);
        UpdateRemoteTlsPinStatus();
        StatusMessage =
            "Observed SPKI added to the editor only. Review the overlap set and Save remote source to persist trust.";
        await Task.CompletedTask;
    }

    private async Task ExportRemoteSourceConfigAsync()
    {
        if (SelectedCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectCatalog;
            return;
        }

        var id = SelectedCatalog.Id;
        var destination = await SaveRemoteSourceBundleInteraction.HandleSafe(RxVoid.Default);
        if (destination.IsNullOrEmpty())
        {
            return;
        }
        if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal))
        {
            StatusMessage = "Catalog selection changed; remote-source export cancelled.";
            return;
        }

        await RunBusyAsync("Exporting portable remote-source trust configuration...", async () =>
        {
            var bundle = await _remoteSourcePortability.ExportAsync(id);
            await _remoteSourcePortability.SaveAsync(bundle, destination);
            StatusMessage =
                $"Remote-source trust configuration exported to {destination}. Runtime cache state and private keys were not included.";
        });
    }

    private async Task PreviewRemoteSourceImportAsync()
    {
        if (SelectedCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectCatalog;
            return;
        }

        var id = SelectedCatalog.Id;
        var sourcePath = await BrowseRemoteSourceBundleInteraction.HandleSafe(RxVoid.Default);
        if (sourcePath.IsNullOrEmpty())
        {
            return;
        }
        if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal))
        {
            StatusMessage = "Catalog selection changed; remote-source import preview cancelled.";
            return;
        }

        _pendingRemoteSourceImport = null;
        HasRemoteSourceImportPreview = false;
        RemoteSourceImportPreview = ResUI.TbDiscoveryImportPreview;
        await RunBusyAsync("Preparing remote-source trust import preview...", async () =>
        {
            var bundle = await _remoteSourcePortability.LoadAsync(sourcePath);
            var preview = await _remoteSourcePortability.PrepareImportAsync(id, bundle);
            if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal))
            {
                StatusMessage = "Catalog selection changed while preparing import; stale preview discarded.";
                return;
            }

            _pendingRemoteSourceImport = preview;
            HasRemoteSourceImportPreview = true;
            RemoteSourceImportPreview = FormatRemoteSourceImportPreview(preview);
            StatusMessage =
                "Remote-source import preview prepared. No source configuration, network fetch, or catalog bytes changed.";
        });
    }

    private async Task ApplyRemoteSourceImportAsync()
    {
        if (Busy)
        {
            return;
        }
        if (SelectedCatalog is null || _pendingRemoteSourceImport is null)
        {
            StatusMessage = "Prepare a remote-source import preview first.";
            return;
        }
        var id = SelectedCatalog.Id;
        var preview = _pendingRemoteSourceImport;
        if (!string.Equals(preview.TargetRegistryId, id, StringComparison.Ordinal))
        {
            _pendingRemoteSourceImport = null;
            HasRemoteSourceImportPreview = false;
            RemoteSourceImportPreview = ResUI.TbDiscoveryImportPreview;
            StatusMessage = "The import preview belongs to another catalog; prepare a fresh preview.";
            return;
        }
        if (!await ConfirmInteraction.HandleSafe(ResUI.TbConfirmRemoteSourceImport))
        {
            return;
        }
        if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal)
            || !ReferenceEquals(_pendingRemoteSourceImport, preview))
        {
            StatusMessage = "Catalog selection or import preview changed; apply cancelled.";
            return;
        }

        await RunBusyAsync("Applying remote-source trust configuration...", async () =>
        {
            await _remoteSourcePortability.ApplyImportAsync(preview);
            _pendingRemoteSourceImport = null;
            HasRemoteSourceImportPreview = false;
            RemoteSourceImportPreview = "Remote-source trust configuration imported.";
            _pendingRemoteCatalogFetch = null;
            HasRemoteCatalogPreview = false;
            RemoteCatalogFetchPreview = ResUI.TbDiscoveryFetchPreview;
            await RefreshRemoteCatalogSourceAsync();
            StatusMessage =
                "Remote-source trust configuration applied. No remote fetch or catalog update was performed.";
        });
    }

    private async Task InspectCatalogArchiveAsync()
    {
        var path = await BrowseCatalogArchiveInteraction.HandleSafe(RxVoid.Default);
        if (path.IsNullOrEmpty())
        {
            return;
        }

        await RunBusyAsync("Inspecting provider catalog archive...", async () =>
        {
            var inspection = await _catalogArchiveInspection.InspectAsync(path);
            ArchiveInspectionSummary = FormatArchiveInspection(inspection);
            StatusMessage =
                "Archive inspected read-only. No catalog was extracted, registered, configured, or applied.";
        });
    }

    private async Task SearchRemoteProvenanceAsync()
        => await RunBusyAsync("Searching persisted remote provenance...", async () =>
        {
            await SearchRemoteProvenanceCoreAsync();
            StatusMessage = "Remote provenance query refreshed from local history. No network fetch was performed.";
        });

    private async Task SearchRemoteProvenanceCoreAsync()
    {
        var selectedRevision = SelectedRemoteProvenance?.RevisionId;
        RemoteProvenance = await _remoteProvenanceQuery.QueryAsync(
            new ProviderAsnCatalogRemoteProvenanceQuery
            {
                SourceHost = RemoteProvenanceSourceHostFilter.NullIfEmpty(),
                TrustedKeyId = RemoteProvenanceTrustedKeyFilter.NullIfEmpty(),
                TlsSpkiPinSha256 = RemoteProvenanceTlsPinFilter.NullIfEmpty(),
                MaxAge = TimeSpan.FromDays(730),
                MaxItems = 250,
            });

        var latest = RemoteProvenance.LatestAppliedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "never";
        RemoteProvenanceSummaryText =
            $"{RemoteProvenance.Total} record(s) · {RemoteProvenance.SignatureValid} valid signature(s) · " +
            $"{RemoteProvenance.TransportPinned} transport-pinned apply(s) · {RemoteProvenance.ServerNotModified} 304 check(s) · latest={latest}";

        SelectedRemoteProvenance = selectedRevision.IsNullOrEmpty()
            ? RemoteProvenance.Entries.FirstOrDefault()
            : RemoteProvenance.Entries.FirstOrDefault(x => x.RevisionId == selectedRevision)
              ?? RemoteProvenance.Entries.FirstOrDefault();
    }

    private async Task PreviewCatalogUpdateAsync()
    {
        if (SelectedCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectCatalog;
            return;
        }

        var id = SelectedCatalog.Id;
        var path = await BrowseCatalogUpdateFileInteraction.HandleSafe(RxVoid.Default);
        if (path.IsNullOrEmpty())
        {
            return;
        }
        if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal))
        {
            StatusMessage = "Catalog selection changed; update preview cancelled.";
            return;
        }

        _pendingCatalogUpdate = null;
        HasCatalogUpdatePreview = false;
        CatalogUpdatePreview = ResUI.TbUpdatePreview;
        await RunBusyAsync("Preparing catalog update preview...", async () =>
        {
            var bytes = await File.ReadAllBytesAsync(path);
            var plan = await _catalogs.PrepareUpdateAsync(id, bytes);
            if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal))
            {
                StatusMessage = "Catalog selection changed while preparing update; stale preview discarded.";
                return;
            }

            _pendingCatalogUpdate = plan;
            HasCatalogUpdatePreview = true;
            CatalogUpdatePreview = FormatCatalogUpdate(plan);
            StatusMessage = "Catalog update preview prepared. Review it before Apply.";
        });
    }

    private async Task ApplyCatalogUpdateAsync()
    {
        if (Busy)
        {
            return;
        }

        if (SelectedCatalog is null || _pendingCatalogUpdate is null)
        {
            StatusMessage = "Prepare a catalog update preview first.";
            return;
        }

        var id = SelectedCatalog.Id;
        var plan = _pendingCatalogUpdate;
        if (!await ConfirmInteraction.HandleSafe(ResUI.TbConfirmCatalogUpdate))
        {
            return;
        }
        if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal)
            || !ReferenceEquals(_pendingCatalogUpdate, plan))
        {
            StatusMessage = "Catalog selection or update preview changed; apply cancelled.";
            return;
        }

        await RunBusyAsync("Applying catalog update...", async () =>
        {
            var revision = await _catalogs.ApplyUpdateAsync(id, plan);
            _pendingCatalogUpdate = null;
            HasCatalogUpdatePreview = false;
            CatalogUpdatePreview = $"Applied revision {revision.Id}.";
            await RefreshCatalogsCoreAsync();
            SelectedCatalog = Catalogs.FirstOrDefault(x => x.Id == id);
            await RefreshCatalogRevisionsAsync();
            StatusMessage = "Catalog update applied and revision recorded.";
        });
    }

    private async Task RollbackCatalogRevisionAsync()
    {
        if (Busy)
        {
            return;
        }

        if (SelectedCatalogRevision is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectRevision;
            return;
        }

        var revision = SelectedCatalogRevision;
        var revisionId = revision.Id;
        var registryId = revision.RegistryId;
        if (!await ConfirmInteraction.HandleSafe(ResUI.TbConfirmCatalogRollback))
        {
            return;
        }
        if (!string.Equals(SelectedCatalogRevision?.Id, revisionId, StringComparison.Ordinal))
        {
            StatusMessage = "Catalog revision selection changed; rollback cancelled.";
            return;
        }

        await RunBusyAsync("Rolling back catalog revision...", async () =>
        {
            await _catalogs.RollbackRevisionAsync(revisionId);
            await RefreshCatalogsCoreAsync();
            SelectedCatalog = Catalogs.FirstOrDefault(x => x.Id == registryId);
            await RefreshCatalogRevisionsAsync();
            StatusMessage = "Catalog revision rolled back.";
        });
    }

    private async Task RefreshRemoteSourceHealthAsync()
        => await RunBusyAsync("Refreshing persisted remote source health...", async () =>
        {
            await RefreshRemoteSourceHealthCoreAsync();
            StatusMessage = "Remote source health refreshed from persisted metadata. No network fetch was performed.";
        });

    private async Task RefreshRemoteSourceHealthCoreAsync()
    {
        var selectedId = SelectedRemoteSourceHealth?.RegistryId;
        RemoteSourceHealth = await _remoteHealth.LoadAsync();
        RemoteSourceHealthSummaryText =
            $"{RemoteSourceHealth.TotalCatalogs} active catalog(s) · {RemoteSourceHealth.Configured} remote source(s) configured · " +
            $"{RemoteSourceHealth.Healthy} healthy · {RemoteSourceHealth.NeedsReview} need review · " +
            $"{RemoteSourceHealth.Unconfigured} local-only/unconfigured";

        SelectedRemoteSourceHealth = selectedId.IsNullOrEmpty()
            ? RemoteSourceHealth.Rows.FirstOrDefault()
            : RemoteSourceHealth.Rows.FirstOrDefault(x => x.RegistryId == selectedId)
              ?? RemoteSourceHealth.Rows.FirstOrDefault();
    }

}
