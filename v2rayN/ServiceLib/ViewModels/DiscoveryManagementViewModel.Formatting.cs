using ServiceLib.Discovery.Models;
using ServiceLib.Discovery.Services;
using ServiceLib.Reviver.Models;

namespace ServiceLib.ViewModels;

/// <summary>
/// Presentation-only formatting for DiscoveryManagementViewModel. Keeping these
/// projections separate prevents the management coordinator from accumulating
/// another layer of UI text concerns while preserving existing bindings.
/// </summary>
public partial class DiscoveryManagementViewModel
{
    private static string FormatCatalogAudit(ProviderAsnCatalogRegistryView? catalog)
    {
        if (catalog is null)
        {
            return "Select a catalog to view its audit details.";
        }

        var audit = catalog.LastAudit;
        if (audit is null)
        {
            return $"{catalog.DisplayName} · {catalog.CatalogVersion} · no stored audit.";
        }

        var freshness = !audit.FreshnessKnown
            ? "freshness unknown"
            : audit.Stale
                ? $"stale ({audit.AgeDays:0.#} days)"
                : $"fresh ({audit.AgeDays:0.#} days)";
        var issues = audit.Errors.Count > 0
            ? $" errors={audit.Errors.Count}"
            : audit.Warnings.Count > 0
                ? $" warnings={audit.Warnings.Count}"
                : string.Empty;

        return $"{catalog.DisplayName} · {catalog.CatalogId} {catalog.CatalogVersion} · " +
               $"{(catalog.Enabled ? "enabled" : "disabled")} · {freshness} · " +
               $"{audit.EnabledEntries}/{audit.TotalEntries} entries · {audit.UniqueAddresses} unique IPs · " +
               $"{audit.ProviderCount} providers / {audit.AsnCount} ASNs / {audit.PopCount} POPs{issues}";
    }

    private static string FormatCatalogUpdate(ProviderAsnCatalogUpdatePlan plan)
    {
        var diff = plan.Diff;
        var audit = plan.AfterAudit;
        var diffText = diff is null
            ? "semantic diff unavailable"
            : $"+{diff.Added} / -{diff.Removed} / ~{diff.Modified} / ={diff.Unchanged}";
        var freshness = !audit.FreshnessKnown
            ? "freshness unknown"
            : audit.Stale
                ? $"stale ({audit.AgeDays:0.#} days)"
                : $"fresh ({audit.AgeDays:0.#} days)";

        return $"{plan.BeforeCatalogVersion.NullIfEmpty() ?? "(unreadable)"} -> {plan.AfterCatalogVersion} · " +
               $"{diffText} · {audit.TotalEntries} entries · {freshness} · " +
               $"SHA {ShortHash(plan.BeforeSha256)} -> {ShortHash(plan.AfterSha256)}";
    }

    private static string FormatCatalogRevision(ProviderAsnCatalogRevisionView? revision)
    {
        if (revision is null)
        {
            return "Select a revision to view its details.";
        }

        var state = revision.Active
            ? "active"
            : revision.RolledBackAt is not null
                ? $"rolled back {revision.RolledBackAt.Value.ToLocalTime():yyyy-MM-dd HH:mm:ss}" +
                  (revision.RollbackForced ? " (forced)" : string.Empty)
                : "inactive";

        var diff = revision.Diff is null
            ? "semantic diff unavailable"
            : $"+{revision.Diff.Added} / -{revision.Diff.Removed} / ~{revision.Diff.Modified} / ={revision.Diff.Unchanged}";

        var audit = revision.AppliedAudit;
        var auditText = audit is null
            ? "applied audit unavailable"
            : $"{(audit.Valid ? "valid" : "invalid")} audit · " +
              $"{(audit.FreshnessKnown ? (audit.Stale ? "stale" : "fresh") : "freshness unknown")} · " +
              $"{audit.TotalEntries} entries · warnings={audit.Warnings.Count} errors={audit.Errors.Count}";

        return $"{revision.BeforeCatalogVersion.NullIfEmpty() ?? "(unknown)"} -> " +
               $"{revision.AfterCatalogVersion.NullIfEmpty() ?? "(unknown)"} · {state}" +
               Environment.NewLine +
               $"{diff} · {auditText}" +
               Environment.NewLine +
               $"SHA {ShortHash(revision.BeforeSha256)} -> {ShortHash(revision.AfterSha256)} · " +
               $"applied {revision.AppliedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
    }

