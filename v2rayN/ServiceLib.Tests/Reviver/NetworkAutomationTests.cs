using System.Security.Cryptography;
using ServiceLib.Reviver.Intelligence;

namespace ServiceLib.Tests.Reviver;

public class NetworkAutomationTests
{
    private const string ValidNetworkKey = "net:v1:0123456789abcdef01234567";
    private const string ValidGenomeKey = "genome:v1:abcdef0123456789abcdef01";
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
                NetworkKey = ValidNetworkKey,
                GenomeKey = ValidGenomeKey,
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
        await decoded[0].NetworkKey.Should().BeEqualTo(ValidNetworkKey);
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
            NetworkKey = ValidNetworkKey,
            GenomeKey = ValidGenomeKey,
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
    public async Task Sharing_ShouldRejectRawIdentifiersAndCoarsenTimeBucket()
    {
        var service = new NetworkAutomationService();
        var valid = Enumerable.Range(0, 5).Select(_ => new AnonymousIntelligenceRecord
        {
            NetworkKey = ValidNetworkKey,
            GenomeKey = ValidGenomeKey,
            FailureClass = "UplinkStall",
            Confidence = 0.8,
            StrategyId = "ipv6",
            Succeeded = true,
            ObservedAtBucketUnixHours = 49,
        }).ToArray();

        var batch = service.PrepareShareBatch(valid, new IntelligenceSharingPolicy
        {
            ExplicitOptIn = true,
            Mode = EIntelligencePrivacyMode.AnonymousAggregate,
            MinimumAggregateSamples = 5,
        });
        await batch.Records.Single().ObservedAtBucketUnixHours.Should().BeEqualTo(48);

        var raw = valid.Select(x => x with { NetworkKey = "private-carrier.example" }).ToArray();
        var rejected = false;
        try
        {
            _ = service.PrepareShareBatch(raw, new IntelligenceSharingPolicy
            {
                ExplicitOptIn = true,
                Mode = EIntelligencePrivacyMode.AnonymousAggregate,
                MinimumAggregateSamples = 5,
            });
        }
        catch (ArgumentException)
        {
            rejected = true;
        }

        await rejected.Should().BeTrue();
    }

    [Test]
    public async Task SyncCodec_ShouldRejectNonDerivedIdentifiers()
    {
        var service = new NetworkAutomationService();
        var key = RandomNumberGenerator.GetBytes(32);
        var rejected = false;
        try
        {
            _ = service.EncryptSyncPayload(
            [
                new AnonymousIntelligenceRecord
                {
                    NetworkKey = "raw-network-name",
                    GenomeKey = ValidGenomeKey,
                    FailureClass = "UplinkStall",
                    Confidence = 0.8,
                    StrategyId = "ipv6",
                    ObservedAtBucketUnixHours = 48,
                }
            ], key);
        }
        catch (InvalidDataException)
        {
            rejected = true;
        }

        await rejected.Should().BeTrue();
    }

    [Test]
    public async Task FleetHealth_ShouldRejectOverlappingThresholds()
    {
        var service = new NetworkAutomationService();
        var genome = new ProxyGenome
        {
            Key = "same",
            Protocol = "VLESS",
            Transport = "ws",
            Core = "Xray",
        };

        var rejected = false;
        try
        {
            _ = service.BuildFleetHealth([(genome, 0.5d)], healthyThreshold: 0.5d, deadThreshold: 0.5d);
        }
        catch (ArgumentOutOfRangeException)
        {
            rejected = true;
        }

        await rejected.Should().BeTrue();
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
    private static SignedUpdateManifest MakeManifest(string version, string hash, string minimum)
        => new()
        {
            Version = version,
            ArtifactSha256 = hash,
            MinimumVersion = minimum,
            PublishedAtUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            SignatureBase64 = string.Empty,
        };

    [Test]
    public async Task UpdateManifest_ShouldRejectSameVersionUnlessReinstallIsExplicit()
    {
        var service = new NetworkAutomationService();
        var artifact = "artifact"u8.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(artifact)).ToLowerInvariant();
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var manifest = Sign(MakeManifest("7.25.2", hash, "7.0.0"), signer);
        var key = signer.ExportSubjectPublicKeyInfo();

        var sameVersion = service.VerifyUpdateManifest(manifest, artifact, key, "7.25.2", TimeSpan.FromDays(14));
        var asReinstall = service.VerifyUpdateManifest(
            manifest, artifact, key, "7.25.2", TimeSpan.FromDays(14), allowSameVersionReinstall: true);

        await sameVersion.Valid.Should().BeFalse("an equal version is not an upgrade");
        await sameVersion.Error.Contains("reinstall", StringComparison.OrdinalIgnoreCase)
            .Should().BeTrue("the refusal must name the way out");
        await asReinstall.Valid.Should().BeTrue("an explicit reinstall flow may re-verify the same build");
    }

