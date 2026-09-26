using ServiceLib.Discovery.Models;
using ServiceLib.Discovery.Services;
using ServiceLib.Reviver.Models;
using ServiceLib.Reviver.Promotion;
using ServiceLib.Reviver.Services;

namespace ServiceLib.ViewModels;

public partial class DiscoveryManagementViewModel : MyReactiveObject, ICloseable
{
    private readonly SqliteProviderAsnCatalogRegistryStore _catalogStore = new();
    private readonly ProviderAsnCatalogRegistryService _catalogs;
    private readonly ProviderAsnCatalogArchiveService _catalogArchive;
    private readonly ProviderAsnCatalogArchiveInspectionService _catalogArchiveInspection = new();
    private readonly SqliteProviderAsnCatalogRemoteSourceStore _remoteSourceStore = new();
    private readonly SqliteProviderAsnCatalogRemoteSourceRevisionStore _remoteSourceRevisionStore = new();
    private readonly SqliteProviderAsnCatalogRemoteApplyProvenanceStore _remoteProvenanceStore = new();
    private readonly ProviderAsnCatalogRemoteUpdateService _remoteCatalogs;
    private readonly ProviderAsnCatalogRemoteHealthService _remoteHealth;
    private readonly ProviderAsnCatalogRemoteSourcePortabilityService _remoteSourcePortability;
    private readonly ProviderAsnCatalogRemoteProvenanceQueryService _remoteProvenanceQuery = new();
    private readonly ProviderAsnCatalogTlsObservationService _tlsObservation = new();
    private readonly SqliteEndpointPoolStore _endpointStore = new();
    private readonly EndpointPoolManagementService _endpointAdmin;
    private readonly EndpointPoolInspectionService _endpointInspection;
    private readonly EndpointHistoryQueryService _endpointHistoryQuery = new();
    private readonly RepairPromotionHistoryQueryService _promotionHistory = new();
    private readonly EndpointMaintenanceService _endpointMaintenance;
    private readonly LifecycleRetentionService _retention = new();
    private readonly MaintenancePayloadEstimateService _maintenanceEstimate = new();
    private readonly SQLiteCompactionService _compaction = new();

    private ProviderAsnCatalogUpdatePlan? _pendingCatalogUpdate;
    private ProviderAsnCatalogRemoteFetchPreview? _pendingRemoteCatalogFetch;
    private ProviderAsnCatalogRemoteSourceImportPreview? _pendingRemoteSourceImport;
    private ProviderAsnCatalogTlsObservation? _observedRemoteTls;
    private IReadOnlyList<string> _savedRemoteTlsSpkiPins = [];
    private EndpointMaintenancePlan? _pendingEndpointMaintenance;
    private LifecycleRetentionPlan? _pendingRetention;
    private SQLiteCompactionPlan? _pendingCompaction;

    public event EventHandler? RequestClose;

    public Interaction<RxVoid, string?> BrowseCatalogFileInteraction { get; } = new();
    public Interaction<RxVoid, string?> BrowseCatalogUpdateFileInteraction { get; } = new();
    public Interaction<RxVoid, string?> SaveCatalogArchiveInteraction { get; } = new();
    public Interaction<RxVoid, string?> BrowseCatalogArchiveInteraction { get; } = new();
    public Interaction<RxVoid, string?> SaveRemoteSourceBundleInteraction { get; } = new();
    public Interaction<RxVoid, string?> BrowseRemoteSourceBundleInteraction { get; } = new();
    public Interaction<string, bool> ConfirmInteraction { get; } = new();

    [Reactive] public partial bool Busy { get; set; }
    [Reactive] public partial bool HasCatalogUpdatePreview { get; set; }
    [Reactive] public partial bool HasRemoteCatalogPreview { get; set; }
    [Reactive] public partial bool HasRemoteSourceImportPreview { get; set; }
    [Reactive] public partial bool HasEndpointMaintenancePreview { get; set; }
    [Reactive] public partial bool HasRetentionPreview { get; set; }
    [Reactive] public partial bool HasCompactionPreview { get; set; }
    [Reactive] public partial string StatusMessage { get; set; } = ResUI.TbDiscoveryReady;

