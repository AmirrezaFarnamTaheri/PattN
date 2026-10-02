using System.Security.Cryptography;
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
    public async Task UplinkProbeEvidence_Alone_ShouldNotClaimAsymmetricUplinkStall()
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

        await assessment.FailureClass.Should().BeEqualTo(ERepairFailureClass.ApplicationProbeFailure);
        await (assessment.Confidence < 0.90d).Should().BeTrue();
    }

    [Test]
    public async Task DerivedKeyValidators_ShouldRejectRawAndLocalOnlySentinelForExport()
    {
        var service = new NetworkIntelligenceService();
        var network = service.BuildFingerprint(new NetworkObservation { Carrier = "carrier" });
        var genome = service.BuildGenome(new ProfileItem
        {
            ConfigType = EConfigType.VLESS,
            Address = "example.com",
        });

        await NetworkIntelligenceService.IsDerivedNetworkKey(network.Key).Should().BeTrue();
        await NetworkIntelligenceService.IsDerivedGenomeKey(genome.Key).Should().BeTrue();
        await NetworkIntelligenceService.IsDerivedNetworkKey("net:unknown").Should().BeFalse();
        await NetworkIntelligenceService.IsLocalNetworkKey("net:unknown").Should().BeTrue();
        await NetworkIntelligenceService.IsDerivedNetworkKey("carrier.example").Should().BeFalse();
        await NetworkIntelligenceService.IsDerivedGenomeKey("example.com").Should().BeFalse();
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

    [Test]
    public void HashToken_ShouldBeKeyedDeterministicAndNotAnUnsaltedDigest()
    {
        var key = new byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(key);
        var service = new NetworkIntelligenceService(key);
        var other = new NetworkIntelligenceService(key);

        var carrier = "MCI";

        // Deterministic for one installation...
        var a = service.BuildFingerprint(new NetworkObservation { Carrier = carrier, Asn = "AS1234", CountryCode = "ir", TcpSucceeded = true });
        var b = other.BuildFingerprint(new NetworkObservation { Carrier = carrier, Asn = "AS1234", CountryCode = "IR", TcpSucceeded = true });
        await a.CarrierKey.Should().BeEqualTo(b.CarrierKey);
        await a.Key.Should().BeEqualTo(b.Key);

        // ...but the key is what makes it a *derived* value: a bare SHA-256 of the carrier name must
        // never appear in the record, otherwise the "anonymous" key is a dictionary lookup.
        var unsalted = Convert.ToHexString(
                System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(carrier.Trim().ToLowerInvariant())))
            [..24].ToLowerInvariant();
        await a.CarrierKey.Should().NotBeEqualTo(unsalted);

        // A different installation must not produce comparable keys.
        var c = new NetworkIntelligenceService(RandomNumberGenerator.GetBytes(32))
            .BuildFingerprint(new NetworkObservation { Carrier = carrier, Asn = "AS1234", CountryCode = "IR", TcpSucceeded = true });
        await c.CarrierKey.Should().NotBeEqualTo(a.CarrierKey);

        await NetworkIntelligenceService.IsKeyedDerivedNetworkKey(a.Key).Should().BeTrue();
        await NetworkIntelligenceService.IsDerivedNetworkKey("net:v1:" + unsalted).Should().BeTrue("legacy rows stay readable");
    }
}