    [Test]
    public async Task UpdateManifest_ShouldRefuseAnEmptyTrustRootRatherThanCallItABadKey()
    {
        var service = new NetworkAutomationService();
        var artifact = "artifact"u8.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(artifact)).ToLowerInvariant();
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var manifest = Sign(MakeManifest("8.0.0", hash, "7.0.0"), signer);

        var empty = service.VerifyUpdateManifest(manifest, artifact, ReadOnlySpan<byte>.Empty, "7.9.0", TimeSpan.FromDays(14));

        await empty.Valid.Should().BeFalse();
        await empty.Error.Should().Be("No trust-root public key was supplied for manifest verification.");
    }

    [Test]
    public async Task UpdateManifest_ShouldRejectTrustRootWithTrailingData()
    {
        var service = new NetworkAutomationService();
        var artifact = "artifact"u8.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(artifact)).ToLowerInvariant();
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var manifest = Sign(MakeManifest("8.0.0", hash, "7.0.0"), signer);

        // A key buffer padded with anything (a second concatenated key, a PEM trailer, a truncated
        // frame that happens to parse) must not silently verify against the prefix.
        var real = signer.ExportSubjectPublicKeyInfo();
        var padded = new byte[real.Length + 8];
        real.CopyTo(padded, 0);

        var verification = service.VerifyUpdateManifest(manifest, artifact, padded, "7.9.0", TimeSpan.FromDays(14));

        await verification.Valid.Should().BeFalse();
        await verification.Error.Contains("trailing data", StringComparison.OrdinalIgnoreCase).Should().BeTrue();
    }

    [Test]
    public async Task UpdateManifest_CurrentVersionFromTheManifestDefeatsTheDowngradeGuard()
    {
        // Documents the call-site hazard the doc comment forbids: if the caller feeds the manifest's
        // own MinimumVersion in as `currentVersion`, a publisher can authorise any target by moving
        // the floor. The verifier cannot see through that -- only a pinned, separately-fetched current
        // version (Utils.GetVersionInfo) or a pinned trust root closes it -- so this stays a
        // *characterisation* test rather than a claim that the code is safe.
        var service = new NetworkAutomationService();
        var artifact = "artifact"u8.ToArray();
        var hash = Convert.ToHexString(SHA256.HashData(artifact)).ToLowerInvariant();
        using var signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var manifest = Sign(MakeManifest("1.2.3", hash, "1.2.3"), signer);

        var naiveCaller = service.VerifyUpdateManifest(
            manifest, artifact, signer.ExportSubjectPublicKeyInfo(), "1.2.3", TimeSpan.FromDays(14),
            allowSameVersionReinstall: true);

        await naiveCaller.Valid.Should().BeTrue("the signature and hash do check out; the guard is only as good as its inputs");
    }

    private static SignedUpdateManifest Sign(SignedUpdateManifest manifest, ECDsa signer)
    {
        var signature = signer.SignData(
            System.Text.Encoding.UTF8.GetBytes(NetworkAutomationService.BuildManifestPayload(manifest)),
            HashAlgorithmName.SHA256);
        return manifest with { SignatureBase64 = Convert.ToBase64String(signature) };
    }

}
