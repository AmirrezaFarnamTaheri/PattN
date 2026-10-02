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

        await SQLiteHelper.Instance.InsertAsync(new StrategyOutcomeHistoryItem
        {
            Id = Utils.GetGuid(false),
            StrategyId = observation.StrategyId,
            GenomeKey = observation.GenomeKey,
            NetworkKey = observation.NetworkKey,
            Succeeded = observation.Succeeded,
            RolledBack = observation.RolledBack,
            HumanConfirmed = observation.HumanConfirmed,
            LatencyMs = observation.LatencyMs is { } latency && double.IsFinite(latency) ? latency : null,
            ObservedAtUnixMs = observation.ObservedAt.ToUnixTimeMilliseconds(),
        });
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

        return Summarize(strategyId, rows, DateTimeOffset.UtcNow);
    }

    public static StrategyEffectivenessSummary Summarize(
        string strategyId,
        IReadOnlyList<StrategyOutcomeHistoryItem> rows,
        DateTimeOffset now,
        double halfLifeDays = 45d)
    {
        if (rows.Count == 0)
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
        var finiteLatency = new List<double>();
        foreach (var row in rows)
        {
            var observedAt = DateTimeOffset.FromUnixTimeMilliseconds(row.ObservedAtUnixMs);
            var timeWeight = NetworkIntelligenceService.ApplyEvidenceDecay(1d, observedAt, now, halfLifeDays);
            var sourceWeight = row.HumanConfirmed ? 2d : 1d;
            var weight = timeWeight * sourceWeight;
            var success = row.Succeeded && !row.RolledBack;

            totalWeight += weight;
            if (success)
            {
                weightedSuccess += weight;
            }
            if (success && row.LatencyMs is { } latency && double.IsFinite(latency) && latency >= 0d)
            {
                finiteLatency.Add(latency);
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
            Samples = rows.Count,
            EffectiveSuccessRate = totalWeight <= 0d ? 0.5d : weightedSuccess / totalWeight,
            Confidence = 1d - Math.Exp(-totalWeight / 6d),
            MedianLatencyMs = median,
            LastObservedAt = rows.Max(x => DateTimeOffset.FromUnixTimeMilliseconds(x.ObservedAtUnixMs)),
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