    [Reactive] public partial IReadOnlyList<ProviderAsnCatalogRegistryView> Catalogs { get; set; } = [];
    [Reactive] public partial ProviderAsnCatalogRegistryView? SelectedCatalog { get; set; }
    [Reactive] public partial IReadOnlyList<ProviderAsnCatalogRevisionView> CatalogRevisions { get; set; } = [];
    [Reactive] public partial ProviderAsnCatalogRevisionView? SelectedCatalogRevision { get; set; }
    [Reactive] public partial string CatalogRevisionDetails { get; set; } = "Select a revision to view its details.";
    [Reactive] public partial string CatalogDisplayNameEdit { get; set; } = string.Empty;
    [Reactive] public partial string CatalogAuditSummary { get; set; } = "Select a catalog to view its audit details.";
    [Reactive] public partial string CatalogUpdatePreview { get; set; } = "Preview an update to review changes before applying.";

    public IReadOnlyList<ProviderAsnCatalogSignaturePolicy> RemoteSignaturePolicies { get; } =
        Enum.GetValues<ProviderAsnCatalogSignaturePolicy>();
    [Reactive] public partial string RemoteCatalogUri { get; set; } = string.Empty;
    [Reactive] public partial string RemoteSignatureUri { get; set; } = string.Empty;
    [Reactive] public partial ProviderAsnCatalogSignaturePolicy RemoteSignaturePolicy { get; set; }
    [Reactive] public partial string RemoteTrustedKeyId { get; set; } = string.Empty;
    [Reactive] public partial string RemoteTrustedPublicKeySpkiBase64 { get; set; } = string.Empty;
    [Reactive] public partial string RemoteTlsSpkiPinsText { get; set; } = string.Empty;
    [Reactive] public partial string RemoteTlsPinStatus { get; set; } = ResUI.TbDiscoveryNoExtraTlsPins;
    [Reactive] public partial string RemoteObservedTlsSpki { get; set; } = string.Empty;
    [Reactive] public partial string RemoteObservedTlsDetails { get; set; } = ResUI.TbDiscoveryInspectSpki;
    [Reactive] public partial string RemoteCatalogSourceSummary { get; set; } = ResUI.TbDiscoveryNoRemoteSource;
    [Reactive] public partial string RemoteCatalogFetchPreview { get; set; } = ResUI.TbDiscoveryFetchPreview;
    [Reactive] public partial string RemoteSourceImportPreview { get; set; } = ResUI.TbDiscoveryImportPreview;

    [Reactive] public partial ProviderAsnCatalogRemoteHealthSummary RemoteSourceHealth { get; set; } = new();
    [Reactive] public partial ProviderAsnCatalogRemoteHealthRow? SelectedRemoteSourceHealth { get; set; }
    [Reactive] public partial string RemoteSourceHealthSummaryText { get; set; } = "Refresh to load saved source health.";
    [Reactive] public partial string RemoteSourceHealthDetails { get; set; } = "Select a source to view its saved health details.";
    [Reactive] public partial string RemoteProvenanceSourceHostFilter { get; set; } = string.Empty;
    [Reactive] public partial string RemoteProvenanceTrustedKeyFilter { get; set; } = string.Empty;
    [Reactive] public partial string RemoteProvenanceTlsPinFilter { get; set; } = string.Empty;
    [Reactive] public partial ProviderAsnCatalogRemoteProvenanceSummary RemoteProvenance { get; set; } = new();
    [Reactive] public partial ProviderAsnCatalogRemoteApplyProvenanceView? SelectedRemoteProvenance { get; set; }
    [Reactive] public partial string RemoteProvenanceSummaryText { get; set; } = "Search to view saved evidence from previous remote updates.";
    [Reactive] public partial string RemoteProvenanceDetails { get; set; } = "Select an update to view its saved provenance.";

