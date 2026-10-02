using ServiceLib.Models.Entities;

namespace ServiceLib.Reviver.Intelligence;

public interface IHumanFeedbackStore
{
    Task RecordAsync(HumanFeedbackObservation observation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<HumanFeedbackObservation>> ListRecentAsync(int limit = 100, CancellationToken cancellationToken = default);
}

public sealed class SqliteHumanFeedbackStore : IHumanFeedbackStore
{
    public async Task RecordAsync(HumanFeedbackObservation observation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(observation);
        cancellationToken.ThrowIfCancellationRequested();

        if (observation.GenomeKey.IsNullOrEmpty() || observation.NetworkKey.IsNullOrEmpty())
        {
            throw new ArgumentException("Human feedback must use derived genome/network keys.");
        }

        await SQLiteHelper.Instance.InsertAsync(new HumanFeedbackHistoryItem
        {
            Id = Utils.GetGuid(false),
            Kind = (int)observation.Kind,
            GenomeKey = observation.GenomeKey,
            NetworkKey = observation.NetworkKey,
            StrategyId = observation.StrategyId,
            ObservedAtUnixMs = observation.ObservedAt.ToUnixTimeMilliseconds(),
        });
    }

    public async Task<IReadOnlyList<HumanFeedbackObservation>> ListRecentAsync(
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await SQLiteHelper.Instance.TableAsync<HumanFeedbackHistoryItem>()
            .OrderByDescending(x => x.ObservedAtUnixMs)
            .Take(limit)
            .ToListAsync();

        return rows.Select(x => new HumanFeedbackObservation
        {
            Kind = Enum.IsDefined(typeof(EHumanFeedbackKind), x.Kind)
                ? (EHumanFeedbackKind)x.Kind
                : EHumanFeedbackKind.FalsePositiveDiagnosis,
            GenomeKey = x.GenomeKey,
            NetworkKey = x.NetworkKey,
            StrategyId = x.StrategyId,
            ObservedAt = DateTimeOffset.FromUnixTimeMilliseconds(x.ObservedAtUnixMs),
        }).ToArray();
    }
}

public sealed class IntelligenceMetrics
{
    private readonly ConcurrentDictionary<string, long> _counters = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, MetricAverage> _averages = new(StringComparer.Ordinal);

    public void Increment(string name, long amount = 1)
        => _counters.AddOrUpdate(name, amount, (_, current) => checked(current + amount));

    public void Observe(string name, double value)
    {
        if (!double.IsFinite(value))
        {
            return;
        }
        _averages.AddOrUpdate(
            name,
            _ => new MetricAverage(1, value),
            (_, current) => current.Add(value));
    }

    public IReadOnlyDictionary<string, long> SnapshotCounters()
        => new Dictionary<string, long>(_counters, StringComparer.Ordinal);

    public IReadOnlyDictionary<string, double> SnapshotAverages()
        => _averages.ToDictionary(x => x.Key, x => x.Value.Average, StringComparer.Ordinal);

    private readonly record struct MetricAverage(long Count, double Sum)
    {
        public double Average => Count == 0 ? 0d : Sum / Count;
        public MetricAverage Add(double value) => new(checked(Count + 1), Sum + value);
    }
}
