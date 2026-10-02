using ServiceLib.Discovery.Models;
using ServiceLib.Discovery.Services;
using ServiceLib.Reviver.Models;
using ServiceLib.Reviver.Promotion;
using ServiceLib.Reviver.Services;

namespace ServiceLib.ViewModels;

/// <summary>Endpoint, promotion-history, retention, and maintenance command handlers.</summary>
public partial class DiscoveryManagementViewModel
{
    private async Task RefreshEndpointsAsync()
        => await RunBusyAsync(ResUI.TbDiscoveryRefreshingEndpointPool, async () =>
        {
            await RefreshEndpointsCoreAsync();
            StatusMessage = ResUI.TbDiscoveryEndpointPoolRefreshed;
        });

    private async Task ToggleEndpointPinnedAsync()
    {
        if (SelectedEndpoint is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectEndpoint;
            return;
        }

        var id = SelectedEndpoint.Id;
        await RunBusyAsync(SelectedEndpoint.Pinned ? ResUI.TbDiscoveryUnpinningEndpoint : ResUI.TbDiscoveryPinningEndpoint, async () =>
        {
            await _endpointAdmin.SetPinnedAsync(id, !SelectedEndpoint.Pinned);
            await RefreshEndpointsCoreAsync();
            SelectedEndpoint = EndpointRows.FirstOrDefault(x => x.Id == id);
            StatusMessage = SelectedEndpoint?.Pinned == true ? ResUI.TbDiscoveryEndpointPinned : ResUI.TbDiscoveryEndpointUnpinned;
        });
    }

    private async Task ToggleEndpointEnabledAsync()
    {
        if (SelectedEndpoint is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectEndpoint;
            return;
        }

        var id = SelectedEndpoint.Id;
        await RunBusyAsync(SelectedEndpoint.Enabled ? ResUI.TbDiscoveryDisablingEndpoint : ResUI.TbDiscoveryEnablingEndpoint, async () =>
        {
            await _endpointAdmin.SetEnabledAsync(id, !SelectedEndpoint.Enabled);
            await RefreshEndpointsCoreAsync();
            SelectedEndpoint = EndpointRows.FirstOrDefault(x => x.Id == id);
            StatusMessage = SelectedEndpoint?.Enabled == true ? ResUI.TbDiscoveryEndpointEnabled : ResUI.TbDiscoveryEndpointDisabled;
        });
    }

    private async Task RelabelEndpointAsync()
    {
        if (SelectedEndpoint is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectEndpoint;
            return;
        }

        var id = SelectedEndpoint.Id;
        await RunBusyAsync(ResUI.TbDiscoveryUpdatingEndpointLabel, async () =>
        {
            await _endpointAdmin.RelabelAsync(id, EndpointLabelEdit);
            await RefreshEndpointsCoreAsync();
            SelectedEndpoint = EndpointRows.FirstOrDefault(x => x.Id == id);
            StatusMessage = ResUI.TbDiscoveryEndpointLabelUpdated;
        });
    }

    private async Task RefreshPromotionHistoryAsync()
        => await RunBusyAsync(ResUI.TbDiscoveryRefreshingRepairHistory, async () =>
        {
            await RefreshPromotionHistoryCoreAsync();
            StatusMessage = ResUI.TbDiscoveryRepairHistoryRefreshed;
        });

    private async Task RefreshProxyTestHistoryAsync()
        => await RunBusyAsync(ResUI.TbRefreshProxyTestHistory, async () =>
        {
            await RefreshProxyTestHistoryCoreAsync();
            StatusMessage = ResUI.TbProxyTestHistoryRefreshed;
        });

