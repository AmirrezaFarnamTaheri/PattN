using ServiceLib.Discovery.Models;
using ServiceLib.Discovery.Services;
using ServiceLib.Reviver.Models;
using ServiceLib.Reviver.Promotion;
using ServiceLib.Reviver.Services;

namespace ServiceLib.ViewModels;

/// <summary>Catalog, remote-trust, archive, and provenance command handlers.</summary>
public partial class DiscoveryManagementViewModel
{
    private async Task RegisterCatalogAsync()
    {
        var path = await BrowseCatalogFileInteraction.HandleSafe(RxVoid.Default);
        if (path.IsNullOrEmpty())
        {
            return;
        }

        await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            var registered = await _catalogs.RegisterAsync(path);
            await RefreshCatalogsCoreAsync();
            SelectedCatalog = Catalogs.FirstOrDefault(x => x.Id == registered.Id);
            StatusMessage = $"Registered catalog '{registered.DisplayName}'.";
        });
    }

    private async Task RefreshSelectedCatalogAsync()
    {
        if (SelectedCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectCatalog;
            return;
        }

        var id = SelectedCatalog.Id;
        await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            await _catalogs.RefreshAsync(id);
            await RefreshCatalogsCoreAsync();
            SelectedCatalog = Catalogs.FirstOrDefault(x => x.Id == id);
            StatusMessage = ResUI.TbDiscoveryCatalogMetadataRefreshed;
        });
    }

    private async Task ToggleCatalogEnabledAsync()
    {
        if (SelectedCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectCatalog;
            return;
        }

        var id = SelectedCatalog.Id;
        var enabled = !SelectedCatalog.Enabled;
        await RunBusyAsync(enabled ? "Enabling catalog..." : "Disabling catalog...", async () =>
        {
            await _catalogs.SetEnabledAsync(id, enabled);
            await RefreshCatalogsCoreAsync();
            SelectedCatalog = Catalogs.FirstOrDefault(x => x.Id == id);
            StatusMessage = enabled ? "Catalog enabled." : "Catalog disabled.";
        });
    }

    private async Task UnregisterCatalogAsync()
    {
        if (Busy)
        {
            return;
        }
        if (SelectedCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectCatalog;
            return;
        }
        var id = SelectedCatalog.Id;
        if (!await ConfirmInteraction.HandleSafe(ResUI.TbConfirmCatalogUnregister))
        {
            return;
        }
        if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal))
        {
            StatusMessage = ResUI.TbDiscoveryCatalogUnregisterStale;
            return;
        }

        await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            var receipt = await _catalogs.UnregisterAsync(id);
            _pendingCatalogUpdate = null;
            HasCatalogUpdatePreview = false;
            CatalogUpdatePreview = ResUI.TbDiscoveryCatalogUpdatePreviewEmpty;
            await RefreshCatalogsCoreAsync();
            await RefreshCatalogRevisionsAsync();
            StatusMessage =
                $"Catalog unregistered. File retained. {receipt.RevisionCount} revision record(s) preserved.";
        });
    }

    private async Task RefreshRetiredCatalogsAsync()
        => await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            await RefreshRetiredCatalogsCoreAsync();
            StatusMessage = ResUI.TbDiscoveryRetiredArchiveRefreshed;
        });

    private async Task ReRegisterRetiredCatalogAsync()
    {
        if (SelectedRetiredCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectRetiredCatalog;
            return;
        }

        var retired = SelectedRetiredCatalog;
        await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            var restored = await _catalogs.RegisterAsync(
                retired.FilePath,
                new ProviderAsnCatalogRegistrationOptions
                {
                    DisplayName = retired.DisplayName,
                    Enabled = false,
                });
            await RefreshCatalogsCoreAsync();
            await RefreshRetiredCatalogsCoreAsync();
            SelectedCatalog = Catalogs.FirstOrDefault(x => x.Id == restored.Id);
            StatusMessage = ResUI.TbDiscoveryCatalogReregisteredDisabled;
        });
    }

    private async Task DiscardRetiredCatalogHistoryAsync()
    {
        if (Busy)
        {
            return;
        }
        if (SelectedRetiredCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectRetiredCatalog;
            return;
        }
        var id = SelectedRetiredCatalog.Id;
        if (!await ConfirmInteraction.HandleSafe(ResUI.TbConfirmDiscardRetiredCatalogHistory))
        {
            return;
        }
        if (!string.Equals(SelectedRetiredCatalog?.Id, id, StringComparison.Ordinal))
        {
            StatusMessage = ResUI.TbDiscoveryRetiredDiscardStale;
            return;
        }

        await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            var receipt = await _catalogs.DiscardRetiredRevisionHistoryAsync(id);
            await RefreshRetiredCatalogsCoreAsync();
            SelectedRetiredCatalog = RetiredCatalogs.FirstOrDefault(x => x.Id == id);
            await RefreshRetiredCatalogRevisionsAsync();
            StatusMessage =
                $"Discarded {receipt.RevisionCount} persisted revision record(s). Catalog file was not deleted.";
        });
    }

    private async Task ExportRetiredCatalogArchiveAsync()
    {
        if (SelectedRetiredCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectRetiredCatalog;
            return;
        }

        var id = SelectedRetiredCatalog.Id;
        var destination = await SaveCatalogArchiveInteraction.HandleSafe(RxVoid.Default);
        if (destination.IsNullOrEmpty())
        {
            return;
        }
        if (!string.Equals(SelectedRetiredCatalog?.Id, id, StringComparison.Ordinal))
        {
            StatusMessage = ResUI.TbDiscoveryRetiredExportStale;
            return;
        }

        await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            var bundle = await _catalogArchive.PrepareAsync(id);
            await _catalogArchive.SaveAsync(bundle, destination);
            StatusMessage = $"Retired catalog archive exported to {destination}.";
        });
    }

    private async Task RenameCatalogAsync()
    {
        if (SelectedCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectCatalog;
            return;
        }

        var id = SelectedCatalog.Id;
        await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            await _catalogs.RenameAsync(id, CatalogDisplayNameEdit);
            await RefreshCatalogsCoreAsync();
            SelectedCatalog = Catalogs.FirstOrDefault(x => x.Id == id);
            StatusMessage = ResUI.TbDiscoveryCatalogLabelUpdated;
        });
    }

    private async Task ConfigureRemoteCatalogSourceAsync()
    {
        if (SelectedCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectCatalog;
            return;
        }
        if (RemoteCatalogUri.IsNullOrEmpty())
        {
            StatusMessage = ResUI.TbDiscoveryEnterHttpsCatalogUrl;
            return;
        }

        var id = SelectedCatalog.Id;
        IReadOnlyList<string> proposedPins;
        try
        {
            proposedPins = ProviderAsnCatalogTransportPinning.ParseEditorText(RemoteTlsSpkiPinsText);
        }
        catch (Exception ex)
        {
            RemoteTlsPinStatus = $"Invalid pin set: {ex.Message}";
            StatusMessage = ResUI.TbDiscoveryFixSpkiPins;
            return;
        }

        var draft = new ProviderAsnCatalogRemoteSourceConfig
        {
            Uri = RemoteCatalogUri,
            SignatureUri = RemoteSignatureUri,
            SignaturePolicy = RemoteSignaturePolicy,
            TrustedKeyId = RemoteTrustedKeyId,
            TrustedPublicKeySpkiBase64 = RemoteTrustedPublicKeySpkiBase64,
            TlsSpkiPinsSha256 = proposedPins,
        };
        var pinDiff = ProviderAsnCatalogTransportPinning.Diff(_savedRemoteTlsSpkiPins, proposedPins);
        if (pinDiff.Removed.Count > 0
            && !await ConfirmInteraction.HandleSafe(
                string.Format(
                    System.Globalization.CultureInfo.CurrentCulture,
                    ResUI.TbConfirmRemoveTlsPins,
                    pinDiff.Removed.Count)))
        {
            StatusMessage = ResUI.TbDiscoveryRemoteSaveTrustCancelled;
            return;
        }
        if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal))
        {
            StatusMessage = ResUI.TbDiscoveryRemoteSaveStale;
            return;
        }

        await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            await _remoteCatalogs.ConfigureAsync(id, draft);
            _pendingRemoteCatalogFetch = null;
            HasRemoteCatalogPreview = false;
            RemoteCatalogFetchPreview = ResUI.TbDiscoveryFetchPreview;
            await RefreshRemoteCatalogSourceAsync();
            StatusMessage = ResUI.TbDiscoveryRemoteSourceSaved;
        });
    }

    private async Task RemoveRemoteCatalogSourceAsync()
    {
        if (SelectedCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectCatalog;
            return;
        }
        var id = SelectedCatalog.Id;
        if (!await ConfirmInteraction.HandleSafe(ResUI.TbConfirmRemoveRemoteCatalogSource))
        {
            return;
        }
        if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal))
        {
            StatusMessage = ResUI.TbDiscoveryRemoteRemovalStale;
            return;
        }

        await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            await _remoteCatalogs.RemoveAsync(id);
            _pendingRemoteCatalogFetch = null;
            HasRemoteCatalogPreview = false;
            ClearRemoteCatalogEditor();
            RemoteCatalogFetchPreview = ResUI.TbDiscoveryFetchPreview;
            RemoteCatalogSourceSummary = ResUI.TbDiscoveryNoRemoteSource;
            StatusMessage = ResUI.TbDiscoveryRemoteSourceRemoved;
        });
    }

    private async Task FetchRemoteCatalogPreviewAsync()
    {
        if (SelectedCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectCatalog;
            return;
        }

        var id = SelectedCatalog.Id;
        _pendingRemoteCatalogFetch = null;
        HasRemoteCatalogPreview = false;
        RemoteCatalogFetchPreview = ResUI.TbDiscoveryFetchPreview;
        await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            var preview = await _remoteCatalogs.FetchPreviewAsync(id);
            if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal))
            {
                StatusMessage = ResUI.TbDiscoveryRemoteFetchStale;
                return;
            }

            _pendingRemoteCatalogFetch = preview;
            HasRemoteCatalogPreview = preview.HasUpdate;
            RemoteCatalogFetchPreview = FormatRemoteCatalogFetch(preview);
            await RefreshRemoteCatalogSourceAsync(updateEditor: false);
            StatusMessage = preview.HasUpdate
                ? "Remote update preview prepared. Review signature and diff before Apply."
                : "Remote source checked; local catalog already matches remote content.";
        });
    }

    private async Task ApplyRemoteCatalogPreviewAsync()
    {
        if (Busy)
        {
            return;
        }
        if (SelectedCatalog is null || _pendingRemoteCatalogFetch is null)
        {
            StatusMessage = ResUI.TbDiscoveryRemotePreviewRequired;
            return;
        }
        var id = SelectedCatalog.Id;
        var preview = _pendingRemoteCatalogFetch;
        if (!preview.HasUpdate)
        {
            StatusMessage = ResUI.TbDiscoveryRemotePreviewNoUpdate;
            return;
        }
        if (!string.Equals(preview.RegistryId, id, StringComparison.Ordinal))
        {
            _pendingRemoteCatalogFetch = null;
            HasRemoteCatalogPreview = false;
            RemoteCatalogFetchPreview = ResUI.TbDiscoveryFetchPreview;
            StatusMessage = ResUI.TbDiscoveryRemotePreviewWrongCatalog;
            return;
        }
        if (!await ConfirmInteraction.HandleSafe(ResUI.TbConfirmCatalogUpdate))
        {
            return;
        }
        if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal)
            || !ReferenceEquals(_pendingRemoteCatalogFetch, preview))
        {
            StatusMessage = ResUI.TbDiscoveryRemoteApplyStale;
            return;
        }

        await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            var revision = await _remoteCatalogs.ApplyAsync(preview);
            _pendingRemoteCatalogFetch = null;
            HasRemoteCatalogPreview = false;
            RemoteCatalogFetchPreview = $"Applied remote revision {revision.Id}.";
            await RefreshCatalogsCoreAsync();
            SelectedCatalog = Catalogs.FirstOrDefault(x => x.Id == id);
            await RefreshCatalogRevisionsAsync();
            SelectedCatalogRevision = CatalogRevisions.FirstOrDefault(x => x.Id == revision.Id)
                                      ?? SelectedCatalogRevision;
            await RefreshRemoteCatalogSourceAsync(updateEditor: false);
            StatusMessage = ResUI.TbDiscoveryRemoteUpdateApplied;
        });
    }

    private async Task RefreshRemoteCatalogSourceAsync(bool updateEditor = true)
    {
        var selectedId = SelectedCatalog?.Id;
        if (selectedId.IsNullOrEmpty())
        {
            ClearRemoteCatalogEditor();
            RemoteCatalogSourceSummary = ResUI.TbDiscoveryNoRemoteSource;
            return;
        }

        try
        {
            var source = await _remoteCatalogs.GetAsync(selectedId);
            if (!string.Equals(SelectedCatalog?.Id, selectedId, StringComparison.Ordinal))
            {
                return;
            }

            if (source is null)
            {
                ClearRemoteCatalogEditor();
                RemoteCatalogSourceSummary = ResUI.TbDiscoveryNoRemoteSource;
                return;
            }

            _savedRemoteTlsSpkiPins = source.TlsSpkiPinsSha256;
            if (updateEditor)
            {
                RemoteCatalogUri = source.Uri;
                RemoteSignatureUri = source.SignatureUri;
                RemoteSignaturePolicy = source.SignaturePolicy;
                RemoteTrustedKeyId = source.TrustedKeyId;
                RemoteTrustedPublicKeySpkiBase64 = source.TrustedPublicKeySpkiBase64;
                RemoteTlsSpkiPinsText = ProviderAsnCatalogTransportPinning.FormatEditorText(source.TlsSpkiPinsSha256);
            }
            UpdateRemoteTlsPinStatus();
            RemoteCatalogSourceSummary = FormatRemoteCatalogSource(source);
        }
        catch (Exception ex)
        {
            if (string.Equals(SelectedCatalog?.Id, selectedId, StringComparison.Ordinal))
            {
                RemoteCatalogSourceSummary = $"Remote source status unavailable: {ex.Message}";
            }
            Logging.SaveLog($"Remote catalog source refresh failed: {ex}");
        }
    }

    private async Task RefreshSelectedCatalogRevisionDetailsAsync(ProviderAsnCatalogRevisionView? revision)
    {
        var local = FormatCatalogRevision(revision);
        if (revision is null)
        {
            CatalogRevisionDetails = local;
            return;
        }

        var revisionId = revision.Id;
        try
        {
            var provenance = await _remoteCatalogs.GetRevisionProvenanceAsync(revisionId);
            if (!string.Equals(SelectedCatalogRevision?.Id, revisionId, StringComparison.Ordinal))
            {
                return;
            }

            CatalogRevisionDetails = provenance is null
                ? local + Environment.NewLine + "Origin: local/manual catalog update."
                : local + Environment.NewLine + FormatRemoteRevisionProvenance(provenance);
        }
        catch (Exception ex)
        {
            if (string.Equals(SelectedCatalogRevision?.Id, revisionId, StringComparison.Ordinal))
            {
                CatalogRevisionDetails = local + Environment.NewLine + $"Remote provenance unavailable: {ex.Message}";
            }
            Logging.SaveLog($"Remote catalog revision provenance refresh failed: {ex}");
        }
    }

    private void ClearRemoteCatalogEditor()
    {
        RemoteCatalogUri = string.Empty;
        RemoteSignatureUri = string.Empty;
        RemoteSignaturePolicy = ProviderAsnCatalogSignaturePolicy.None;
        RemoteTrustedKeyId = string.Empty;
        RemoteTrustedPublicKeySpkiBase64 = string.Empty;
        _savedRemoteTlsSpkiPins = [];
        RemoteTlsSpkiPinsText = string.Empty;
        RemoteTlsPinStatus = ResUI.TbDiscoveryNoExtraTlsPins;
        _observedRemoteTls = null;
        RemoteObservedTlsSpki = string.Empty;
        RemoteObservedTlsDetails = ResUI.TbDiscoveryInspectSpki;
        _pendingRemoteSourceImport = null;
            HasRemoteSourceImportPreview = false;
        RemoteSourceImportPreview = ResUI.TbDiscoveryImportPreview;
    }

    private void UpdateRemoteTlsPinStatus()
    {
        try
        {
            var proposed = ProviderAsnCatalogTransportPinning.ParseEditorText(RemoteTlsSpkiPinsText);
            var diff = ProviderAsnCatalogTransportPinning.Diff(_savedRemoteTlsSpkiPins, proposed);

            if (proposed.Count == 0)
            {
                RemoteTlsPinStatus = _savedRemoteTlsSpkiPins.Count == 0
                    ? "No extra TLS pins. Standard certificate validation is active."
                    : $"Valid pending change: saving removes {_savedRemoteTlsSpkiPins.Count} configured pin(s).";
                return;
            }

            var rotation = diff.Added.Count > 0 && diff.Unchanged.Count > 0
                ? " · overlap rotation ready (old and new pins coexist until Save)"
                : string.Empty;
            RemoteTlsPinStatus =
                $"{proposed.Count}/{ProviderAsnCatalogTransportPinning.MaximumPins} valid pin(s) · " +
                $"+{diff.Added.Count} -{diff.Removed.Count} unchanged={diff.Unchanged.Count}{rotation}";
        }
        catch (Exception ex)
        {
            RemoteTlsPinStatus = $"Invalid pin set: {ex.Message}";
        }
    }

    private async Task InspectRemoteTlsSpkiAsync()
    {
        if (RemoteCatalogUri.IsNullOrEmpty())
        {
            StatusMessage = ResUI.TbDiscoveryEnterHttpsCatalogUrl;
            return;
        }

        var sourceUri = RemoteCatalogUri;
        await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            var observation = await _tlsObservation.ObserveAsync(sourceUri);
            if (!string.Equals(RemoteCatalogUri, sourceUri, StringComparison.Ordinal))
            {
                StatusMessage = ResUI.TbDiscoveryTlsObservationStale;
                return;
            }

            _observedRemoteTls = observation;
            RemoteObservedTlsSpki = observation.SpkiSha256;
            RemoteObservedTlsDetails = FormatTlsObservation(observation);
            StatusMessage =
                "Observed the server SPKI through ordinary validated HTTPS. It has not been trusted or saved.";
        });
    }

    private async Task AddObservedRemoteTlsPinAsync()
    {
        if (_observedRemoteTls is null || RemoteObservedTlsSpki.IsNullOrEmpty())
        {
            StatusMessage = ResUI.TbDiscoveryInspectServerKeyFirst;
            return;
        }

        if (!ProviderAsnCatalogTlsObservationService.CanUseObservedPinForSource(
                _observedRemoteTls,
                RemoteCatalogUri,
                out var incompatibility))
        {
            StatusMessage = incompatibility;
            return;
        }

        IReadOnlyList<string> proposed;
        try
        {
            proposed = ProviderAsnCatalogTransportPinning.ParseEditorText(RemoteTlsSpkiPinsText);
        }
        catch (Exception ex)
        {
            RemoteTlsPinStatus = $"Invalid pin set: {ex.Message}";
            StatusMessage = ResUI.TbDiscoveryFixPinEditor;
            return;
        }

        var merged = ProviderAsnCatalogTransportPinning.NormalizePins(
            proposed.Append(_observedRemoteTls.SpkiSha256));
        RemoteTlsSpkiPinsText = ProviderAsnCatalogTransportPinning.FormatEditorText(merged);
        UpdateRemoteTlsPinStatus();
        StatusMessage =
            "Observed SPKI added to the editor only. Review the overlap set and Save remote source to persist trust.";
        await Task.CompletedTask;
    }

    private async Task ExportRemoteSourceConfigAsync()
    {
        if (SelectedCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectCatalog;
            return;
        }

        var id = SelectedCatalog.Id;
        var destination = await SaveRemoteSourceBundleInteraction.HandleSafe(RxVoid.Default);
        if (destination.IsNullOrEmpty())
        {
            return;
        }
        if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal))
        {
            StatusMessage = ResUI.TbDiscoveryRemoteExportStale;
            return;
        }

        await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            var bundle = await _remoteSourcePortability.ExportAsync(id);
            await _remoteSourcePortability.SaveAsync(bundle, destination);
            StatusMessage =
                $"Remote-source trust configuration exported to {destination}. Runtime cache state and private keys were not included.";
        });
    }

    private async Task PreviewRemoteSourceImportAsync()
    {
        if (SelectedCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectCatalog;
            return;
        }

        var id = SelectedCatalog.Id;
        var sourcePath = await BrowseRemoteSourceBundleInteraction.HandleSafe(RxVoid.Default);
        if (sourcePath.IsNullOrEmpty())
        {
            return;
        }
        if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal))
        {
            StatusMessage = ResUI.TbDiscoveryRemoteImportPreviewStale;
            return;
        }

        _pendingRemoteSourceImport = null;
        HasRemoteSourceImportPreview = false;
        RemoteSourceImportPreview = ResUI.TbDiscoveryImportPreview;
        await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            var bundle = await _remoteSourcePortability.LoadAsync(sourcePath);
            var preview = await _remoteSourcePortability.PrepareImportAsync(id, bundle);
            if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal))
            {
                StatusMessage = ResUI.TbDiscoveryRemoteImportPreparedStale;
                return;
            }

            _pendingRemoteSourceImport = preview;
            HasRemoteSourceImportPreview = true;
            RemoteSourceImportPreview = FormatRemoteSourceImportPreview(preview);
            StatusMessage =
                "Remote-source import preview prepared. No source configuration, network fetch, or catalog bytes changed.";
        });
    }

    private async Task ApplyRemoteSourceImportAsync()
    {
        if (Busy)
        {
            return;
        }
        if (SelectedCatalog is null || _pendingRemoteSourceImport is null)
        {
            StatusMessage = ResUI.TbDiscoveryRemoteImportPreviewRequired;
            return;
        }
        var id = SelectedCatalog.Id;
        var preview = _pendingRemoteSourceImport;
        if (!string.Equals(preview.TargetRegistryId, id, StringComparison.Ordinal))
        {
            _pendingRemoteSourceImport = null;
            HasRemoteSourceImportPreview = false;
            RemoteSourceImportPreview = ResUI.TbDiscoveryImportPreview;
            StatusMessage = ResUI.TbDiscoveryRemoteImportWrongCatalog;
            return;
        }
        if (!await ConfirmInteraction.HandleSafe(ResUI.TbConfirmRemoteSourceImport))
        {
            return;
        }
        if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal)
            || !ReferenceEquals(_pendingRemoteSourceImport, preview))
        {
            StatusMessage = ResUI.TbDiscoveryRemoteImportApplyStale;
            return;
        }

        await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            await _remoteSourcePortability.ApplyImportAsync(preview);
            _pendingRemoteSourceImport = null;
            HasRemoteSourceImportPreview = false;
            RemoteSourceImportPreview = ResUI.TbDiscoveryRemoteTrustImported;
            _pendingRemoteCatalogFetch = null;
            HasRemoteCatalogPreview = false;
            RemoteCatalogFetchPreview = ResUI.TbDiscoveryFetchPreview;
            await RefreshRemoteCatalogSourceAsync();
            StatusMessage =
                "Remote-source trust configuration applied. No remote fetch or catalog update was performed.";
        });
    }

    private async Task InspectCatalogArchiveAsync()
    {
        var path = await BrowseCatalogArchiveInteraction.HandleSafe(RxVoid.Default);
        if (path.IsNullOrEmpty())
        {
            return;
        }

        await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            var inspection = await _catalogArchiveInspection.InspectAsync(path);
            ArchiveInspectionSummary = FormatArchiveInspection(inspection);
            StatusMessage =
                "Archive inspected read-only. No catalog was extracted, registered, configured, or applied.";
        });
    }

    private async Task SearchRemoteProvenanceAsync()
        => await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            await SearchRemoteProvenanceCoreAsync();
            StatusMessage = ResUI.TbDiscoveryRemoteProvenanceRefreshed;
        });

    private async Task SearchRemoteProvenanceCoreAsync()
    {
        var selectedRevision = SelectedRemoteProvenance?.RevisionId;
        RemoteProvenance = await _remoteProvenanceQuery.QueryAsync(
            new ProviderAsnCatalogRemoteProvenanceQuery
            {
                SourceHost = RemoteProvenanceSourceHostFilter.NullIfEmpty(),
                TrustedKeyId = RemoteProvenanceTrustedKeyFilter.NullIfEmpty(),
                TlsSpkiPinSha256 = RemoteProvenanceTlsPinFilter.NullIfEmpty(),
                MaxAge = TimeSpan.FromDays(730),
                MaxItems = 250,
            });

        var latest = RemoteProvenance.LatestAppliedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "never";
        RemoteProvenanceSummaryText =
            $"{RemoteProvenance.Total} record(s) · {RemoteProvenance.SignatureValid} valid signature(s) · " +
            $"{RemoteProvenance.TransportPinned} transport-pinned apply(s) · {RemoteProvenance.ServerNotModified} 304 check(s) · latest={latest}";

        SelectedRemoteProvenance = selectedRevision.IsNullOrEmpty()
            ? RemoteProvenance.Entries.FirstOrDefault()
            : RemoteProvenance.Entries.FirstOrDefault(x => x.RevisionId == selectedRevision)
              ?? RemoteProvenance.Entries.FirstOrDefault();
    }

    private async Task PreviewCatalogUpdateAsync()
    {
        if (SelectedCatalog is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectCatalog;
            return;
        }

        var id = SelectedCatalog.Id;
        var path = await BrowseCatalogUpdateFileInteraction.HandleSafe(RxVoid.Default);
        if (path.IsNullOrEmpty())
        {
            return;
        }
        if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal))
        {
            StatusMessage = ResUI.TbDiscoveryCatalogPreviewStale;
            return;
        }

        _pendingCatalogUpdate = null;
        HasCatalogUpdatePreview = false;
        CatalogUpdatePreview = ResUI.TbUpdatePreview;
        await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            var bytes = await File.ReadAllBytesAsync(path);
            var plan = await _catalogs.PrepareUpdateAsync(id, bytes);
            if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal))
            {
                StatusMessage = ResUI.TbDiscoveryCatalogPreviewPreparedStale;
                return;
            }

            _pendingCatalogUpdate = plan;
            HasCatalogUpdatePreview = true;
            CatalogUpdatePreview = FormatCatalogUpdate(plan);
            StatusMessage = ResUI.TbDiscoveryCatalogPreviewPrepared;
        });
    }

    private async Task ApplyCatalogUpdateAsync()
    {
        if (Busy)
        {
            return;
        }

        if (SelectedCatalog is null || _pendingCatalogUpdate is null)
        {
            StatusMessage = ResUI.TbDiscoveryCatalogPreviewRequired;
            return;
        }

        var id = SelectedCatalog.Id;
        var plan = _pendingCatalogUpdate;
        if (!await ConfirmInteraction.HandleSafe(ResUI.TbConfirmCatalogUpdate))
        {
            return;
        }
        if (!string.Equals(SelectedCatalog?.Id, id, StringComparison.Ordinal)
            || !ReferenceEquals(_pendingCatalogUpdate, plan))
        {
            StatusMessage = ResUI.TbDiscoveryCatalogApplyStale;
            return;
        }

        await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            var revision = await _catalogs.ApplyUpdateAsync(id, plan);
            _pendingCatalogUpdate = null;
            HasCatalogUpdatePreview = false;
            CatalogUpdatePreview = $"Applied revision {revision.Id}.";
            await RefreshCatalogsCoreAsync();
            SelectedCatalog = Catalogs.FirstOrDefault(x => x.Id == id);
            await RefreshCatalogRevisionsAsync();
            StatusMessage = ResUI.TbDiscoveryCatalogUpdateApplied;
        });
    }

    private async Task RollbackCatalogRevisionAsync()
    {
        if (Busy)
        {
            return;
        }

        if (SelectedCatalogRevision is null)
        {
            StatusMessage = ResUI.TbDiscoverySelectRevision;
            return;
        }

        var revision = SelectedCatalogRevision;
        var revisionId = revision.Id;
        var registryId = revision.RegistryId;
        if (!await ConfirmInteraction.HandleSafe(ResUI.TbConfirmCatalogRollback))
        {
            return;
        }
        if (!string.Equals(SelectedCatalogRevision?.Id, revisionId, StringComparison.Ordinal))
        {
            StatusMessage = ResUI.TbDiscoveryRollbackStale;
            return;
        }

        await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            await _catalogs.RollbackRevisionAsync(revisionId);
            await RefreshCatalogsCoreAsync();
            SelectedCatalog = Catalogs.FirstOrDefault(x => x.Id == registryId);
            await RefreshCatalogRevisionsAsync();
            StatusMessage = ResUI.TbDiscoveryRollbackApplied;
        });
    }

    private async Task RefreshRemoteSourceHealthAsync()
        => await RunBusyAsync(ResUI.TbDiscoveryWorking, async () =>
        {
            await RefreshRemoteSourceHealthCoreAsync();
            StatusMessage = ResUI.TbDiscoveryRemoteHealthRefreshed;
        });

    private async Task RefreshRemoteSourceHealthCoreAsync()
    {
        var selectedId = SelectedRemoteSourceHealth?.RegistryId;
        RemoteSourceHealth = await _remoteHealth.LoadAsync();
        RemoteSourceHealthSummaryText =
            $"{RemoteSourceHealth.TotalCatalogs} active catalog(s) · {RemoteSourceHealth.Configured} remote source(s) configured · " +
            $"{RemoteSourceHealth.Healthy} healthy · {RemoteSourceHealth.NeedsReview} need review · " +
            $"{RemoteSourceHealth.Unconfigured} local-only/unconfigured";

        SelectedRemoteSourceHealth = selectedId.IsNullOrEmpty()
            ? RemoteSourceHealth.Rows.FirstOrDefault()
            : RemoteSourceHealth.Rows.FirstOrDefault(x => x.RegistryId == selectedId)
              ?? RemoteSourceHealth.Rows.FirstOrDefault();
    }

}
