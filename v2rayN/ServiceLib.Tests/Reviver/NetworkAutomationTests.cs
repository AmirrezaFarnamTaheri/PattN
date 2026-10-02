using System.Security.Cryptography;
using ServiceLib.Reviver.Intelligence;

namespace ServiceLib.Tests.Reviver;

public class NetworkAutomationTests
{
    [Test]
    public async Task ShadowTesting_ShouldRequireMeaningfulImprovement()
    {
        var service = new NetworkAutomationService();
        var baseline = new[]
        {
            new ShadowSample { Success = true, LatencyMs = 120 },
            new ShadowSample { Success = false },
            new ShadowSample { Success = true, LatencyMs = 130 },
        };
        var candidate = new[]
        {
            new ShadowSample { Success = true, LatencyMs = 70 },
            new ShadowSample { Success = true, LatencyMs = 75 },
            new ShadowSample { Success = true, LatencyMs = 80 },
        };

        var decision = service.EvaluateShadow(baseline, candidate);

        await decision.PromoteCandidate.Should().BeTrue();
        await (decision.Improvement >= 0.08d).Should().BeTrue();
    }

    [Test]
    public async Task Optimization_ShouldRefuseAutoApplyWhenConfidenceIsLow()
    {
        var service = new NetworkAutomationService();
        var decision = service.ChooseOptimization(
        [
            new OptimizationOption
            {
                Id = "candidate",
                RuntimeScore = 95,
                LearnedSuccessRate = 0.95,
                EvidenceConfidence = 0.60,
                MutationRisk = 0.05,
            }
        ]);

        await decision.SelectedId.Should().BeEqualTo("candidate");
        await decision.AutoApplyAllowed.Should().BeFalse();
    }

    [Test]
    public async Task Optimization_ShouldRejectNonFiniteInput()
    {
        var service = new NetworkAutomationService();
        var threw = false;
        try
        {
            _ = service.ChooseOptimization(
            [
                new OptimizationOption
                {
                    Id = "invalid",
                    RuntimeScore = double.NaN,
                    LearnedSuccessRate = 0.5d,
                    EvidenceConfidence = 0.9d,
                    MutationRisk = 0.1d,
                }
            ]);
        }
        catch (ArgumentException)
        {
            threw = true;
        }

        await threw.Should().BeTrue();
    }

    [Test]
    public async Task ShadowTesting_ShouldRejectInvalidThresholdAndLatency()
    {
        var service = new NetworkAutomationService();
        var badThreshold = false;
        try
        {
            _ = service.EvaluateShadow(
                [new ShadowSample { Success = true, LatencyMs = 10d }],
                [new ShadowSample { Success = true, LatencyMs = 10d }],
                minimumImprovement: double.NaN,
                minimumSamples: 1);
        }
        catch (ArgumentOutOfRangeException)
        {
            badThreshold = true;
        }

        var badLatency = false;
        try
        {
            _ = service.EvaluateShadow(
                [new ShadowSample { Success = true, LatencyMs = double.PositiveInfinity }],
                [new ShadowSample { Success = true, LatencyMs = 10d }],
                minimumImprovement: 0d,
                minimumSamples: 1);
        }
        catch (ArgumentException)
        {
            badLatency = true;
        }

        await badThreshold.Should().BeTrue();
        await badLatency.Should().BeTrue();
    }

    [Test]
    public async Task SyncCodec_ShouldRoundTripAnonymousRecords()
    {
        var service = new NetworkAutomationService();
        var key = RandomNumberGenerator.GetBytes(32);
        var records = new[]
        {
            new AnonymousIntelligenceRecord
            {
                NetworkKey = "net:key",
                GenomeKey = "genome:key",
                FailureClass = "UplinkStall",
                Confidence = 0.8,
                StrategyId = "ipv6",
                Succeeded = true,
                ObservedAtBucketUnixHours = 42,
            }
        };

        var encrypted = service.EncryptSyncPayload(records, key);
        var decoded = service.DecryptSyncPayload(encrypted, key);

        await decoded.Count.Should().BeEqualTo(1);
        await decoded[0].NetworkKey.Should().BeEqualTo("net:key");
    }

    [Test]
    public async Task SyncCodec_ShouldRejectOversizedEncryptedPayload()
    {
        var service = new NetworkAutomationService();
        var key = RandomNumberGenerator.GetBytes(32);
        var payload = new byte[8 * 1024 * 1024 + 30];
        payload[0] = 1;

        var threw = false;
        try
        {
            _ = service.DecryptSyncPayload(payload, key);
        }
        catch (InvalidDataException)
        {
            threw = true;
        }

        await threw.Should().BeTrue();
    }

    [Test]
    public async Task UpdateManifest_ShouldVerifyHashSignatureAndRejectDowngrade()
    {
        var service = new NetworkAutomationService();
        var artifact = "artifact"u8.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(artifact)).ToLowerInvariant();
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var unsigned = new SignedUpdateManifest
        {
            Version = "8.0.0",
            ArtifactSha256 = hash,
            MinimumVersion = "7.0.0",
            PublishedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            SignatureBase64 = string.Empty,
        };
        var manifest = Sign(unsigned, signer);

