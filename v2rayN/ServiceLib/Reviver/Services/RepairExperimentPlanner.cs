using ServiceLib.Reviver.Models;

namespace ServiceLib.Reviver.Services;

/// <summary>
/// Builds a deterministic, non-destructive experiment order from already bounded repair
/// candidates. Conflicting mutations are surfaced instead of merged, and inexpensive,
/// high-signal candidates are validated first.
/// </summary>
public static class RepairExperimentPlanner
{
    public static RepairExperimentPlan Plan(
        ERepairFailureClass failureClass,
        IReadOnlyList<RepairCandidate> candidates,
        RepairFailureAssessment? failureAssessment = null)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        var eligible = candidates
            .Where(x => x.State is ERepairCandidateState.StaticValidated or ERepairCandidateState.RuntimeValidated)
            .ToArray();

        var conflicts = BuildConflicts(eligible);
        var conflictFields = conflicts
            .Select(x => x.Field)
            .ToHashSet(StringComparer.Ordinal);
        var strategyCounts = eligible
            .GroupBy(x => x.StrategyId ?? string.Empty, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.Count(), StringComparer.Ordinal);

        var arms = eligible
            .Select(candidate => BuildArm(candidate, conflictFields, strategyCounts, failureAssessment))
            .OrderByDescending(x => x.PriorityScore)
            .ThenByDescending(x => x.InformationValue)
            .ThenBy(x => x.EstimatedCost)
            .ThenBy(x => x.CandidateId, StringComparer.Ordinal)
            .ToArray();

        return new RepairExperimentPlan
        {
            FailureClass = failureClass,
            FailureConfidence = failureAssessment?.Confidence ?? 0d,
            Arms = arms,
            Conflicts = conflicts,
        };
    }

    public static IReadOnlyList<RepairCandidate> OrderForValidation(
        IReadOnlyList<RepairCandidate> candidates,
        RepairExperimentPlan plan)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(plan);

        var byId = candidates.ToDictionary(x => x.Id, StringComparer.Ordinal);
        var ordered = new List<RepairCandidate>(candidates.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var arm in plan.Arms)
        {
            if (byId.TryGetValue(arm.CandidateId, out var candidate)
                && seen.Add(candidate.Id))
            {
                ordered.Add(candidate);
            }
        }

        foreach (var candidate in candidates)
        {
            if (seen.Add(candidate.Id))
            {
                ordered.Add(candidate);
            }
        }

        return ordered;
    }

    private static RepairExperimentArm BuildArm(
        RepairCandidate candidate,
        HashSet<string> conflictFields,
        IReadOnlyDictionary<string, int> strategyCounts,
        RepairFailureAssessment? assessment)
    {
        var mutationFields = candidate.Mutations
            .Select(x => x.Field ?? string.Empty)
            .Where(x => x.IsNotEmpty())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        var candidateConflicts = mutationFields
            .Where(conflictFields.Contains)
            .ToArray();

        var risk = candidate.Mutations.Count == 0
            ? 0d
            : candidate.Mutations.Max(x => ConfidenceRisk(x.Confidence));
        var estimatedCost = 1d
                            + Math.Max(0, candidate.Mutations.Count - 1) * 0.35d
                            + risk * 0.75d;

        var strategyId = candidate.StrategyId ?? string.Empty;
        var strategyNovelty = strategyCounts.TryGetValue(strategyId, out var count) && count > 0
            ? 1d / count
            : 1d;
        var uncertainty = 1d - Math.Clamp(assessment?.Confidence ?? 0.5d, 0d, 1d);
        var conflictValue = candidateConflicts.Length > 0 ? 0.35d : 0d;
        var evidenceValue = candidate.Evidence.Count > 0 ? 0.25d : 0d;
        var informationValue = Math.Clamp(
            0.5d + strategyNovelty * 0.35d + uncertainty * 0.25d + conflictValue + evidenceValue,
            0d,
            2d);
        var priority = informationValue / estimatedCost;

        var rationale = candidateConflicts.Length > 0
            ? $"Validates an alternative for conflicting field(s): {string.Join(", ", candidateConflicts)}."
            : candidate.Mutations.Count <= 1
                ? "Low-complexity candidate with isolated mutation scope."
                : "Multi-mutation candidate retained after simpler/high-information arms.";

        return new RepairExperimentArm
        {
            CandidateId = candidate.Id,
            StrategyId = strategyId,
            MutationFields = mutationFields,
            ConflictFields = candidateConflicts,
            InformationValue = Math.Round(informationValue, 4),
            EstimatedCost = Math.Round(estimatedCost, 4),
            PriorityScore = Math.Round(priority, 4),
            Rationale = rationale,
        };
    }

    private static IReadOnlyList<RepairMutationConflict> BuildConflicts(
        IReadOnlyList<RepairCandidate> candidates)
        => candidates
            .SelectMany(candidate => candidate.Mutations.Select(mutation => new
            {
                CandidateId = candidate.Id,
                Field = mutation.Field ?? string.Empty,
                Target = mutation.To ?? string.Empty,
            }))
            .Where(x => x.Field.IsNotEmpty())
            .GroupBy(x => x.Field, StringComparer.Ordinal)
            .Select(group => new
            {
                Field = group.Key,
                CandidateIds = group.Select(x => x.CandidateId)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray(),
                Targets = group.Select(x => x.Target)
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray(),
            })
            .Where(x => x.CandidateIds.Length > 1 && x.Targets.Length > 1)
            .OrderBy(x => x.Field, StringComparer.Ordinal)
            .Select(x => new RepairMutationConflict
            {
                Field = x.Field,
                CandidateIds = x.CandidateIds,
                TargetValues = x.Targets,
            })
            .ToArray();

    private static double ConfidenceRisk(ERepairConfidence confidence)
        => confidence switch
        {
            ERepairConfidence.Equivalent => 0d,
            ERepairConfidence.LowRisk => 0.2d,
            ERepairConfidence.EvidenceBacked => 0.45d,
            ERepairConfidence.Speculative => 1d,
            _ => 0.6d,
        };
}
