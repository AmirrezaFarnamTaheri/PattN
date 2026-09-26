using ServiceLib.Discovery.Models;
using ServiceLib.Discovery.Services;
using ServiceLib.Reviver.Models;
using ServiceLib.Reviver.Promotion;
using ServiceLib.Reviver.Services;

namespace ServiceLib.ViewModels;

public partial class DNSSettingViewModel : MyReactiveObject, ICloseable
{
    private readonly DnsHealthDashboardService _dnsHealthDashboard = new();
    private readonly DnsHistoryService _dnsHistory = new();
    private readonly DnsSettingsRepairService _dnsSettingsRepair = new(new SqliteDnsSettingsRepairHistoryStore());
    private DnsSettingsRepairPlan? _dnsRepairPlan;
    private DnsSettingsRepairReceipt? _dnsRepairReceipt;
    private string _dnsRepairCatalogVersion = string.Empty;
    private bool _dnsRepairCatalogEligible;

    public event EventHandler? RequestClose;
    public Interaction<string, bool> ConfirmInteraction { get; } = new();

    [Reactive] public partial bool UseSystemHosts { get; set; }
    [Reactive] public partial bool AddCommonHosts { get; set; }
    [Reactive] public partial bool FakeIP { get; set; }
    [Reactive] public partial bool BlockBindingQuery { get; set; }
    [Reactive] public partial bool BlockAAAAQuery { get; set; }
    [Reactive] public partial string DirectDNS { get; set; }
    [Reactive] public partial string RemoteDNS { get; set; }
    [Reactive] public partial string BootstrapDNS { get; set; }
    [Reactive] public partial string Strategy4Freedom { get; set; }
    [Reactive] public partial string Strategy4Proxy { get; set; }
    [Reactive] public partial string Strategy4ProxyDial { get; set; }
    [Reactive] public partial string Hosts { get; set; }
    [Reactive] public partial string DirectExpectedIPs { get; set; }
    [Reactive] public partial bool ParallelQuery { get; set; }
    [Reactive] public partial bool ServeStale { get; set; }
    [Reactive] public partial bool EnableHappyEyeballs { get; set; }

    [Reactive] public partial bool UseSystemHostsCompatible { get; set; }
    [Reactive] public partial string DomainStrategy4FreedomCompatible { get; set; } = string.Empty;
    [Reactive] public partial string DomainDNSAddressCompatible { get; set; } = string.Empty;
    [Reactive] public partial string NormalDNSCompatible { get; set; } = string.Empty;
    [Reactive] public partial string TunDNSCompatible { get; set; } = string.Empty;

    [Reactive] public partial string DomainStrategy4Freedom2Compatible { get; set; } = string.Empty;
    [Reactive] public partial string DomainDNSAddress2Compatible { get; set; } = string.Empty;
    [Reactive] public partial string NormalDNS2Compatible { get; set; } = string.Empty;
    [Reactive] public partial string TunDNS2Compatible { get; set; } = string.Empty;
    [Reactive] public partial bool RayCustomDNSEnableCompatible { get; set; }
    [Reactive] public partial bool SBCustomDNSEnableCompatible { get; set; }

    [Reactive] public partial string DnsHealthSummary { get; set; } = "No resolver telemetry yet.";
    [Reactive] public partial string DnsCatalogStatus { get; set; } = "Catalog status not loaded.";
    [Reactive] public partial string DnsResolverDetails { get; set; } = string.Empty;
    [Reactive] public partial string DnsHealthLastUpdated { get; set; } = "Never";
    [Reactive] public partial string DnsHealthError { get; set; } = string.Empty;
    [Reactive] public partial bool DnsHealthBusy { get; set; }
    [Reactive] public partial IReadOnlyList<DnsResolverOption> DnsResolverOptions { get; set; } = [];
    [Reactive] public partial DnsResolverOption? SelectedDnsResolver { get; set; }
    [Reactive] public partial string DnsRepairPreview { get; set; } = "Refresh DNS health, choose a resolver, then preview the exact changes.";
    [Reactive] public partial string DnsRepairStatus { get; set; } = string.Empty;
    [Reactive] public partial bool DnsRepairBusy { get; set; }
    [Reactive] public partial bool DnsRepairCanPreview { get; set; }
    [Reactive] public partial bool DnsRepairCanApply { get; set; }
    [Reactive] public partial bool DnsRepairCanRollback { get; set; }
    [Reactive] public partial string DnsResolverTrendDetails { get; set; } = "No resolver history yet. Refresh DNS health to collect observations.";
    [Reactive] public partial string DnsOperationHistory { get; set; } = "No DNS or repair actions yet.";
    [Reactive] public partial string DnsHistoryLastUpdated { get; set; } = "Never";
    [Reactive] public partial string DnsHistoryError { get; set; } = string.Empty;