    [Reactive] public partial IReadOnlyList<ProviderAsnCatalogRegistryView> RetiredCatalogs { get; set; } = [];
    [Reactive] public partial ProviderAsnCatalogRegistryView? SelectedRetiredCatalog { get; set; }
    [Reactive] public partial IReadOnlyList<ProviderAsnCatalogRevisionView> RetiredCatalogRevisions { get; set; } = [];
    [Reactive] public partial ProviderAsnCatalogRevisionView? SelectedRetiredCatalogRevision { get; set; }
    [Reactive] public partial string RetiredCatalogDetails { get; set; } = "Select a retired catalog to view its preserved history.";
    [Reactive] public partial string ArchiveInspectionSummary { get; set; } = "Choose an archive to inspect it without importing it.";

    [Reactive] public partial IReadOnlyList<EndpointPoolInspectionRow> EndpointRows { get; set; } = [];
    [Reactive] public partial EndpointPoolInspectionRow? SelectedEndpoint { get; set; }
    [Reactive] public partial string EndpointLabelEdit { get; set; } = string.Empty;
    [Reactive] public partial string EndpointHistorySummaryText { get; set; } = "Select an endpoint to view its history.";
    [Reactive] public partial IReadOnlyList<EndpointHistoryPoint> EndpointTimeline { get; set; } = [];

    [Reactive] public partial RepairPromotionHistorySummary PromotionHistory { get; set; } = new();
    [Reactive] public partial string PromotionHistorySummaryText { get; set; } = "Refresh to load repair history.";
    [Reactive] public partial RepairPromotionHistoryEntry? SelectedPromotionEvent { get; set; }
    [Reactive] public partial string PromotionEventDetails { get; set; } = "Select an event to view its details.";

    [Reactive] public partial string EndpointMaintenancePreview { get; set; } = ResUI.TbDiscoveryEndpointCleanupPreview;
    [Reactive] public partial string RetentionPreview { get; set; } = ResUI.TbDiscoveryRetentionPreview;
    [Reactive] public partial string DatabaseCompactionPreview { get; set; } = ResUI.TbDiscoveryCompactionPreview;

    public ReactiveCommand<RxVoid, RxVoid> RefreshAllCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> RegisterCatalogCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> RefreshCatalogCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> ToggleCatalogEnabledCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> UnregisterCatalogCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> RenameCatalogCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> PreviewCatalogUpdateCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> ApplyCatalogUpdateCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> RollbackCatalogRevisionCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> ConfigureRemoteCatalogSourceCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> RemoveRemoteCatalogSourceCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> FetchRemoteCatalogPreviewCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> ApplyRemoteCatalogPreviewCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> InspectRemoteTlsSpkiCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> AddObservedRemoteTlsPinCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> ExportRemoteSourceConfigCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> PreviewRemoteSourceImportCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> ApplyRemoteSourceImportCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> RefreshRemoteSourceHealthCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> SearchRemoteProvenanceCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> RefreshRetiredCatalogsCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> ReRegisterRetiredCatalogCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> DiscardRetiredCatalogHistoryCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> ExportRetiredCatalogArchiveCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> InspectCatalogArchiveCmd { get; }

    public ReactiveCommand<RxVoid, RxVoid> RefreshEndpointPoolCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> ToggleEndpointPinnedCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> ToggleEndpointEnabledCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> RelabelEndpointCmd { get; }

    public ReactiveCommand<RxVoid, RxVoid> RefreshPromotionHistoryCmd { get; }

    public ReactiveCommand<RxVoid, RxVoid> PreviewEndpointMaintenanceCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> ApplyEndpointMaintenanceCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> PreviewRetentionCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> ApplyRetentionCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> PreviewDatabaseCompactionCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> ApplyDatabaseCompactionCmd { get; }

    public ReactiveCommand<RxVoid, RxVoid> CloseCmd { get; }

