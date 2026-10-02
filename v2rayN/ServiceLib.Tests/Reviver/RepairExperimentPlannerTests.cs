using ServiceLib.Reviver.Models;
using ServiceLib.Reviver.Services;

namespace ServiceLib.Tests.Reviver;

public class RepairExperimentPlannerTests
{
    [Test]
    public async Task Plan_ShouldSurfaceConflictingTargetsWithoutCombiningCandidates()
    {
        var ipv4 = Candidate("a", "dns-a", nameof(ProfileItem.TargetStrategy), "UseIPv4");
        var ipv6 = Candidate("b", "dns-b", nameof(ProfileItem.TargetStrategy), "UseIPv6");

        var plan = RepairExperimentPlanner.Plan(
            ERepairFailureClass.NoUsableAddressFamily,
            [ipv4, ipv6],
            new RepairFailureAssessment
            {
                FailureClass = ERepairFailureClass.NoUsableAddressFamily,
                Confidence = 0.55,
            });

        await plan.Conflicts.Should().HaveCount(1);
        await plan.Conflicts[0].Field.Should().BeEqualTo(nameof(ProfileItem.TargetStrategy));
        await plan.Conflicts[0].TargetValues.Should().Contain("UseIPv4");
        await plan.Conflicts[0].TargetValues.Should().Contain("UseIPv6");
        await plan.Arms.Should().HaveCount(2);
        await plan.Arms.All(x => x.ConflictFields.Contains(nameof(ProfileItem.TargetStrategy))).Should().BeTrue();
    }

    [Test]
    public async Task Plan_ShouldPreferIsolatedLowCostArmBeforeMultiMutationArm()
    {
        var simple = Candidate("simple", "endpoint", nameof(ProfileItem.Address), "203.0.113.10");
        simple.Evidence =
        [
            new RepairEvidence
            {
                Kind = "discovery.endpoint",
                Summary = "observed endpoint",
            }
        ];

        var complex = new RepairCandidate
        {
            Id = "complex",
            SessionId = "session",
            StrategyId = "multi",
            Profile = new ProfileItem(),
            State = ERepairCandidateState.StaticValidated,
            Mutations =
            [
                Mutation(nameof(ProfileItem.Address), "203.0.113.20", ERepairConfidence.EvidenceBacked),
                Mutation(nameof(ProfileItem.CoreType), ECoreType.Xray.ToString(), ERepairConfidence.Speculative),
            ],
        };

        var plan = RepairExperimentPlanner.Plan(
            ERepairFailureClass.ConnectionTimeout,
            [complex, simple]);

        await plan.NextCandidateId.Should().BeEqualTo("simple");
        await (plan.Arms[0].EstimatedCost < plan.Arms[1].EstimatedCost).Should().BeTrue();

        var ordered = RepairExperimentPlanner.OrderForValidation([complex, simple], plan);
        await ordered[0].Id.Should().BeEqualTo("simple");
        await ordered[1].Id.Should().BeEqualTo("complex");
    }

    [Test]
    public async Task OrderForValidation_ShouldRetainCandidatesMissingFromAdvisoryPlan()
    {
        var planned = Candidate("planned", "one", nameof(ProfileItem.Address), "203.0.113.1");
        var rejected = Candidate("rejected", "two", nameof(ProfileItem.Address), "203.0.113.2");
        rejected.State = ERepairCandidateState.Rejected;

        var plan = RepairExperimentPlanner.Plan(
            ERepairFailureClass.ConnectionTimeout,
            [planned, rejected]);
        var ordered = RepairExperimentPlanner.OrderForValidation([rejected, planned], plan);

        await ordered.Should().HaveCount(2);
        await ordered[0].Id.Should().BeEqualTo("planned");
        await ordered[1].Id.Should().BeEqualTo("rejected");
    }

    private static RepairCandidate Candidate(
        string id,
        string strategy,
        string field,
        string target)
        => new()
        {
            Id = id,
            SessionId = "session",
            StrategyId = strategy,
            Profile = new ProfileItem(),
            State = ERepairCandidateState.StaticValidated,
            Mutations = [Mutation(field, target, ERepairConfidence.EvidenceBacked)],
        };

    private static RepairMutation Mutation(
        string field,
        string target,
        ERepairConfidence confidence)
        => new()
        {
            Kind = ERepairMutationKind.Canonicalize,
            Field = field,
            From = "old",
            To = target,
            Reason = "test",
            Confidence = confidence,
        };
}
