using ServiceLib.Manager;
using ServiceLib.Reviver.Models;
using ServiceLib.Reviver.Normalization;

namespace ServiceLib.Reviver.Strategies;

/// <summary>
/// Optional, operator-configured mitigation for an observed in-band upload stall.
/// PattN deliberately does not embed a network-specific fragment recipe: the supplied finalMask is treated
/// as a candidate mutation, not a proven cure, and must pass normal real-core validation before promotion.
/// </summary>
public sealed class UploadStallMitigationStrategy(
    ProfileCoreCompatibility compatibility,
    string finalMaskJson,
    Func<ProfileItem, ECoreType>? coreResolver = null) : IRepairStrategy
{
    private readonly Func<ProfileItem, ECoreType> _coreResolver =
        coreResolver ?? (profile => AppManager.Instance.GetCoreType(profile, profile.ConfigType));

    public string Id => "upload-stall-finalmask";
    public ERepairConfidence Confidence => ERepairConfidence.EvidenceBacked;

    public int PriorityFor(ERepairFailureClass failureClass)
        => failureClass == ERepairFailureClass.UploadStall ? 0 : 1000;

    public bool CanApply(ProfileItem profile, ERepairFailureClass failureClass)
        => PriorityFor(failureClass) < 1000
           && !profile.IsComplex()
           && profile.ConfigType != EConfigType.Outbound
           && _coreResolver(profile) == ECoreType.Xray
           && compatibility.Supports(profile, ECoreType.Xray)
           && TryNormalizeTemplate(finalMaskJson, out var template)
           && !string.Equals(profile.Finalmask, template, StringComparison.Ordinal);

    public async IAsyncEnumerable<RepairCandidate> GenerateAsync(
        RepairSession session,
        ERepairFailureClass failureClass,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var baseline = session.Original.CreateWorkingCopy();
        if (!CanApply(baseline, failureClass)
            || !TryNormalizeTemplate(finalMaskJson, out var template))
        {
            yield break;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var candidate = session.Original.CreateWorkingCopy();
        candidate.Finalmask = template;
        if (!ProfileMutationGuard.ChangesOnly(
                baseline,
                candidate,
                nameof(ProfileItem.Finalmask)))
        {
            throw new InvalidOperationException(
                "Upload-stall mitigation modified fields outside the configured finalMask.");
        }

        yield return new RepairCandidate
        {
            SessionId = session.Id,
            Profile = candidate,
            FailureClassAddressed = failureClass,
            Mutations =
            [
                new RepairMutation
                {
                    Kind = ERepairMutationKind.ApplyNetworkAdaptation,
                    Field = nameof(ProfileItem.Finalmask),
                    From = baseline.Finalmask,
                    To = "[configured-finalmask]",
                    Reason =
                        "An in-band upload probe stalled after application reachability succeeded; try the operator-configured Xray finalMask and require real-core validation before promotion.",
                    Confidence = ERepairConfidence.EvidenceBacked,
                }
            ],
            Evidence =
            [
                new RepairEvidence
                {
                    Kind = "reviver.upload-stall",
                    Summary =
                        "Application reachability succeeded but the configured upload probe timed out; the mitigation template is operator supplied.",
                    Source = nameof(UploadStallMitigationStrategy),
                    Data = new Dictionary<string, string>
                    {
                        ["failureClass"] = ERepairFailureClass.UploadStall.ToString(),
                        ["templateSource"] = "local-config",
                    },
                }
            ],
        };

        await Task.Yield();
    }

    internal static bool TryNormalizeTemplate(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (value.IsNullOrEmpty())
        {
            return false;
        }

        try
        {
            var node = JsonNode.Parse(value!);
            if (node is not JsonObject obj
                || obj["tcp"] is not JsonArray tcp
                || tcp.Count == 0)
            {
                return false;
            }

            normalized = obj.ToJsonString(new JsonSerializerOptions
            {
                WriteIndented = false,
            });
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
