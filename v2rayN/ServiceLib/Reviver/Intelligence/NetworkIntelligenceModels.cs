using ServiceLib.Reviver.Models;

namespace ServiceLib.Reviver.Intelligence;

public sealed record NetworkObservation
{
    public string Carrier { get; init; } = string.Empty;
    public string Asn { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public bool? DnsSucceeded { get; init; }
    public bool? TcpSucceeded { get; init; }
    public bool? TlsSucceeded { get; init; }
    public bool? UploadSucceeded { get; init; }
    public bool? DownstreamSucceeded { get; init; }
    public bool? IPv4Succeeded { get; init; }
    public bool? IPv6Succeeded { get; init; }
    public bool ResetObserved { get; init; }
    public double? PacketLossRate { get; init; }
    public double? LatencyMs { get; init; }
    public DateTimeOffset ObservedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record NetworkDpiSignals
{
    public bool UploadStall { get; init; }
    public bool TlsInterferenceSuspected { get; init; }
    public bool ResetPattern { get; init; }
    public bool PacketLossPattern { get; init; }
    public double Confidence { get; init; }
}

public sealed record NetworkFingerprint
{
    public required string Key { get; init; }
    public string CarrierKey { get; init; } = string.Empty;
    public string Asn { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public bool? IPv4Healthy { get; init; }
    public bool? IPv6Healthy { get; init; }
    public NetworkDpiSignals DpiSignals { get; init; } = new();
    public DateTimeOffset ObservedAt { get; init; }
}

public sealed record ProxyGenome
{
    public required string Key { get; init; }
    public required string Protocol { get; init; }
    public required string Transport { get; init; }
    public required string Core { get; init; }
    public bool UsesTls { get; init; }
    public bool UsesReality { get; init; }
    public bool HasExplicitSni { get; init; }
    public bool HasHttpHost { get; init; }
    public string AddressFamily { get; init; } = "unknown";
    public string Provider { get; init; } = string.Empty;
}

public sealed record FailureAssessment
{
    public ERepairFailureClass FailureClass { get; init; } = ERepairFailureClass.Unknown;
    public double Confidence { get; init; }
    public IReadOnlyList<string> Reasons { get; init; } = [];
    public DateTimeOffset ObservedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record StrategyOutcomeObservation
{
    public string CandidateId { get; init; } = string.Empty;
    public required string StrategyId { get; init; }
    public required string GenomeKey { get; init; }
    public required string NetworkKey { get; init; }
    public bool Succeeded { get; init; }
    public bool RolledBack { get; init; }
    public bool HumanConfirmed { get; init; }
    public double? LatencyMs { get; init; }
    public DateTimeOffset ObservedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record StrategyEffectivenessSummary
{
    public required string StrategyId { get; init; }
    public int Samples { get; init; }
    public double EffectiveSuccessRate { get; init; }
    public double Confidence { get; init; }
    public double? MedianLatencyMs { get; init; }
    public DateTimeOffset? LastObservedAt { get; init; }
}

public sealed record RepairExplanation
{
    public required string Decision { get; init; }
    public double Confidence { get; init; }
    public IReadOnlyList<string> Reasons { get; init; } = [];
}

public sealed record ExperimentHypothesis
{
    public required string Id { get; init; }
    public required string Description { get; init; }
    public double ExpectedInformationGain { get; init; }
    public double EstimatedCostSeconds { get; init; }
    public double MutationRisk { get; init; }
}

public sealed record ExperimentPlan
{
    public IReadOnlyList<ExperimentHypothesis> OrderedHypotheses { get; init; } = [];
}

public enum EEndpointLifecycleStage
{
    Unknown = 0,
    Birth,
    Peak,
    Degrading,
    Dead,
    Revived,
}

public sealed record EndpointLifecycleSnapshot
{
    public EEndpointLifecycleStage Stage { get; init; }
    public double RecentSuccessRate { get; init; }
    public double PreviousSuccessRate { get; init; }
    public DateTimeOffset ObservedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record CarrierProfile
{
    public required string CarrierKey { get; init; }
    public string Asn { get; init; } = string.Empty;
    public int Samples { get; init; }
    public double? IPv4SuccessRate { get; init; }
    public double? IPv6SuccessRate { get; init; }
    public double UploadStallRate { get; init; }
    public double Confidence { get; init; }
}

public sealed record IncidentTimelineEvent
{
    public required DateTimeOffset ObservedAt { get; init; }
    public required string Kind { get; init; }
    public required string Summary { get; init; }
    public double Confidence { get; init; }
}

public sealed record AnonymousIntelligenceRecord
{
    public required string NetworkKey { get; init; }
    public required string GenomeKey { get; init; }
    public required string FailureClass { get; init; }
    public double Confidence { get; init; }
    public string StrategyId { get; init; } = string.Empty;
    public bool? Succeeded { get; init; }
    public long ObservedAtBucketUnixHours { get; init; }
}

public sealed record IntelligenceGraphEdge
{
    public required string From { get; init; }
    public required string To { get; init; }
    public required string Relation { get; init; }
    public double Weight { get; init; }
}

public sealed record FailureRiskPrediction
{
    public double Risk { get; init; }
    public IReadOnlyList<string> Reasons { get; init; } = [];
}