        var verified = service.VerifyUpdateManifest(
            manifest,
            artifact,
            signer.ExportSubjectPublicKeyInfo(),
            "7.9.0",
            TimeSpan.FromDays(14));
        var downgrade = service.VerifyUpdateManifest(
            Sign(unsigned with { Version = "6.9.0" }, signer),
            artifact,
            signer.ExportSubjectPublicKeyInfo(),
            "7.9.0",
            TimeSpan.FromDays(14));

        await verified.Valid.Should().BeTrue();
        await downgrade.Valid.Should().BeFalse();
    }

    [Test]
    public async Task UpdateManifest_ShouldRejectPattNPatchDowngradeWithValidSignature()
    {
        var service = new NetworkAutomationService();
        var artifact = "artifact"u8.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(artifact)).ToLowerInvariant();
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        var downgrade = Sign(new SignedUpdateManifest
        {
            Version = "7.25.2-P1",
            ArtifactSha256 = hash,
            MinimumVersion = "7.25.2",
            PublishedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            SignatureBase64 = string.Empty,
        }, signer);

        var verified = service.VerifyUpdateManifest(
            downgrade,
            artifact,
            signer.ExportSubjectPublicKeyInfo(),
            "7.25.2-P10",
            TimeSpan.FromDays(14));

        await verified.Valid.Should().BeFalse();
        await verified.Error.Contains("downgrade", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
    }

    [Test]
    public async Task StrategyPlanner_ShouldRejectConflictingTargetsForSameField()
    {
        var service = new NetworkAutomationService();
        var first = new RepairCandidate
        {
            Id = "ipv4",
            SessionId = "s",
            Profile = new ProfileItem(),
            Score = 90,
            Mutations =
            [
                new RepairMutation
                {
                    Kind = ERepairMutationKind.PreferAddressFamily,
                    Field = nameof(ProfileItem.TargetStrategy),
                    To = "UseIPv4",
                    Reason = "test",
                    Confidence = ERepairConfidence.EvidenceBacked,
                }
            ],
        };
        var second = new RepairCandidate
        {
            Id = "ipv6",
            SessionId = "s",
            Profile = new ProfileItem(),
            Score = 80,
            Mutations =
            [
                new RepairMutation
                {
                    Kind = ERepairMutationKind.PreferAddressFamily,
                    Field = nameof(ProfileItem.TargetStrategy),
                    To = "UseIPv6",
                    Reason = "test",
                    Confidence = ERepairConfidence.EvidenceBacked,
                }
            ],
        };

        var plan = service.PlanCompatibleCandidates([second, first]);

        await plan.SelectedCandidateIds.SequenceEqual(["ipv4"]).Should().BeTrue();
        await plan.Conflicts.Count.Should().BeEqualTo(1);
        await plan.Conflicts[0].CandidateId.Should().BeEqualTo("ipv6");
    }

    [Test]
    public async Task Sharing_ShouldRequireExplicitAnonymousAggregateOptInAndKAnonymity()
    {
        var service = new NetworkAutomationService();
        var rows = Enumerable.Range(0, 5).Select(_ => new AnonymousIntelligenceRecord
        {
            NetworkKey = "net:key",
            GenomeKey = "genome:key",
            FailureClass = "UplinkStall",
            Confidence = 0.8,
            StrategyId = "ipv6",
            Succeeded = true,
            ObservedAtBucketUnixHours = 42,
        }).ToArray();

        var batch = service.PrepareShareBatch(rows, new IntelligenceSharingPolicy
        {
            ExplicitOptIn = true,
            Mode = EIntelligencePrivacyMode.AnonymousAggregate,
            MinimumAggregateSamples = 5,
        });

        await batch.Records.Count.Should().BeEqualTo(1);
        var threw = false;
        try
        {
            _ = service.PrepareShareBatch(rows, new IntelligenceSharingPolicy
            {
                ExplicitOptIn = false,
                Mode = EIntelligencePrivacyMode.AnonymousAggregate,
                MinimumAggregateSamples = 5,
            });
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        await threw.Should().BeTrue();
    }

    [Test]
    public async Task FleetHealth_ShouldCountDuplicateGenomes()
    {
        var service = new NetworkAutomationService();
        var genome = new ProxyGenome
        {
            Key = "same",
            Protocol = "VLESS",
            Transport = "ws",
            Core = "Xray",
        };

        var health = service.BuildFleetHealth(
        [
            (genome, 0.95d),
            (genome, 0.05d),
        ]);

        await health.Total.Should().BeEqualTo(2);
        await health.DuplicateGenomes.Should().BeEqualTo(1);
    }
    private static SignedUpdateManifest Sign(SignedUpdateManifest manifest, ECDsa signer)
    {
        var signature = signer.SignData(
            System.Text.Encoding.UTF8.GetBytes(NetworkAutomationService.BuildManifestPayload(manifest)),
            HashAlgorithmName.SHA256);
        return manifest with { SignatureBase64 = Convert.ToBase64String(signature) };
    }

}
