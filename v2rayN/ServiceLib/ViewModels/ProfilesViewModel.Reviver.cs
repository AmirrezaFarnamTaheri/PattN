using System.Globalization;
using ServiceLib.Discovery.Services;
using ServiceLib.Reviver.Models;
using ServiceLib.Reviver.Normalization;
using ServiceLib.Reviver.Promotion;
using ServiceLib.Reviver.Services;
using ServiceLib.Reviver.Strategies;
using ServiceLib.Reviver.Support;
using ServiceLib.Reviver.Validation;

namespace ServiceLib.ViewModels;

public partial class ProfilesViewModel
{
    private readonly SemaphoreSlim _reviverRunGate = new(1, 1);
    private CancellationTokenSource? _reviverCts;
    private RepairRunResult? _lastReviverRun;

    public Interaction<string, string?> SaveSupportBundleInteraction { get; } = new();

    [Reactive]
    public partial bool ReviverBusy { get; set; }

    [Reactive]
    public partial bool HasReviverResult { get; set; }

    [Reactive]
    public partial bool ReviverCancelable { get; set; }

    [Reactive]
    public partial bool ReviverStatusVisible { get; set; }

    [Reactive]
    public partial string ReviverStatus { get; set; }

    public ReactiveCommand<RxVoid, RxVoid> ReviveSelectedProfileCmd { get; private set; } = null!;
    public ReactiveCommand<RxVoid, RxVoid> CancelReviverCmd { get; private set; } = null!;
    public ReactiveCommand<RxVoid, RxVoid> ExportReviverSupportBundleCmd { get; private set; } = null!;

    private void InitializeReviverCommands()
    {
        ReviverStatus = string.Empty;

        var canStart = this.WhenAnyValue(
            x => x.SelectedProfile,
            x => x.ReviverBusy,
            (selected, busy) => selected is not null
                                && selected.IndexId.IsNotEmpty()
                                && !busy);
        var canCancel = this.WhenAnyValue(x => x.ReviverCancelable);
        var canExport = this.WhenAnyValue(
            x => x.HasReviverResult,
            x => x.ReviverBusy,
            (hasResult, busy) => hasResult && !busy);

        ReviveSelectedProfileCmd = ReactiveCommand.CreateFromTask(ReviveSelectedProfileAsync, canStart);
        CancelReviverCmd = ReactiveCommand.CreateFromTask(async () =>
        {
            CancelReviver();
            await Task.CompletedTask;
        }, canCancel);
        ExportReviverSupportBundleCmd = ReactiveCommand.CreateFromTask(ExportReviverSupportBundleAsync, canExport);
    }

    public void CancelReviver()
    {
        if (!ReviverBusy)
        {
            return;
        }

        ReviverStatus = ResUI.TbReviverCancelling;
        ReviverStatusVisible = true;
        ReviverCancelable = false;
        _reviverCts?.Cancel();
    }

    private async Task ReviveSelectedProfileAsync()
    {
        if (!await _reviverRunGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            var selectedId = SelectedProfile?.IndexId;
            if (selectedId.IsNullOrEmpty())
            {
                NoticeManager.Instance.Enqueue(ResUI.PleaseSelectServer);
                return;
            }

            var profile = await AppManager.Instance.GetProfileItem(selectedId);
            if (profile is null)
            {
                NoticeManager.Instance.Enqueue(ResUI.PleaseSelectServer);
                return;
            }

            if (profile.IsComplex() || profile.ConfigType is EConfigType.Custom or EConfigType.Outbound)
            {
                SetReviverStatus(ResUI.TbReviverUnsupported);
                return;
            }

            _lastReviverRun = null;
            HasReviverResult = false;
            ReviverBusy = true;
            ReviverCancelable = true;
            SetReviverStatus(ResUI.TbReviverDiagnosing);

            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            _reviverCts = timeout;
            var cancellationToken = timeout.Token;

            await using var engine = new DiscoveryEngineService();
            var policy = new RepairPolicy();
            var compatibility = new ProfileCoreCompatibility();
            var invariants = new ProfileInvariantRegistry();
            var validator = new CoreBackedRepairValidator(policy);
            var diagnostic = new RepairBaselineDiagnostic(
                invariants,
                compatibility,
                new ReviverEndpointPreflight(),
                validator,
                policy);
            var candidateProvider = DiscoveryCandidateComposition.CreateWithRegisteredProviderCatalogs(engine);
            var dnsHistory = new SqliteDnsRepairHistoryStore();
            var strategies = ReviverStrategyCatalog.CreateDefault(
                candidateProvider,
                new DiscoveryDnsRepairEvidenceProvider(engine),
                compatibility);
            var reviver = new ReviverService(
                new ProfileNormalizer(),
                invariants,
                strategies,
                policy,
                observers: ReviverStrategyCatalog.CreateDefaultObservers(dnsHistory));

            var run = await reviver.ReviveAsync(
                profile,
                diagnostic,
                validator,
                cancellationToken: cancellationToken);

            ReviverCancelable = false;
            _lastReviverRun = run;
            HasReviverResult = true;

            if (run.Diagnosis.IsHealthy)
            {
                SetReviverStatus(ResUI.TbReviverHealthy);
                return;
            }

            var candidate = run.RecommendedCandidate;
            if (candidate is null)
            {
                SetReviverStatus(ResUI.TbReviverNoCandidate);
                return;
            }

            var prompt = BuildReviverPromotionPrompt(run, candidate);
            if (!await ShowYesNoInteraction.HandleSafe(prompt))
            {
                SetReviverStatus(ResUI.TbReviverValidatedNotApplied);
                return;
            }

            // Promotion is intentionally not cancellable after user confirmation:
            // the service performs compensating writes and must finish its durable
            // transaction boundary. It always creates a detached child and never
            // overwrites the source or changes the current default here.
            var promotion = new RepairPromotionService(new SqliteRepairPromotionHistoryStore());
            var plan = promotion.Prepare(run.Session, candidate);
            var receipt = await promotion.PromoteAsync(
                _config,
                plan,
                makeDefault: false,
                CancellationToken.None);

            _pendingSelectIndexId = receipt.PromotedProfileId;
            await RefreshServers();
            SetReviverStatus(ResUI.TbReviverPromoted);
        }
        catch (OperationCanceledException)
        {
            SetReviverStatus(ResUI.TbReviverCancelled);
        }
        catch (Exception ex)
        {
            Logging.SaveLog(nameof(ReviveSelectedProfileAsync), ex);
            SetReviverStatus(ResUI.TbReviverAnalysisFailed);
        }
        finally
        {
            _reviverCts = null;
            ReviverCancelable = false;
            ReviverBusy = false;
            _reviverRunGate.Release();
        }
    }