    public bool IsSimpleDNSEnabled => !(RayCustomDNSEnableCompatible && SBCustomDNSEnableCompatible);

    public ReactiveCommand<RxVoid, RxVoid> SaveCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> ImportDefConfig4V2rayCompatibleCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> ImportDefConfig4SingboxCompatibleCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> RefreshDnsHealthCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> PreviewDnsRepairCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> ApplyDnsRepairCmd { get; }
    public ReactiveCommand<RxVoid, RxVoid> RollbackDnsRepairCmd { get; }

    public DNSSettingViewModel()
    {
        _config = AppManager.Instance.Config;
        SaveCmd = ReactiveCommand.CreateFromTask(SaveSettingAsync);
        RefreshDnsHealthCmd = ReactiveCommand.CreateFromTask(RefreshDnsHealthAsync);
        PreviewDnsRepairCmd = ReactiveCommand.CreateFromTask(
            PreviewDnsRepairAsync,
            this.WhenAnyValue(x => x.DnsRepairCanPreview));
        ApplyDnsRepairCmd = ReactiveCommand.CreateFromTask(
            ApplyDnsRepairAsync,
            this.WhenAnyValue(x => x.DnsRepairCanApply));
        RollbackDnsRepairCmd = ReactiveCommand.CreateFromTask(
            RollbackDnsRepairAsync,
            this.WhenAnyValue(x => x.DnsRepairCanRollback));

        ImportDefConfig4V2rayCompatibleCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            NormalDNSCompatible = EmbedUtils.GetEmbedText(Global.DNSV2rayNormalFileName);
            TunDNSCompatible = EmbedUtils.GetEmbedText(Global.DNSV2rayNormalFileName);
            await Task.CompletedTask;
        });

        ImportDefConfig4SingboxCompatibleCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            NormalDNS2Compatible = EmbedUtils.GetEmbedText(Global.DNSSingboxNormalFileName);
            TunDNS2Compatible = EmbedUtils.GetEmbedText(Global.TunSingboxDNSFileName);
            await Task.CompletedTask;
        });

        this.WhenAnyValue(x => x.RayCustomDNSEnableCompatible, x => x.SBCustomDNSEnableCompatible)
            .Subscribe(_ =>
            {
                this.RaisePropertyChanged(nameof(IsSimpleDNSEnabled));
                UpdateDnsRepairCommandState();
            });

        this.WhenAnyValue(x => x.SelectedDnsResolver)
            .Subscribe(_ =>
            {
                _dnsRepairPlan = null;
                DnsRepairPreview = SelectedDnsResolver is null
                    ? "Select a resolver after a live DNS health refresh."
                    : $"Selected {SelectedDnsResolver.DisplayName}. Preview before applying.";
                DnsRepairStatus = string.Empty;
                UpdateDnsRepairCommandState();
            });

        _ = Init();
    }

    private async Task Init()
    {
        _config = AppManager.Instance.Config;
        var item = _config.SimpleDNSItem;
        UseSystemHosts = item.UseSystemHosts ?? false;
        AddCommonHosts = item.AddCommonHosts ?? false;
        FakeIP = item.FakeIP ?? false;
        BlockBindingQuery = item.BlockBindingQuery ?? false;
        BlockAAAAQuery = item.BlockAAAAQuery ?? false;
        DirectDNS = item.DirectDNS ?? string.Empty;
        RemoteDNS = item.RemoteDNS ?? string.Empty;
        BootstrapDNS = item.BootstrapDNS ?? string.Empty;
        Strategy4Freedom = item.Strategy4Freedom ?? string.Empty;
        Strategy4Proxy = item.Strategy4Proxy ?? string.Empty;
        Strategy4ProxyDial = item.Strategy4ProxyDial ?? string.Empty;
        Hosts = item.Hosts ?? string.Empty;
        DirectExpectedIPs = item.DirectExpectedIPs ?? string.Empty;
        ParallelQuery = item.ParallelQuery ?? false;
        ServeStale = item.ServeStale ?? false;
        EnableHappyEyeballs = item.EnableHappyEyeballs ?? false;
        var item1 = await AppManager.Instance.GetDNSItem(ECoreType.Xray);
        RayCustomDNSEnableCompatible = item1.Enabled;
        UseSystemHostsCompatible = item1.UseSystemHosts;
        DomainStrategy4FreedomCompatible = item1?.DomainStrategy4Freedom ?? string.Empty;
        DomainDNSAddressCompatible = item1?.DomainDNSAddress ?? string.Empty;
        NormalDNSCompatible = item1?.NormalDNS ?? string.Empty;
        TunDNSCompatible = item1?.TunDNS ?? string.Empty;

        var item2 = await AppManager.Instance.GetDNSItem(ECoreType.sing_box);
        SBCustomDNSEnableCompatible = item2.Enabled;
        DomainStrategy4Freedom2Compatible = item2?.DomainStrategy4Freedom ?? string.Empty;
        DomainDNSAddress2Compatible = item2?.DomainDNSAddress ?? string.Empty;
        NormalDNS2Compatible = item2?.NormalDNS ?? string.Empty;
        TunDNS2Compatible = item2?.TunDNS ?? string.Empty;

        try
        {
            ApplyDnsHealthSnapshot(await _dnsHealthDashboard.LoadCachedAsync());
            _dnsRepairReceipt = await _dnsSettingsRepair.GetLatestActiveReceiptAsync();
            if (_dnsRepairReceipt is not null)
            {
                DnsRepairStatus = $"A DNS repair from catalog {_dnsRepairReceipt.CatalogVersion} can be rolled back if the applied settings are still unchanged.";
            }
            UpdateDnsRepairCommandState();
            await RefreshDnsHistoryAsync();
        }
        catch (Exception ex)
        {
            DnsHealthError = ex.Message;
        }
    }

    private async Task RefreshDnsHealthAsync()
    {
        if (DnsHealthBusy)
        {
            return;
        }

        DnsHealthBusy = true;
        DnsHealthError = string.Empty;
        UpdateDnsRepairCommandState();
        try
        {
            ApplyDnsHealthSnapshot(await _dnsHealthDashboard.RefreshAsync());
            await RefreshDnsHistoryAsync();
        }
        catch (Exception ex)
        {
            DnsHealthError = ex.Message;
        }
        finally
        {
            DnsHealthBusy = false;
            UpdateDnsRepairCommandState();
        }
    }

    private void ApplyDnsHealthSnapshot(DnsHealthDashboardSnapshot snapshot)
    {
        var previousResolverId = SelectedDnsResolver?.CatalogId;
        DnsResolverOptions = snapshot.ResolverOptions;
        SelectedDnsResolver = previousResolverId.IsNullOrEmpty()
            ? null
            : DnsResolverOptions.FirstOrDefault(x => x.CatalogId == previousResolverId);
        _dnsRepairCatalogVersion = snapshot.CatalogVersion;
        _dnsRepairCatalogEligible = snapshot.CatalogAuditKnown
            && snapshot.CatalogValid
            && snapshot.CatalogStaleCount == 0;
        DnsHealthSummary = snapshot.Resolvers.Count == 0
            ? "No resolver telemetry yet."
            : $"{snapshot.HealthyCount} healthy · {snapshot.WatchCount} watch · {snapshot.DegradedCount} degraded";

        if (!snapshot.CatalogAuditKnown)
        {
            DnsCatalogStatus = snapshot.CatalogVersion.IsNullOrEmpty()
                ? "Catalog audit not run yet."
                : $"Catalog {snapshot.CatalogVersion} · telemetry cached; catalog freshness not yet audited";
        }
        else
        {
            var freshness = snapshot.CatalogStaleCount == 0 ? "fresh" : $"{snapshot.CatalogStaleCount} stale";
            var validity = snapshot.CatalogValid ? "valid" : "invalid";
            var auditedAt = snapshot.CatalogAuditedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "unknown time";
            DnsCatalogStatus = $"Catalog {snapshot.CatalogVersion} · {validity} · {freshness} (max age {snapshot.CatalogMaxAgeDays} days) · audited {auditedAt}";
        }

        DnsResolverDetails = snapshot.Resolvers.Count == 0
            ? "Use Refresh to run non-gating resolver diagnostics."
            : string.Join(Environment.NewLine, snapshot.Resolvers.Select(x => x.DisplayLine));

        DnsHealthLastUpdated = snapshot.RefreshedAt is null
            ? "Never"
            : snapshot.RefreshedAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

        if (!snapshot.Error.IsNullOrEmpty())
        {
            DnsHealthError = snapshot.Error;
        }
    }

    private async Task RefreshDnsHistoryAsync()
    {
        DnsHistoryError = string.Empty;
        try
        {
            var snapshot = await _dnsHistory.LoadAsync();
            DnsResolverTrendDetails = snapshot.ResolverTrends.Count == 0
                ? "No resolver history yet. Refresh DNS health to collect observations."
                : string.Join(Environment.NewLine, snapshot.ResolverTrends.Select(x => x.DisplayLine));

            DnsOperationHistory = snapshot.Events.Count == 0
                ? "No DNS or repair actions yet."
                : string.Join(Environment.NewLine, snapshot.Events.Select(x => x.DisplayLine));

            DnsHistoryLastUpdated = snapshot.NewestAt is null
                ? "Never"
                : snapshot.NewestAt.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
        }
        catch (Exception ex)
        {
            DnsHistoryError = ex.Message;
        }
    }

    private async Task PreviewDnsRepairAsync()
    {
        var selected = SelectedDnsResolver;
        if (selected is null)
        {
            DnsRepairStatus = "Choose a resolver first.";
            UpdateDnsRepairCommandState();
            return;
        }

        DnsRepairBusy = true;
        DnsRepairStatus = string.Empty;
        try
        {
            var recommendation = new DnsResolverRecommendation
            {
                CatalogId = selected.CatalogId,
                Provider = selected.Provider,
                Name = selected.Name,
                IPv4 = selected.IPv4,
                IPv6 = selected.IPv6,
                DotServerName = selected.DotServerName,
                DotPort = selected.DotPort,
                DohUrl = selected.DohUrl,
                Policy = selected.Policy,
                ReferenceEligible = selected.ReferenceEligible,
            };

            _dnsRepairPlan = _dnsSettingsRepair.Prepare(_config, recommendation, _dnsRepairCatalogVersion);
            var lines = new List<string>
            {
                $"Resolver: {selected.Name}",
                $"Policy: {selected.Policy}",
                $"Current health: {selected.HealthClass} / {selected.Quality}",
                $"Catalog: {_dnsRepairCatalogVersion}",
                $"RemoteDNS: {_dnsRepairPlan.Before.RemoteDNS} -> {_dnsRepairPlan.After.RemoteDNS}",
                $"BootstrapDNS: {_dnsRepairPlan.Before.BootstrapDNS} -> {_dnsRepairPlan.After.BootstrapDNS}",
                $"Changed fields: {string.Join(", ", _dnsRepairPlan.Changes)}",
            };

            if (!_dnsRepairCatalogEligible)
            {
                lines.Add("Apply blocked: resolver catalog audit is missing, invalid, or stale.");
            }
            if (!selected.IsSafeToApply)
            {
                lines.Add("Apply blocked: current resolver telemetry does not meet the live safety gate.");
            }
            if (!IsSimpleDNSEnabled)
            {
                lines.Add("Apply blocked: Simple DNS is currently disabled by custom DNS settings.");
            }

            DnsRepairPreview = string.Join(Environment.NewLine, lines);
            DnsRepairStatus = _dnsRepairCatalogEligible && selected.IsSafeToApply && IsSimpleDNSEnabled
                ? "Preview ready. Apply persists immediately; Cancel does not undo it. Use Rollback if needed."
                : "Preview ready, but Apply is blocked by the current safety gate.";
        }
        catch (Exception ex)
        {
            _dnsRepairPlan = null;
            DnsRepairStatus = ex.Message;
        }
        finally
        {
            DnsRepairBusy = false;
            UpdateDnsRepairCommandState();
        }
    }

    private async Task ApplyDnsRepairAsync()
    {
        if (_dnsRepairPlan is null || SelectedDnsResolver is null)
        {
            DnsRepairStatus = "Preview the current resolver before applying.";
            UpdateDnsRepairCommandState();
            return;
        }
        if (!_dnsRepairCatalogEligible || !SelectedDnsResolver.IsSafeToApply || !IsSimpleDNSEnabled)
        {
            DnsRepairStatus = "Apply is blocked until Simple DNS is enabled and a fresh valid catalog with safe live resolver telemetry is available.";
            UpdateDnsRepairCommandState();
            return;
        }

        if (!await ConfirmInteraction.HandleSafe(ResUI.TbConfirmDNSRepairApply))
        {
            DnsRepairStatus = ResUI.TbDNSRepairApplyCancelled;
            return;
        }

        DnsRepairBusy = true;
        try
        {
            _dnsRepairReceipt = await _dnsSettingsRepair.ApplyAsync(_config, _dnsRepairPlan);
            RemoteDNS = _dnsRepairReceipt.Applied.RemoteDNS ?? string.Empty;
            BootstrapDNS = _dnsRepairReceipt.Applied.BootstrapDNS ?? string.Empty;
            DnsRepairStatus = $"Applied {SelectedDnsResolver.Name}. The previous Simple DNS snapshot is available for rollback.";
            _dnsRepairPlan = null;
            await RefreshDnsHistoryAsync();
        }
        catch (Exception ex)
        {
            DnsRepairStatus = ex.Message;
        }
        finally
        {
            DnsRepairBusy = false;
            UpdateDnsRepairCommandState();
        }
    }

    private async Task RollbackDnsRepairAsync()
    {
        if (_dnsRepairReceipt is null)
        {
            DnsRepairStatus = "There is no applied DNS repair to roll back.";
            UpdateDnsRepairCommandState();
            return;
        }

        if (!await ConfirmInteraction.HandleSafe(ResUI.TbConfirmDNSRepairRollback))
        {
            DnsRepairStatus = ResUI.TbDNSRepairRollbackCancelled;
            return;
        }

        DnsRepairBusy = true;
        try
        {
            await _dnsSettingsRepair.RollbackAsync(_config, _dnsRepairReceipt);
            RemoteDNS = _dnsRepairReceipt.Before.RemoteDNS ?? string.Empty;
            BootstrapDNS = _dnsRepairReceipt.Before.BootstrapDNS ?? string.Empty;
            _dnsRepairReceipt = await _dnsSettingsRepair.GetLatestActiveReceiptAsync();
            DnsRepairStatus = _dnsRepairReceipt is null
                ? "DNS repair rolled back to the exact previous Simple DNS snapshot."
                : "DNS repair rolled back one level. An earlier applied DNS repair is still available for rollback.";
            _dnsRepairPlan = null;
            await RefreshDnsHistoryAsync();
        }
        catch (Exception ex)
        {
            DnsRepairStatus = ex.Message;
        }
        finally
        {
            DnsRepairBusy = false;
            UpdateDnsRepairCommandState();
        }
    }

    private void UpdateDnsRepairCommandState()
    {
        DnsRepairCanPreview = SelectedDnsResolver is not null && !DnsRepairBusy && !DnsHealthBusy;
        DnsRepairCanApply = _dnsRepairPlan is not null
            && SelectedDnsResolver?.IsSafeToApply == true
            && _dnsRepairCatalogEligible
            && IsSimpleDNSEnabled
            && !DnsRepairBusy
            && !DnsHealthBusy;
        DnsRepairCanRollback = _dnsRepairReceipt is not null && !DnsRepairBusy && !DnsHealthBusy;
    }

    internal static bool IsValidXrayDnsText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        // Legacy line-oriented DNS values are still supported. If the user
        // entered JSON-looking content, require a real Xray DNS object with
        // at least one server rather than silently accepting malformed/stub JSON.
        if (!value.Contains('{') && !value.Contains('}'))
        {
            return true;
        }

        var obj = JsonUtils.ParseJson(value);
        return obj?["servers"] is System.Text.Json.Nodes.JsonArray { Count: > 0 };
    }

    internal static bool IsValidSingBoxDnsText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var obj = JsonUtils.Deserialize<Dns4Sbox>(value);
        return obj?.servers is { Count: > 0 } servers
            && servers.All(server => server is not null && server.type.IsNotEmpty());
    }

    private async Task SaveSettingAsync()
    {
        // Validate every editable payload before mutating the live config. A rejected
        // Save must be side-effect-free even in memory.
        if (!IsValidXrayDnsText(NormalDNSCompatible)
            || !IsValidXrayDnsText(TunDNSCompatible)
            || !IsValidSingBoxDnsText(NormalDNS2Compatible)
            || !IsValidSingBoxDnsText(TunDNS2Compatible))
        {
            NoticeManager.Instance.Enqueue(ResUI.FillCorrectDNSText);
            return;
        }

        _config.SimpleDNSItem.UseSystemHosts = UseSystemHosts;
        _config.SimpleDNSItem.AddCommonHosts = AddCommonHosts;
        _config.SimpleDNSItem.FakeIP = FakeIP;
        _config.SimpleDNSItem.BlockBindingQuery = BlockBindingQuery;
        _config.SimpleDNSItem.BlockAAAAQuery = BlockAAAAQuery;
        _config.SimpleDNSItem.DirectDNS = DirectDNS;
        _config.SimpleDNSItem.RemoteDNS = RemoteDNS;
        _config.SimpleDNSItem.BootstrapDNS = BootstrapDNS;
        _config.SimpleDNSItem.Strategy4Freedom = Strategy4Freedom;
        _config.SimpleDNSItem.Strategy4Proxy = Strategy4Proxy;
        _config.SimpleDNSItem.Strategy4ProxyDial = Strategy4ProxyDial;
        _config.SimpleDNSItem.Hosts = Hosts;
        _config.SimpleDNSItem.DirectExpectedIPs = DirectExpectedIPs;
        _config.SimpleDNSItem.ParallelQuery = ParallelQuery;
        _config.SimpleDNSItem.ServeStale = ServeStale;
        _config.SimpleDNSItem.EnableHappyEyeballs = EnableHappyEyeballs;

        var item1 = await AppManager.Instance.GetDNSItem(ECoreType.Xray);
        item1.Enabled = RayCustomDNSEnableCompatible;
        item1.DomainStrategy4Freedom = DomainStrategy4FreedomCompatible;
        item1.DomainDNSAddress = DomainDNSAddressCompatible;
        item1.UseSystemHosts = UseSystemHostsCompatible;
        item1.NormalDNS = NormalDNSCompatible;
        item1.TunDNS = TunDNSCompatible;
        await ConfigHandler.SaveDNSItems(_config, item1);

        var item2 = await AppManager.Instance.GetDNSItem(ECoreType.sing_box);
        item2.Enabled = SBCustomDNSEnableCompatible;
        item2.DomainStrategy4Freedom = DomainStrategy4Freedom2Compatible;
        item2.DomainDNSAddress = DomainDNSAddress2Compatible;
        item2.NormalDNS = JsonUtils.Serialize(JsonUtils.Deserialize<Dns4Sbox>(NormalDNS2Compatible));
        item2.TunDNS = JsonUtils.Serialize(JsonUtils.Deserialize<Dns4Sbox>(TunDNS2Compatible));
        await ConfigHandler.SaveDNSItems(_config, item2);

        await ConfigHandler.SaveConfig(_config);
        RequestClose?.Invoke(this, EventArgs.Empty);
    }
}
