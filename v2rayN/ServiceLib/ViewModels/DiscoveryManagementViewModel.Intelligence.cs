using System.Globalization;
using ServiceLib.Models.Entities;
using ServiceLib.Reviver.Intelligence;

namespace ServiceLib.ViewModels;

public partial class DiscoveryManagementViewModel
{
    private readonly SqliteNetworkFingerprintStore _networkFingerprintStore = new();
    private readonly NetworkAutomationService _networkAutomation = new();

    [Reactive] public partial IReadOnlyList<HeatmapCell> NetworkHeatmapRows { get; set; } = [];
    [Reactive] public partial string NetworkIntelligenceSummaryText { get; set; } = ResUI.TbNetworkIntelligenceSummaryEmpty;
    [Reactive] public partial string StrategyLearningSummaryText { get; set; } = ResUI.TbStrategyLearningSummaryEmpty;

    public ReactiveCommand<RxVoid, RxVoid> RefreshNetworkIntelligenceCmd { get; private set; } = null!;

    private async Task RefreshNetworkIntelligenceAsync()
        => await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            await RefreshNetworkIntelligenceCoreAsync();
            StatusMessage = ResUI.TbDiscoveryManagementRefreshed;
        });

    private async Task RefreshNetworkIntelligenceCoreAsync()
    {
        var fingerprints = await _networkFingerprintStore.ListRecentAsync(500);
        NetworkHeatmapRows = _networkAutomation.BuildHeatmap(fingerprints);

        if (fingerprints.Count == 0)
        {
            NetworkIntelligenceSummaryText = ResUI.TbNetworkIntelligenceSummaryEmpty;
        }
        else
        {
            var latest = fingerprints[0];
            var uploadStalls = fingerprints.Count(x => x.DpiSignals.UploadStall);
            NetworkIntelligenceSummaryText = string.Format(
                CultureInfo.CurrentCulture,
                ResUI.TbNetworkIntelligenceSummaryLine,
                fingerprints.Count,
                uploadStalls,
                latest.DpiSignals.Confidence,
                latest.ObservedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.CurrentCulture));
        }

        var strategyRows = await SQLiteHelper.Instance.TableAsync<StrategyOutcomeHistoryItem>()
            .OrderByDescending(x => x.ObservedAtUnixMs)
            .Take(500)
            .ToListAsync();
        if (strategyRows.Count == 0)
        {
            StrategyLearningSummaryText = ResUI.TbStrategyLearningSummaryEmpty;
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var summaries = strategyRows
            .GroupBy(x => x.StrategyId, StringComparer.Ordinal)
            .Select(group => SqliteStrategyOutcomeStore.Summarize(group.Key, group.ToArray(), now))
            .OrderByDescending(x => x.Confidence)
            .ThenByDescending(x => x.EffectiveSuccessRate)
            .ThenByDescending(x => x.Samples)
            .Take(8)
            .Select(x => $"{x.StrategyId}: {x.EffectiveSuccessRate:P0} ({x.Samples})")
            .ToArray();

        StrategyLearningSummaryText = string.Format(
            CultureInfo.CurrentCulture,
            ResUI.TbStrategyLearningSummaryLine,
            string.Join(" · ", summaries));
    }
}