    public DiscoveryManagementViewModel()
    {
        _catalogs = new ProviderAsnCatalogRegistryService(_catalogStore);
        _catalogArchive = new ProviderAsnCatalogArchiveService(_catalogs, _remoteProvenanceStore);
        _remoteCatalogs = new ProviderAsnCatalogRemoteUpdateService(
            _catalogs,
            _remoteSourceStore,
            provenanceStore: _remoteProvenanceStore,
            sourceRevisionStore: _remoteSourceRevisionStore);
        _remoteHealth = new ProviderAsnCatalogRemoteHealthService(_catalogs, _remoteSourceStore);
        _remoteSourcePortability = new ProviderAsnCatalogRemoteSourcePortabilityService(
            _catalogs,
            _remoteSourceStore,
            _remoteCatalogs);
        _endpointAdmin = new EndpointPoolManagementService(_endpointStore, _endpointStore);
        _endpointInspection = new EndpointPoolInspectionService(_endpointStore, new EndpointHistoryQueryService());
        _endpointMaintenance = new EndpointMaintenanceService(
            _endpointStore,
            _endpointStore);

        var canRun = this.WhenAnyValue(x => x.Busy, busy => !busy);
        var canUseCatalog = this.WhenAnyValue(
            x => x.SelectedCatalog,
            x => x.Busy,
            (selected, busy) => selected is not null && !busy);
        var canUseCatalogRevision = this.WhenAnyValue(
            x => x.SelectedCatalogRevision,
            x => x.Busy,
            (selected, busy) => selected is not null && !busy);
        var canUseRetiredCatalog = this.WhenAnyValue(
            x => x.SelectedRetiredCatalog,
            x => x.Busy,
            (selected, busy) => selected is not null && !busy);
        var canUseEndpoint = this.WhenAnyValue(
            x => x.SelectedEndpoint,
            x => x.Busy,
            (selected, busy) => selected is not null && !busy);
        var canApplyCatalogUpdate = this.WhenAnyValue(
            x => x.SelectedCatalog, x => x.HasCatalogUpdatePreview, x => x.Busy,
            (selected, hasPreview, busy) => selected is not null && hasPreview && !busy);
        var canApplyRemoteCatalog = this.WhenAnyValue(
            x => x.SelectedCatalog, x => x.HasRemoteCatalogPreview, x => x.Busy,
            (selected, hasPreview, busy) => selected is not null && hasPreview && !busy);
        var canApplyRemoteImport = this.WhenAnyValue(
            x => x.SelectedCatalog, x => x.HasRemoteSourceImportPreview, x => x.Busy,
            (selected, hasPreview, busy) => selected is not null && hasPreview && !busy);
        var canApplyEndpointMaintenance = this.WhenAnyValue(
            x => x.HasEndpointMaintenancePreview, x => x.Busy,
            (hasPreview, busy) => hasPreview && !busy);
        var canApplyRetention = this.WhenAnyValue(
            x => x.HasRetentionPreview, x => x.Busy,
            (hasPreview, busy) => hasPreview && !busy);
        var canApplyCompaction = this.WhenAnyValue(
            x => x.HasCompactionPreview, x => x.Busy,
            (hasPreview, busy) => hasPreview && !busy);

        RefreshAllCmd = ReactiveCommand.CreateFromTask(RefreshAllAsync, canRun);
        RegisterCatalogCmd = ReactiveCommand.CreateFromTask(RegisterCatalogAsync, canRun);
        RefreshCatalogCmd = ReactiveCommand.CreateFromTask(RefreshSelectedCatalogAsync, canUseCatalog);
        ToggleCatalogEnabledCmd = ReactiveCommand.CreateFromTask(ToggleCatalogEnabledAsync, canUseCatalog);
        UnregisterCatalogCmd = ReactiveCommand.CreateFromTask(UnregisterCatalogAsync, canUseCatalog);
        RenameCatalogCmd = ReactiveCommand.CreateFromTask(RenameCatalogAsync, canUseCatalog);
        PreviewCatalogUpdateCmd = ReactiveCommand.CreateFromTask(PreviewCatalogUpdateAsync, canUseCatalog);
        ApplyCatalogUpdateCmd = ReactiveCommand.CreateFromTask(ApplyCatalogUpdateAsync, canApplyCatalogUpdate);
        RollbackCatalogRevisionCmd = ReactiveCommand.CreateFromTask(RollbackCatalogRevisionAsync, canUseCatalogRevision);
        ConfigureRemoteCatalogSourceCmd = ReactiveCommand.CreateFromTask(ConfigureRemoteCatalogSourceAsync, canUseCatalog);
        RemoveRemoteCatalogSourceCmd = ReactiveCommand.CreateFromTask(RemoveRemoteCatalogSourceAsync, canUseCatalog);
        FetchRemoteCatalogPreviewCmd = ReactiveCommand.CreateFromTask(FetchRemoteCatalogPreviewAsync, canUseCatalog);
        ApplyRemoteCatalogPreviewCmd = ReactiveCommand.CreateFromTask(ApplyRemoteCatalogPreviewAsync, canApplyRemoteCatalog);
        InspectRemoteTlsSpkiCmd = ReactiveCommand.CreateFromTask(InspectRemoteTlsSpkiAsync, canUseCatalog);
        AddObservedRemoteTlsPinCmd = ReactiveCommand.CreateFromTask(AddObservedRemoteTlsPinAsync, canUseCatalog);
        ExportRemoteSourceConfigCmd = ReactiveCommand.CreateFromTask(ExportRemoteSourceConfigAsync, canUseCatalog);
        PreviewRemoteSourceImportCmd = ReactiveCommand.CreateFromTask(PreviewRemoteSourceImportAsync, canUseCatalog);
        ApplyRemoteSourceImportCmd = ReactiveCommand.CreateFromTask(ApplyRemoteSourceImportAsync, canApplyRemoteImport);
        RefreshRemoteSourceHealthCmd = ReactiveCommand.CreateFromTask(RefreshRemoteSourceHealthAsync, canRun);
        SearchRemoteProvenanceCmd = ReactiveCommand.CreateFromTask(SearchRemoteProvenanceAsync, canRun);
        RefreshRetiredCatalogsCmd = ReactiveCommand.CreateFromTask(RefreshRetiredCatalogsAsync, canRun);
        ReRegisterRetiredCatalogCmd = ReactiveCommand.CreateFromTask(ReRegisterRetiredCatalogAsync, canUseRetiredCatalog);
        DiscardRetiredCatalogHistoryCmd = ReactiveCommand.CreateFromTask(DiscardRetiredCatalogHistoryAsync, canUseRetiredCatalog);
        ExportRetiredCatalogArchiveCmd = ReactiveCommand.CreateFromTask(ExportRetiredCatalogArchiveAsync, canUseRetiredCatalog);
        InspectCatalogArchiveCmd = ReactiveCommand.CreateFromTask(InspectCatalogArchiveAsync, canRun);

        RefreshEndpointPoolCmd = ReactiveCommand.CreateFromTask(RefreshEndpointsAsync, canRun);
        ToggleEndpointPinnedCmd = ReactiveCommand.CreateFromTask(ToggleEndpointPinnedAsync, canUseEndpoint);
        ToggleEndpointEnabledCmd = ReactiveCommand.CreateFromTask(ToggleEndpointEnabledAsync, canUseEndpoint);
        RelabelEndpointCmd = ReactiveCommand.CreateFromTask(RelabelEndpointAsync, canUseEndpoint);

        RefreshPromotionHistoryCmd = ReactiveCommand.CreateFromTask(RefreshPromotionHistoryAsync, canRun);

        PreviewEndpointMaintenanceCmd = ReactiveCommand.CreateFromTask(PreviewEndpointMaintenanceAsync, canRun);
        ApplyEndpointMaintenanceCmd = ReactiveCommand.CreateFromTask(ApplyEndpointMaintenanceAsync, canApplyEndpointMaintenance);
        PreviewRetentionCmd = ReactiveCommand.CreateFromTask(PreviewRetentionAsync, canRun);
        ApplyRetentionCmd = ReactiveCommand.CreateFromTask(ApplyRetentionAsync, canApplyRetention);
        PreviewDatabaseCompactionCmd = ReactiveCommand.CreateFromTask(PreviewDatabaseCompactionAsync, canRun);
        ApplyDatabaseCompactionCmd = ReactiveCommand.CreateFromTask(ApplyDatabaseCompactionAsync, canApplyCompaction);

        CloseCmd = ReactiveCommand.Create(() => RequestClose?.Invoke(this, EventArgs.Empty));

        this.WhenAnyValue(x => x.SelectedCatalog)
            .Subscribe(catalog =>
            {
                CatalogDisplayNameEdit = catalog?.DisplayName ?? string.Empty;
                CatalogAuditSummary = FormatCatalogAudit(catalog);
                _pendingCatalogUpdate = null;
                HasCatalogUpdatePreview = false;
                _pendingRemoteCatalogFetch = null;
                HasRemoteCatalogPreview = false;
                _pendingRemoteSourceImport = null;
                HasRemoteSourceImportPreview = false;
                _observedRemoteTls = null;
                CatalogUpdatePreview = ResUI.TbUpdatePreview;
                RemoteCatalogFetchPreview = ResUI.TbDiscoveryFetchPreview;
                RemoteSourceImportPreview = ResUI.TbDiscoveryImportPreview;
                RemoteObservedTlsSpki = string.Empty;
                RemoteObservedTlsDetails = ResUI.TbDiscoveryInspectSpki;
                _ = RefreshCatalogRevisionsAsync();
                _ = RefreshRemoteCatalogSourceAsync();
            });

        this.WhenAnyValue(x => x.SelectedCatalogRevision)
            .Subscribe(revision => _ = RefreshSelectedCatalogRevisionDetailsAsync(revision));

        this.WhenAnyValue(x => x.SelectedRemoteSourceHealth)
            .Subscribe(item => RemoteSourceHealthDetails = FormatRemoteSourceHealth(item));

        this.WhenAnyValue(x => x.SelectedRemoteProvenance)
            .Subscribe(item => RemoteProvenanceDetails = FormatRemoteProvenance(item));

        this.WhenAnyValue(x => x.RemoteTlsSpkiPinsText)
            .Subscribe(_ => UpdateRemoteTlsPinStatus());

        this.WhenAnyValue(x => x.SelectedRetiredCatalog)
            .Subscribe(catalog =>
            {
                RetiredCatalogDetails = FormatRetiredCatalog(catalog);
                _ = RefreshRetiredCatalogRevisionsAsync();
            });

        this.WhenAnyValue(x => x.SelectedEndpoint)
            .Subscribe(endpoint =>
            {
                EndpointLabelEdit = endpoint?.Label ?? string.Empty;
                EndpointHistorySummaryText = FormatEndpointHistory(endpoint);
                _ = RefreshSelectedEndpointHistoryAsync(endpoint);
            });

        this.WhenAnyValue(x => x.SelectedPromotionEvent)
            .Subscribe(item => PromotionEventDetails = FormatPromotionEvent(item));

        _ = RefreshAllAsync();
    }

