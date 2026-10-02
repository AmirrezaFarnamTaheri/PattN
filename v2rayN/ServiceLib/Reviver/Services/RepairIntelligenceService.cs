using ServiceLib.Reviver.Models;

namespace ServiceLib.Reviver.Services;

/// <summary>
/// Local, deterministic intelligence derived only from evidence PattN already owns.
/// No remote service, carrier guess, profile secret, endpoint hostname, or credential is introduced here.
/// </summary>
public static class RepairIntelligenceService
{
    private static readonly TimeSpan DefaultEvidenceHalfLife = TimeSpan.FromDays(90);

    public static RepairFailureAssessment Assess(
        RepairDiagnosis diagnosis,
        DateTimeOffset? now = null,
        TimeSpan? evidenceHalfLife = null)
    {
        ArgumentNullException.ThrowIfNull(diagnosis);
        var observedAt = now ?? DateTimeOffset.UtcNow;
        var halfLife = evidenceHalfLife ?? DefaultEvidenceHalfLife;
        if (halfLife <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(evidenceHalfLife));
        }

        var signals = new HashSet<string>(StringComparer.Ordinal)
        {
            $"failure:{diagnosis.FailureClass}",
        };
        foreach (var evidence in diagnosis.Evidence)
        {
            if (evidence.Kind.IsNotEmpty())
            {
                signals.Add($"evidence:{evidence.Kind}");
            }
        }
        foreach (var address in diagnosis.ResolvedAddresses)
        {
            if (!IPAddress.TryParse(address.Trim('[', ']'), out var parsed))
            {
                continue;
            }

            signals.Add(parsed.AddressFamily == AddressFamily.InterNetwork
                ? "resolved:ipv4"
                : "resolved:ipv6");
        }

        var freshness = EvidenceFreshness(diagnosis.Evidence, observedAt, halfLife);
        double confidence;
        string basis;

        if (diagnosis.IsHealthy)
        {
            confidence = 1d;
            basis = "Runtime validation satisfied the configured health quorum.";
        }
        else if (diagnosis.FailureClass is
                 ERepairFailureClass.ProfileSemanticallyInvalid
                 or ERepairFailureClass.CoreUnsupported
                 or ERepairFailureClass.CoreConfigurationInvalid)
        {
            confidence = 1d;
            basis = "The failure is produced by deterministic local validation.";
        }
        else if (diagnosis.RuntimeValidation is { Attempts: > 0 } runtime)
        {
            var failures = runtime.Failures
                .Where(x => x != ERepairFailureClass.Unknown)
                .ToArray();
            var consistent = failures.Length == 0
                ? 0d
                : failures.Count(x => x == diagnosis.FailureClass) / (double)failures.Length;
            var coverage = Math.Clamp(runtime.Attempts / 3d, 0d, 1d);
            confidence = consistent * (0.5d + 0.5d * coverage) * freshness;
            if (runtime.IntegritySuspect)
            {
                confidence = Math.Min(confidence, 0.25d);
                signals.Add("validation:integrity-suspect");
            }
            basis =
                $"Runtime evidence repeated the selected failure in {failures.Count(x => x == diagnosis.FailureClass)}/{Math.Max(1, failures.Length)} failed attempt(s).";
        }
        else if (diagnosis.FailureClass == ERepairFailureClass.DnsResolutionFailure)
        {
            confidence = 0.90d * freshness;
            basis = "Endpoint preflight produced no usable address for the configured host.";
        }
        else if (diagnosis.Evidence.Count > 0)
        {
            confidence = 0.80d * freshness;
            basis = "Endpoint preflight supplied direct network evidence for the selected failure class.";
        }
        else
        {
            confidence = diagnosis.FailureClass == ERepairFailureClass.Unknown ? 0d : 0.35d;
            basis = "The failure class has limited corroborating evidence.";
        }

