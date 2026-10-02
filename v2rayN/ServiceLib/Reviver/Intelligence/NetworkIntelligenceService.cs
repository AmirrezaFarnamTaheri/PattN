using System.Net;
using System.Security.Cryptography;
using System.Text;
using ServiceLib.Discovery.Protocol;
using ServiceLib.Reviver.Models;
using ServiceLib.Reviver.Normalization;

namespace ServiceLib.Reviver.Intelligence;

/// <summary>
/// Privacy-first reasoning layer above Discovery + Reviver. It deliberately produces probabilistic
/// assessments rather than claiming that a network failure proves censorship or DPI.
/// </summary>
public sealed class NetworkIntelligenceService
{
    /// <summary>
    /// Secret backing the derived local keys. It is injected in tests so that expected digests are
    /// reproducible; in the application it comes from <see cref="PrivacyKeyStore"/> (per-installation,
    /// never shared, never logged).
    /// </summary>
    private readonly byte[] _privacyKey;

    public NetworkIntelligenceService()
        : this(PrivacyKeyStore.GetKey())
    {
    }

    public NetworkIntelligenceService(byte[] privacyKey)
    {
        ArgumentNullException.ThrowIfNull(privacyKey);
        _privacyKey = privacyKey.Length >= 16 ? privacyKey : RandomNumberGenerator.GetBytes(32);
    }

    public FailureAssessment ClassifyUplinkProbe(
        DiscoveryUplinkProbeResult result,
        DateTimeOffset? observedAt = null,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(result);
        return Classify(new NetworkObservation
        {
            TcpSucceeded = result.Connected,
            TlsSucceeded = result.TlsHandshakeSucceeded,
            UploadSucceeded = result.BodyFullyRead,
            // A response on the same HTTP exchange is not independent proof that the remote
            // peer consumed the complete request body. Servers may answer early. Treat this
            // probe as upload/application evidence only; UplinkStall requires a separate
            // downstream-success signal in Classify(NetworkObservation).
            DownstreamSucceeded = null,
            LatencyMs = result.DurationMs,
            ObservedAt = observedAt ?? DateTimeOffset.UtcNow,
        }, now);
    }

    public FailureAssessment Classify(NetworkObservation observation, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(observation);
        var reasons = new List<string>();
        var failure = ERepairFailureClass.Unknown;
        var confidence = 0.35d;

        if (observation.DnsSucceeded == false)
        {
            failure = ERepairFailureClass.DnsResolutionFailure;
            confidence = 0.95d;
            reasons.Add("DNS resolution failed before a transport connection was established.");
        }
        else if (observation.TcpSucceeded == false)
        {
            failure = ERepairFailureClass.NetworkUnreachable;
            confidence = 0.86d;
            reasons.Add("The transport connection could not be established.");
        }
        else if (observation.TcpSucceeded == true && observation.TlsSucceeded == false)
        {
            failure = ERepairFailureClass.TlsHandshakeFailure;
            confidence = 0.86d;
            reasons.Add("TCP succeeded but the TLS handshake did not.");
        }
        else if (observation.TlsSucceeded == true
                 && observation.UploadSucceeded == false
                 && observation.DownstreamSucceeded == true)
        {
            failure = ERepairFailureClass.UplinkStall;
            confidence = 0.90d;
            reasons.Add("Handshake/downstream traffic succeeded while upstream payload delivery stalled.");
            reasons.Add("This pattern is compatible with asymmetric path interference, but does not by itself prove DPI.");
        }
        else if (observation.TlsSucceeded == true
                 && observation.UploadSucceeded == false)
        {
            failure = ERepairFailureClass.ApplicationProbeFailure;
            confidence = 0.72d;
            reasons.Add("The secure connection was established but the application probe could not complete.");
        }
        else if ((observation.PacketLossRate ?? 0d) >= 0.50d)
        {
            failure = ERepairFailureClass.PerformanceDegraded;
            confidence = 0.70d;
            reasons.Add("Observed packet loss is high enough to make the connection unreliable.");
        }
        else if (observation.ResetObserved)
        {
            failure = ERepairFailureClass.IntermittentFailure;
            confidence = 0.62d;
            reasons.Add("Connection resets were observed without enough evidence to attribute a mechanism.");
        }

        if (observation.IPv4Succeeded == false && observation.IPv6Succeeded == true)
        {
            confidence = Math.Min(0.98d, confidence + 0.06d);
            reasons.Add("IPv6 succeeded while IPv4 failed, which localizes the problem to the IPv4 path.");
        }
        else if (observation.IPv6Succeeded == false && observation.IPv4Succeeded == true)
        {
            reasons.Add("IPv4 succeeded while IPv6 failed, which localizes the problem to the IPv6 path.");
        }

        return new FailureAssessment
        {
            FailureClass = failure,
            Confidence = ApplyEvidenceDecay(confidence, observation.ObservedAt, now ?? DateTimeOffset.UtcNow),
            Reasons = reasons,
            ObservedAt = observation.ObservedAt,
        };
    }

