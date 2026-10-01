namespace ServiceLib.ViewModels;

public partial class ProfilesViewModel
{
    private readonly ProxyTestHistoryService _proxyTestHistoryService = new();
    private int _historyOperationActive;

    /// <summary>
    /// True while any history refresh/preview/apply/rank operation runs. The four commands share
    /// one gate so a destructive apply can never interleave with a rank or a second preview.
    /// </summary>
    [Reactive]
    public partial bool HistoryBusy { get; set; }

    [Reactive]
    public partial string HistoryInsight { get; set; }

    [Reactive]
    public partial string HistoryPolicyPreview { get; set; }

    public ReactiveCommand<RxVoid, RxVoid> RefreshTestHistoryCmd { get; private set; } = null!;
    public ReactiveCommand<RxVoid, RxVoid> PreviewTestHistoryPolicyCmd { get; private set; } = null!;
    public ReactiveCommand<RxVoid, RxVoid> ApplyTestHistoryPolicyCmd { get; private set; } = null!;
    public ReactiveCommand<RxVoid, RxVoid> RankByTestHistoryCmd { get; private set; } = null!;

    private void InitializeHistoryCommands()
    {
        HistoryInsight = ResUI.TbProxyTestHistoryEmpty;
        HistoryPolicyPreview = ResUI.TbProxyTestHistoryPolicyPreviewEmpty;
        var canRunHistoryOperation = this.WhenAnyValue(x => x.HistoryBusy).Select(busy => !busy);
        RefreshTestHistoryCmd = ReactiveCommand.CreateFromTask(
            () => RunHistoryOperationAsync(RefreshTestHistoryAsync), canRunHistoryOperation);
        PreviewTestHistoryPolicyCmd = ReactiveCommand.CreateFromTask(
            () => RunHistoryOperationAsync(PreviewTestHistoryPolicyAsync), canRunHistoryOperation);
        ApplyTestHistoryPolicyCmd = ReactiveCommand.CreateFromTask(
            () => RunHistoryOperationAsync(ApplyTestHistoryPolicyAsync), canRunHistoryOperation);
        RankByTestHistoryCmd = ReactiveCommand.CreateFromTask(
            () => RunHistoryOperationAsync(RankByTestHistoryAsync), canRunHistoryOperation);

        this.WhenAnyValue(x => x.SelectedProfile)
            .Where(x => x is not null && x.IndexId.IsNotEmpty())
            .SubscribeAsync(async profile => await RefreshSelectedHistoryInsightAsync(profile.IndexId));
    }

    private async Task RunHistoryOperationAsync(Func<Task> operation)
    {
        // CanExecute is observed asynchronously by bindings; the interlocked flag is the authoritative
        // guard against a second invocation slipping in before HistoryBusy propagates.
        if (Interlocked.Exchange(ref _historyOperationActive, 1) != 0)
        {
            return;
        }

        HistoryBusy = true;
        try
        {
            await operation();
        }
        finally
        {
            HistoryBusy = false;
            Volatile.Write(ref _historyOperationActive, 0);
        }
    }

