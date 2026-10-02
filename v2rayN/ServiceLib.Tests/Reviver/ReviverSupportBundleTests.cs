using ServiceLib.Models.Entities;
using ServiceLib.Reviver.Models;
using ServiceLib.Reviver.Support;

namespace ServiceLib.Tests.Reviver;

public class ReviverSupportBundleTests
{
    [Test]
    public async Task Bundle_ShouldPreserveDiagnosticShapeWithoutLeakingProfileSecrets()
    {
        const string host = "private-front.example";
        const string ip = "203.0.113.77";
        const string password = "0b6d9d5e-21e6-4aa2-9f2f-a13de5ac45b8";
        const string publicKey = "VERY-PRIVATE-REALITY-PUBLIC-KEY";
        const string path = "/private/customer/path?token=super-secret";

        var profile = new ProfileItem
        {
            ConfigType = EConfigType.VLESS,
            CoreType = ECoreType.Xray,
            ConfigVersion = 4,
            IndexId = "profile-private-id",
            Subid = "subscription-private-id",
            IsSub = true,
            Remarks = "customer private remarks",
            Address = host,
            Port = 443,
            Password = password,
            Username = "private-user",
            Network = "ws",
            StreamSecurity = "tls",
            Sni = host,
            PublicKey = publicKey,
            ShortId = "private-short-id",
            Fingerprint = "fingerprint-secret.example",
            Alpn = "h2",
        };
        profile.SetTransportExtra(new TransportExtraItem
        {
            Host = host,
            Path = path,
            GrpcAuthority = host,
            GrpcServiceName = "private-grpc-service",
        });

        var session = new RepairSession { Original = ProfileSnapshot.Capture(profile) };
        var candidate = new RepairCandidate
        {
            SessionId = session.Id,
            Profile = profile,
            FailureClassAddressed = ERepairFailureClass.NetworkUnreachable,
            State = ERepairCandidateState.RuntimeValidated,
            Mutations =
            [
                new RepairMutation
                {
                    Kind = ERepairMutationKind.ReplaceEndpoint,
                    Field = "Address",
                    From = host,
                    To = ip,
                    Reason = "observed endpoint replacement",
                    Confidence = ERepairConfidence.EvidenceBacked,
                },
                new RepairMutation
                {
                    Kind = ERepairMutationKind.ReplaceEndpoint,
                    Field = "customer-secret-key.example",
                    From = "1234567890123456",
                    To = "9876543210987654",
                    Reason = "unknown metadata must be tokenized",
                    Confidence = ERepairConfidence.EvidenceBacked,
                },
            ],
            Evidence =
            [
                new RepairEvidence
                {
                    Kind = "endpoint.probe",
                    Summary = "raw summary contains " + host,
                    Source = "https://" + host + "/api?token=secret",
                    Data = new Dictionary<string, string>
                    {
                        ["address"] = ip,
                        ["latencyMs"] = "12.5",
                        [" latencyMs"] = "13.5",
                        ["status"] = "secret.example/private",
                        ["opaque"] = "another-private-value",
                        ["numericToken"] = "1234567890123456",
                        ["customer-secret-key.example"] = "1",
                    },
                },
            ],
            Validation = new RepairValidationEvidence { Attempts = 3, Successes = 3, MedianLatencyMs = 12.5 },
        };

        var result = new RepairRunResult
        {
            Session = session,
            Diagnosis = new RepairDiagnosis
            {
                FailureClass = ERepairFailureClass.NetworkUnreachable,
                ResolvedAddresses = [ip],
                Evidence = candidate.Evidence,
            },
            PlannedCandidates = [candidate],
            ValidatedCandidates = [candidate],
            RecommendedCandidate = candidate,
        };

        var builder = new ReviverSupportBundleBuilder(Enumerable.Range(1, 32).Select(x => (byte)x).ToArray());
        var bundle = builder.Build(result);
        var json = builder.Serialize(result);

        await bundle.Profile.AddressToken.Should().BeEqualTo(bundle.Profile.SniToken);
        await bundle.Profile.AddressToken.Should().BeEqualTo(bundle.Profile.HttpHostToken);
        await bundle.Candidates[0].Mutations[0].From.Should().BeEqualTo(bundle.Profile.AddressToken);
        await bundle.Candidates[0].Mutations[0].To.Should().BeEqualTo(bundle.ResolvedAddressTokens[0]);
        await bundle.Evidence[0].Data["latencyMs"].Should().BeEqualTo("12.5");
        await bundle.Evidence[0].Data["latencyMs#2"].Should().BeEqualTo("13.5");
        await bundle.Evidence[0].Data.ContainsKey("numericToken").Should().BeFalse();
        await bundle.Evidence[0].Data
            .Count(x => x.Key.StartsWith("key:", StringComparison.Ordinal)
                        && x.Value.StartsWith("tok:", StringComparison.Ordinal))
            .Should().BeEqualTo(3);
        await bundle.Candidates[0].Mutations[1].Field.StartsWith("tok:", StringComparison.Ordinal).Should().BeTrue();

        foreach (var secret in new[]
                 {
                     host, ip, password, publicKey, path, "profile-private-id", "subscription-private-id",
                     "customer private remarks", "private-user", "private-grpc-service",
                     "another-private-value", "token=secret", "numericToken", "fingerprint-secret.example",
                     "secret.example/private", "customer-secret-key.example",
                     "1234567890123456", "9876543210987654",
                 })
        {
            await json.Contains(secret, StringComparison.Ordinal).Should().BeFalse();
        }

        await json.Contains("NetworkUnreachable", StringComparison.Ordinal).Should().BeTrue();
        await json.Contains("ReplaceEndpoint", StringComparison.Ordinal).Should().BeTrue();
        await json.Contains("tok:", StringComparison.Ordinal).Should().BeTrue();
    }