    private async Task RefreshAllAsync()
        => await RunBusyAsync("Refreshing discovery management data...", async () =>
        {
            await RefreshCatalogsCoreAsync();
            await RefreshRemoteSourceHealthCoreAsync();
            await SearchRemoteProvenanceCoreAsync();
            await RefreshRetiredCatalogsCoreAsync();
            await RefreshEndpointsCoreAsync();
            await RefreshPromotionHistoryCoreAsync();
            StatusMessage = "Discovery management data refreshed.";
        });

    private async Task RefreshCatalogsCoreAsync()
    {
        var selectedId = SelectedCatalog?.Id;
        Catalogs = await _catalogs.ListAsync(new ProviderAsnCatalogRegistryQuery
        {
            IncludeDisabled = true,
            MaxItems = 500,
        });
        SelectedCatalog = selectedId.IsNullOrEmpty()
            ? Catalogs.FirstOrDefault()
            : Catalogs.FirstOrDefault(x => x.Id == selectedId) ?? Catalogs.FirstOrDefault();
    }

    private async Task RefreshCatalogRevisionsAsync()
    {
        if (SelectedCatalog is null)
        {
            CatalogRevisions = [];
            SelectedCatalogRevision = null;
            return;
        }

        var selectedCatalogId = SelectedCatalog.Id;
        var selectedRevisionId = SelectedCatalogRevision?.Id;
        try
        {
            var revisions = await _catalogs.ListRevisionsAsync(new ProviderAsnCatalogRevisionQuery
            {
                RegistryId = selectedCatalogId,
                IncludeRolledBack = true,
                MaxItems = 200,
            });
            if (!string.Equals(SelectedCatalog?.Id, selectedCatalogId, StringComparison.Ordinal))
            {
                return;
            }

            CatalogRevisions = revisions;
            SelectedCatalogRevision = selectedRevisionId.IsNullOrEmpty()
                ? CatalogRevisions.FirstOrDefault()
                : CatalogRevisions.FirstOrDefault(x => x.Id == selectedRevisionId) ?? CatalogRevisions.FirstOrDefault();
        }
        catch (Exception ex)
        {
            if (string.Equals(SelectedCatalog?.Id, selectedCatalogId, StringComparison.Ordinal))
            {
                CatalogRevisions = [];
                SelectedCatalogRevision = null;
                CatalogRevisionDetails = $"Revision history unavailable: {ex.Message}";
            }
            Logging.SaveLog($"Catalog revision refresh failed: {ex}");
        }
    }