    private List<string> GetHistoryScopeIds()
    {
        return ProfileItems
            .Select(x => x.IndexId)
            .Where(x => x.IsNotEmpty())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private async Task RefreshTestHistoryAsync()
    {
        var selectedId = SelectedProfile?.IndexId;
        if (selectedId.IsNotEmpty())
        {
            await RefreshSelectedHistoryInsightAsync(selectedId);
        }
        else
        {
            HistoryInsight = ResUI.TbProxyTestHistoryEmpty;
        }

        NoticeManager.Instance.Enqueue(ResUI.TbProxyTestHistoryRefreshed);
    }

    private async Task PreviewTestHistoryPolicyAsync()
    {
        await BuildHistoryPolicyPreviewAsync();
    }

    private async Task ApplyTestHistoryPolicyAsync()
    {
        var matches = await BuildHistoryPolicyPreviewAsync();
        if (matches.Count == 0)
        {
            NoticeManager.Instance.Enqueue(HistoryPolicyPreview);
            return;
        }

        var removableCount = matches.Count(x =>
            !string.Equals(x.ProfileIndexId, _config.IndexId, StringComparison.Ordinal));
        if (removableCount == 0)
        {
            NoticeManager.Instance.Enqueue(ResUI.TbProxyTestHistoryPolicyNoMatches);
            return;
        }

        if (!await ShowYesNoInteraction.HandleSafe(ResUI.TbConfirmProxyTestHistoryPolicyApply))
        {
            return;
        }

        // Apply only to the exact profiles that were present in the preview. The service
        // re-evaluates those profiles against current history and still protects the active
        // profile, so concurrent test completion can make the plan more conservative but
        // cannot expand deletion to profiles the user did not preview.
        var result = await _proxyTestHistoryService.ApplyRemovalPolicyAsync(
            _config,
            matches.Select(x => x.ProfileIndexId));

        NoticeManager.Instance.Enqueue(string.Format(
            ResUI.TbProxyTestHistoryPolicyApplied,
            result.RemovedCount,
            result.ProtectedCount));

        AppEvents.ProfilesChangedRequested.Publish();
        HistoryPolicyPreview = ResUI.TbProxyTestHistoryPolicyPreviewEmpty;

        var selectedId = SelectedProfile?.IndexId;
        if (selectedId.IsNotEmpty())
        {
            await RefreshSelectedHistoryInsightAsync(selectedId);
        }
    }

    private async Task<List<ProxyTestHistoryPolicyMatch>> BuildHistoryPolicyPreviewAsync()
    {
        if (!HasValidHistoryPolicy())
        {
            HistoryPolicyPreview = ResUI.TbProxyTestHistoryPolicyNoRules;
            return [];
        }

        var ids = GetHistoryScopeIds();
        if (ids.Count == 0)
        {
            HistoryPolicyPreview = ResUI.TbProxyTestHistoryPolicyNoMatches;
            return [];
        }

        var matches = await _proxyTestHistoryService.EvaluateAsync(ids, _config.SpeedTestItem);
        if (matches.Count == 0)
        {
            HistoryPolicyPreview = ResUI.TbProxyTestHistoryPolicyNoMatches;
            return [];
        }

        HistoryPolicyPreview = string.Join(
            Environment.NewLine,
            matches.Select(match => string.Format(
                ResUI.TbProxyTestHistoryPolicyLine,
                ProfileItems.FirstOrDefault(x =>
                    string.Equals(x.IndexId, match.ProfileIndexId, StringComparison.Ordinal))?.Remarks
                    ?? match.ProfileIndexId,
                match.FailureCount,
                match.SampleCount,
                match.ConsecutiveFailures,
                match.SuccessRate)));

        NoticeManager.Instance.Enqueue(ResUI.TbProxyTestHistoryPolicyPrepared);
        return matches;
    }

    private bool HasValidHistoryPolicy()
    {
        var policy = _config.SpeedTestItem;
        var hasWindowFields = policy.HistoryPolicyFailureCount > 0 || policy.HistoryPolicyWindowCount > 0;
        if (hasWindowFields
            && (policy.HistoryPolicyFailureCount <= 0
                || policy.HistoryPolicyWindowCount <= 0
                || policy.HistoryPolicyFailureCount > policy.HistoryPolicyWindowCount))
        {
            return false;
        }

        return (policy.HistoryPolicyFailureCount > 0 && policy.HistoryPolicyWindowCount > 0)
               || policy.HistoryPolicyConsecutiveFailures > 0;
    }

    private async Task RankByTestHistoryAsync()
    {
        var ids = GetHistoryScopeIds();
        if (ids.Count == 0)
        {
            return;
        }

        var diagnostics = await _proxyTestHistoryService.GetDiagnosticsAsync(
            ids,
            GetHistoryDiagnosticWindow());
        var ranked = ProxyTestHistoryService.OrderDiagnosticsForBestConnection(diagnostics);
        var rankById = ranked
            .Select((item, index) => (item.ProfileIndexId, index))
            .ToDictionary(x => x.ProfileIndexId, x => x.index, StringComparer.Ordinal);

        var selectedId = SelectedProfile?.IndexId;
        var ordered = ProfileItems
            .OrderBy(x => rankById.TryGetValue(x.IndexId, out var rank) ? rank : int.MaxValue)
            .ThenBy(x => x.Sort)
            .ToList();
        ProfileItems.ReplaceRange(ordered);

        if (selectedId.IsNotEmpty())
        {
            SelectedProfile = ordered.FirstOrDefault(x =>
                string.Equals(x.IndexId, selectedId, StringComparison.Ordinal)) ?? ordered[0];
        }

        NoticeManager.Instance.Enqueue(string.Format(
            ResUI.TbProxyTestHistoryRanked,
            ranked.Count,
            ordered.Count));
        await DispatcherRefreshServersBizInteraction.HandleSafe(RxVoid.Default);
    }

    private async Task RefreshSelectedHistoryInsightAsync(string profileIndexId)
    {
        var diagnostics = await _proxyTestHistoryService.GetDiagnosticsAsync(
            [profileIndexId],
            GetHistoryDiagnosticWindow());
        if (!string.Equals(SelectedProfile?.IndexId, profileIndexId, StringComparison.Ordinal))
        {
            return;
        }

        var summary = diagnostics.FirstOrDefault();
        HistoryInsight = summary is null
            ? ResUI.TbProxyTestHistoryEmpty
            : string.Format(
                ResUI.TbProxyTestHistoryDiagnosticLine,
                SelectedProfile?.Remarks ?? profileIndexId,
                summary.SuccessCount,
                summary.SampleCount,
                summary.SuccessRate,
                summary.ConsecutiveFailures,
                summary.AverageDelayMs,
                summary.AverageSpeed,
                DateTimeOffset.FromUnixTimeMilliseconds(summary.LastTestedAtUnixMs).LocalDateTime);
    }

    private int GetHistoryDiagnosticWindow()
    {
        var policy = _config.SpeedTestItem;
        return Math.Clamp(
            Math.Max(20, Math.Max(policy.HistoryPolicyWindowCount, policy.HistoryPolicyConsecutiveFailures)),
            1,
            500);
    }
}