    [Test]
    public async Task DefaultBuilder_ShouldUseFreshTokenSaltForEachBundle()
    {
        var profile = new ProfileItem
        {
            ConfigType = EConfigType.VLESS,
            Address = "same-private-endpoint.example",
            Port = 443,
            Password = Guid.NewGuid().ToString(),
            Network = "tcp",
            StreamSecurity = "tls",
        };
        var result = new RepairRunResult
        {
            Session = new RepairSession { Original = ProfileSnapshot.Capture(profile) },
            Diagnosis = new RepairDiagnosis { FailureClass = ERepairFailureClass.ConnectionTimeout },
        };

        var builder = new ReviverSupportBundleBuilder();
        var first = builder.Build(result);
        var second = builder.Build(result);

        await (first.Profile.AddressToken != second.Profile.AddressToken).Should().BeTrue();
    }

    [Test]
    public async Task Bundle_ShouldTokenizeUnknownEvidenceKinds()
    {
        const string secretKind = "customer-secret-key.example";
        var profile = new ProfileItem
        {
            ConfigType = EConfigType.VLESS,
            Address = "example.com",
            Port = 443,
            Password = Guid.NewGuid().ToString(),
            Network = "tcp",
            StreamSecurity = "tls",
        };
        var session = new RepairSession { Original = ProfileSnapshot.Capture(profile) };
        var result = new RepairRunResult
        {
            Session = session,
            Diagnosis = new RepairDiagnosis
            {
                FailureClass = ERepairFailureClass.ConnectionTimeout,
                Evidence =
                [
                    new RepairEvidence
                    {
                        Kind = secretKind,
                        Summary = "unknown metadata kind",
                        Data = new Dictionary<string, string> { ["attempts"] = "3" },
                    },
                ],
            },
        };

        var builder = new ReviverSupportBundleBuilder(Enumerable.Range(1, 32).Select(x => (byte)x).ToArray());
        var bundle = builder.Build(result);
        var json = builder.Serialize(result);

        await bundle.Evidence[0].Kind.StartsWith("tok:", StringComparison.Ordinal).Should().BeTrue();
        await json.Contains(secretKind, StringComparison.Ordinal).Should().BeFalse();
    }

    [Test]
    public async Task Export_ShouldWriteOnlyRedactedBundle()
    {
        var profile = new ProfileItem
        {
            ConfigType = EConfigType.VMess,
            Address = "secret.example",
            Port = 443,
            Password = "93011d5e-80fd-4626-a0ed-3f329d2bf6d6",
            Network = "tcp",
            StreamSecurity = "tls",
            Remarks = "do-not-export",
        };
        var session = new RepairSession { Original = ProfileSnapshot.Capture(profile) };
        var result = new RepairRunResult
        {
            Session = session,
            Diagnosis = new RepairDiagnosis { FailureClass = ERepairFailureClass.ConnectionTimeout },
        };

        var root = Path.Combine(Path.GetTempPath(), "pattn-support-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var path = Path.Combine(root, "support.json");
            await new ReviverSupportBundleBuilder(new byte[32]).ExportAsync(path, result);
            var json = await File.ReadAllTextAsync(path);

            await json.Contains("secret.example", StringComparison.Ordinal).Should().BeFalse();
            await json.Contains(profile.Password, StringComparison.Ordinal).Should().BeFalse();
            await json.Contains(profile.Remarks, StringComparison.Ordinal).Should().BeFalse();
            await json.Contains("ConnectionTimeout", StringComparison.Ordinal).Should().BeTrue();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