        return new RepairFailureAssessment
        {
            FailureClass = diagnosis.FailureClass,
            Confidence = Math.Round(Math.Clamp(confidence, 0d, 1d), 4),
            Basis = basis,
            Signals = signals.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            ObservedAt = observedAt,
        };
    }

    public static ProxyGenome BuildProxyGenome(ProfileItem profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var addressKind = "domain";
        var host = profile.Address?.Trim().Trim('[', ']') ?? string.Empty;
        if (IPAddress.TryParse(host, out var address))
        {
            addressKind = address.AddressFamily == AddressFamily.InterNetwork ? "ipv4" : "ipv6";
        }

        var transport = profile.GetTransportExtra();
        var genome = new ProxyGenome
        {
            Protocol = profile.ConfigType.ToString(),
            Transport = profile.GetNetwork(),
            Core = profile.CoreType?.ToString() ?? string.Empty,
            TlsMode = profile.StreamSecurity ?? string.Empty,
            AddressKind = addressKind,
            TargetStrategy = profile.GetTargetStrategy() ?? Global.AsIs,
            HasExplicitSni = profile.Sni.IsNotEmpty(),
            HasHttpHost = transport.Host.IsNotEmpty(),
            UsesReality = string.Equals(
                profile.StreamSecurity,
                Global.StreamSecurityReality,
                StringComparison.Ordinal),
            UsesEch = profile.EchConfigList.IsNotEmpty(),
            UsesFinalMask = profile.Finalmask.IsNotEmpty(),
            UsesMux = profile.MuxEnabled == true,
        };

        var canonical = string.Join(
            "|",
            genome.Schema,
            genome.Protocol,
            genome.Transport,
            genome.Core,
            genome.TlsMode,
            genome.AddressKind,
            genome.TargetStrategy,
            genome.HasExplicitSni,
            genome.HasHttpHost,
            genome.UsesReality,
            genome.UsesEch,
            genome.UsesFinalMask,
            genome.UsesMux);
        return genome with { Fingerprint = $"genome:v1:{Hash(canonical)}" };
    }

    public static NetworkObservationFingerprint BuildNetworkFingerprint(
        RepairDiagnosis diagnosis,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(diagnosis);
        var providers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var asns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pops = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var signals = new HashSet<string>(StringComparer.Ordinal)
        {
            $"failure:{diagnosis.FailureClass}",
        };
        var hasIPv4 = false;
        var hasIPv6 = false;

        foreach (var raw in diagnosis.ResolvedAddresses)
        {
            if (!IPAddress.TryParse(raw.Trim('[', ']'), out var address))
            {
                continue;
            }
            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                hasIPv4 = true;
            }
            else if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                hasIPv6 = true;
            }
        }

        foreach (var evidence in diagnosis.Evidence)
        {
            if (evidence.Kind.IsNotEmpty())
            {
                signals.Add(evidence.Kind);
            }
            AddEvidenceValue(evidence.Data, "provider", providers);
            AddEvidenceValue(evidence.Data, "asn", asns);
            AddEvidenceValue(evidence.Data, "pop", pops);
        }

        var providerValues = providers.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        var asnValues = asns.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        var popValues = pops.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
        var signalValues = signals.OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var canonical = string.Join(
            "|",
            "network-observation-v1",
            hasIPv4,
            hasIPv6,
            string.Join(",", providerValues),
            string.Join(",", asnValues),
            string.Join(",", popValues),
            string.Join(",", signalValues));

        return new NetworkObservationFingerprint
        {
            HasIPv4 = hasIPv4,
            HasIPv6 = hasIPv6,
            Providers = providerValues,
            Asns = asnValues,
            Pops = popValues,
            Signals = signalValues,
            ObservedAt = now ?? DateTimeOffset.UtcNow,
            Fingerprint = $"network:v1:{Hash(canonical)}",
        };
    }

    public static RepairRecommendationExplanation Explain(
        RepairCandidate candidate,
        RepairFailureAssessment assessment)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(assessment);

        var reasons = new List<string>();
        var risks = new List<string>();
        if (candidate.StrategyId.IsNotEmpty())
        {
            reasons.Add($"strategy:{candidate.StrategyId}");
        }
        if (candidate.Validation is { } validation)
        {
            reasons.Add($"runtime-success:{validation.Successes}/{validation.Attempts}");
            reasons.Add($"consecutive-success:{validation.ConsecutiveSuccesses}");
            if (validation.MedianLatencyMs is { } latency && double.IsFinite(latency))
            {
                reasons.Add($"median-latency-ms:{latency.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}");
            }
            if (validation.IntegritySuspect)
            {
                risks.Add("runtime-evidence-integrity-suspect");
            }
        }
        if (candidate.ScoreBreakdown is { } score)
        {
            reasons.Add($"reliability:{score.Reliability.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}");
            reasons.Add($"mutation-safety:{score.MutationSafety.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)}");
        }

        reasons.Add($"failure-confidence:{assessment.Confidence.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture)}");
        reasons.Add($"declared-mutations:{candidate.Mutations.Count}");

        if (assessment.Confidence < 0.60d)
        {
            risks.Add("failure-classification-low-confidence");
        }
        if (candidate.Mutations.Any(x => x.Confidence == ERepairConfidence.Speculative))
        {
            risks.Add("contains-speculative-mutation");
        }
        if (candidate.Mutations.Count > 1)
        {
            risks.Add("multi-field-repair");
        }

        return new RepairRecommendationExplanation
        {
            CandidateId = candidate.Id,
            StrategyId = candidate.StrategyId,
            Score = candidate.Score,
            Reasons = reasons,
            Risks = risks.Distinct(StringComparer.Ordinal).ToArray(),
        };
    }

    private static double EvidenceFreshness(
        IReadOnlyList<RepairEvidence> evidence,
        DateTimeOffset now,
        TimeSpan halfLife)
    {
        if (evidence.Count == 0)
        {
            return 1d;
        }

        var total = 0d;
        foreach (var item in evidence)
        {
            var age = now - item.ObservedAt;
            var days = Math.Max(0d, age.TotalDays);
            total += Math.Pow(0.5d, days / halfLife.TotalDays);
        }
        return Math.Clamp(total / evidence.Count, 0d, 1d);
    }

    private static void AddEvidenceValue(
        IReadOnlyDictionary<string, string> data,
        string key,
        HashSet<string> values)
    {
        if (data.TryGetValue(key, out var value) && value.IsNotEmpty())
        {
            values.Add(value.Trim());
        }
    }

    private static string Hash(string value)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))
            .ToLowerInvariant();
}