    public NetworkFingerprint BuildFingerprint(NetworkObservation observation)
    {
        ArgumentNullException.ThrowIfNull(observation);
        var carrierKey = HashToken(observation.Carrier.Trim().ToLowerInvariant());
        var uploadStall = observation.TlsSucceeded == true
                          && observation.UploadSucceeded == false
                          && observation.DownstreamSucceeded == true;
        var lossPattern = (observation.PacketLossRate ?? 0d) >= 0.35d;
        var tlsInterference = observation.TcpSucceeded == true && observation.TlsSucceeded == false;
        var signalCount = new[] { uploadStall, lossPattern, tlsInterference, observation.ResetObserved }.Count(x => x);
        var dpiConfidence = Math.Clamp(0.20d + signalCount * 0.18d + (uploadStall ? 0.18d : 0d), 0d, 0.90d);

        var keyMaterial = string.Join("|",
            carrierKey,
            observation.Asn.Trim().ToUpperInvariant(),
            observation.CountryCode.Trim().ToUpperInvariant(),
            observation.IPv4Succeeded?.ToString() ?? "?",
            observation.IPv6Succeeded?.ToString() ?? "?",
            uploadStall ? "uplink-stall" : "uplink-ok-or-unknown",
            tlsInterference ? "tls-fail" : "tls-ok-or-unknown");

        return new NetworkFingerprint
        {
            Key = "net:v2:" + HashToken(keyMaterial),
            CarrierKey = carrierKey,
            Asn = observation.Asn.Trim().ToUpperInvariant(),
            CountryCode = observation.CountryCode.Trim().ToUpperInvariant(),
            IPv4Healthy = observation.IPv4Succeeded,
            IPv6Healthy = observation.IPv6Succeeded,
            DpiSignals = new NetworkDpiSignals
            {
                UploadStall = uploadStall,
                TlsInterferenceSuspected = tlsInterference,
                ResetPattern = observation.ResetObserved,
                PacketLossPattern = lossPattern,
                Confidence = dpiConfidence,
            },
            ObservedAt = observation.ObservedAt,
        };
    }

    public ProxyGenome BuildGenome(ProfileItem profile, string provider = "")
    {
        ArgumentNullException.ThrowIfNull(profile);
        var transport = profile.GetTransportExtra();
        var logicalHost = ProfileIdentityResolver.ResolveServerName(profile);
        var addressFamily = IPAddress.TryParse(profile.Address.Trim().Trim('[', ']'), out var address)
            ? address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? "ipv4" : "ipv6"
            : "domain";

        var protocol = profile.ConfigType.ToString();
        var network = profile.GetNetwork();
        var core = profile.CoreType?.ToString() ?? "auto";
        var usesTls = profile.StreamSecurity.IsNotEmpty();
        var usesReality = string.Equals(profile.StreamSecurity, "reality", StringComparison.OrdinalIgnoreCase);
        var hasSni = profile.Sni.IsNotEmpty();
        var hasHost = transport.Host.IsNotEmpty();

        var keyMaterial = string.Join("|",
            protocol, network, core, usesTls, usesReality, hasSni, hasHost, addressFamily,
            provider.Trim().ToLowerInvariant(),
            logicalHost.IsNotEmpty() ? "logical-host-present" : "logical-host-absent");

        return new ProxyGenome
        {
            Key = "genome:v2:" + HashToken(keyMaterial),
            Protocol = protocol,
            Transport = network,
            Core = core,
            UsesTls = usesTls,
            UsesReality = usesReality,
            HasExplicitSni = hasSni,
            HasHttpHost = hasHost,
            AddressFamily = addressFamily,
            Provider = provider.Trim(),
        };
    }

