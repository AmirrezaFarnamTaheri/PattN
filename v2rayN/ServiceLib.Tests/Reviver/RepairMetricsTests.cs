using ServiceLib.Reviver.Models;
using ServiceLib.Reviver.Services;

namespace ServiceLib.Tests.Reviver;

public class RepairMetricsTests
{
    [Test]
    [NotInParallel]
    public async Task Metrics_ShouldCountOnlyAggregateNonIdentifyingDimensions()
    {
        var strategyId = $"test-{Guid.NewGuid():N}";
        var before = RepairMetrics.Snapshot();

        RepairMetrics.RecordDiagnosis(new RepairDiagnosis
        {
            FailureClass = ERepairFailureClass.UploadStall,
        });
        RepairMetrics.RecordPlanned(2);
        RepairMetrics.RecordValidated(1);
        RepairMetrics.RecordPromotion(strategyId);
        RepairMetrics.RecordRollback(strategyId);

        var after = RepairMetrics.Snapshot();

        await (after.Diagnoses - before.Diagnoses).Should().BeEqualTo(1);
        await (after.CandidatesPlanned - before.CandidatesPlanned).Should().BeEqualTo(2);
        await (after.CandidatesValidated - before.CandidatesValidated).Should().BeEqualTo(1);
        await (after.Promotions - before.Promotions).Should().BeEqualTo(1);
        await (after.Rollbacks - before.Rollbacks).Should().BeEqualTo(1);
        await after.FailureClasses.ContainsKey(ERepairFailureClass.UploadStall.ToString()).Should().BeTrue();
        await after.StrategyPromotions[strategyId].Should().BeEqualTo(1);
        await after.StrategyRollbacks[strategyId].Should().BeEqualTo(1);

        var json = JsonUtils.Serialize(after, false);
        await json.Contains("Address", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        await json.Contains("ProfileId", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
    }
}