    private async Task RefreshRetiredCatalogsCoreAsync()
    {
        var selectedId = SelectedRetiredCatalog?.Id;
        var all = await _catalogs.ListAsync(new ProviderAsnCatalogRegistryQuery
        {
            IncludeDisabled = true,
            IncludeUnregistered = true,
            MaxItems = 500,
        });
        RetiredCatalogs = all
            .Where(x => !x.Registered)
            .OrderByDescending(x => x.UnregisteredAt)
            .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        SelectedRetiredCatalog = selectedId.IsNullOrEmpty()
            ? RetiredCatalogs.FirstOrDefault()
            : RetiredCatalogs.FirstOrDefault(x => x.Id == selectedId) ?? RetiredCatalogs.FirstOrDefault();
    }

    private async Task RefreshRetiredCatalogRevisionsAsync()
    {
        if (SelectedRetiredCatalog is null)
        {
            RetiredCatalogRevisions = [];
            SelectedRetiredCatalogRevision = null;
            return;
        }

        var selectedCatalogId = SelectedRetiredCatalog.Id;
        var selectedRevisionId = SelectedRetiredCatalogRevision?.Id;
        try
        {
            var revisions = await _catalogs.ListRevisionsAsync(new ProviderAsnCatalogRevisionQuery
            {
                RegistryId = selectedCatalogId,
                IncludeRolledBack = true,
                MaxItems = 500,
            });
            if (!string.Equals(SelectedRetiredCatalog?.Id, selectedCatalogId, StringComparison.Ordinal))
            {
                return;
            }

            RetiredCatalogRevisions = revisions;
            SelectedRetiredCatalogRevision = selectedRevisionId.IsNullOrEmpty()
                ? RetiredCatalogRevisions.FirstOrDefault()
                : RetiredCatalogRevisions.FirstOrDefault(x => x.Id == selectedRevisionId)
                  ?? RetiredCatalogRevisions.FirstOrDefault();
            RetiredCatalogDetails = FormatRetiredCatalog(SelectedRetiredCatalog, RetiredCatalogRevisions.Count);
        }
        catch (Exception ex)
        {
            if (string.Equals(SelectedRetiredCatalog?.Id, selectedCatalogId, StringComparison.Ordinal))
            {
                RetiredCatalogRevisions = [];
                SelectedRetiredCatalogRevision = null;
                RetiredCatalogDetails = FormatRetiredCatalog(SelectedRetiredCatalog, 0)
                    + Environment.NewLine
                    + $"Revision history unavailable: {ex.Message}";
            }
            Logging.SaveLog($"Retired catalog revision refresh failed: {ex}");
        }
    }

