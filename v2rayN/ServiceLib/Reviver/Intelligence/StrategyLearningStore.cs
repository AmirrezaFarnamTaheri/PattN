using System.Security.Cryptography;
using System.Text;
using ServiceLib.Models.Entities;
using ServiceLib.Reviver.Models;
using ServiceLib.Reviver.Services;

namespace ServiceLib.Reviver.Intelligence;

public interface IStrategyOutcomeStore
{
    Task RecordAsync(StrategyOutcomeObservation observation, CancellationToken cancellationToken = default);

    Task<StrategyEffectivenessSummary> SummarizeAsync(
        string strategyId,
        string genomeKey,
        string networkKey,
        int maxSamples = 500,
        CancellationToken cancellationToken = default);
}

public sealed class SqliteStrategyOutcomeStore : IStrategyOutcomeStore
{
    public async Task RecordAsync(StrategyOutcomeObservation observation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observation);
        cancellationToken.ThrowIfCancellationRequested();

        if (observation.StrategyId.IsNullOrEmpty()
            || observation.GenomeKey.IsNullOrEmpty()
            || observation.NetworkKey.IsNullOrEmpty())
        {
            throw new ArgumentException("Strategy learning requires strategy, genome, and network keys.");
        }

        var row = new StrategyOutcomeHistoryItem
        {
            Id = observation.CandidateId.IsNotEmpty()
                ? CandidateOutcomeId(observation)
                : Utils.GetGuid(false),
            CandidateId = observation.CandidateId,
            StrategyId = observation.StrategyId,
            GenomeKey = observation.GenomeKey,
            NetworkKey = observation.NetworkKey,
            Succeeded = observation.Succeeded,
            RolledBack = observation.RolledBack,
            HumanConfirmed = observation.HumanConfirmed,
            LatencyMs = observation.LatencyMs is { } latency && double.IsFinite(latency) ? latency : null,
            ObservedAtUnixMs = observation.ObservedAt.ToUnixTimeMilliseconds(),
        };

