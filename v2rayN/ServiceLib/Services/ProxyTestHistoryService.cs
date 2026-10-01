namespace ServiceLib.Services;

public sealed record ProxyTestHistoryPolicyMatch(
    string ProfileIndexId,
    int SampleCount,
    int FailureCount,
    int ConsecutiveFailures,
    double SuccessRate,
    double AverageDelayMs,
    decimal AverageSpeed,
    string ProfileFingerprint = "");

public sealed record ProxyTestHistoryDiagnosticSummary(
    string ProfileIndexId,
    int SampleCount,
    int SuccessCount,
    int FailureCount,
    int ConsecutiveFailures,
    double SuccessRate,
    double AverageDelayMs,
    decimal AverageSpeed,
    long LastTestedAtUnixMs,
    bool LastSuccess);

public sealed record ProxyTestHistoryPolicyDeploymentResult(
    IReadOnlyList<ProxyTestHistoryPolicyMatch> Matches,
    int RemovedCount,
    int ProtectedCount);

public sealed class ProxyTestHistoryService
{
    public ProxyTestHistoryService()
    {
        SQLiteHelper.Instance.CreateTable<ProxyTestHistoryItem>();
        SQLiteHelper.Instance.RunInTransaction(db =>
        {
            db.Execute("""
                CREATE INDEX IF NOT EXISTS IX_ProxyTestHistory_ProfileTime
                ON ProxyTestHistoryItem (ProfileIndexId, TestedAtUnixMs DESC)
                """);
            db.Execute("""
                CREATE INDEX IF NOT EXISTS IX_ProxyTestHistory_FinalProfileTime
                ON ProxyTestHistoryItem (ProfileIndexId, IsFinalOutcome, TestedAtUnixMs DESC)
                """);
            db.Execute("""
                CREATE INDEX IF NOT EXISTS IX_ProxyTestHistory_Run
                ON ProxyTestHistoryItem (RunId, ProfileIndexId)
                """);
            db.Execute("""
                CREATE INDEX IF NOT EXISTS IX_ProxyTestHistory_ProfileFingerprintTime
                ON ProxyTestHistoryItem (ProfileIndexId, ProfileFingerprint, IsFinalOutcome, TestedAtUnixMs DESC)
                """);
        });
    }

    public async Task RecordAsync(IEnumerable<ProxyTestHistoryItem> records)
    {
        var rows = records.ToList();
        if (rows.Count > 0)
        {
            await SQLiteHelper.Instance.InsertAllAsync(rows);
        }
    }

    public Task PruneAsync(int retentionDays)
    {
        if (retentionDays <= 0)
        {
            return Task.CompletedTask;
        }

        var cutoff = DateTimeOffset.UtcNow.AddDays(-retentionDays).ToUnixTimeMilliseconds();
        return SQLiteHelper.Instance.ExecuteAsync(
            "DELETE FROM ProxyTestHistoryItem WHERE TestedAtUnixMs < ?",
            cutoff);
    }

    public async Task<List<string>> ListKnownProfileIdsAsync()
    {
        var rows = await SQLiteHelper.Instance.QueryAsync<ProxyTestHistoryItem>(
            """
            SELECT ProfileIndexId
            FROM ProxyTestHistoryItem
            WHERE ProfileIndexId <> ''
            GROUP BY ProfileIndexId
            """);
        return rows
            .Select(x => x.ProfileIndexId)
            .Where(x => x.IsNotEmpty())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    public async Task<List<ProxyTestHistoryPolicyMatch>> EvaluateAsync(
        IEnumerable<string> profileIndexIds,
        SpeedTestItem policy)
    {
        var window = Math.Max(0, policy.HistoryPolicyWindowCount);
        var consecutive = Math.Max(0, policy.HistoryPolicyConsecutiveFailures);
        var limit = Math.Max(window, consecutive);
        if (limit <= 0)
        {
            return [];
        }

        var ids = profileIndexIds
            .Where(id => id.IsNotEmpty())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var profiles = await AppManager.Instance.GetProfileItemsByIndexIdsAsMap(ids);

        var matches = new List<ProxyTestHistoryPolicyMatch>();
        foreach (var profileIndexId in ids)
        {
            if (!profiles.TryGetValue(profileIndexId, out var profile))
            {
                continue;
            }

            // Destructive policy decisions must use evidence from the exact connection
            // configuration currently represented by this profile id. Legacy rows without
            // a fingerprint remain available to diagnostics, but can never delete a profile.
            var fingerprint = ComputeProfileFingerprint(profile);
            var rows = await LoadOutcomeRowsAsync(profile, limit, includeLegacy: false);
            var match = EvaluateRecords(profileIndexId, rows, policy, fingerprint);
            if (match is not null)
            {
                matches.Add(match);
            }
        }

        return matches;
    }

    public async Task<List<ProxyTestHistoryDiagnosticSummary>> GetDiagnosticsAsync(
        IEnumerable<string> profileIndexIds,
        int maxRuns = 20)
    {
        if (maxRuns is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRuns));
        }