    private async Task ExportReviverSupportBundleAsync()
    {
        var run = _lastReviverRun;
        if (run is null)
        {
            SetReviverStatus(ResUI.TbReviverExportUnavailable);
            return;
        }

        try
        {
            var destination = await SaveSupportBundleInteraction.HandleSafe("pattn-repair-diagnostic.json");
            if (destination.IsNullOrEmpty())
            {
                return;
            }
            if (Path.GetExtension(destination).IsNullOrEmpty())
            {
                destination += ".json";
            }

            await new ReviverSupportBundleBuilder().ExportAsync(destination, run);
            SetReviverStatus(ResUI.TbReviverExported);
        }
        catch (Exception ex)
        {
            Logging.SaveLog(nameof(ExportReviverSupportBundleAsync), ex);
            SetReviverStatus(ResUI.TbReviverAnalysisFailed);
        }
    }

    private void SetReviverStatus(string status)
    {
        ReviverStatus = status;
        ReviverStatusVisible = status.IsNotEmpty();
    }

    private static string BuildReviverPromotionPrompt(RepairRunResult run, RepairCandidate candidate)
    {
        var lines = new List<string>
        {
            string.Format(
                CultureInfo.CurrentCulture,
                ResUI.TbReviverDiagnosisLine,
                HumanizeIdentifier(run.Diagnosis.FailureClass.ToString()))
        };

        if (candidate.Validation is { } validation)
        {
            var latency = validation.MedianLatencyMs is { } latencyMs
                ? string.Format(CultureInfo.CurrentCulture, "{0:0.#} ms", latencyMs)
                : "—";
            var loss = validation.LossRate is { } lossRate
                ? lossRate.ToString("P0", CultureInfo.CurrentCulture)
                : "—";
            lines.Add(string.Format(
                CultureInfo.CurrentCulture,
                ResUI.TbReviverValidationLine,
                validation.Successes,
                validation.Attempts,
                latency,
                loss));
        }

        foreach (var mutation in candidate.Mutations)
        {
            lines.Add("• " + string.Format(
                CultureInfo.CurrentCulture,
                ResUI.TbReviverMutationLine,
                HumanizeIdentifier(mutation.Field),
                mutation.From.IsNullOrEmpty() ? ResUI.TbReviverEmptyValue : mutation.From,
                mutation.To.IsNullOrEmpty() ? ResUI.TbReviverEmptyValue : mutation.To,
                HumanizeIdentifier(mutation.Confidence.ToString())));
        }

        var details = string.Join(Environment.NewLine, lines);
        return string.Format(
                CultureInfo.CurrentCulture,
                ResUI.TbReviverConfirmPromotion,
                details)
            .Replace("\\n", Environment.NewLine, StringComparison.Ordinal);
    }

    private static string HumanizeIdentifier(string value)
    {
        if (value.IsNullOrEmpty())
        {
            return string.Empty;
        }

        var chars = new List<char>(value.Length + 8);
        for (var i = 0; i < value.Length; i++)
        {
            var ch = value[i];
            if (i > 0
                && char.IsUpper(ch)
                && (char.IsLower(value[i - 1]) || char.IsDigit(value[i - 1])))
            {
                chars.Add(' ');
            }
            chars.Add(ch);
        }
        return new string([.. chars]);
    }
}
