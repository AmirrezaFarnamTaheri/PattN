using ServiceLib.Reviver.Models;
using ServiceLib.Reviver.Validation;

namespace ServiceLib.Tests.Reviver;

public class RepairValidationAccumulatorTests
{
    [Test]
    public async Task Build_ShouldComputeQuorumEvidenceAndMedian()
    {
        var accumulator = new RepairValidationAccumulator();
        accumulator.AddSuccess(70);
        accumulator.AddFailure(ERepairFailureClass.ApplicationProbeFailure);
        accumulator.AddSuccess(50);

        var result = accumulator.Build();
        await result.Attempts.Should().BeEqualTo(3);
        await result.Successes.Should().BeEqualTo(2);
        await result.ConsecutiveSuccesses.Should().BeEqualTo(1);
        await result.MedianLatencyMs.Should().BeEqualTo(60);
        await result.LossRate.Should().BeEqualTo(1d / 3d);
        await result.MeetsQuorum(2).Should().BeTrue();
    }

    [Test]
    public async Task Build_ShouldMarkSuspectEvidenceWithoutRewritingTheCounts()
    {
        var accumulator = new RepairValidationAccumulator();
        accumulator.AddSuccess(70);
        accumulator.AddSuccess(50);

        var clean = accumulator.Build();
        var suspect = accumulator.Build(integritySuspect: true);

        // The measurements were really taken, so they stay in the record unchanged ...
        await suspect.Attempts.Should().BeEqualTo(clean.Attempts);
        await suspect.Successes.Should().BeEqualTo(clean.Successes);
        await suspect.MedianLatencyMs.Should().BeEqualTo(clean.MedianLatencyMs);
        await suspect.MeetsQuorum(2).Should().BeTrue();

        // ... but authorising callers must not accept them: the core that produced them can no
        // longer be shown to be the one serving the probed port.
        await clean.IntegritySuspect.Should().BeFalse();
        await suspect.IntegritySuspect.Should().BeTrue();
        await suspect.MeetsQuorumWithoutIntegrityDoubt(2).Should().BeFalse();
        await clean.MeetsQuorumWithoutIntegrityDoubt(2).Should().BeTrue();
    }
}
