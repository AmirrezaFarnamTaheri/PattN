namespace v2rayN.Views;

public partial class DNSSettingWindow
{
    private static Config _config;

    public DNSSettingWindow()
    {
        InitializeComponent();

        _config = AppManager.Instance.Config;

        cmbDirectDNSStrategy.ItemsSource = Global.DomainStrategy;
        cmbRemoteDNSStrategy.ItemsSource = Global.DomainStrategy;
        cmbProxyDialDNSStrategy.ItemsSource = Global.DomainStrategy;
        cmbDirectDNS.ItemsSource = Global.DomainDirectDNSAddress;
        cmbRemoteDNS.ItemsSource = Global.DomainRemoteDNSAddress;
        cmbBootstrapDNS.ItemsSource = Global.DomainPureIPDNSAddress;
        cmbDirectExpectedIPs.ItemsSource = Global.ExpectedIPs;

        cmbdomainStrategy4FreedomCompatible.ItemsSource = Global.DomainStrategy;
        cmbdomainStrategy4OutCompatible.ItemsSource = Global.DomainStrategies4Sbox;
        cmbdomainDNSAddressCompatible.ItemsSource = Global.DomainPureIPDNSAddress;
        cmbdomainDNSAddress2Compatible.ItemsSource = Global.DomainPureIPDNSAddress;

        this.WhenActivated(disposables =>
        {
            this.Bind(ViewModel, vm => vm.UseSystemHosts, v => v.togUseSystemHosts.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.AddCommonHosts, v => v.togAddCommonHosts.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.FakeIP, v => v.togFakeIP.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.BlockBindingQuery, v => v.togBlockBindingQuery.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.BlockAAAAQuery, v => v.togBlockAAAAQuery.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.DirectDNS, v => v.cmbDirectDNS.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.RemoteDNS, v => v.cmbRemoteDNS.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.BootstrapDNS, v => v.cmbBootstrapDNS.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.Strategy4Freedom, v => v.cmbDirectDNSStrategy.SelectedItem).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.Strategy4Proxy, v => v.cmbRemoteDNSStrategy.SelectedItem).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.Strategy4ProxyDial, v => v.cmbProxyDialDNSStrategy.SelectedItem).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.Hosts, v => v.txtHosts.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.DirectExpectedIPs, v => v.cmbDirectExpectedIPs.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.ParallelQuery, v => v.togParallelQuery.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.ServeStale, v => v.togServeStale.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.EnableHappyEyeballs, v => v.togEnableHappyEyeballs.IsChecked).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.SaveCmd, v => v.btnSave).DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.RayCustomDNSEnableCompatible, v => v.togRayCustomDNSEnableCompatible.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SBCustomDNSEnableCompatible, v => v.togSBCustomDNSEnableCompatible.IsChecked).DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.UseSystemHostsCompatible, v => v.togUseSystemHostsCompatible.IsChecked).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.DomainStrategy4FreedomCompatible, v => v.cmbdomainStrategy4FreedomCompatible.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.DomainDNSAddressCompatible, v => v.cmbdomainDNSAddressCompatible.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.NormalDNSCompatible, v => v.txtnormalDNSCompatible.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.TunDNSCompatible, v => v.txttunDNSCompatible.Text).DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.DomainStrategy4Freedom2Compatible, v => v.cmbdomainStrategy4OutCompatible.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.DomainDNSAddress2Compatible, v => v.cmbdomainDNSAddress2Compatible.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.NormalDNS2Compatible, v => v.txtnormalDNS2Compatible.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.TunDNS2Compatible, v => v.txttunDNS2Compatible.Text).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.ImportDefConfig4V2rayCompatibleCmd, v => v.btnImportDefConfig4V2rayCompatible).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ImportDefConfig4SingboxCompatibleCmd, v => v.btnImportDefConfig4SingboxCompatible).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.RefreshDnsHealthCmd, v => v.btnRefreshDnsHealth).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.DnsHealthSummary, v => v.txtDnsHealthSummary.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.DnsCatalogStatus, v => v.txtDnsCatalogStatus.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.DnsHealthLastUpdated, v => v.txtDnsHealthLastUpdated.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.DnsHealthError, v => v.txtDnsHealthError.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.DnsResolverDetails, v => v.txtDnsResolverDetails.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.DnsResolverOptions, v => v.cmbDnsRepairResolver.ItemsSource).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedDnsResolver, v => v.cmbDnsRepairResolver.SelectedItem).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.DnsRepairPreview, v => v.txtDnsRepairPreview.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.DnsRepairStatus, v => v.txtDnsRepairStatus.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.DnsRepairBusy, v => v.dnsRepairProgress.Visibility, busy => busy ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.DnsResolverTrendDetails, v => v.txtDnsResolverTrendDetails.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.DnsOperationHistory, v => v.txtDnsOperationHistory.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.DnsHistoryLastUpdated, v => v.txtDnsHistoryLastUpdated.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.DnsHistoryError, v => v.txtDnsHistoryError.Text).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.PreviewDnsRepairCmd, v => v.btnPreviewDnsRepair).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ApplyDnsRepairCmd, v => v.btnApplyDnsRepair).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RollbackDnsRepairCmd, v => v.btnRollbackDnsRepair).DisposeWith(disposables);

            ViewModel.ConfirmInteraction.RegisterHandler(interaction =>
            {
                interaction.SetOutput(UI.ShowYesNo(interaction.Input) == MessageBoxResult.Yes);
            }).DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel.DnsHealthBusy)
                .Select(busy => !busy)
                .BindTo(this, x => x.btnRefreshDnsHealth.IsEnabled)
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.ViewModel.IsSimpleDNSEnabled)
                .Select(b => b ? Visibility.Collapsed : Visibility.Visible)
                .BindTo(this, x => x.txtBasicDNSSettingsInvalid.Visibility)
                .DisposeWith(disposables);
            this.WhenAnyValue(x => x.ViewModel.IsSimpleDNSEnabled)
                .Select(b => b ? Visibility.Collapsed : Visibility.Visible)
                .BindTo(this, x => x.txtAdvancedDNSSettingsInvalid.Visibility)
                .DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.IsSimpleDNSEnabled, v => v.gridBasicDNSSettings.IsEnabled).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.IsSimpleDNSEnabled, v => v.gridAdvancedDNSSettings.IsEnabled).DisposeWith(disposables);
        });
        WindowsUtils.SetDarkBorder(this, AppManager.Instance.Config.UiItem.CurrentTheme);
    }

    private void linkDnsObjectDoc_Click(object sender, RoutedEventArgs e)
    {
        ProcUtils.ProcessStart("https://xtls.github.io/config/dns.html#dnsobject");
    }

    private void linkDnsSingboxObjectDoc_Click(object sender, RoutedEventArgs e)
    {
        ProcUtils.ProcessStart("https://sing-box.sagernet.org/zh/configuration/dns/");
    }
}