    private static string FormatTlsObservation(ProviderAsnCatalogTlsObservation observation)
        => $"{observation.Host} · HTTP {observation.HttpStatusCode} · observed {observation.ObservedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}" +
           Environment.NewLine +
           $"request={observation.RequestedUri} · final={observation.FinalUri}" +
           Environment.NewLine +
           $"SPKI SHA-256: {observation.SpkiSha256}" +
           Environment.NewLine +
           $"certificate: {observation.Subject} · issuer: {observation.Issuer}" +
           Environment.NewLine +
           $"validity: {observation.NotBefore.ToLocalTime():yyyy-MM-dd HH:mm:ss} -> {observation.NotAfter.ToLocalTime():yyyy-MM-dd HH:mm:ss}" +
           Environment.NewLine +
           "Observation is evidence only; it is not trusted until explicitly added to the editor and saved.";

    private string FormatRemoteSourceImportPreview(ProviderAsnCatalogRemoteSourceImportPreview preview)
    {
        var warnings = preview.Warnings.Count == 0 ? "none" : string.Join(", ", preview.Warnings);
        var pins = ProviderAsnCatalogTransportPinning.NormalizePins(preview.Source.TlsSpkiPinsSha256);
        var pinDiff = ProviderAsnCatalogTransportPinning.Diff(_savedRemoteTlsSpkiPins, pins);
        return $"Prepared {preview.PreparedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss} · catalog={preview.CatalogId} · " +
               $"bundle version={preview.BundleCatalogVersion.NullIfEmpty() ?? "unspecified"} · current version={preview.CurrentCatalogVersion}" +
               Environment.NewLine +
               $"source={preview.Source.Uri} · signature policy={preview.Source.SignaturePolicy} · key={preview.Source.TrustedKeyId.NullIfEmpty() ?? "none"} · TLS pins={pins.Count}" +
               Environment.NewLine +
               $"pin change: +{pinDiff.Added.Count} -{pinDiff.Removed.Count} unchanged={pinDiff.Unchanged.Count} · trust fingerprint={ShortHash(preview.TrustedPublicKeySha256)} · warnings={warnings}" +
               Environment.NewLine +
               "Apply changes source/trust configuration only; it does not fetch or apply catalog content.";
    }

    private static string FormatArchiveInspection(ProviderAsnCatalogArchiveInspection inspection)
    {
        var audit = inspection.CatalogAudit;
        var freshness = audit is null
            ? "audit unavailable"
            : !audit.Valid
                ? "audit invalid"
                : !audit.FreshnessKnown
                    ? "freshness unknown"
                    : audit.Stale
                        ? "metadata stale"
                        : "metadata fresh";
        var warnings = inspection.Warnings.Count == 0 ? "none" : string.Join(", ", inspection.Warnings);

        return $"{Path.GetFileName(inspection.SourcePath)} · format v{inspection.FormatVersion} · created {inspection.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}" +
               Environment.NewLine +
               $"catalog={inspection.CatalogId} {inspection.CatalogVersion} · entries={inspection.CatalogEntries} · bytes={inspection.CatalogBytes} · SHA={ShortHash(inspection.CatalogFileSha256)}" +
               Environment.NewLine +
               $"revisions={inspection.Revisions} · remote provenance={inspection.RemoteProvenanceRecords} · registry SHA matches payload={inspection.RegistryShaMatchesPayload}" +
               Environment.NewLine +
               $"{freshness} · warnings={warnings}" +
               Environment.NewLine +
               "Inspection is read-only; nothing was extracted, registered, trusted, fetched, or applied.";
    }

