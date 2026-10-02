namespace ServiceLib.Reviver.Models;

public enum EProxyFleetHealthState
{
    Untested = 0,
    Healthy = 1,
    Degraded = 2,
    Unhealthy = 3,
}

public sealed record ProxyFleetHealthEntry
{
    public required string ProfileIndexId { get; init; }
    public EProxyFleetHealthState State { get; init; }
    public double? SuccessRate { get; init; }
    public int ConsecutiveFailures { get; init; }
    public double? AverageDelayMs { get; init; }
    public decimal? AverageSpeed { get; init; }
    public long? LastTestedAtUnixMs { get; init; }
}

public sealed record SubscriptionFleetHealthSummary
{
    public required string SubscriptionId { get; init; }
    public int TotalProfiles { get; init; }
    public int TestedProfiles { get; init; }
    public int UntestedProfiles { get; init; }
    public int HealthyProfiles { get; init; }
    public int DegradedProfiles { get; init; }
    public int UnhealthyProfiles { get; init; }
    public double? AverageSuccessRate { get; init; }
    public double? AverageDelayMs { get; init; }
    public IReadOnlyList<ProxyFleetHealthEntry> Profiles { get; init; } = [];
}
