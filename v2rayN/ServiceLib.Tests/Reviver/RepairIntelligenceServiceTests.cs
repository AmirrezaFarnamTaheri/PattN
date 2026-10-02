using ServiceLib.Reviver.Models;
using ServiceLib.Reviver.Services;

namespace ServiceLib.Tests.Reviver;

public class RepairIntelligenceServiceTests
{
    [Test]
    public async Task ProxyGenome_ShouldDescribeShapeWithoutProfileSecrets()
    {
        var profile = new ProfileItem
        {
            ConfigType = EConfigType.VLESS,
            CoreType = ECoreType.Xray,
            Address = "secret-origin.example",
            Port = 443,
            Password = "11111111-2222-3333-4444-555555555555",
            Network = nameof(ETransport.ws),
            StreamSecurity = Global.StreamSecurity,
            Sni = "secret-sni.example",
            Finalmask = """{"tcp":[{"type":"fragment"}]}""",
            MuxEnabled = true,
        };
        profile.SetTransportExtra(new TransportExtraItem
        {
            Host = "secret-host.example",
            Path = "/ws",
        });

        var genome = RepairIntelligenceService.BuildProxyGenome(profile);
        var json = JsonUtils.Serialize(genome, false);

        await genome.Protocol.Should().BeEqualTo(EConfigType.VLESS.ToString());
        await genome.Transport.Should().BeEqualTo(nameof(ETransport.ws));
        await genome.HasExplicitSni.Should().BeTrue();
        await genome.HasHttpHost.Should().BeTrue();
        await genome.UsesFinalMask.Should().BeTrue();
        await genome.Fingerprint.StartsWith("genome:v1:", StringComparison.Ordinal).Should().BeTrue();
        await json.Contains("secret-origin.example", StringComparison.Ordinal).Should().BeFalse();
        await json.Contains(profile.Password, StringComparison.Ordinal).Should().BeFalse();
        await json.Contains("secret-sni.example", StringComparison.Ordinal).Should().BeFalse();
        await json.Contains("secret-host.example", StringComparison.Ordinal).Should().BeFalse();
    }

    [Test]
    public async Task NetworkFingerprint_ShouldKeepFamiliesAndExplicitMetadataButDropRawEndpoints()
    {
        var diagnosis = new RepairDiagnosis
        {
            FailureClass = ERepairFailureClass.ConnectionTimeout,
            ResolvedAddresses = ["203.0.113.10", "2001:db8::10"],
            Evidence =
            [
                new RepairEvidence
                {
                    Kind = "discovery.endpoint",
                    Summary = "test",
                    Data = new Dictionary<string, string>
                    {
                        ["provider"] = "provider-a",
                        ["asn"] = "AS64500",
                        ["pop"] = "edge-1",
                        ["address"] = "203.0.113.10",
                    },
                }
            ],
        };

        var fingerprint = RepairIntelligenceService.BuildNetworkFingerprint(diagnosis);
        var json = JsonUtils.Serialize(fingerprint, false);

        await fingerprint.HasIPv4.Should().BeTrue();
        await fingerprint.HasIPv6.Should().BeTrue();
        await fingerprint.Providers.Should().Contain("provider-a");
        await fingerprint.Asns.Should().Contain("AS64500");
        await json.Contains("203.0.113.10", StringComparison.Ordinal).Should().BeFalse();
        await json.Contains("2001:db8::10", StringComparison.Ordinal).Should().BeFalse();
    }

    [Test]
    public async Task FailureAssessment_ShouldDecayOldEvidence()
    {
        var now = new DateTimeOffset(2026, 10, 2, 0, 0, 0, TimeSpan.Zero);
        RepairDiagnosis Build(DateTimeOffset observedAt) => new()
        {
            FailureClass = ERepairFailureClass.ConnectionTimeout,
            RuntimeValidation = new RepairValidationEvidence
            {
                Attempts = 3,
                Failures =
                [
                    ERepairFailureClass.ConnectionTimeout,
                    ERepairFailureClass.ConnectionTimeout,
                    ERepairFailureClass.ConnectionTimeout,
                ],
            },
            Evidence =
            [
                new RepairEvidence
                {
                    Kind = "test",
                    Summary = "timeout",
                    ObservedAt = observedAt,
                }
            ],
        };

        var fresh = RepairIntelligenceService.Assess(Build(now), now, TimeSpan.FromDays(90));
        var old = RepairIntelligenceService.Assess(Build(now.AddDays(-90)), now, TimeSpan.FromDays(90));

        await fresh.Confidence.Should().BeEqualTo(1d);
        await old.Confidence.Should().BeEqualTo(0.5d);
    }

    [Test]
    public async Task FailureAssessment_ShouldCapIntegritySuspectRuntimeEvidence()
    {
        var diagnosis = new RepairDiagnosis
        {
            FailureClass = ERepairFailureClass.ApplicationProbeFailure,
            RuntimeValidation = new RepairValidationEvidence
            {
                Attempts = 3,
                IntegritySuspect = true,
                Failures =
                [
                    ERepairFailureClass.ApplicationProbeFailure,
                    ERepairFailureClass.ApplicationProbeFailure,
                    ERepairFailureClass.ApplicationProbeFailure,
                ],
            },
        };

        var assessment = RepairIntelligenceService.Assess(diagnosis);

        await (assessment.Confidence <= 0.25d).Should().BeTrue();
        await assessment.Signals.Should().Contain("validation:integrity-suspect");
    }

    [Test]
    public async Task Explain_ShouldSurfaceEvidenceAndRisksWithoutInventedImprovementPercentages()
    {
        var candidate = new RepairCandidate
        {
            Id = "candidate",
            SessionId = "session",
            StrategyId = "endpoint-replacement",
            Profile = new ProfileItem(),
            Mutations =
            [
                new RepairMutation
                {
                    Kind = ERepairMutationKind.ReplaceEndpoint,
                    Field = nameof(ProfileItem.Address),
                    From = "old",
                    To = "new",
                    Reason = "test",
                    Confidence = ERepairConfidence.EvidenceBacked,
                }
            ],
            Validation = new RepairValidationEvidence
            {
                Attempts = 3,
                Successes = 3,
                ConsecutiveSuccesses = 3,
                MedianLatencyMs = 42,
            },
            Score = 91,
            ScoreBreakdown = new RepairCandidateScore
            {
                Overall = 91,
                Reliability = 1,
                MutationSafety = 0.82,
            },
        };
        var assessment = new RepairFailureAssessment
        {
            FailureClass = ERepairFailureClass.ConnectionTimeout,
            Confidence = 0.9,
        };

        var explanation = RepairIntelligenceService.Explain(candidate, assessment);

        await explanation.StrategyId.Should().BeEqualTo("endpoint-replacement");
        await explanation.Reasons.Should().Contain("runtime-success:3/3");
        await explanation.Reasons.Any(x => x.Contains("expected", StringComparison.OrdinalIgnoreCase)).Should().BeFalse();
    }
}