    private async Task RefreshSelectedEndpointHistoryAsync(EndpointPoolInspectionRow? endpoint)
    {
        if (endpoint is null)
        {
            EndpointTimeline = [];
            return;
        }

        try
        {
            var endpointId = endpoint.Id;
            var detail = await _endpointHistoryQuery.GetAsync(
                new DiscoveryCandidateRequest
                {
                    OriginalAddress = endpoint.LogicalHost,
                    OriginalPort = endpoint.Port,
                    LogicalHost = endpoint.LogicalHost,
                    HttpHost = endpoint.HttpHost.NullIfEmpty() ?? endpoint.LogicalHost,
                    Network = endpoint.Network,
                    StreamSecurity = endpoint.StreamSecurity,
                },
                endpoint.Address,
                TimeSpan.FromDays(30),
                maxPoints: 50);
            if (string.Equals(SelectedEndpoint?.Id, endpointId, StringComparison.Ordinal))
            {
                EndpointTimeline = detail.Points;
            }
        }
        catch (Exception ex)
        {
            if (string.Equals(SelectedEndpoint?.Id, endpoint.Id, StringComparison.Ordinal))
            {
                EndpointTimeline = [];
            }
            Logging.SaveLog($"Endpoint timeline refresh failed: {ex}");
        }
    }

