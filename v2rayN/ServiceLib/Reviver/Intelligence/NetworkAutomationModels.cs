namespace ServiceLib.Reviver.Intelligence;

public enum EHumanFeedbackKind
{
    ProxyWorks = 0,
    RepairWorked,
    RepairFailed,
    FalsePositiveDiagnosis,
}

public sealed record HumanFeedbackObservation
{
    public required EHumanFeedbackKind Kind { get; init; }
    public required string GenomeKey { get; init; }
    public required string NetworkKey { get; init; }
    public string StrategyId { get; init; } = string.Empty;
    public DateTimeOffset ObservedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record ShadowSample
{
    public bool Success { get; init; }
    public double? LatencyMs { get; init; }
}

public sealed record ShadowTestDecision
{
    public bool PromoteCandidate { get; init; }
    public double BaselineScore { get; init; }
    public double CandidateScore { get; init; }
    public double Improvement { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public sealed record FleetHealthSnapshot
{
    public int Total { get; init; }
    public int Healthy { get; init; }
    public int Degraded { get; init; }
    public int Dead { get; init; }
    public int UniqueGenomes { get; init; }
    public int DuplicateGenomes { get; init; }
}

public sealed record OptimizationOption
{
    public required string Id { get; init; }
    public double RuntimeScore { get; init; }
    public double LearnedSuccessRate { get; init; } = 0.5d;
    public double EvidenceConfidence { get; init; }
    public double MutationRisk { get; init; }
}

public sealed record OptimizationDecision
{
    public string? SelectedId { get; init; }
    public double Score { get; init; }
    public bool AutoApplyAllowed { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public sealed record ArchitectureHealthSnapshot
{
    public bool CodeTestsPassing { get; init; }
    public bool PackagingTestsPassing { get; init; }
    public bool DurabilityTestsPassing { get; init; }
    public int KnownSecurityFindings { get; init; }
    public int StaleDependencies { get; init; }
    public double Score { get; init; }
}

public sealed record ReleaseComponentSecurity
{
    public required string Component { get; init; }
    public bool DigestVerified { get; init; }
    public bool SignatureVerified { get; init; }
    public bool ProvenanceVerified { get; init; }
}

public sealed record ReleaseSecurityAssessment
{
    public IReadOnlyList<ReleaseComponentSecurity> Components { get; init; } = [];
    public bool AllDigestsVerified { get; init; }
    public bool AllRequiredSignaturesVerified { get; init; }
    public bool ProvenanceComplete { get; init; }
}

public sealed record SignedUpdateManifest
{
    public required string Version { get; init; }
    public required string ArtifactSha256 { get; init; }
    public required string MinimumVersion { get; init; }
    public required long PublishedAtUnixSeconds { get; init; }
    public required string SignatureBase64 { get; init; }
}

public sealed record UpdateManifestVerification
{
    public bool Valid { get; init; }
    public string Error { get; init; } = string.Empty;
}

public sealed record HeatmapCell
{
    public required string CountryCode { get; init; }
    public required string CarrierKey { get; init; }
    public int Samples { get; init; }
    public double IPv4SuccessRate { get; init; }
    public double IPv6SuccessRate { get; init; }
    public double UploadStallRate { get; init; }
}
