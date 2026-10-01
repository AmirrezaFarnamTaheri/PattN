namespace ServiceLib.Tests.Reviver;

public class ProxyTestHistoryPolicyServiceTests
{
    [Test]
    public async Task EvaluateRecords_MatchesFailureCountWithinWindow()
    {
        var policy = new SpeedTestItem
        {
            HistoryPolicyWindowCount = 5,
            HistoryPolicyFailureCount = 3
        };
        var rows = Rows(false, true, false, true, false);

        var match = ProxyTestHistoryService.EvaluateRecords("p1", rows, policy);

        await (match is not null).Should().BeTrue();
        await match!.SampleCount.Should().BeEqualTo(5);
        await match.FailureCount.Should().BeEqualTo(3);
    }

    [Test]
    public async Task EvaluateRecords_CarriesConfigurationFingerprintIntoDeploymentMatch()
    {
        var policy = new SpeedTestItem
        {
            HistoryPolicyConsecutiveFailures = 2
        };

        var match = ProxyTestHistoryService.EvaluateRecords(
            "p1",
            Rows(false, false),
            policy,
            "fingerprint-v1");

        await (match is not null).Should().BeTrue();
        await match!.ProfileFingerprint.Should().BeEqualTo("fingerprint-v1");
    }

    [Test]
    public async Task EvaluateRecords_MatchesConsecutiveFailures()
    {
        var policy = new SpeedTestItem
        {
            HistoryPolicyConsecutiveFailures = 3
        };
        var rows = Rows(false, false, false, true);

        var match = ProxyTestHistoryService.EvaluateRecords("p1", rows, policy);

        await (match is not null).Should().BeTrue();
        await match!.ConsecutiveFailures.Should().BeEqualTo(3);
    }

    [Test]
    public async Task EvaluateRecords_IgnoresSkippedRows()
    {
        var policy = new SpeedTestItem
        {
            HistoryPolicyWindowCount = 3,
            HistoryPolicyFailureCount = 2
        };
        var rows = Rows(false, true, false);
        rows.Insert(0, new ProxyTestHistoryItem { Skipped = true, Success = false, TestedAtUnixMs = 99 });

        var match = ProxyTestHistoryService.EvaluateRecords("p1", rows, policy);

        await (match is not null).Should().BeTrue();
        await match!.SampleCount.Should().BeEqualTo(3);
    }

    [Test]
    public async Task EvaluateRecords_RequiresCompleteWindow()
    {
        var policy = new SpeedTestItem
        {
            HistoryPolicyWindowCount = 5,
            HistoryPolicyFailureCount = 3
        };
        var rows = Rows(false, false, false, true);

        await (ProxyTestHistoryService.EvaluateRecords("p1", rows, policy) is null).Should().BeTrue();
    }

    [Test]
    public async Task SummarizeRecords_ComputesRecentReliabilityDiagnostics()
    {
        var rows = Rows(false, false, true, true);
        rows[2].DelayMs = 100;
        rows[2].Speed = 20;
        rows[3].DelayMs = 200;
        rows[3].Speed = 40;

        var summary = ProxyTestHistoryService.SummarizeRecords("p1", rows);

        await (summary is not null).Should().BeTrue();
        await summary!.SampleCount.Should().BeEqualTo(4);
        await summary.SuccessCount.Should().BeEqualTo(2);
        await summary.FailureCount.Should().BeEqualTo(2);
        await summary.ConsecutiveFailures.Should().BeEqualTo(2);
        await summary.SuccessRate.Should().BeEqualTo(0.5);
        await summary.AverageDelayMs.Should().BeEqualTo(150);
        await summary.AverageSpeed.Should().BeEqualTo(30);
        await summary.LastSuccess.Should().BeFalse();
    }

    [Test]
    public async Task SummarizeRecords_IgnoresSkippedAttempts()
    {
        var rows = Rows(false, true);
        rows.Insert(0, new ProxyTestHistoryItem
        {
            Id = Guid.NewGuid().ToString("N"),
            ProfileIndexId = "p1",
            Skipped = true,
            Success = false,
            TestedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 1
        });

        var summary = ProxyTestHistoryService.SummarizeRecords("p1", rows);

        await (summary is not null).Should().BeTrue();
        await summary!.SampleCount.Should().BeEqualTo(2);
        await summary.SuccessCount.Should().BeEqualTo(1);
        await summary.FailureCount.Should().BeEqualTo(1);
    }

    [Test]
    public async Task SummarizeRecords_IgnoresMissingMetricSamples()
    {
        var rows = Rows(true, true, true);
        rows[0].DelayMs = 100;
        rows[0].Speed = 40;
        rows[1].DelayMs = 0;
        rows[1].Speed = 0;
        rows[2].DelayMs = 200;
        rows[2].Speed = 20;

        var summary = ProxyTestHistoryService.SummarizeRecords("p1", rows);

        await (summary is not null).Should().BeTrue();
        await summary!.AverageDelayMs.Should().BeEqualTo(150);
        await summary.AverageSpeed.Should().BeEqualTo(30);
    }

