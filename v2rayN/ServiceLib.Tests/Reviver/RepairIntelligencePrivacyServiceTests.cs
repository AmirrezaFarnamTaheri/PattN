using ServiceLib.Reviver.Models;
using ServiceLib.Reviver.Services;

namespace ServiceLib.Tests.Reviver;

public class RepairIntelligencePrivacyServiceTests
{
    [Test]
    public async Task LocalOnly_ShouldProduceNoShareablePayload()
    {
        var result = RepairIntelligencePrivacyService.BuildLearningEvent(
            new IntelligencePrivacyPolicy(),
            Genome(),
            Network(),
            Assessment(),
            "endpoint-replacement",
            "improved",
            humanConfirmed: false);

        await (result is null).Should().BeTrue();
    }

    [Test]
    public async Task AnonymousAggregate_ShouldExcludeRawProfileAndNetworkIdentifiers()
    {
        var result = RepairIntelligencePrivacyService.BuildLearningEvent(
            new IntelligencePrivacyPolicy
            {
                SharingMode = EIntelligenceSharingMode.AnonymousAggregate,
                IncludeNetworkFingerprint = false,
            },
            Genome(),
            Network(),
            Assessment(),
            "custom strategy containing secret.example",
            "improved",
            humanConfirmed: true,
            observedAt: new DateTimeOffset(2026, 10, 2, 18, 30, 0, TimeSpan.Zero));

        var json = JsonUtils.Serialize(result, false);

        await (result is not null).Should().BeTrue();
        await result!.NetworkFingerprint.Should().BeEqualTo(string.Empty);
        await result.StrategyId.StartsWith("custom:", StringComparison.Ordinal).Should().BeTrue();
        await result.FailureConfidenceBucket.Should().BeEqualTo(0.9d);
        await result.ObservedDayUtc.Should().BeEqualTo(new DateOnly(2026, 10, 2));
        await json.Contains("secret.example", StringComparison.OrdinalIgnoreCase).Should().BeFalse();
        await json.Contains("203.0.113.44", StringComparison.Ordinal).Should().BeFalse();
    }

    private static ProxyGenome Genome()
        => new()
        {
            Protocol = "VLESS",
            Transport = "ws",
            Fingerprint = "genome:v1:abc",
        };

    private static NetworkObservationFingerprint Network()
        => new()
        {
            HasIPv4 = true,
            Providers = ["provider-a"],
            Asns = ["AS64500"],
            Fingerprint = "network:v1:def",
        };

    private static RepairFailureAssessment Assessment()
        => new()
        {
            FailureClass = ERepairFailureClass.UploadStall,
            Confidence = 0.86d,
            Signals = ["upload-stall", "203.0.113.44"],
        };
}