    private static string FormatRemoteProvenance(ProviderAsnCatalogRemoteApplyProvenanceView? item)
    {
        if (item is null)
        {
            return "Select an update to view its saved provenance.";
        }

        var signature = item.SignatureValidation is null
            ? "signature unavailable"
            : $"signature={item.SignatureValidation.Status}; valid={item.SignatureValidation.Valid}; policy satisfied={item.SignatureValidation.PolicySatisfied}";
        var pins = item.TlsSpkiPinsSha256.Count == 0
            ? "none"
            : string.Join(", ", item.TlsSpkiPinsSha256.Select(ShortHash));

        return $"revision={item.RevisionId} · registry={item.RegistryId}" +
               Environment.NewLine +
               $"source={item.SourceUri} · remote SHA={ShortHash(item.RemoteContentSha256)} · ETag={item.ETag.NullIfEmpty() ?? "none"}" +
               Environment.NewLine +
               $"{signature} · trusted key={item.TrustedKeyId.NullIfEmpty() ?? "none"}" +
               Environment.NewLine +
               $"transport SPKI pins={pins} · checked={item.CheckedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss} · applied={item.AppliedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}";
    }

    private static string FormatRemoteCatalogSource(ProviderAsnCatalogRemoteSourceView source)
    {
        var checkedAt = source.LastCheckedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "never";
        var fetchedAt = source.LastFetchedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "never";
        var signature = source.LastSignatureValid is null
            ? source.LastSignatureStatus.NullIfEmpty() ?? "not checked"
            : source.LastSignatureValid == true
                ? $"valid ({source.LastSignatureStatus})"
                : $"invalid ({source.LastSignatureStatus})";

        var pins = source.TlsSpkiPinsSha256.Count == 0
            ? "none"
            : string.Join(", ", source.TlsSpkiPinsSha256.Select(ShortHash));

        return $"{source.Uri} · policy={source.SignaturePolicy} · checked={checkedAt} · fetched={fetchedAt}" +
               Environment.NewLine +
               $"cache SHA={ShortHash(source.RemoteContentSha256)} · ETag={source.ETag.NullIfEmpty() ?? "none"} · " +
               $"signature={signature} · key={source.LastSignatureKeyId.NullIfEmpty() ?? source.TrustedKeyId.NullIfEmpty() ?? "none"}" +
               Environment.NewLine +
               $"HTTPS SPKI pins={pins}";
    }

    private static string FormatRemoteCatalogFetch(ProviderAsnCatalogRemoteFetchPreview preview)
    {
        var update = preview.UpdatePlan is null
            ? "no content update"
            : FormatCatalogUpdate(preview.UpdatePlan);
        var signature = preview.SignatureValidation is null
            ? "signature evidence unavailable"
            : $"signature policy={preview.SignatureValidation.Policy}; " +
              $"attempted={preview.SignatureValidation.Attempted}; valid={preview.SignatureValidation.Valid}; " +
              $"satisfied={preview.SignatureValidation.PolicySatisfied}; status={preview.SignatureValidation.Status}; " +
              $"key={preview.SignatureValidation.KeyId.NullIfEmpty() ?? "none"}";

        return $"Checked {preview.CheckedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss} · " +
               $"304={preview.ServerNotModified} · local-match={preview.LocalAlreadyMatchesRemote} · " +
               $"remote SHA={ShortHash(preview.RemoteContentSha256)}" +
               Environment.NewLine +
               signature +
               Environment.NewLine +
               update;
    }