    private async Task RefreshEndpointsCoreAsync()
    {
        var selectedId = SelectedEndpoint?.Id;
        EndpointRows = await _endpointInspection.InspectAsync(new EndpointPoolInspectionQuery
        {
            IncludeDisabled = true,
            MaxItems = 200,
            HistoryAge = TimeSpan.FromDays(30),
            HistoryPoints = 12,
        });
        SelectedEndpoint = selectedId.IsNullOrEmpty()
            ? EndpointRows.FirstOrDefault()
            : EndpointRows.FirstOrDefault(x => x.Id == selectedId) ?? EndpointRows.FirstOrDefault();
    }

    private async Task RefreshPromotionHistoryCoreAsync()
    {
        var selectedId = SelectedPromotionEvent?.Id;
        PromotionHistory = await _promotionHistory.QueryAsync(new RepairPromotionHistoryQuery
        {
            MaxAge = TimeSpan.FromDays(365),
            MaxItems = 250,
        });
        PromotionHistorySummaryText =
            $"{PromotionHistory.TotalEvents} events · {PromotionHistory.Promotions} promotions · " +
            $"{PromotionHistory.Rollbacks} rollbacks · {PromotionHistory.Improved} improved · " +
            $"{PromotionHistory.Stable} stable · {PromotionHistory.Regressed} regressed";
        SelectedPromotionEvent = selectedId.IsNullOrEmpty()
            ? PromotionHistory.Entries.FirstOrDefault()
            : PromotionHistory.Entries.FirstOrDefault(x => x.Id == selectedId)
              ?? PromotionHistory.Entries.FirstOrDefault();
    }

    private async Task RunBusyAsync(string operation, Func<Task> action)
    {
        if (Busy)
        {
            return;
        }

        Busy = true;
        StatusMessage = ResUI.TbDiscoveryWorking;
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            StatusMessage = ResUI.TbDiscoveryOperationFailed;
            Logging.SaveLog($"Discovery management operation '{operation}' failed: {ex}");
        }
        finally
        {
            Busy = false;
        }
    }

}