    public RepairExplanation Explain(
        string decision,
        FailureAssessment failure,
        StrategyEffectivenessSummary? effectiveness = null,
        double? candidateScore = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(decision);
        ArgumentNullException.ThrowIfNull(failure);

        var reasons = new List<string>(failure.Reasons);
        var confidenceParts = new List<double> { failure.Confidence };

        if (effectiveness is { Samples: > 0 })
        {
            reasons.Add($"Historical strategy effectiveness: {effectiveness.EffectiveSuccessRate:P0} across {effectiveness.Samples} weighted sample(s).");
            confidenceParts.Add(effectiveness.Confidence);
        }
        if (candidateScore is not null && double.IsFinite(candidateScore.Value))
        {
            reasons.Add($"Runtime candidate score: {Math.Clamp(candidateScore.Value, 0d, 100d):0.#}/100.");
            confidenceParts.Add(Math.Clamp(candidateScore.Value / 100d, 0d, 1d));
        }

        return new RepairExplanation
        {
            Decision = decision,
            Confidence = confidenceParts.Average(),
            Reasons = reasons,
        };
    }

    public ExperimentPlan PlanExperiments(IEnumerable<ExperimentHypothesis> hypotheses, int limit = 3)
    {
        ArgumentNullException.ThrowIfNull(hypotheses);
        if (limit <= 0)
        {
            return new ExperimentPlan();
        }

        var ordered = hypotheses
            .Where(x => x.ExpectedInformationGain > 0d && x.EstimatedCostSeconds >= 0d)
            .OrderByDescending(x => ExperimentUtility(x))
            .ThenBy(x => x.EstimatedCostSeconds)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .Take(limit)
            .ToArray();

        return new ExperimentPlan { OrderedHypotheses = ordered };
    }

    public EndpointLifecycleSnapshot ClassifyEndpointLifecycle(
        double recentSuccessRate,
        double previousSuccessRate,
        bool previouslyDead = false,
        DateTimeOffset? observedAt = null)
    {
        recentSuccessRate = Clamp01(recentSuccessRate);
        previousSuccessRate = Clamp01(previousSuccessRate);

        var stage = recentSuccessRate switch
        {
            >= 0.85d when previouslyDead => EEndpointLifecycleStage.Revived,
            >= 0.85d => EEndpointLifecycleStage.Peak,
            <= 0.10d => EEndpointLifecycleStage.Dead,
            _ when previousSuccessRate - recentSuccessRate >= 0.25d => EEndpointLifecycleStage.Degrading,
            _ when previousSuccessRate <= 0.10d && recentSuccessRate >= 0.50d => EEndpointLifecycleStage.Birth,
            _ => EEndpointLifecycleStage.Unknown,
        };

        return new EndpointLifecycleSnapshot
        {
            Stage = stage,
            RecentSuccessRate = recentSuccessRate,
            PreviousSuccessRate = previousSuccessRate,
            ObservedAt = observedAt ?? DateTimeOffset.UtcNow,
        };
    }

    public CarrierProfile BuildCarrierProfile(IEnumerable<NetworkFingerprint> fingerprints)
    {
        ArgumentNullException.ThrowIfNull(fingerprints);
        var rows = fingerprints.ToArray();
        if (rows.Length == 0)
        {
            throw new ArgumentException("At least one network fingerprint is required.", nameof(fingerprints));
        }

        var carrierKey = rows.GroupBy(x => x.CarrierKey).OrderByDescending(x => x.Count()).First().Key;
        var asn = rows.GroupBy(x => x.Asn).OrderByDescending(x => x.Count()).First().Key;
        var ipv4 = rows.Where(x => x.IPv4Healthy is not null).Select(x => x.IPv4Healthy == true ? 1d : 0d).ToArray();
        var ipv6 = rows.Where(x => x.IPv6Healthy is not null).Select(x => x.IPv6Healthy == true ? 1d : 0d).ToArray();
        var stallRate = rows.Count(x => x.DpiSignals.UploadStall) / (double)rows.Length;

        return new CarrierProfile
        {
            CarrierKey = carrierKey,
            Asn = asn,
            Samples = rows.Length,
            IPv4SuccessRate = ipv4.Length == 0 ? null : ipv4.Average(),
            IPv6SuccessRate = ipv6.Length == 0 ? null : ipv6.Average(),
            UploadStallRate = stallRate,
            Confidence = 1d - Math.Exp(-rows.Length / 8d),
        };
    }

