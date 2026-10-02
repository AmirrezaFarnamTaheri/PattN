using ServiceLib.Reviver.Models;

namespace ServiceLib.Reviver.Services;

/// <summary>
/// Aggregates proxy health inside a subscription without collapsing the subscription into
/// one proxy. Every profile remains an independent entry and contributes its own test history.
/// </summary>
public sealed class SubscriptionFleetHealthService(
    ProxyTestHistoryService? historyService = null)
{
    private readonly ProxyTestHistoryService _history = historyService ?? new ProxyTestHistoryService();

    public async Task<SubscriptionFleetHealthSummary> GetAsync(
        string subscriptionId,
        int maxRunsPerProfile = 20,
        CancellationToken cancellationToken = default)
    {
        if (subscriptionId.IsNullOrEmpty())
        {
            throw new ArgumentException("Subscription fleet health requires a subscription ID.", nameof(subscriptionId));
        }
        if (maxRunsPerProfile is < 1 or > 500)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRunsPerProfile));
        }

        cancellationToken.ThrowIfCancellationRequested();
        var profiles = await AppManager.Instance.ProfileItems(subscriptionId) ?? [];
        cancellationToken.ThrowIfCancellationRequested();
        var diagnostics = await _history.GetDiagnosticsAsync(
            profiles.Select(x => x.IndexId),
            maxRunsPerProfile);
        cancellationToken.ThrowIfCancellationRequested();

        return Summarize(
            subscriptionId,
            profiles.Select(x => x.IndexId).ToArray(),
            diagnostics);
    }

    public static SubscriptionFleetHealthSummary Summarize(
        string subscriptionId,
        IReadOnlyList<string> profileIndexIds,
        IReadOnlyList<ProxyTestHistoryDiagnosticSummary> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(profileIndexIds);
        ArgumentNullException.ThrowIfNull(diagnostics);

        var ids = profileIndexIds
            .Where(x => x.IsNotEmpty())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var byId = diagnostics
            .Where(x => x.ProfileIndexId.IsNotEmpty())
            .GroupBy(x => x.ProfileIndexId, StringComparer.Ordinal)
            .ToDictionary(
                x => x.Key,
                x => x.OrderByDescending(y => y.LastTestedAtUnixMs).First(),
                StringComparer.Ordinal);

        var entries = ids
            .Select(id => byId.TryGetValue(id, out var diagnostic)
                ? FromDiagnostic(diagnostic)
                : new ProxyFleetHealthEntry
                {
                    ProfileIndexId = id,
                    State = EProxyFleetHealthState.Untested,
                })
            .ToArray();

        var tested = entries.Where(x => x.State != EProxyFleetHealthState.Untested).ToArray();
        var delaySamples = tested
            .Where(x => x.AverageDelayMs is > 0)
            .Select(x => x.AverageDelayMs!.Value)
            .ToArray();

        return new SubscriptionFleetHealthSummary
        {
            SubscriptionId = subscriptionId,
            TotalProfiles = entries.Length,
            TestedProfiles = tested.Length,
            UntestedProfiles = entries.Count(x => x.State == EProxyFleetHealthState.Untested),
            HealthyProfiles = entries.Count(x => x.State == EProxyFleetHealthState.Healthy),
            DegradedProfiles = entries.Count(x => x.State == EProxyFleetHealthState.Degraded),
            UnhealthyProfiles = entries.Count(x => x.State == EProxyFleetHealthState.Unhealthy),
            AverageSuccessRate = tested.Length == 0
                ? null
                : tested.Average(x => x.SuccessRate ?? 0d),
            AverageDelayMs = delaySamples.Length == 0 ? null : delaySamples.Average(),
            Profiles = entries,
        };
    }

    private static ProxyFleetHealthEntry FromDiagnostic(
        ProxyTestHistoryDiagnosticSummary diagnostic)
    {
        var state = diagnostic.LastSuccess
                    && diagnostic.SuccessRate >= 0.80d
                    && diagnostic.ConsecutiveFailures == 0
            ? EProxyFleetHealthState.Healthy
            : diagnostic.LastSuccess || diagnostic.SuccessRate >= 0.50d
                ? EProxyFleetHealthState.Degraded
                : EProxyFleetHealthState.Unhealthy;

        return new ProxyFleetHealthEntry
        {
            ProfileIndexId = diagnostic.ProfileIndexId,
            State = state,
            SuccessRate = diagnostic.SuccessRate,
            ConsecutiveFailures = diagnostic.ConsecutiveFailures,
            AverageDelayMs = diagnostic.AverageDelayMs,
            AverageSpeed = diagnostic.AverageSpeed,
            LastTestedAtUnixMs = diagnostic.LastTestedAtUnixMs,
        };
    }
}
