namespace ServiceLib.Reviver.Models;

/// <summary>
/// One non-destructive validation arm in a repair experiment. The planner never combines
/// mutations from different candidates; every arm remains a normal Reviver candidate that
/// must pass existing static and real-core validation before promotion.
/// </summary>
public sealed record RepairExperimentArm
{
    public required string CandidateId { get; init; }
    public string StrategyId { get; init; } = string.Empty;
    public IReadOnlyList<string> MutationFields { get; init; } = [];
    public IReadOnlyList<string> ConflictFields { get; init; } = [];
    public double InformationValue { get; init; }
    public double EstimatedCost { get; init; }
    public double PriorityScore { get; init; }
    public string Rationale { get; init; } = string.Empty;
}

public sealed record RepairMutationConflict
{
    public required string Field { get; init; }
    public IReadOnlyList<string> CandidateIds { get; init; } = [];
    public IReadOnlyList<string> TargetValues { get; init; } = [];
}

/// <summary>
/// Explainable A/B-style plan for deciding which already-bounded Reviver candidates to
/// validate first. It is advisory ordering only; it never routes user traffic or promotes
/// a profile on its own.
/// </summary>
public sealed record RepairExperimentPlan
{
    public ERepairFailureClass FailureClass { get; init; } = ERepairFailureClass.Unknown;
    public double FailureConfidence { get; init; }
    public bool BaselineRequired { get; init; } = true;
    public IReadOnlyList<RepairExperimentArm> Arms { get; init; } = [];
    public IReadOnlyList<RepairMutationConflict> Conflicts { get; init; } = [];
    public string? NextCandidateId => Arms.FirstOrDefault()?.CandidateId;
}
