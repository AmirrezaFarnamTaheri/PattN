namespace ServiceLib.Reviver.Models;

public sealed record RepairEvidence
{
    public required string Kind { get; init; }
    public required string Summary { get; init; }
    public string? Source { get; init; }
    public DateTimeOffset ObservedAt { get; init; } = DateTimeOffset.UtcNow;
    public IReadOnlyDictionary<string, string> Data { get; init; } = new Dictionary<string, string>();
}

public sealed record RepairValidationEvidence
{
    public int Attempts { get; init; }
    public int Successes { get; init; }
    public int ConsecutiveSuccesses { get; init; }
    public double? MedianLatencyMs { get; init; }
    public double? LossRate { get; init; }
    public double? ThroughputMbps { get; init; }
    public IReadOnlyList<ERepairFailureClass> Failures { get; init; } = [];

    /// <summary>
    /// Set when the evidence cannot be attributed to the core that was started for it -- e.g. the
    /// core process exited, or stopped serving the probed loopback port, while the validation was
    /// running. Successes measured against a *different* local listener still look like successes
    /// (MeetsQuorum only counts them), so this flag is how the validator says "count these, but do
    /// not trust them". Promotion and ranking gates must refuse such evidence.
    /// </summary>
    public bool IntegritySuspect { get; init; }

    public bool MeetsQuorum(int minimumSuccesses = 2)
        => Attempts > 0 && Successes >= minimumSuccesses;

    public bool MeetsQuorumWithoutIntegrityDoubt(int minimumSuccesses = 2)
        => MeetsQuorum(minimumSuccesses) && !IntegritySuspect;
}
