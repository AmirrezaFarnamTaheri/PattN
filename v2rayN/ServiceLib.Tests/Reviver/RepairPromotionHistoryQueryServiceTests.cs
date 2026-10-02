using ServiceLib.Models.Entities;
using ServiceLib.Reviver.Models;
using ServiceLib.Reviver.Services;

namespace ServiceLib.Tests.Reviver;

public class RepairPromotionHistoryQueryServiceTests
{
    [Test]
    public async Task Summarize_ShouldProjectTypedEvidenceAndOutcomeCounts()
    {
        var now = new DateTimeOffset(2026, 9, 23, 20, 0, 0, TimeSpan.Zero);
        var rows = new[]
        {
            new RepairPromotionHistoryItem
            {
                Id = "promote-1",
                EventKind = "promoted",
                SessionId = "session-1",
                CandidateId = "candidate-1",
                StrategyId = "endpoint-replacement",
                OriginalProfileId = "profile-1",
                PromotedProfileId = "profile-2",
                Score = 95,
                OutcomeVerdict = "improved",
                MutationsJson = JsonUtils.Serialize(new[]
                {
                    new RepairMutation
                    {
                        Kind = ERepairMutationKind.ReplaceEndpoint,
                        Field = nameof(ProfileItem.Address),
                        From = "origin.example",
                        To = "203.0.113.10",
                        Reason = "validated",
                        Confidence = ERepairConfidence.EvidenceBacked,
                    }
                }, false),
                BaselineValidationJson = JsonUtils.Serialize(new RepairValidationEvidence
                {
                    Attempts = 3,
                    Successes = 1,
                    ConsecutiveSuccesses = 1,
                }, false),
                CandidateValidationJson = JsonUtils.Serialize(new RepairValidationEvidence
                {
                    Attempts = 3,
                    Successes = 3,
                    ConsecutiveSuccesses = 3,
                }, false),
                OutcomeComparisonJson = JsonUtils.Serialize(new RepairOutcomeComparison
                {
                    BaselineAvailable = true,
                    CandidateReliability = 1,
                    BaselineReliability = 1d / 3d,
                    ReliabilityDelta = 2d / 3d,
                    Verdict = "improved",
                    Reasons = ["reliability-improved"],
                }, false),
                ObservedAtUnixMs = now.ToUnixTimeMilliseconds(),
            },
            new RepairPromotionHistoryItem
            {
                Id = "rollback-1",
                EventKind = "rolled-back",
                SessionId = "session-1",
                CandidateId = "candidate-1",
                StrategyId = "endpoint-replacement",
                OriginalProfileId = "profile-1",
                PromotedProfileId = "profile-2",
                OutcomeVerdict = string.Empty,
                ObservedAtUnixMs = now.AddMinutes(5).ToUnixTimeMilliseconds(),
            },
        };

        var summary = RepairPromotionHistoryQueryService.Summarize(rows, now.AddMinutes(5));

        await summary.TotalEvents.Should().BeEqualTo(2);
        await summary.Promotions.Should().BeEqualTo(1);
        await summary.Rollbacks.Should().BeEqualTo(1);
        await summary.Improved.Should().BeEqualTo(1);
        await summary.Unknown.Should().BeEqualTo(1);
        await summary.Strategies.Should().HaveCount(1);
        await summary.Strategies[0].StrategyId.Should().BeEqualTo("endpoint-replacement");
        await summary.Strategies[0].Promotions.Should().BeEqualTo(1);
        await summary.Strategies[0].Rollbacks.Should().BeEqualTo(1);
        await summary.Strategies[0].Improved.Should().BeEqualTo(1);
        await summary.Strategies[0].RatedPromotions.Should().BeEqualTo(1);
        await summary.Strategies[0].SuccessRate.Should().BeEqualTo(1d);
        await summary.Strategies[0].RollbackRate.Should().BeEqualTo(1d);
        await summary.Strategies[0].RecencyWeightedSuccessRate.Should().BeEqualTo(1d);
        await summary.Entries[0].EventKind.Should().BeEqualTo("rolled-back");
        await summary.Entries[1].StrategyId.Should().BeEqualTo("endpoint-replacement");
        await summary.Entries[1].Mutations.Count.Should().BeEqualTo(1);
        await summary.Entries[1].BaselineValidation!.Successes.Should().BeEqualTo(1);
        await summary.Entries[1].CandidateValidation!.Successes.Should().BeEqualTo(3);
        await summary.Entries[1].OutcomeComparison!.Verdict.Should().BeEqualTo("improved");
    }

    [Test]
    public async Task Summarize_ShouldDownWeightOldStrategyOutcomes()
    {
        var now = new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero);
        var rows = new[]
        {
            new RepairPromotionHistoryItem
            {
                Id = "recent-success",
                EventKind = "promoted",
                StrategyId = "dns-address-family",
                OutcomeVerdict = "improved",
                ObservedAtUnixMs = now.ToUnixTimeMilliseconds(),
            },
            new RepairPromotionHistoryItem
            {
                Id = "old-regression",
                EventKind = "promoted",
                StrategyId = "dns-address-family",
                OutcomeVerdict = "regressed",
                ObservedAtUnixMs = now.AddDays(-90).ToUnixTimeMilliseconds(),
            },
        };

        var summary = RepairPromotionHistoryQueryService.Summarize(rows, now);
        var strategy = summary.Strategies.Single();

        await strategy.SuccessRate.Should().BeEqualTo(0.5d);
        await (strategy.RecencyWeightedSuccessRate > strategy.SuccessRate).Should().BeTrue();
        await strategy.RatedPromotions.Should().BeEqualTo(2);
    }

    [Test]
    public async Task Summarize_ShouldWeightHumanConfirmationAboveAutomaticOutcome()
    {
        var now = new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero);
        var rows = new[]
        {
            new RepairPromotionHistoryItem
            {
                Id = "auto-success",
                EventKind = "promoted",
                StrategyId = "endpoint-replacement",
                OutcomeVerdict = "improved",
                ObservedAtUnixMs = now.ToUnixTimeMilliseconds(),
            },
            new RepairPromotionHistoryItem
            {
                Id = "human-negative",
                EventKind = "human-confirmed",
                StrategyId = "endpoint-replacement",
                OutcomeVerdict = "regressed",
                ObservedAtUnixMs = now.ToUnixTimeMilliseconds(),
            },
        };

        var summary = RepairPromotionHistoryQueryService.Summarize(rows, now);
        var strategy = summary.Strategies.Single();

        await strategy.SuccessRate.Should().BeEqualTo(1d);
        await strategy.HumanConfirmedPositive.Should().BeEqualTo(0);
        await strategy.HumanConfirmedNegative.Should().BeEqualTo(1);
        await strategy.HumanConfirmedSuccessRate.Should().BeEqualTo(0d);
        await strategy.RecencyWeightedSuccessRate.Should().BeEqualTo(1d);
        await strategy.RecencyWeightedLearningSuccessRate.Should().BeEqualTo(1d / 3d);
    }

    [Test]
    public async Task Summarize_ShouldRejectMalformedHistoricalEvidence()
    {
        var rows = new[]
        {
            new RepairPromotionHistoryItem
            {
                Id = "corrupt",
                EventKind = "promoted",
                OutcomeVerdict = "unknown",
                MutationsJson = "{broken",
                ObservedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            }
        };

        var threw = false;
        try
        {
            _ = RepairPromotionHistoryQueryService.Summarize(rows);
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains(nameof(RepairPromotionHistoryItem.MutationsJson), StringComparison.Ordinal);
        }

        await threw.Should().BeTrue();
    }
}