    private static string FormatRemoteRevisionProvenance(ProviderAsnCatalogRemoteApplyProvenanceView provenance)
    {
        var signature = provenance.SignatureValidation;
        var signatureText = signature is null
            ? "signature evidence unavailable"
            : $"signature={signature.Status}; valid={signature.Valid}; policy={signature.Policy}; " +
              $"key={signature.KeyId.NullIfEmpty() ?? provenance.TrustedKeyId.NullIfEmpty() ?? "none"}" +
              (provenance.SignatureFinalUri.IsNullOrEmpty()
               || string.Equals(provenance.SignatureFinalUri, provenance.SignatureUri, StringComparison.Ordinal)
                  ? string.Empty
                  : $"; fetched={provenance.SignatureFinalUri}");

        var pins = provenance.TlsSpkiPinsSha256.Count == 0
            ? "none"
            : string.Join(", ", provenance.TlsSpkiPinsSha256.Select(ShortHash));
        var sourceRoute = provenance.SourceFinalUri.IsNullOrEmpty()
                          || string.Equals(provenance.SourceFinalUri, provenance.SourceUri, StringComparison.Ordinal)
            ? provenance.SourceUri
            : $"{provenance.SourceUri} -> {provenance.SourceFinalUri}";

        return $"Origin: remote source {sourceRoute}" +
               Environment.NewLine +
               $"remote SHA={ShortHash(provenance.RemoteContentSha256)} · ETag={provenance.ETag.NullIfEmpty() ?? "none"} · " +
               $"checked={provenance.CheckedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss} · applied={provenance.AppliedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}" +
               Environment.NewLine +
               $"{signatureText} · transport pins={pins}";
    }

    private static string FormatRemoteSourceHealth(ProviderAsnCatalogRemoteHealthRow? item)
    {
        if (item is null)
        {
            return "Select a source to view its saved health details.";
        }

        var checkedAt = item.LastCheckedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "never";
        var fetchedAt = item.LastFetchedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "never";
        var reasons = item.Reasons.Count == 0 ? "none" : string.Join(", ", item.Reasons);

        return $"{item.DisplayName} · {item.CatalogId} {item.CatalogVersion} · {item.HealthClass}" +
               Environment.NewLine +
               $"configured={item.Configured} · enabled={item.CatalogEnabled} · policy={item.SignaturePolicy} · checked={checkedAt} · fetched={fetchedAt}" +
               Environment.NewLine +
               $"source={item.SourceUri.NullIfEmpty() ?? "none"} · remote SHA={ShortHash(item.RemoteContentSha256)}" +
               Environment.NewLine +
               $"local audit valid={item.CatalogAuditValid} · freshness known={item.CatalogFreshnessKnown} · stale={item.CatalogStale} · reasons={reasons}";
    }

