namespace v2rayN.Views;

public partial class DiscoveryManagementWindow
{
    public DiscoveryManagementWindow()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, vm => vm.Catalogs, v => v.lstCatalogs.ItemsSource).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedCatalog, v => v.lstCatalogs.SelectedItem).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.CatalogRevisions, v => v.lstCatalogRevisions.ItemsSource).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedCatalogRevision, v => v.lstCatalogRevisions.SelectedItem).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.CatalogRevisionDetails, v => v.txtCatalogRevisionDetails.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.CatalogDisplayNameEdit, v => v.txtCatalogDisplayName.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.CatalogAuditSummary, v => v.txtCatalogAudit.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.CatalogUpdatePreview, v => v.txtCatalogUpdatePreview.Text).DisposeWith(disposables);

            this.Bind(ViewModel, vm => vm.RemoteCatalogUri, v => v.txtRemoteCatalogUri.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.RemoteSignatureUri, v => v.txtRemoteSignatureUri.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.RemoteSignaturePolicies, v => v.cmbRemoteSignaturePolicy.ItemsSource).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.RemoteSignaturePolicy, v => v.cmbRemoteSignaturePolicy.SelectedItem).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.RemoteTrustedKeyId, v => v.txtRemoteTrustedKeyId.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.RemoteTrustedPublicKeySpkiBase64, v => v.txtRemoteTrustedPublicKey.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.RemoteTlsSpkiPinsText, v => v.txtRemoteTlsSpkiPins.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.RemoteTlsPinStatus, v => v.txtRemoteTlsPinStatus.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.RemoteObservedTlsSpki, v => v.txtRemoteObservedTlsSpki.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.RemoteObservedTlsDetails, v => v.txtRemoteObservedTlsDetails.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.RemoteCatalogSourceSummary, v => v.txtRemoteSourceStatus.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.RemoteCatalogFetchPreview, v => v.txtRemoteFetchPreview.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.RemoteSourceImportPreview, v => v.txtRemoteSourceImportPreview.Text).DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.RemoteSourceHealth.Rows, v => v.lstRemoteSourceHealth.ItemsSource).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedRemoteSourceHealth, v => v.lstRemoteSourceHealth.SelectedItem).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.RemoteSourceHealthSummaryText, v => v.txtRemoteSourceHealthSummary.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.RemoteSourceHealthDetails, v => v.txtRemoteSourceHealthDetails.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.RemoteProvenanceSourceHostFilter, v => v.txtRemoteProvenanceSourceHost.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.RemoteProvenanceTrustedKeyFilter, v => v.txtRemoteProvenanceTrustedKey.Text).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.RemoteProvenanceTlsPinFilter, v => v.txtRemoteProvenanceTlsPin.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.RemoteProvenance.Entries, v => v.lstRemoteProvenance.ItemsSource).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedRemoteProvenance, v => v.lstRemoteProvenance.SelectedItem).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.RemoteProvenanceSummaryText, v => v.txtRemoteProvenanceSummary.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.RemoteProvenanceDetails, v => v.txtRemoteProvenanceDetails.Text).DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.RetiredCatalogs, v => v.lstRetiredCatalogs.ItemsSource).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedRetiredCatalog, v => v.lstRetiredCatalogs.SelectedItem).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.RetiredCatalogRevisions, v => v.lstRetiredCatalogRevisions.ItemsSource).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedRetiredCatalogRevision, v => v.lstRetiredCatalogRevisions.SelectedItem).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.RetiredCatalogDetails, v => v.txtRetiredCatalogDetails.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.ArchiveInspectionSummary, v => v.txtArchiveInspectionSummary.Text).DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.EndpointRows, v => v.lstEndpoints.ItemsSource).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedEndpoint, v => v.lstEndpoints.SelectedItem).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.EndpointLabelEdit, v => v.txtEndpointLabel.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.EndpointHistorySummaryText, v => v.txtEndpointHistory.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.EndpointTimeline, v => v.lstEndpointTimeline.ItemsSource).DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.PromotionHistory.Entries, v => v.lstRepairHistory.ItemsSource).DisposeWith(disposables);
            this.Bind(ViewModel, vm => vm.SelectedPromotionEvent, v => v.lstRepairHistory.SelectedItem).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.PromotionHistorySummaryText, v => v.txtRepairHistorySummary.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.PromotionEventDetails, v => v.txtRepairEventDetails.Text).DisposeWith(disposables);

            this.OneWayBind(ViewModel, vm => vm.EndpointMaintenancePreview, v => v.txtEndpointMaintenancePreview.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.RetentionPreview, v => v.txtRetentionPreview.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.DatabaseCompactionPreview, v => v.txtDatabaseCompactionPreview.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.StatusMessage, v => v.txtStatus.Text).DisposeWith(disposables);
            this.OneWayBind(ViewModel, vm => vm.Busy, v => v.busyProgress.Visibility, busy => busy ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.RefreshAllCmd, v => v.btnRefreshAll).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.CloseCmd, v => v.btnClose).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.RegisterCatalogCmd, v => v.btnRegisterCatalog).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RefreshCatalogCmd, v => v.btnRefreshCatalog).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ToggleCatalogEnabledCmd, v => v.btnToggleCatalog).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.UnregisterCatalogCmd, v => v.btnUnregisterCatalog).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RenameCatalogCmd, v => v.btnRenameCatalog).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.PreviewCatalogUpdateCmd, v => v.btnPreviewCatalogUpdate).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ApplyCatalogUpdateCmd, v => v.btnApplyCatalogUpdate).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RollbackCatalogRevisionCmd, v => v.btnRollbackRevision).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.ConfigureRemoteCatalogSourceCmd, v => v.btnSaveRemoteSource).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RemoveRemoteCatalogSourceCmd, v => v.btnRemoveRemoteSource).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.FetchRemoteCatalogPreviewCmd, v => v.btnFetchRemotePreview).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ApplyRemoteCatalogPreviewCmd, v => v.btnApplyRemotePreview).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.InspectRemoteTlsSpkiCmd, v => v.btnInspectRemoteTlsSpki).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.AddObservedRemoteTlsPinCmd, v => v.btnAddObservedRemoteTlsPin).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ExportRemoteSourceConfigCmd, v => v.btnExportRemoteSourceConfig).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.PreviewRemoteSourceImportCmd, v => v.btnPreviewRemoteSourceImport).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ApplyRemoteSourceImportCmd, v => v.btnApplyRemoteSourceImport).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.RefreshRemoteSourceHealthCmd, v => v.btnRefreshRemoteSourceHealth).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.SearchRemoteProvenanceCmd, v => v.btnSearchRemoteProvenance).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RefreshRetiredCatalogsCmd, v => v.btnRefreshRetiredCatalogs).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ReRegisterRetiredCatalogCmd, v => v.btnReRegisterRetiredCatalog).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.DiscardRetiredCatalogHistoryCmd, v => v.btnDiscardRetiredHistory).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ExportRetiredCatalogArchiveCmd, v => v.btnExportRetiredArchive).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.InspectCatalogArchiveCmd, v => v.btnInspectCatalogArchive).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.RefreshEndpointPoolCmd, v => v.btnRefreshEndpoints).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ToggleEndpointPinnedCmd, v => v.btnToggleEndpointPinned).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ToggleEndpointEnabledCmd, v => v.btnToggleEndpointEnabled).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.RelabelEndpointCmd, v => v.btnRelabelEndpoint).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.RefreshPromotionHistoryCmd, v => v.btnRefreshRepairHistory).DisposeWith(disposables);

            this.BindCommand(ViewModel, vm => vm.PreviewEndpointMaintenanceCmd, v => v.btnPreviewEndpointMaintenance).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ApplyEndpointMaintenanceCmd, v => v.btnApplyEndpointMaintenance).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.PreviewRetentionCmd, v => v.btnPreviewRetention).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ApplyRetentionCmd, v => v.btnApplyRetention).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.PreviewDatabaseCompactionCmd, v => v.btnPreviewDatabaseCompaction).DisposeWith(disposables);
            this.BindCommand(ViewModel, vm => vm.ApplyDatabaseCompactionCmd, v => v.btnApplyDatabaseCompaction).DisposeWith(disposables);

            ViewModel.ConfirmInteraction.RegisterHandler(interaction =>
            {
                interaction.SetOutput(UI.ShowYesNo(interaction.Input) == MessageBoxResult.Yes);
            }).DisposeWith(disposables);

            ViewModel.BrowseCatalogFileInteraction.RegisterHandler(interaction =>
            {
                if (UI.OpenFileDialog(out var fileName, "JSON|*.json|All|*.*") != true)
                {
                    interaction.SetOutput(null);
                    return;
                }
                interaction.SetOutput(fileName);
            }).DisposeWith(disposables);

            ViewModel.BrowseCatalogUpdateFileInteraction.RegisterHandler(interaction =>
            {
                if (UI.OpenFileDialog(out var fileName, "JSON|*.json|All|*.*") != true)
                {
                    interaction.SetOutput(null);
                    return;
                }
                interaction.SetOutput(fileName);
            }).DisposeWith(disposables);

            ViewModel.SaveCatalogArchiveInteraction.RegisterHandler(interaction =>
            {
                if (UI.SaveFileDialog(out var fileName, "JSON|*.json|All|*.*") != true)
                {
                    interaction.SetOutput(null);
                    return;
                }
                interaction.SetOutput(fileName);
            }).DisposeWith(disposables);

            ViewModel.BrowseCatalogArchiveInteraction.RegisterHandler(interaction =>
            {
                if (UI.OpenFileDialog(out var fileName, "JSON|*.json|All|*.*") != true)
                {
                    interaction.SetOutput(null);
                    return;
                }
                interaction.SetOutput(fileName);
            }).DisposeWith(disposables);

            ViewModel.SaveRemoteSourceBundleInteraction.RegisterHandler(interaction =>
            {
                if (UI.SaveFileDialog(out var fileName, "JSON|*.json|All|*.*") != true)
                {
                    interaction.SetOutput(null);
                    return;
                }
                interaction.SetOutput(fileName);
            }).DisposeWith(disposables);

            ViewModel.BrowseRemoteSourceBundleInteraction.RegisterHandler(interaction =>
            {
                if (UI.OpenFileDialog(out var fileName, "JSON|*.json|All|*.*") != true)
                {
                    interaction.SetOutput(null);
                    return;
                }
                interaction.SetOutput(fileName);
            }).DisposeWith(disposables);
        });

        WindowsUtils.SetDarkBorder(this, AppManager.Instance.Config.UiItem.CurrentTheme);
    }
}
