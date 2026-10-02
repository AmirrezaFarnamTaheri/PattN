using ServiceLib.Reviver.Models;
using ServiceLib.Reviver.Services;
using ServiceLib.Services;

namespace ServiceLib.Tests.Reviver;

public class SubscriptionFleetHealthServiceTests
{
    [Test]
    public async Task Summarize_ShouldKeepEveryProxyIndependentInsideSubscription()
    {
        var diagnostics = new[]
        {
            Diagnostic("p1", successRate: 1, consecutiveFailures: 0, lastSuccess: true, delay: 40),
            Diagnostic("p2", successRate: 0.6, consecutiveFailures: 1, lastSuccess: true, delay: 90),
            Diagnostic("p3", successRate: 0.1, consecutiveFailures: 4, lastSuccess: false, delay: 0),
        };

        var summary = SubscriptionFleetHealthService.Summarize(
            "sub-1",
            ["p1", "p2", "p3", "p4"],
            diagnostics);

        await summary.TotalProfiles.Should().BeEqualTo(4);
        await summary.TestedProfiles.Should().BeEqualTo(3);
        await summary.HealthyProfiles.Should().BeEqualTo(1);
        await summary.DegradedProfiles.Should().BeEqualTo(1);
        await summary.UnhealthyProfiles.Should().BeEqualTo(1);
        await summary.UntestedProfiles.Should().BeEqualTo(1);
        await summary.Profiles.Should().HaveCount(4);
        await summary.Profiles.Select(x => x.ProfileIndexId).Should().Contain("p4");
    }

    [Test]
    public async Task Summarize_ShouldDeduplicateInputProfileIdsWithoutMergingHealthRows()
    {
        var summary = SubscriptionFleetHealthService.Summarize(
            "sub-1",
            ["p1", "p1", "p2"],
            [
                Diagnostic("p1", 1, 0, true, 30),
                Diagnostic("p2", 0, 3, false, 0),
            ]);

        await summary.TotalProfiles.Should().BeEqualTo(2);
        await summary.Profiles.Should().HaveCount(2);
        await summary.HealthyProfiles.Should().BeEqualTo(1);
        await summary.UnhealthyProfiles.Should().BeEqualTo(1);
    }

    private static ProxyTestHistoryDiagnosticSummary Diagnostic(
        string id,
        double successRate,
        int consecutiveFailures,
        bool lastSuccess,
        double delay)
        => new(
            id,
            SampleCount: 10,
            SuccessCount: (int)Math.Round(successRate * 10),
            FailureCount: 10 - (int)Math.Round(successRate * 10),
            ConsecutiveFailures: consecutiveFailures,
            SuccessRate: successRate,
            AverageDelayMs: delay,
            AverageSpeed: 100,
            LastTestedAtUnixMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            LastSuccess: lastSuccess);
}
