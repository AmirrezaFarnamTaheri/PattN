namespace ServiceLib.Reviver.Services;

public sealed record RepairMetricsSnapshot
{
    public long Diagnoses { get; init; }
    public long HealthyDiagnoses { get; init; }
    public long CandidatesPlanned { get; init; }
    public long CandidatesValidated { get; init; }
    public long Promotions { get; init; }
    public long Rollbacks { get; init; }
    public IReadOnlyDictionary<string, long> FailureClasses { get; init; }
        = new Dictionary<string, long>();
    public IReadOnlyDictionary<string, long> StrategyPromotions { get; init; }
        = new Dictionary<string, long>();
    public IReadOnlyDictionary<string, long> StrategyRollbacks { get; init; }
        = new Dictionary<string, long>();
}

/// <summary>
/// Process-local operational counters. They contain no profile IDs, endpoints, credentials, domains, or IPs.
/// </summary>
public static class RepairMetrics
{
    private static long _diagnoses;
    private static long _healthyDiagnoses;
    private static long _candidatesPlanned;
    private static long _candidatesValidated;
    private static long _promotions;
    private static long _rollbacks;
    private static readonly ConcurrentDictionary<string, long> _failureClasses =
        new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, long> _strategyPromotions =
        new(StringComparer.Ordinal);
    private static readonly ConcurrentDictionary<string, long> _strategyRollbacks =
        new(StringComparer.Ordinal);

    public static void RecordDiagnosis(RepairDiagnosis diagnosis)
    {
        ArgumentNullException.ThrowIfNull(diagnosis);
        Interlocked.Increment(ref _diagnoses);
        if (diagnosis.IsHealthy)
        {
            Interlocked.Increment(ref _healthyDiagnoses);
        }

        _failureClasses.AddOrUpdate(
            diagnosis.FailureClass.ToString(),
            1,
            (_, current) => checked(current + 1));
    }

    public static void RecordPlanned(int count)
    {
        if (count > 0)
        {
            Interlocked.Add(ref _candidatesPlanned, count);
        }
    }

    public static void RecordValidated(int count)
    {
        if (count > 0)
        {
            Interlocked.Add(ref _candidatesValidated, count);
        }
    }

    public static void RecordPromotion(string? strategyId)
    {
        Interlocked.Increment(ref _promotions);
        RecordStrategy(_strategyPromotions, strategyId);
    }

    public static void RecordRollback(string? strategyId)
    {
        Interlocked.Increment(ref _rollbacks);
        RecordStrategy(_strategyRollbacks, strategyId);
    }

    public static RepairMetricsSnapshot Snapshot()
        => new()
        {
            Diagnoses = Interlocked.Read(ref _diagnoses),
            HealthyDiagnoses = Interlocked.Read(ref _healthyDiagnoses),
            CandidatesPlanned = Interlocked.Read(ref _candidatesPlanned),
            CandidatesValidated = Interlocked.Read(ref _candidatesValidated),
            Promotions = Interlocked.Read(ref _promotions),
            Rollbacks = Interlocked.Read(ref _rollbacks),
            FailureClasses = Snapshot(_failureClasses),
            StrategyPromotions = Snapshot(_strategyPromotions),
            StrategyRollbacks = Snapshot(_strategyRollbacks),
        };

    private static void RecordStrategy(
        ConcurrentDictionary<string, long> target,
        string? strategyId)
    {
        var key = strategyId.IsNullOrEmpty() ? "unknown" : strategyId!;
        target.AddOrUpdate(
            key,
            1,
            (_, current) => checked(current + 1));
    }

    private static IReadOnlyDictionary<string, long> Snapshot(
        ConcurrentDictionary<string, long> source)
        => source
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
}