    public IReadOnlyList<IncidentTimelineEvent> BuildTimeline(IEnumerable<IncidentTimelineEvent> events)
        => events.OrderBy(x => x.ObservedAt).ThenBy(x => x.Kind, StringComparer.Ordinal).ToArray();

    public AnonymousIntelligenceRecord BuildAnonymousRecord(
        NetworkFingerprint network,
        ProxyGenome genome,
        FailureAssessment failure,
        string strategyId = "",
        bool? succeeded = null)
    {
        ArgumentNullException.ThrowIfNull(network);
        ArgumentNullException.ThrowIfNull(genome);
        ArgumentNullException.ThrowIfNull(failure);

        return new AnonymousIntelligenceRecord
        {
            NetworkKey = network.Key,
            GenomeKey = genome.Key,
            FailureClass = failure.FailureClass.ToString(),
            Confidence = failure.Confidence,
            StrategyId = strategyId,
            Succeeded = succeeded,
            ObservedAtBucketUnixHours = failure.ObservedAt.ToUnixTimeSeconds() / 3600,
        };
    }

    public IReadOnlyList<IntelligenceGraphEdge> BuildKnowledgeGraph(
        IEnumerable<AnonymousIntelligenceRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);
        return records
            .GroupBy(x => new { x.NetworkKey, x.GenomeKey, x.FailureClass, x.StrategyId, x.Succeeded })
            .SelectMany(group =>
            {
                var weight = group.Count();
                var failureNode = "failure:" + group.Key.FailureClass;
                var environmentNode = group.Key.NetworkKey + "|" + group.Key.GenomeKey;
                var edges = new List<IntelligenceGraphEdge>
                {
                    new()
                    {
                        From = environmentNode,
                        To = failureNode,
                        Relation = "observed",
                        Weight = weight,
                    }
                };
                if (group.Key.StrategyId.IsNotEmpty())
                {
                    edges.Add(new IntelligenceGraphEdge
                    {
                        From = failureNode,
                        To = "strategy:" + group.Key.StrategyId,
                        Relation = group.Key.Succeeded == true ? "recovered-by" : "failed-with",
                        Weight = weight,
                    });
                }
                return edges;
            })
            .OrderByDescending(x => x.Weight)
            .ThenBy(x => x.From, StringComparer.Ordinal)
            .ToArray();
    }

    public FailureRiskPrediction PredictFailure(
        IReadOnlyList<double> recentSuccessRates,
        IReadOnlyList<double> recentLatencyMs)
    {
        var reasons = new List<string>();
        var risk = 0d;
        if (recentSuccessRates.Count >= 2)
        {
            var latest = Clamp01(recentSuccessRates[^1]);
            var baseline = recentSuccessRates.Take(recentSuccessRates.Count - 1).Average(Clamp01);
            var drop = baseline - latest;
            if (drop > 0d)
            {
                risk += Math.Min(0.65d, drop);
                reasons.Add($"Success rate fell by {drop:P0} versus the preceding window.");
            }
        }
        if (recentLatencyMs.Count >= 2)
        {
            var latest = recentLatencyMs[^1];
            var baseline = recentLatencyMs.Take(recentLatencyMs.Count - 1).Where(double.IsFinite).DefaultIfEmpty(latest).Average();
            if (double.IsFinite(latest) && baseline > 0d && latest > baseline * 1.5d)
            {
                risk += 0.25d;
                reasons.Add("Latency is more than 50% above the preceding window.");
            }
        }
        return new FailureRiskPrediction { Risk = Clamp01(risk), Reasons = reasons };
    }

    public static double ApplyEvidenceDecay(
        double confidence,
        DateTimeOffset observedAt,
        DateTimeOffset now,
        double halfLifeDays = 45d)
    {
        if (halfLifeDays <= 0d)
        {
            throw new ArgumentOutOfRangeException(nameof(halfLifeDays));
        }
        var ageDays = Math.Max(0d, (now - observedAt).TotalDays);
        var factor = Math.Pow(0.5d, ageDays / halfLifeDays);
        return Clamp01(confidence) * factor;
    }

    public static string StrategyIdFor(RepairCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        if (candidate.StrategyId.IsNotEmpty())
        {
            return candidate.StrategyId;
        }
        if (candidate.Mutations.Count == 0)
        {
            return "baseline";
        }
        return string.Join("+", candidate.Mutations.Select(x => x.Kind.ToString()).Distinct().OrderBy(x => x, StringComparer.Ordinal));
    }

    // v2 is the keyed derivation. v1 stays readable so history recorded before the key existed is
    // still parseable, but it is legacy: those digests were unsalted and must never be handed to an
    // exchange or an export that assumes the keyed property.
    public static bool IsDerivedNetworkKey(string? value)
        => IsDerivedKey(value, "net:v2:") || IsDerivedKey(value, "net:v1:");

    public static bool IsDerivedGenomeKey(string? value)
        => IsDerivedKey(value, "genome:v2:") || IsDerivedKey(value, "genome:v1:");

    /// <summary>True only for the current, keyed derivation.</summary>
    public static bool IsKeyedDerivedNetworkKey(string? value)
        => IsDerivedKey(value, "net:v2:");

    public static bool IsLocalNetworkKey(string? value)
        => IsDerivedNetworkKey(value)
           || string.Equals(value, "net:unknown", StringComparison.Ordinal);

    public static bool IsSafeStrategyId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128)
        {
            return false;
        }

        return value.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' or '.' or ':' or '+');
    }

    public static bool IsKnownFailureClass(string? value)
        => !string.IsNullOrWhiteSpace(value)
           && Enum.TryParse<ERepairFailureClass>(value, ignoreCase: false, out var parsed)
           && Enum.IsDefined(parsed);

    private static bool IsDerivedKey(string? value, string prefix)
    {
        if (value is null || value.Length != prefix.Length + 24 || !value.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        return value.AsSpan(prefix.Length).ToArray().All(
            ch => ch is >= '0' and <= '9' or >= 'a' and <= 'f');
    }

    private static double ExperimentUtility(ExperimentHypothesis hypothesis)
    {
        var cost = Math.Max(1d, hypothesis.EstimatedCostSeconds);
        return Clamp01(hypothesis.ExpectedInformationGain)
               * (1d - Clamp01(hypothesis.MutationRisk) * 0.75d)
               / Math.Sqrt(cost);
    }

    /// <summary>
    /// Keyed derivation, deliberately *not* a bare hash. The material being keyed (carrier name,
    /// ASN, country code, boolean link signals) has so few possible values that an unsalted digest is
    /// inverted by simply hashing the whole candidate set, and equal digests would identify the same
    /// subscriber across installations. HMAC with the per-installation secret defeats both, and the
    /// digest is truncated only after the MAC (12 bytes of a MAC are not separately attackable).
    /// </summary>
    private string HashToken(string value)
    {
        if (value.IsNullOrEmpty())
        {
            return string.Empty;
        }

        Span<byte> mac = stackalloc byte[32];
        if (!System.Security.Cryptography.HMACSHA256.TryComputeHash(
                _privacyKey,
                Encoding.UTF8.GetBytes(value),
                mac,
                out var written)
            || written != mac.Length)
        {
            // Never fall back to an unkeyed digest: an unavailable key must not silently degrade
            // the only privacy control this layer has.
            return string.Empty;
        }

        return Convert.ToHexString(mac[..12]).ToLowerInvariant();
    }

    private static double Clamp01(double value)
        => double.IsFinite(value) ? Math.Clamp(value, 0d, 1d) : 0d;
}