    private async Task PreviewProxyTestHistoryPolicyAsync()
        => await RunBusyAsync(ResUI.TbPreviewProxyTestHistoryPolicy, async () =>
        {
            _pendingProxyTestHistoryRemovalIds = [];
            HasProxyTestHistoryPolicyPreview = false;

            var policy = AppManager.Instance.Config.SpeedTestItem;
            var hasWindowPolicy = policy.HistoryPolicyFailureCount > 0
                                  && policy.HistoryPolicyWindowCount > 0
                                  && policy.HistoryPolicyFailureCount <= policy.HistoryPolicyWindowCount;
            var hasConsecutivePolicy = policy.HistoryPolicyConsecutiveFailures > 0;
            if (!hasWindowPolicy && !hasConsecutivePolicy)
            {
                ProxyTestHistoryPolicyPreview = ResUI.TbProxyTestHistoryPolicyNoRules;
                StatusMessage = ResUI.TbProxyTestHistoryPolicyNoRules;
                return;
            }

            var ids = await _proxyTestHistory.ListKnownProfileIdsAsync();
            var matches = await _proxyTestHistory.EvaluateAsync(ids, policy);
            var activeId = AppManager.Instance.Config.IndexId;
            _pendingProxyTestHistoryRemovalIds = matches
                .Select(x => x.ProfileIndexId)
                .Where(id => id.IsNotEmpty() && !string.Equals(id, activeId, StringComparison.Ordinal))
                .Distinct(StringComparer.Ordinal)
                .ToList();
            HasProxyTestHistoryPolicyPreview = _pendingProxyTestHistoryRemovalIds.Count > 0;

            ProxyTestHistoryPolicyPreview = matches.Count == 0
                ? ResUI.TbProxyTestHistoryPolicyNoMatches
                : string.Join(
                    Environment.NewLine,
                    matches.Select(x => string.Format(
                        ResUI.TbProxyTestHistoryPolicyLine,
                        x.ProfileIndexId,
                        x.FailureCount,
                        x.SampleCount,
                        x.ConsecutiveFailures,
                        x.SuccessRate)));
            StatusMessage = ResUI.TbProxyTestHistoryPolicyPrepared;
        });

    private async Task ApplyProxyTestHistoryPolicyAsync()
    {
        if (Busy)
        {
            return;
        }

        var plannedIds = _pendingProxyTestHistoryRemovalIds.ToArray();
        if (!HasProxyTestHistoryPolicyPreview || plannedIds.Length == 0)
        {
            StatusMessage = ResUI.TbDiscoveryPreviewRequired;
            return;
        }

        if (!await ConfirmInteraction.HandleSafe(ResUI.TbConfirmProxyTestHistoryPolicyApply))
        {
            return;
        }

        await RunBusyAsync(ResUI.TbApplyProxyTestHistoryPolicy, async () =>
        {
            var result = await _proxyTestHistory.ApplyRemovalPolicyAsync(
                AppManager.Instance.Config,
                plannedIds);
            _pendingProxyTestHistoryRemovalIds = [];
            HasProxyTestHistoryPolicyPreview = false;
            ProxyTestHistoryPolicyPreview = string.Format(
                ResUI.TbProxyTestHistoryPolicyApplied,
                result.RemovedCount,
                result.ProtectedCount);
            await RefreshProxyTestHistoryCoreAsync();
            if (result.RemovedCount > 0)
            {
                AppEvents.ProfilesChangedRequested.Publish();
            }
            StatusMessage = ProxyTestHistoryPolicyPreview;
        });
    }

    private async Task PreviewEndpointMaintenanceAsync()
        => await RunBusyAsync(ResUI.TbDiscoveryPreparingEndpointCleanup, async () =>
        {
            _pendingEndpointMaintenance = null;
            HasEndpointMaintenancePreview = false;
            _pendingEndpointMaintenance = await _endpointMaintenance.PreviewAsync();
            HasEndpointMaintenancePreview = true;
            var estimate = await _maintenanceEstimate.EstimateEndpointMaintenanceAsync(_pendingEndpointMaintenance);
            EndpointMaintenancePreview = FormatEndpointMaintenance(_pendingEndpointMaintenance, estimate);
            StatusMessage = ResUI.TbDiscoveryEndpointCleanupPrepared;
        });

