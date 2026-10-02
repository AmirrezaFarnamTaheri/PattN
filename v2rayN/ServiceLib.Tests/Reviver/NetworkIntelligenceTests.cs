using ServiceLib.Discovery.Protocol;
using ServiceLib.Models.Entities;
using ServiceLib.Reviver.Intelligence;
using ServiceLib.Reviver.Models;

namespace ServiceLib.Tests.Reviver;

public class NetworkIntelligenceTests
{
    [Test]
    public async Task Classify_ShouldDetectAsymmetricUplinkStallWithoutClaimingDpiAsFact()
    {
        var service = new NetworkIntelligenceService();
        var assessment = service.Classify(new NetworkObservation
        {
            DnsSucceeded = true,
            TcpSucceeded = true,
            TlsSucceeded = true,
            UploadSucceeded = false,
            DownstreamSucceeded = true,
            IPv4Succeeded = false,
            IPv6Succeeded = true,
            ObservedAt = DateTimeOffset.UtcNow,
        });

        await assessment.FailureClass.Should().BeEqualTo(ERepairFailureClass.UplinkStall);
        await (assessment.Confidence >= 0.90d).Should().BeTrue();
        await assessment.Reasons.Any(x => x.Contains("does not by itself prove DPI", StringComparison.Ordinal)).Should().BeTrue();
    }

    [Test]
    public async Task UplinkProbeEvidence_ShouldClassifyPostTlsUploadStall()
    {
        var service = new NetworkIntelligenceService();
        var assessment = service.ClassifyUplinkProbe(new DiscoveryUplinkProbeResult
        {
            Connected = true,
            TlsHandshakeSucceeded = true,
            BodyFullyRead = false,
            ResponseReceived = true,
            StatusCode = 204,
            BytesPlanned = 16 * 1024,
            BytesReadByClient = 8 * 1024,
            ChunksEmitted = 4,
            DurationMs = 2500,
        });

        await assessment.FailureClass.Should().BeEqualTo(ERepairFailureClass.UplinkStall);
    }

    [Test]
    public async Task BuildGenome_ShouldNotContainCredentialsOrRawEndpoint()
    {
        var service = new NetworkIntelligenceService();
        var profile = new ProfileItem
        {
            ConfigType = EConfigType.VLESS,
            Address = "secret-origin.example",
            Port = 443,
            Password = "sensitive-uuid",
            Network = "ws",
            StreamSecurity = "tls",
            Sni = "logical.example",
        };

        var genome = service.BuildGenome(profile);
        var json = JsonUtils.Serialize(genome, false);

        await json.Contains("sensitive-uuid", StringComparison.Ordinal).Should().BeFalse();
        await json.Contains("secret-origin.example", StringComparison.Ordinal).Should().BeFalse();
        await json.Contains("logical.example", StringComparison.Ordinal).Should().BeFalse();
    }

    [Test]
    public async Task EvidenceDecay_ShouldHalveConfidenceAtConfiguredHalfLife()
    {
        var now = DateTimeOffset.UtcNow;
        var decayed = NetworkIntelligenceService.ApplyEvidenceDecay(0.8d, now.AddDays(-45), now, 45d);

        await Math.Abs(decayed - 0.4d).Should().BeLessThan(0.0001d);
    }

    [Test]
    public async Task StrategySummary_ShouldWeightHumanEvidenceAndRollbackAsFailure()
    {
        var now = DateTimeOffset.UtcNow;
        var rows = new[]
        {
            new StrategyOutcomeHistoryItem
            {
                StrategyId = "ipv6",
                GenomeKey = "g",
                NetworkKey = "n",
                Succeeded = true,
                HumanConfirmed = true,
                ObservedAtUnixMs = now.ToUnixTimeMilliseconds(),
            },
            new StrategyOutcomeHistoryItem
            {
                StrategyId = "ipv6",
                GenomeKey = "g",
                NetworkKey = "n",
                Succeeded = true,
                RolledBack = true,
                ObservedAtUnixMs = now.ToUnixTimeMilliseconds(),
            },
        };

        var summary = SqliteStrategyOutcomeStore.Summarize("ipv6", rows, now);

        await summary.Samples.Should().BeEqualTo(2);
        await Math.Abs(summary.EffectiveSuccessRate - (2d / 3d)).Should().BeLessThan(0.0001d);
    }

