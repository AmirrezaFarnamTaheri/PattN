namespace ServiceLib.Reviver.Models;

/// <summary>
/// Explainable confidence attached to the already-selected repair failure class.
/// Confidence is descriptive evidence quality; it never authorizes mutation by itself.
/// </summary>
public sealed record RepairFailureAssessment
{
    public ERepairFailureClass FailureClass { get; init; } = ERepairFailureClass.Unknown;
    public double Confidence { get; init; }
    public string Basis { get; init; } = string.Empty;
    public IReadOnlyList<string> Signals { get; init; } = [];
    public DateTimeOffset ObservedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Non-secret semantic profile fingerprint used to find similar proxy shapes without storing credentials,
/// endpoint hostnames/IPs, UUIDs, certificates, or subscription identifiers.
/// </summary>
public sealed record ProxyGenome
{
    public string Schema { get; init; } = "proxy-genome-v1";
    public string Protocol { get; init; } = string.Empty;
    public string Transport { get; init; } = string.Empty;
    public string Core { get; init; } = string.Empty;
    public string TlsMode { get; init; } = string.Empty;
    public string AddressKind { get; init; } = string.Empty;
    public string TargetStrategy { get; init; } = string.Empty;
    public bool HasExplicitSni { get; init; }
    public bool HasHttpHost { get; init; }
    public bool UsesReality { get; init; }
    public bool UsesEch { get; init; }
    public bool UsesFinalMask { get; init; }
    public bool UsesMux { get; init; }
    public string Fingerprint { get; init; } = string.Empty;
}

/// <summary>
/// Privacy-preserving summary of network evidence. Raw endpoint addresses/domains are deliberately excluded.
/// Provider/ASN/POP values appear only when an evidence source explicitly supplied them.
/// </summary>
public sealed record NetworkObservationFingerprint
{
    public string Schema { get; init; } = "network-observation-v1";
    public bool HasIPv4 { get; init; }
    public bool HasIPv6 { get; init; }
    public IReadOnlyList<string> Providers { get; init; } = [];
    public IReadOnlyList<string> Asns { get; init; } = [];
    public IReadOnlyList<string> Pops { get; init; } = [];
    public IReadOnlyList<string> Signals { get; init; } = [];
    public DateTimeOffset ObservedAt { get; init; } = DateTimeOffset.UtcNow;
    public string Fingerprint { get; init; } = string.Empty;
}

public sealed record RepairRecommendationExplanation
{
    public string CandidateId { get; init; } = string.Empty;
    public string StrategyId { get; init; } = string.Empty;
    public double? Score { get; init; }
    public IReadOnlyList<string> Reasons { get; init; } = [];
    public IReadOnlyList<string> Risks { get; init; } = [];
}