    [Test]
    public async Task OrderDiagnosticsForBestConnection_PrefersReliabilityThenQuality()
    {
        var summaries = new[]
        {
            new ProxyTestHistoryDiagnosticSummary("flaky", 20, 18, 2, 1, 0.90, 50, 100, 30, true),
            new ProxyTestHistoryDiagnosticSummary("reliable-slow", 20, 20, 0, 0, 1.00, 200, 20, 20, true),
            new ProxyTestHistoryDiagnosticSummary("reliable-fast", 20, 20, 0, 0, 1.00, 80, 50, 10, true),
            new ProxyTestHistoryDiagnosticSummary("last-failed", 20, 20, 0, 0, 1.00, 20, 200, 40, false)
        };

        var ranked = ProxyTestHistoryService.OrderDiagnosticsForBestConnection(summaries);

        await ranked[0].ProfileIndexId.Should().BeEqualTo("reliable-fast");
        await ranked[1].ProfileIndexId.Should().BeEqualTo("reliable-slow");
        await ranked[2].ProfileIndexId.Should().BeEqualTo("flaky");
        await ranked[3].ProfileIndexId.Should().BeEqualTo("last-failed");
    }

    [Test]
    public async Task ComputeProfileFingerprint_IgnoresPresentationMetadata()
    {
        var first = new ProfileItem
        {
            IndexId = "one",
            Subid = "subscription-a",
            IsSub = true,
            DisplayLog = true,
            Remarks = "First label",
            ConfigType = EConfigType.VLESS,
            CoreType = ECoreType.Xray,
            Address = "example.com",
            Port = 443,
            Password = "11111111-1111-1111-1111-111111111111",
            Network = "tcp",
            StreamSecurity = "tls",
            Sni = "example.com"
        };
        var second = JsonUtils.DeepCopy(first)!;
        second.IndexId = "two";
        second.Subid = "subscription-b";
        second.IsSub = false;
        second.DisplayLog = false;
        second.Remarks = "Renamed";

        await ProxyTestHistoryService.ComputeProfileFingerprint(first)
            .Should().BeEqualTo(ProxyTestHistoryService.ComputeProfileFingerprint(second));
    }

    [Test]
    public async Task ComputeProfileFingerprint_IsStableAcrossStructuredExtraSerialization()
    {
        var first = new ProfileItem
        {
            ConfigType = EConfigType.VLESS,
            CoreType = ECoreType.Xray,
            Address = "example.com",
            Port = 443,
            Password = "11111111-1111-1111-1111-111111111111",
            Network = "ws",
            StreamSecurity = "tls",
            ProtoExtra = """{"Flow":null,"VlessEncryption":"none"}""",
            TransportExtra = """{"Host":"example.com","Path":null,"GrpcMode":null}"""
        };
        var second = JsonUtils.DeepCopy(first)!;
        second.ProtoExtra = """{ "VlessEncryption":"none", "Flow":"" }""";
        second.TransportExtra = """{ "GrpcMode":"", "Path":"", "Host":"example.com" }""";

        var firstFingerprint = ProxyTestHistoryService.ComputeProfileFingerprint(first);
        var secondFingerprint = ProxyTestHistoryService.ComputeProfileFingerprint(second);

        await firstFingerprint.StartsWith("fp2:", StringComparison.Ordinal).Should().BeTrue();
        await firstFingerprint.Should().BeEqualTo(secondFingerprint);
    }

    [Test]
    public async Task ComputeProfileFingerprint_ChangesWhenConnectionChanges()
    {
        var first = new ProfileItem
        {
            ConfigType = EConfigType.VLESS,
            CoreType = ECoreType.Xray,
            Address = "example.com",
            Port = 443,
            Password = "11111111-1111-1111-1111-111111111111",
            Network = "tcp",
            StreamSecurity = "tls",
            Sni = "example.com"
        };
        var second = JsonUtils.DeepCopy(first)!;
        second.Password = "22222222-2222-2222-2222-222222222222";

        await (ProxyTestHistoryService.ComputeProfileFingerprint(first)
               != ProxyTestHistoryService.ComputeProfileFingerprint(second))
            .Should().BeTrue();
    }

    private static List<ProxyTestHistoryItem> Rows(params bool[] successes)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return successes.Select((success, index) => new ProxyTestHistoryItem
        {
            Id = Guid.NewGuid().ToString("N"),
            ProfileIndexId = "p1",
            Success = success,
            TestedAtUnixMs = now - index,
            DelayMs = success ? 100 : 0,
            Speed = success ? 10 : 0
        }).ToList();
    }
}