    private static string FormatEndpointHistory(EndpointPoolInspectionRow? endpoint)
    {
        if (endpoint is null)
        {
            return "Select an endpoint to inspect its history.";
        }

        var history = endpoint.History;
        if (history is null)
        {
            return $"{endpoint.Address}:{endpoint.Port} · no recent observation history.";
        }

        return $"{endpoint.Address}:{endpoint.Port} · current={endpoint.CurrentObservationState} · " +
               $"samples={history.Samples} · reliability={history.DecayedReliability:P0} · " +
               $"latency={(history.DecayedLatencyMs is null ? "n/a" : $"{history.DecayedLatencyMs:0.#} ms")} · " +
               $"failure streak={history.RecentFailureStreak} · " +
               $"{(endpoint.HistoricallyGood ? "historically good" : "not historically eligible")}";
    }

    private static string FormatPromotionEvent(RepairPromotionHistoryEntry? item)
    {
        if (item is null)
        {
            return "Select an event to view its details.";
        }

        var comparison = item.OutcomeComparison;
        var comparisonText = comparison is null
            ? "outcome comparison unavailable"
            : $"{comparison.Verdict}; reliability Δ={FormatDelta(comparison.ReliabilityDelta)}; " +
              $"latency Δ={FormatDelta(comparison.LatencyDeltaMs)} ms; loss Δ={FormatDelta(comparison.LossDelta)}";

        return $"{item.EventKind} · {item.ObservedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss} · " +
               $"candidate={item.CandidateId} · score={(item.Score?.ToString("0.##") ?? "n/a")} · " +
               $"{comparisonText}";
    }

    private static string FormatEndpointMaintenance(
        EndpointMaintenancePlan plan,
        MaintenancePayloadEstimate estimate)
    {
        if (plan.PoolEntriesToRemove.Count == 0)
        {
            return $"Preview {plan.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}: no disabled/unpinned endpoint-pool entries are eligible for removal. " +
                   "Approximate serialized payload: 0 B.";
        }

        var examples = string.Join(
            Environment.NewLine,
            plan.PoolEntriesToRemove
                .Take(8)
                .Select(x => $"{x.LogicalHost} -> {x.Address} ({x.Id})"));

        return $"Preview {plan.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}: " +
               $"{plan.PoolEntriesToRemove.Count} endpoint entries eligible for removal. " +
               $"Approximate serialized payload: {estimate.HumanReadable}. " +
               "SQLite file size may not shrink until compaction." +
               Environment.NewLine + examples;
    }

    private static string FormatRetention(
        LifecycleRetentionPlan plan,
        MaintenancePayloadEstimate estimate)
    {
        var lines = plan.Tables.Select(x =>
        {
            estimate.BytesByCategory.TryGetValue(x.Table, out var bytes);
            return $"{x.Table}: {x.CandidateCount} · ~{MaintenancePayloadEstimate.FormatBytes(bytes)}";
        });
        return $"Preview {plan.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}: {plan.TotalCandidates} records eligible · " +
               $"~{estimate.HumanReadable} serialized payload. SQLite file size may not shrink until compaction." +
               Environment.NewLine + string.Join(Environment.NewLine, lines);
    }

    private static string FormatRetiredCatalog(
        ProviderAsnCatalogRegistryView? catalog,
        int? revisionCount = null)
    {
        if (catalog is null)
        {
            return "Select a retired catalog to view its preserved history.";
        }

        var retiredAt = catalog.UnregisteredAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "unknown";
        var revisions = revisionCount is null ? "revision count not loaded" : $"{revisionCount} revision(s) preserved";
        return $"{catalog.DisplayName} · {catalog.CatalogId} {catalog.CatalogVersion} · retired {retiredAt}" +
               Environment.NewLine +
               $"{revisions} · file retained at {catalog.FilePath}" +
               Environment.NewLine +
               "Re-register keeps the existing file and returns it disabled. Discard history is irreversible and does not delete the file.";
    }

    private static string FormatCompactionPlan(SQLiteCompactionPlan plan)
        => $"Preview {plan.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm:ss}: database={MaintenancePayloadEstimate.FormatBytes(plan.DatabaseLengthBytes)} · " +
           $"free pages={plan.FreePageCount}/{plan.PageCount} · estimated reclaim={MaintenancePayloadEstimate.FormatBytes(plan.EstimatedReclaimBytes)}. " +
           "VACUUM is explicit, stale-checked, and may temporarily require additional disk space.";

    private static string FormatCompactionResult(SQLiteCompactionResult result)
        => $"Compaction complete: file {MaintenancePayloadEstimate.FormatBytes(result.Before.DatabaseLengthBytes)} -> " +
           $"{MaintenancePayloadEstimate.FormatBytes(result.After.DatabaseLengthBytes)} · reclaimed {MaintenancePayloadEstimate.FormatBytes(result.FileBytesReclaimed)}.";

    private static string ShortHash(string value)
        => value.IsNullOrEmpty() ? "none" : value[..Math.Min(12, value.Length)];

    private static string FormatDelta(double? value)
        => value is null ? "n/a" : value.Value.ToString("+0.###;-0.###;0");
}
