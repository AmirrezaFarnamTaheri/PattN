using ServiceLib.Discovery.Services;
using ServiceLib.Reviver.Intelligence;
using ServiceLib.Reviver.Normalization;
using ServiceLib.Reviver.Services;

namespace ServiceLib.Reviver.Strategies;

/// <summary>
/// Canonical composition for PattN's built-in Reviver strategies. Ordering is not the final execution order:
/// ReviverService still sorts by failure-specific priority, confidence, and strategy ID.
/// </summary>
public static class ReviverStrategyCatalog
{
    public static IReadOnlyList<IRepairLifecycleObserver> CreateDefaultObservers(
        IDnsRepairHistoryStore? dnsHistory)
        => CreateDefaultObservers(dnsHistory, null);

    public static IReadOnlyList<IRepairLifecycleObserver> CreateDefaultObservers(
        IDnsRepairHistoryStore? dnsHistory,
        IStrategyOutcomeStore? strategyOutcomes,
        NetworkIntelligenceService? intelligence = null,
        Func<NetworkFingerprint?>? currentNetwork = null)
    {
        var observers = new List<IRepairLifecycleObserver>();
        if (dnsHistory is not null)
        {
            observers.Add(new DnsRepairHistoryObserver(dnsHistory));
        }
        if (strategyOutcomes is not null)
        {
            observers.Add(new StrategyLearningObserver(
                strategyOutcomes,
                intelligence ?? new NetworkIntelligenceService(),
                currentNetwork));
        }
        return observers;
    }

    public static IReadOnlyList<IRepairStrategy> CreateDefault(
        IDiscoveryCandidateProvider discoveryCandidates,
        IDnsRepairEvidenceProvider dnsRepairEvidence,
        ProfileCoreCompatibility? compatibility = null,
        string? uploadStallFinalMaskJson = null)
    {
        ArgumentNullException.ThrowIfNull(discoveryCandidates);
        ArgumentNullException.ThrowIfNull(dnsRepairEvidence);

        compatibility ??= new ProfileCoreCompatibility();
        var strategies = new List<IRepairStrategy>
        {
            new DnsAddressFamilyStrategy(dnsRepairEvidence, compatibility),
            new EndpointReplacementStrategy(discoveryCandidates),
            new CoreFallbackStrategy(compatibility),
        };
        if (uploadStallFinalMaskJson.IsNotEmpty())
        {
            strategies.Add(new UploadStallMitigationStrategy(
                compatibility,
                uploadStallFinalMaskJson!));
        }

        return strategies;
    }
}