        if (observation.CandidateId.IsNotEmpty())
        {
            await SQLiteHelper.Instance.ReplaceAsync(row);
        }
        else
        {
            await SQLiteHelper.Instance.InsertAsync(row);
        }
    }

    private static string CandidateOutcomeId(StrategyOutcomeObservation observation)
    {
        var material = string.Join("|",
            observation.CandidateId.Trim(),
            observation.StrategyId.Trim(),
            observation.GenomeKey.Trim(),
            observation.NetworkKey.Trim());
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return "candidate:" + Convert.ToHexString(digest.AsSpan(0, 16)).ToLowerInvariant();
    }

    public async Task<StrategyEffectivenessSummary> SummarizeAsync(
        string strategyId,
        string genomeKey,
        string networkKey,
        int maxSamples = 500,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(strategyId);
        if (maxSamples is < 1 or > 5000)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSamples));
        }
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await SQLiteHelper.Instance.TableAsync<StrategyOutcomeHistoryItem>()
            .Where(x => x.StrategyId == strategyId
                        && x.GenomeKey == genomeKey
                        && x.NetworkKey == networkKey)
            .OrderByDescending(x => x.ObservedAtUnixMs)
            .Take(maxSamples)
            .ToListAsync();
        var aggregates = await SQLiteHelper.Instance.TableAsync<StrategyOutcomeAggregateItem>()
            .Where(x => x.StrategyId == strategyId
                        && x.GenomeKey == genomeKey
                        && x.NetworkKey == networkKey)
            .OrderByDescending(x => x.DayBucketUnixSeconds)
            .Take(maxSamples)
            .ToListAsync();

        return Summarize(strategyId, rows, aggregates, DateTimeOffset.UtcNow);
    }

    public static StrategyEffectivenessSummary Summarize(
        string strategyId,
        IReadOnlyList<StrategyOutcomeHistoryItem> rows,
        DateTimeOffset now,
        double halfLifeDays = 45d)
        => Summarize(strategyId, rows, [], now, halfLifeDays);

    public static StrategyEffectivenessSummary Summarize(
        string strategyId,
        IReadOnlyList<StrategyOutcomeHistoryItem> rows,
        IReadOnlyList<StrategyOutcomeAggregateItem> aggregates,
        DateTimeOffset now,
        double halfLifeDays = 45d)
    {
        if (halfLifeDays <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(halfLifeDays));
        }
        if (rows.Count == 0 && aggregates.Count == 0)
        {
            return new StrategyEffectivenessSummary
            {
                StrategyId = strategyId,
                Samples = 0,
                EffectiveSuccessRate = 0.5d,
                Confidence = 0d,
            };
        }

        var weightedSuccess = 0d;
        var totalWeight = 0d;
        var samples = 0;
        DateTimeOffset? latest = null;
        var finiteLatency = new List<double>();

        foreach (var row in rows)
        {
            var observedAt = DateTimeOffset.FromUnixTimeMilliseconds(row.ObservedAtUnixMs);
            var timeWeight = NetworkIntelligenceService.ApplyEvidenceDecay(1d, observedAt, now, halfLifeDays);
            var sourceWeight = row.HumanConfirmed ? 2d : 1d;
            var weight = timeWeight * sourceWeight;
            var success = row.Succeeded && !row.RolledBack;

            samples++;
            totalWeight += weight;
            if (success)
            {
                weightedSuccess += weight;
            }
            if (success && row.LatencyMs is { } latency && double.IsFinite(latency) && latency >= 0d)
            {
                finiteLatency.Add(latency);
            }
            if (latest is null || observedAt > latest.Value)
            {
                latest = observedAt;
            }
        }

        foreach (var aggregate in aggregates)
        {
            var observedAt = DateTimeOffset.FromUnixTimeSeconds(aggregate.DayBucketUnixSeconds);
            var timeWeight = NetworkIntelligenceService.ApplyEvidenceDecay(1d, observedAt, now, halfLifeDays);
            var aggregateSamples = Math.Max(0, aggregate.Samples);
            var humanConfirmed = Math.Clamp(aggregate.HumanConfirmed, 0, aggregateSamples);
            var successes = Math.Clamp(aggregate.Successes, 0, aggregateSamples);
            var humanSuccesses = Math.Clamp(
                aggregate.HumanConfirmedSuccesses,
                0,
                Math.Min(humanConfirmed, successes));

            samples += aggregateSamples;
            totalWeight += (aggregateSamples + humanConfirmed) * timeWeight;
            weightedSuccess += (successes + humanSuccesses) * timeWeight;
            if (latest is null || observedAt > latest.Value)
            {
                latest = observedAt;
            }
        }

        finiteLatency.Sort();
        double? median = finiteLatency.Count == 0
            ? null
            : finiteLatency.Count % 2 == 1
                ? finiteLatency[finiteLatency.Count / 2]
                : (finiteLatency[finiteLatency.Count / 2 - 1] + finiteLatency[finiteLatency.Count / 2]) / 2d;

        return new StrategyEffectivenessSummary
        {
            StrategyId = strategyId,
            Samples = samples,
            EffectiveSuccessRate = totalWeight <= 0d ? 0.5d : weightedSuccess / totalWeight,
            Confidence = 1d - Math.Exp(-totalWeight / 6d),
            MedianLatencyMs = median,
            LastObservedAt = latest,
        };
    }
}

/// <summary>
/// Persists validation outcomes without storing raw endpoints, domains, credentials, or user identifiers.
/// </summary>
public sealed class StrategyLearningObserver(
    IStrategyOutcomeStore store,
    NetworkIntelligenceService intelligence,
    Func<NetworkFingerprint?>? currentNetwork = null) : IRepairLifecycleObserver
{
    public Task CandidatePlannedAsync(RepairCandidate candidate, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public async Task CandidateValidatedAsync(RepairCandidate candidate, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        var genome = intelligence.BuildGenome(candidate.Profile);
        var network = currentNetwork?.Invoke();

        await store.RecordAsync(new StrategyOutcomeObservation
        {
            CandidateId = candidate.Id,
            StrategyId = NetworkIntelligenceService.StrategyIdFor(candidate),
            GenomeKey = genome.Key,
            NetworkKey = network?.Key ?? "net:unknown",
            Succeeded = candidate.State == ERepairCandidateState.RuntimeValidated,
            RolledBack = false,
            HumanConfirmed = false,
            LatencyMs = candidate.Validation?.MedianLatencyMs,
            ObservedAt = DateTimeOffset.UtcNow,
        }, cancellationToken);
    }
}