    [Test]
    public async Task StrategySummary_ShouldIncludeRetainedDailyAggregates()
    {
        var now = DateTimeOffset.UtcNow;
        var day = new DateTimeOffset(now.UtcDateTime.Date.AddDays(-60), TimeSpan.Zero);
        var aggregates = new[]
        {
            new StrategyOutcomeAggregateItem
            {
                StrategyId = "endpoint-replacement",
                GenomeKey = "g",
                NetworkKey = "n",
                DayBucketUnixSeconds = day.ToUnixTimeSeconds(),
                Samples = 10,
                Successes = 8,
                HumanConfirmed = 2,
                HumanConfirmedSuccesses = 2,
            },
        };

        var summary = SqliteStrategyOutcomeStore.Summarize(
            "endpoint-replacement",
            [],
            aggregates,
            now,
            halfLifeDays: 90d);

        await summary.Samples.Should().BeEqualTo(10);
        await (summary.EffectiveSuccessRate > 0.80d).Should().BeTrue();
        await summary.LastObservedAt.Should().BeEqualTo(day);
    }

    [Test]
    public async Task ExperimentPlanner_ShouldPreferInformationGainPerCostAndRisk()
    {
        var service = new NetworkIntelligenceService();
        var plan = service.PlanExperiments(
        [
            new ExperimentHypothesis
            {
                Id = "expensive",
                Description = "expensive",
                ExpectedInformationGain = 0.9,
                EstimatedCostSeconds = 120,
                MutationRisk = 0.2,
            },
            new ExperimentHypothesis
            {
                Id = "cheap",
                Description = "cheap",
                ExpectedInformationGain = 0.7,
                EstimatedCostSeconds = 8,
                MutationRisk = 0.1,
            },
        ]);

        await plan.OrderedHypotheses[0].Id.Should().BeEqualTo("cheap");
    }

    [Test]
    public async Task AnonymousRecord_ShouldContainOnlyDerivedKeysAndNoRawCarrier()
    {
        var service = new NetworkIntelligenceService();
        var observation = new NetworkObservation
        {
            Carrier = "private-carrier-name",
            Asn = "AS64500",
            CountryCode = "IR",
            TcpSucceeded = true,
            TlsSucceeded = true,
            UploadSucceeded = false,
            DownstreamSucceeded = true,
        };
        var network = service.BuildFingerprint(observation);
        var genome = service.BuildGenome(new ProfileItem { ConfigType = EConfigType.VLESS, Address = "example.com" });
        var record = service.BuildAnonymousRecord(network, genome, service.Classify(observation));
        var json = JsonUtils.Serialize(record, false);

        await json.Contains("private-carrier-name", StringComparison.Ordinal).Should().BeFalse();
        await json.Contains("example.com", StringComparison.Ordinal).Should().BeFalse();
    }

    [Test]
    public async Task StrategyIdFor_ShouldPreferOriginatingStrategyId()
    {
        var candidate = new RepairCandidate
        {
            SessionId = "session",
            StrategyId = "endpoint-replacement",
            Profile = new ProfileItem(),
            Mutations =
            [
                new RepairMutation
                {
                    Kind = ERepairMutationKind.ReplaceEndpoint,
                    Field = nameof(ProfileItem.Address),
                    To = "203.0.113.10",
                    Reason = "test",
                    Confidence = ERepairConfidence.EvidenceBacked,
                }
            ],
        };

        await NetworkIntelligenceService.StrategyIdFor(candidate)
            .Should().BeEqualTo("endpoint-replacement");
    }

    [Test]
    public async Task PredictFailure_ShouldRaiseRiskOnSharpSuccessDropAndLatencySpike()
    {
        var service = new NetworkIntelligenceService();
        var prediction = service.PredictFailure([0.95, 0.92, 0.40], [80, 85, 180]);

        await (prediction.Risk >= 0.65d).Should().BeTrue();
        await (prediction.Reasons.Count >= 2).Should().BeTrue();
    }
}