    private async Task ApplyEndpointMaintenanceAsync()
    {
        if (Busy)
        {
            return;
        }

        if (_pendingEndpointMaintenance is null)
        {
            StatusMessage = ResUI.TbDiscoveryPreviewRequired;
            return;
        }

        var plan = _pendingEndpointMaintenance;
        if (!await ConfirmInteraction.HandleSafe(ResUI.TbConfirmEndpointCleanup))
        {
            return;
        }
        if (!ReferenceEquals(_pendingEndpointMaintenance, plan))
        {
            StatusMessage = ResUI.TbDiscoveryPreviewChanged;
            return;
        }

        await RunBusyAsync(ResUI.TbDiscoveryApplyingEndpointCleanup, async () =>
        {
            var result = await _endpointMaintenance.ApplyAsync(plan);
            _pendingEndpointMaintenance = null;
            HasEndpointMaintenancePreview = false;
            EndpointMaintenancePreview = string.Format(
                ResUI.TbDiscoveryEndpointCleanupResult,
                result.RemovedPoolEntries,
                result.PlannedPoolEntries,
                result.SkippedChangedPoolEntryIds.Count);
            await RefreshEndpointsCoreAsync();
            StatusMessage = ResUI.TbDiscoveryEndpointCleanupApplied;
        });
    }

    private async Task PreviewRetentionAsync()
        => await RunBusyAsync(ResUI.TbDiscoveryPreparingRetention, async () =>
        {
            _pendingRetention = null;
            HasRetentionPreview = false;
            _pendingRetention = await _retention.PreviewAsync();
            HasRetentionPreview = true;
            var estimate = await _maintenanceEstimate.EstimateLifecycleRetentionAsync(_pendingRetention);
            RetentionPreview = FormatRetention(_pendingRetention, estimate);
            StatusMessage = ResUI.TbDiscoveryRetentionPrepared;
        });

    private async Task ApplyRetentionAsync()
    {
        if (Busy)
        {
            return;
        }

        if (_pendingRetention is null)
        {
            StatusMessage = ResUI.TbDiscoveryPreviewRequired;
            return;
        }

        var plan = _pendingRetention;
        if (!await ConfirmInteraction.HandleSafe(ResUI.TbConfirmLifecycleRetention))
        {
            return;
        }
        if (!ReferenceEquals(_pendingRetention, plan))
        {
            StatusMessage = ResUI.TbDiscoveryPreviewChanged;
            return;
        }

        await RunBusyAsync(ResUI.TbDiscoveryApplyingRetention, async () =>
        {
            var result = await _retention.ApplyAsync(plan);
            _pendingRetention = null;
            HasRetentionPreview = false;
            RetentionPreview = string.Format(
                ResUI.TbDiscoveryRetentionResult,
                result.Deleted,
                result.Planned,
                result.AlreadyMissing);
            await RefreshPromotionHistoryCoreAsync();
            StatusMessage = ResUI.TbDiscoveryRetentionApplied;
        });
    }

    private async Task PreviewDatabaseCompactionAsync()
        => await RunBusyAsync(ResUI.TbDiscoveryPreparingCompaction, async () =>
        {
            _pendingCompaction = null;
            HasCompactionPreview = false;
            _pendingCompaction = await _compaction.PreviewAsync();
            HasCompactionPreview = true;
            DatabaseCompactionPreview = FormatCompactionPlan(_pendingCompaction);
            StatusMessage = ResUI.TbDiscoveryCompactionPrepared;
        });

    private async Task ApplyDatabaseCompactionAsync()
    {
        if (Busy)
        {
            return;
        }
        if (_pendingCompaction is null)
        {
            StatusMessage = ResUI.TbDiscoveryPreviewRequired;
            return;
        }

        var plan = _pendingCompaction;
        if (!await ConfirmInteraction.HandleSafe(ResUI.TbConfirmDatabaseCompaction))
        {
            return;
        }
        if (!ReferenceEquals(_pendingCompaction, plan))
        {
            StatusMessage = ResUI.TbDiscoveryPreviewChanged;
            return;
        }

        await RunBusyAsync(ResUI.TbDiscoveryCompactingDatabase, async () =>
        {
            var result = await _compaction.ApplyAsync(plan);
            _pendingCompaction = null;
            HasCompactionPreview = false;
            DatabaseCompactionPreview = FormatCompactionResult(result);
            StatusMessage = ResUI.TbDiscoveryCompactionCompleted;
        });
    }

}
