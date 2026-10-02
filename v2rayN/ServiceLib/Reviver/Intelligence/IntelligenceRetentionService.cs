using System.Security.Cryptography;
using System.Text;
using ServiceLib.Models.Entities;

namespace ServiceLib.Reviver.Intelligence;

public sealed record IntelligenceRetentionPolicy
{
    public int RawRetentionDays { get; init; } = 30;
    public int AggregateRetentionDays { get; init; } = 365;
    public int BatchSize { get; init; } = 2000;
}

public sealed record IntelligenceRetentionResult
{
    public int RolledUpRows { get; init; }
    public int DeletedRawRows { get; init; }
    public int DeletedFeedbackRows { get; init; }
    public int DeletedNetworkFingerprintRows { get; init; }
    public int DeletedAggregateRows { get; init; }
}

/// <summary>
/// Converts old raw strategy outcomes into daily anonymous aggregates, then prunes raw and expired
/// aggregate history. This keeps the intelligence layer useful without requiring an unbounded event log.
/// </summary>
public sealed class IntelligenceRetentionService
{
    public async Task<IntelligenceRetentionResult> RollupAndPruneAsync(
        IntelligenceRetentionPolicy? policy = null,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        policy ??= new IntelligenceRetentionPolicy();
        Validate(policy);
        var timestamp = now ?? DateTimeOffset.UtcNow;
        var rawCutoff = timestamp.AddDays(-policy.RawRetentionDays).ToUnixTimeMilliseconds();
        var aggregateCutoff = timestamp.AddDays(-policy.AggregateRetentionDays).ToUnixTimeSeconds();
        var rolledUp = 0;
        var deletedRaw = 0;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var batchCount = 0;
            var batchDeleted = 0;

            // Selection, aggregation, and deletion must share the same exclusive write gate.
            // Otherwise two concurrent retention runs can select the same raw rows before
            // either acquires the gate and then double-increment the aggregate counters.
            await SQLiteHelper.Instance.RunExclusiveWriteAsync(db =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rows = db.Table<StrategyOutcomeHistoryItem>()
                    .Where(x => x.ObservedAtUnixMs < rawCutoff)
                    .OrderBy(x => x.ObservedAtUnixMs)
                    .Take(policy.BatchSize)
                    .ToList();
                batchCount = rows.Count;
                if (batchCount == 0)
                {
                    return Task.CompletedTask;
                }

                var aggregates = rows
                    .GroupBy(x => new
                    {
                        x.StrategyId,
                        x.GenomeKey,
                        x.NetworkKey,
                        Day = DayBucket(DateTimeOffset.FromUnixTimeMilliseconds(x.ObservedAtUnixMs)),
                    })
                    .Select(group => new StrategyOutcomeAggregateItem
                    {
                        Id = AggregateId(group.Key.StrategyId, group.Key.GenomeKey, group.Key.NetworkKey, group.Key.Day),
                        StrategyId = group.Key.StrategyId,
                        GenomeKey = group.Key.GenomeKey,
                        NetworkKey = group.Key.NetworkKey,
                        DayBucketUnixSeconds = group.Key.Day,
                        Samples = group.Count(),
                        Successes = group.Count(x => x.Succeeded && !x.RolledBack),
                        Rollbacks = group.Count(x => x.RolledBack),
                        HumanConfirmed = group.Count(x => x.HumanConfirmed),
                        HumanConfirmedSuccesses = group.Count(x => x.HumanConfirmed && x.Succeeded && !x.RolledBack),
                        LatencySumMs = group
                            .Where(x => x.LatencyMs is { } v && double.IsFinite(v) && v >= 0d)
                            .Sum(x => x.LatencyMs!.Value),
                        LatencySamples = group.Count(x => x.LatencyMs is { } v && double.IsFinite(v) && v >= 0d),
                    })
                    .ToArray();

                foreach (var aggregate in aggregates)
                {
                    var existing = db.Find<StrategyOutcomeAggregateItem>(aggregate.Id);
                    if (existing is null)
                    {
                        db.Insert(aggregate);
                    }
                    else
                    {
                        existing.Samples += aggregate.Samples;
                        existing.Successes += aggregate.Successes;
                        existing.Rollbacks += aggregate.Rollbacks;
                        existing.HumanConfirmed += aggregate.HumanConfirmed;
                        existing.HumanConfirmedSuccesses += aggregate.HumanConfirmedSuccesses;
                        existing.LatencySumMs += aggregate.LatencySumMs;
                        existing.LatencySamples += aggregate.LatencySamples;
                        db.Update(existing);
                    }
                }

                foreach (var row in rows)
                {
                    batchDeleted += db.Delete(row);
                }
                return Task.CompletedTask;
            }, cancellationToken);

            if (batchCount == 0)
            {
                break;
            }

            rolledUp += batchCount;
            deletedRaw += batchDeleted;
            if (batchCount < policy.BatchSize)
            {
                break;
            }
        }

        var feedbackCutoff = timestamp.AddDays(-policy.RawRetentionDays).ToUnixTimeMilliseconds();
        var deletedFeedback = await SQLiteHelper.Instance.ExecuteAsync(
            "DELETE FROM HumanFeedbackHistoryItem WHERE ObservedAtUnixMs < ?",
            feedbackCutoff);
        var deletedNetworkFingerprints = await SQLiteHelper.Instance.ExecuteAsync(
            "DELETE FROM NetworkFingerprintHistoryItem WHERE ObservedAtUnixMs < ?",
            feedbackCutoff);
        var deletedAggregates = await SQLiteHelper.Instance.ExecuteAsync(
            "DELETE FROM StrategyOutcomeAggregateItem WHERE DayBucketUnixSeconds < ?",
            aggregateCutoff);

        return new IntelligenceRetentionResult
        {
            RolledUpRows = rolledUp,
            DeletedRawRows = deletedRaw,
            DeletedFeedbackRows = deletedFeedback,
            DeletedNetworkFingerprintRows = deletedNetworkFingerprints,
            DeletedAggregateRows = deletedAggregates,
        };
    }

    private static long DayBucket(DateTimeOffset time)
        => new DateTimeOffset(time.UtcDateTime.Date, TimeSpan.Zero).ToUnixTimeSeconds();

    private static string AggregateId(string strategy, string genome, string network, long day)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{strategy}|{genome}|{network}|{day}"));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static void Validate(IntelligenceRetentionPolicy policy)
    {
        if (policy.RawRetentionDays is < 1 or > 3650)
        {
            throw new ArgumentOutOfRangeException(nameof(policy.RawRetentionDays));
        }
        if (policy.AggregateRetentionDays < policy.RawRetentionDays || policy.AggregateRetentionDays > 36500)
        {
            throw new ArgumentOutOfRangeException(nameof(policy.AggregateRetentionDays));
        }
        if (policy.BatchSize is < 1 or > 10000)
        {
            throw new ArgumentOutOfRangeException(nameof(policy.BatchSize));
        }
    }
}