        var ids = profileIndexIds
            .Where(id => id.IsNotEmpty())
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var profiles = await AppManager.Instance.GetProfileItemsByIndexIdsAsMap(ids);

        var summaries = new List<ProxyTestHistoryDiagnosticSummary>();
        foreach (var profileIndexId in ids)
        {
            if (!profiles.TryGetValue(profileIndexId, out var profile))
            {
                continue;
            }

            // Reliability ranking must describe the exact current connection
            // configuration. Legacy rows without a fingerprint may belong to an older
            // credential/transport configuration that reused the same profile id/endpoint.
            var rows = await LoadOutcomeRowsAsync(profile, maxRuns, includeLegacy: false);
            var summary = SummarizeRecords(profileIndexId, rows);
            if (summary is not null)
            {
                summaries.Add(summary);
            }
        }

        return summaries;
    }

    public async Task<ProxyTestHistoryPolicyDeploymentResult> ApplyRemovalPolicyAsync(
        Config config,
        IEnumerable<string>? profileIndexIds = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(config);

        var ids = profileIndexIds?.Where(x => x.IsNotEmpty()).Distinct(StringComparer.Ordinal).ToList()
                  ?? await ListKnownProfileIdsAsync();
        var matches = await EvaluateAsync(ids, config.SpeedTestItem);
        cancellationToken.ThrowIfCancellationRequested();

        var protectedIds = matches
            .Where(x => string.Equals(x.ProfileIndexId, config.IndexId, StringComparison.Ordinal))
            .Select(x => x.ProfileIndexId)
            .ToHashSet(StringComparer.Ordinal);
        var removableIds = matches
            .Select(x => x.ProfileIndexId)
            .Where(id => id.IsNotEmpty() && !protectedIds.Contains(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (removableIds.Count == 0)
        {
            return new(matches, 0, protectedIds.Count);
        }

        var profiles = await AppManager.Instance.GetProfileItemsByIndexIds(removableIds);
        cancellationToken.ThrowIfCancellationRequested();
        if (profiles.Count == 0)
        {
            return new(matches, 0, protectedIds.Count);
        }

        var expectedFingerprintById = matches
            .Where(x => x.ProfileFingerprint.IsNotEmpty())
            .ToDictionary(x => x.ProfileIndexId, x => x.ProfileFingerprint, StringComparer.Ordinal);

        var removed = await ConfigHandler.RemoveServersIfCurrentAsync(
            config,
            profiles,
            persisted =>
                !string.Equals(persisted.IndexId, config.IndexId, StringComparison.Ordinal)
                && expectedFingerprintById.TryGetValue(persisted.IndexId, out var expected)
                && string.Equals(
                    ComputeProfileFingerprint(persisted),
                    expected,
                    StringComparison.Ordinal));

        // The predicate is evaluated inside the delete transaction, so a profile changed
        // after evaluation is left in place and is not counted as removed.
        return new(matches, removed.Count, protectedIds.Count);
    }

    public static ProxyTestHistoryPolicyMatch? EvaluateRecords(
        string profileIndexId,
        IReadOnlyList<ProxyTestHistoryItem> newestFirst,
        SpeedTestItem policy,
        string profileFingerprint = "")
    {
        var rows = newestFirst.Where(x => !x.Skipped).ToList();
        if (rows.Count == 0)
        {
            return null;
        }

        var windowCount = Math.Max(0, policy.HistoryPolicyWindowCount);
        var requiredFailures = Math.Max(0, policy.HistoryPolicyFailureCount);
        var windowMatch = false;
        var failureCount = 0;

        if (windowCount > 0 && requiredFailures > 0 && requiredFailures <= windowCount && rows.Count >= windowCount)
        {
            var window = rows.Take(windowCount).ToList();
            failureCount = window.Count(x => !x.Success);
            windowMatch = failureCount >= requiredFailures;
        }

        var consecutiveThreshold = Math.Max(0, policy.HistoryPolicyConsecutiveFailures);
        var consecutiveFailures = CountConsecutiveFailures(rows);

        var consecutiveMatch = consecutiveThreshold > 0 && consecutiveFailures >= consecutiveThreshold;
        if (!windowMatch && !consecutiveMatch)
        {
            return null;
        }

        var diagnosticRows = windowCount > 0 ? rows.Take(Math.Min(windowCount, rows.Count)).ToList() : rows;
        var successCount = diagnosticRows.Count(x => x.Success);
        var successful = diagnosticRows.Where(x => x.Success).ToList();
        var delaySamples = successful.Where(x => x.DelayMs > 0).ToList();
        var speedSamples = successful.Where(x => x.Speed > 0).ToList();
        var averageDelay = delaySamples.Count == 0 ? 0 : delaySamples.Average(x => (double)x.DelayMs);
        var averageSpeed = speedSamples.Count == 0 ? 0 : speedSamples.Average(x => x.Speed);

        return new(
            profileIndexId,
            diagnosticRows.Count,
            diagnosticRows.Count - successCount,
            consecutiveFailures,
            diagnosticRows.Count == 0 ? 0 : (double)successCount / diagnosticRows.Count,
            averageDelay,
            averageSpeed,
            profileFingerprint);
    }

    public static ProxyTestHistoryDiagnosticSummary? SummarizeRecords(
        string profileIndexId,
        IReadOnlyList<ProxyTestHistoryItem> newestFirst)
    {
        var rows = newestFirst.Where(x => !x.Skipped).ToList();
        if (rows.Count == 0)
        {
            return null;
        }

        var successful = rows.Where(x => x.Success).ToList();
        var delaySamples = successful.Where(x => x.DelayMs > 0).ToList();
        var speedSamples = successful.Where(x => x.Speed > 0).ToList();
        var failureCount = rows.Count - successful.Count;
        return new(
            profileIndexId,
            rows.Count,
            successful.Count,
            failureCount,
            CountConsecutiveFailures(rows),
            (double)successful.Count / rows.Count,
            delaySamples.Count == 0 ? 0 : delaySamples.Average(x => (double)x.DelayMs),
            speedSamples.Count == 0 ? 0 : speedSamples.Average(x => x.Speed),
            rows[0].TestedAtUnixMs,
            rows[0].Success);
    }

    public static List<ProxyTestHistoryDiagnosticSummary> OrderDiagnosticsForBestConnection(
        IEnumerable<ProxyTestHistoryDiagnosticSummary> summaries)
    {
        ArgumentNullException.ThrowIfNull(summaries);

        return summaries
            .OrderByDescending(x => x.LastSuccess)
            .ThenByDescending(x => x.SuccessRate)
            .ThenBy(x => x.ConsecutiveFailures)
            .ThenBy(x => x.AverageDelayMs > 0 ? x.AverageDelayMs : double.MaxValue)
            .ThenByDescending(x => x.AverageSpeed)
            .ThenByDescending(x => x.SampleCount)
            .ThenByDescending(x => x.LastTestedAtUnixMs)
            .ThenBy(x => x.ProfileIndexId, StringComparer.Ordinal)
            .ToList();
    }

    private async Task<List<ProxyTestHistoryItem>> LoadOutcomeRowsAsync(
        ProfileItem profile,
        int limit,
        bool includeLegacy)
    {
        var profileIndexId = profile.IndexId;
        var fingerprint = ComputeProfileFingerprint(profile);
        var exact = await SQLiteHelper.Instance.QueryAsync<ProxyTestHistoryItem>(
            """
            SELECT *
            FROM ProxyTestHistoryItem
            WHERE ProfileIndexId = ?
              AND ProfileFingerprint = ?
              AND Skipped = 0
              AND (IsFinalOutcome = 1 OR (IsFinalOutcome = 0 AND RunId = ''))
            ORDER BY TestedAtUnixMs DESC, Id DESC
            LIMIT ?
            """,
            profileIndexId,
            fingerprint,
            limit);

        if (!includeLegacy || exact.Count >= limit)
        {
            return exact.Take(limit).ToList();
        }

        // Rows written before ProfileFingerprint existed can still inform diagnostics when
        // the coarse endpoint identity matches. They are never eligible for deletion policy.
        var legacy = await SQLiteHelper.Instance.QueryAsync<ProxyTestHistoryItem>(
            """
            SELECT *
            FROM ProxyTestHistoryItem
            WHERE ProfileIndexId = ?
              AND ProfileFingerprint = ''
              AND ConfigType = ?
              AND Port = ?
              AND Address = ? COLLATE NOCASE
              AND Skipped = 0
              AND (IsFinalOutcome = 1 OR (IsFinalOutcome = 0 AND RunId = ''))
            ORDER BY TestedAtUnixMs DESC, Id DESC
            LIMIT ?
            """,
            profileIndexId,
            (int)profile.ConfigType,
            profile.Port,
            profile.Address ?? string.Empty,
            limit);

        return exact
            .Concat(legacy)
            .OrderByDescending(x => x.TestedAtUnixMs)
            .ThenByDescending(x => x.Id, StringComparer.Ordinal)
            .Take(limit)
            .ToList();
    }

    public static string ComputeProfileFingerprint(ProfileItem profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var copy = JsonUtils.DeepCopy(profile)
                   ?? throw new InvalidOperationException("Could not snapshot profile for test-history fingerprinting.");

        // Keep connection-affecting fields while excluding presentation/ownership metadata.
        copy.IndexId = string.Empty;
        copy.Subid = string.Empty;
        copy.IsSub = false;
        copy.DisplayLog = false;
        copy.Remarks = string.Empty;

        var json = JsonUtils.Serialize(copy, false);
        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));
    }

    private static int CountConsecutiveFailures(IReadOnlyList<ProxyTestHistoryItem> newestFirst)
    {
        var count = 0;
        foreach (var row in newestFirst)
        {
            if (row.Success)
            {
                break;
            }
            count++;
        }
        return count;
    }
}
