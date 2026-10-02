using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ServiceLib.Reviver.Models;

namespace ServiceLib.Reviver.Intelligence;

public sealed class NetworkAutomationService
{
    public IReadOnlyList<RepairCandidate> RemoveConflictingCandidates(IEnumerable<RepairCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var selected = new List<RepairCandidate>();
        var signatures = new HashSet<string>(StringComparer.Ordinal);

        foreach (var candidate in candidates)
        {
            var signature = MutationConflictSignature(candidate.Mutations);
            if (signatures.Add(signature))
            {
                selected.Add(candidate);
            }
        }

        return selected;
    }

    public CompatibleStrategyPlan PlanCompatibleCandidates(IEnumerable<RepairCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        var selected = new List<string>();
        var conflicts = new List<StrategyConflict>();
        var fieldTargets = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var candidate in candidates
                     .OrderByDescending(x => x.Score ?? 0d)
                     .ThenBy(x => x.Mutations.Count)
                     .ThenBy(x => x.Id, StringComparer.Ordinal))
        {
            StrategyConflict? conflict = null;
            foreach (var mutation in candidate.Mutations)
            {
                if (mutation.Field.IsNullOrEmpty())
                {
                    continue;
                }
                var target = mutation.To ?? string.Empty;
                if (fieldTargets.TryGetValue(mutation.Field, out var existing)
                    && !string.Equals(existing, target, StringComparison.Ordinal))
                {
                    conflict = new StrategyConflict
                    {
                        CandidateId = candidate.Id,
                        Field = mutation.Field,
                        ExistingTarget = existing,
                        ConflictingTarget = target,
                    };
                    break;
                }
            }

            if (conflict is not null)
            {
                conflicts.Add(conflict);
                continue;
            }

            selected.Add(candidate.Id);
            foreach (var mutation in candidate.Mutations)
            {
                if (mutation.Field.IsNotEmpty())
                {
                    fieldTargets.TryAdd(mutation.Field, mutation.To ?? string.Empty);
                }
            }
        }

        return new CompatibleStrategyPlan
        {
            SelectedCandidateIds = selected,
            Conflicts = conflicts,
        };
    }

    public IntelligenceShareBatch PrepareShareBatch(
        IEnumerable<AnonymousIntelligenceRecord> records,
        IntelligenceSharingPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(policy);

        if (!policy.ExplicitOptIn || policy.Mode != EIntelligencePrivacyMode.AnonymousAggregate)
        {
            throw new InvalidOperationException(
                "External intelligence sharing requires explicit opt-in and AnonymousAggregate privacy mode.");
        }
        if (policy.MinimumAggregateSamples is < 2 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(policy.MinimumAggregateSamples));
        }

        var grouped = records
            .GroupBy(x => new
            {
                x.NetworkKey,
                x.GenomeKey,
                x.FailureClass,
                x.StrategyId,
                x.Succeeded,
                x.ObservedAtBucketUnixHours,
            })
            .Where(x => x.Count() >= policy.MinimumAggregateSamples)
            .Select(group => new AnonymousIntelligenceRecord
            {
                NetworkKey = group.Key.NetworkKey,
                GenomeKey = group.Key.GenomeKey,
                FailureClass = group.Key.FailureClass,
                StrategyId = group.Key.StrategyId,
                Succeeded = group.Key.Succeeded,
                ObservedAtBucketUnixHours = group.Key.ObservedAtBucketUnixHours,
                Confidence = group.Average(x => x.Confidence),
            })
            .ToArray();

        return new IntelligenceShareBatch { Records = grouped };
    }

    public ShadowTestDecision EvaluateShadow(
        IReadOnlyList<ShadowSample> baseline,
        IReadOnlyList<ShadowSample> candidate,
        double minimumImprovement = 0.08d,
        int minimumSamples = 3)
    {
        if (baseline.Count < minimumSamples || candidate.Count < minimumSamples)
        {
            return new ShadowTestDecision { Reason = "Not enough shadow samples to make a promotion decision." };
        }

        var baselineScore = ShadowScore(baseline);
        var candidateScore = ShadowScore(candidate);
        var improvement = candidateScore - baselineScore;
        return new ShadowTestDecision
        {
            PromoteCandidate = improvement >= minimumImprovement,
            BaselineScore = baselineScore,
            CandidateScore = candidateScore,
            Improvement = improvement,
            Reason = improvement >= minimumImprovement
                ? "Candidate exceeded the configured canary improvement margin."
                : "Candidate did not clear the canary improvement margin.",
        };
    }

    public FleetHealthSnapshot BuildFleetHealth(
        IEnumerable<(ProxyGenome Genome, double SuccessRate)> proxies,
        double healthyThreshold = 0.80d,
        double deadThreshold = 0.10d)
    {
        var rows = proxies.ToArray();
        var healthy = rows.Count(x => x.SuccessRate >= healthyThreshold);
        var dead = rows.Count(x => x.SuccessRate <= deadThreshold);
        var degraded = rows.Length - healthy - dead;
        var unique = rows.Select(x => x.Genome.Key).Distinct(StringComparer.Ordinal).Count();

        return new FleetHealthSnapshot
        {
            Total = rows.Length,
            Healthy = healthy,
            Degraded = degraded,
            Dead = dead,
            UniqueGenomes = unique,
            DuplicateGenomes = Math.Max(0, rows.Length - unique),
        };
    }

    public OptimizationDecision ChooseOptimization(
        IEnumerable<OptimizationOption> options,
        double autoApplyConfidenceThreshold = 0.85d,
        double maximumMutationRisk = 0.30d)
    {
        var ranked = options
            .Select(x => new
            {
                Option = x,
                Score = Math.Clamp(x.RuntimeScore / 100d, 0d, 1d) * 0.50d
                        + Math.Clamp(x.LearnedSuccessRate, 0d, 1d) * 0.30d
                        + Math.Clamp(x.EvidenceConfidence, 0d, 1d) * 0.20d
                        - Math.Clamp(x.MutationRisk, 0d, 1d) * 0.30d,
            })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Option.Id, StringComparer.Ordinal)
            .ToArray();

        if (ranked.Length == 0)
        {
            return new OptimizationDecision { Reason = "No optimization options were supplied." };
        }

        var best = ranked[0];
        var allowed = best.Option.EvidenceConfidence >= autoApplyConfidenceThreshold
                      && best.Option.MutationRisk <= maximumMutationRisk
                      && best.Score > 0d;

        return new OptimizationDecision
        {
            SelectedId = best.Option.Id,
            Score = best.Score,
            AutoApplyAllowed = allowed,
            Reason = allowed
                ? "Best option satisfies the configured confidence and mutation-risk gates."
                : "Recommendation is informational only because automatic-action safety gates were not met.",
        };
    }

    public ArchitectureHealthSnapshot BuildArchitectureHealth(
        bool codeTestsPassing,
        bool packagingTestsPassing,
        bool durabilityTestsPassing,
        int knownSecurityFindings,
        int staleDependencies)
    {
        var score = 100d;
        if (!codeTestsPassing) score -= 35d;
        if (!packagingTestsPassing) score -= 30d;
        if (!durabilityTestsPassing) score -= 20d;
        score -= Math.Min(10, Math.Max(0, knownSecurityFindings)) * 2d;
        score -= Math.Min(10, Math.Max(0, staleDependencies)) * 0.8d;

        return new ArchitectureHealthSnapshot
        {
            CodeTestsPassing = codeTestsPassing,
            PackagingTestsPassing = packagingTestsPassing,
            DurabilityTestsPassing = durabilityTestsPassing,
            KnownSecurityFindings = Math.Max(0, knownSecurityFindings),
            StaleDependencies = Math.Max(0, staleDependencies),
            Score = Math.Clamp(score, 0d, 100d),
        };
    }

    public ReleaseSecurityAssessment AssessReleaseSecurity(
        IEnumerable<ReleaseComponentSecurity> components,
        bool requireSignatures = true)
    {
        var rows = components.ToArray();
        return new ReleaseSecurityAssessment
        {
            Components = rows,
            AllDigestsVerified = rows.Length > 0 && rows.All(x => x.DigestVerified),
            AllRequiredSignaturesVerified = !requireSignatures || (rows.Length > 0 && rows.All(x => x.SignatureVerified)),
            ProvenanceComplete = rows.Length > 0 && rows.All(x => x.ProvenanceVerified),
        };
    }

    public UpdateManifestVerification VerifyUpdateManifest(
        SignedUpdateManifest manifest,
        ReadOnlySpan<byte> artifactBytes,
        ReadOnlySpan<byte> subjectPublicKeyInfo,
        string currentVersion,
        TimeSpan maximumManifestAge)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        if (!TryParsePattNVersion(manifest.Version, out var target)
            || !TryParsePattNVersion(currentVersion, out var current)
            || !TryParsePattNVersion(manifest.MinimumVersion, out var minimum))
        {
            return new UpdateManifestVerification { Error = "Manifest or current version is invalid." };
        }
        if (target.CompareTo(current) < 0)
        {
            return new UpdateManifestVerification { Error = "Update manifest would downgrade the application." };
        }
        if (target.CompareTo(minimum) < 0)
        {
            return new UpdateManifestVerification { Error = "Target version is below the manifest's minimum supported version." };
        }
        if (current.CompareTo(minimum) < 0)
        {
            return new UpdateManifestVerification { Error = "Current application is below the manifest's minimum supported version." };
        }
        if (maximumManifestAge <= TimeSpan.Zero)
        {
            return new UpdateManifestVerification { Error = "Maximum manifest age must be positive." };
        }

        DateTimeOffset published;
        try
        {
            published = DateTimeOffset.FromUnixTimeSeconds(manifest.PublishedAtUnixSeconds);
        }
        catch (ArgumentOutOfRangeException)
        {
            return new UpdateManifestVerification { Error = "Manifest publication time is invalid." };
        }
        var age = DateTimeOffset.UtcNow - published;
        if (age < TimeSpan.FromMinutes(-5) || age > maximumManifestAge)
        {
            return new UpdateManifestVerification { Error = "Update manifest publication time is outside the accepted freshness window." };
        }

        var actualHash = Convert.ToHexString(SHA256.HashData(artifactBytes)).ToLowerInvariant();
        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(actualHash),
                Encoding.ASCII.GetBytes(manifest.ArtifactSha256.Trim().ToLowerInvariant())))
        {
            return new UpdateManifestVerification { Error = "Artifact SHA-256 does not match the signed manifest." };
        }

        byte[] signature;
        try
        {
            signature = Convert.FromBase64String(manifest.SignatureBase64);
        }
        catch (FormatException)
        {
            return new UpdateManifestVerification { Error = "Manifest signature is not valid base64." };
        }

        var payload = BuildManifestPayload(manifest);
        try
        {
            using var ecdsa = ECDsa.Create();
            ecdsa.ImportSubjectPublicKeyInfo(subjectPublicKeyInfo, out _);
            var valid = ecdsa.VerifyData(
                Encoding.UTF8.GetBytes(payload),
                signature,
                HashAlgorithmName.SHA256);
            return valid
                ? new UpdateManifestVerification { Valid = true }
                : new UpdateManifestVerification { Error = "Manifest signature verification failed." };
        }
        catch (CryptographicException ex)
        {
            return new UpdateManifestVerification { Error = $"Manifest public key is invalid: {ex.Message}" };
        }
    }

    public IReadOnlyList<HeatmapCell> BuildHeatmap(IEnumerable<NetworkFingerprint> fingerprints)
    {
        return fingerprints
            .GroupBy(x => new { x.CountryCode, x.CarrierKey })
            .Select(group =>
            {
                var v4 = group.Where(x => x.IPv4Healthy is not null).ToArray();
                var v6 = group.Where(x => x.IPv6Healthy is not null).ToArray();
                var samples = group.Count();
                return new HeatmapCell
                {
                    CountryCode = group.Key.CountryCode,
                    CarrierKey = group.Key.CarrierKey,
                    Samples = samples,
                    IPv4SuccessRate = v4.Length == 0 ? 0d : v4.Count(x => x.IPv4Healthy == true) / (double)v4.Length,
                    IPv6SuccessRate = v6.Length == 0 ? 0d : v6.Count(x => x.IPv6Healthy == true) / (double)v6.Length,
                    UploadStallRate = group.Count(x => x.DpiSignals.UploadStall) / (double)samples,
                };
            })
            .OrderByDescending(x => x.Samples)
            .ThenBy(x => x.CountryCode, StringComparer.Ordinal)
            .ToArray();
    }

    public byte[] EncryptSyncPayload(
        IReadOnlyList<AnonymousIntelligenceRecord> records,
        ReadOnlySpan<byte> key)
    {
        if (key.Length != 32)
        {
            throw new ArgumentException("Sync encryption requires a 256-bit key.", nameof(key));
        }

        var plaintext = JsonSerializer.SerializeToUtf8Bytes(records);
        var nonce = RandomNumberGenerator.GetBytes(12);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using (var aes = new AesGcm(key, tag.Length))
        {
            aes.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        var output = new byte[1 + nonce.Length + tag.Length + ciphertext.Length];
        output[0] = 1;
        nonce.CopyTo(output.AsSpan(1, nonce.Length));
        tag.CopyTo(output.AsSpan(1 + nonce.Length, tag.Length));
        ciphertext.CopyTo(output.AsSpan(1 + nonce.Length + tag.Length));
        return output;
    }

    public IReadOnlyList<AnonymousIntelligenceRecord> DecryptSyncPayload(
        ReadOnlySpan<byte> payload,
        ReadOnlySpan<byte> key)
    {
        if (key.Length != 32)
        {
            throw new ArgumentException("Sync encryption requires a 256-bit key.", nameof(key));
        }
        if (payload.Length < 30 || payload[0] != 1)
        {
            throw new InvalidDataException("Unsupported or truncated intelligence sync payload.");
        }

        var nonce = payload.Slice(1, 12);
        var tag = payload.Slice(13, 16);
        var ciphertext = payload.Slice(29);
        var plaintext = new byte[ciphertext.Length];
        using (var aes = new AesGcm(key, tag.Length))
        {
            aes.Decrypt(nonce, ciphertext, tag, plaintext);
        }

        return JsonSerializer.Deserialize<List<AnonymousIntelligenceRecord>>(plaintext) ?? [];
    }

    public static string BuildManifestPayload(SignedUpdateManifest manifest)
        => string.Join("\n",
            manifest.Version.Trim(),
            manifest.ArtifactSha256.Trim().ToLowerInvariant(),
            manifest.MinimumVersion.Trim(),
            manifest.PublishedAtUnixSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture));

    private static string MutationConflictSignature(IReadOnlyList<RepairMutation> mutations)
    {
        var fieldTargets = mutations
            .GroupBy(x => x.Field, StringComparer.Ordinal)
            .Select(group => group.Key + "=" + string.Join(",", group.Select(x => x.To ?? string.Empty).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal)))
            .OrderBy(x => x, StringComparer.Ordinal);
        return string.Join("|", fieldTargets);
    }

    private static double ShadowScore(IReadOnlyList<ShadowSample> samples)
    {
        var successRate = samples.Count(x => x.Success) / (double)samples.Count;
        var latency = samples.Where(x => x.Success && x.LatencyMs is { } v && double.IsFinite(v) && v >= 0d)
            .Select(x => x.LatencyMs!.Value)
            .ToArray();
        var latencyQuality = latency.Length == 0 ? 0.5d : 1d / (1d + latency.Average() / 100d);
        return successRate * 0.8d + latencyQuality * 0.2d;
    }

    private static bool TryParsePattNVersion(string value, out PattNVersion version)
    {
        version = default;
        if (value.IsNullOrEmpty())
        {
            return false;
        }

        var clean = value.Trim().TrimStart('v', 'V');
        var parts = clean.Split('-', 2, StringSplitOptions.None);
        var numeric = parts[0].Split('.', StringSplitOptions.None);
        if (numeric.Length != 3
            || numeric.Any(x => x.Length == 0 || x.Any(ch => !char.IsAsciiDigit(ch)))
            || !int.TryParse(numeric[0], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var major)
            || !int.TryParse(numeric[1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var minor)
            || !int.TryParse(numeric[2], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var patch))
        {
            return false;
        }

        var pattnRevision = 0;
        if (parts.Length == 2)
        {
            var suffix = parts[1];
            if (suffix.Length < 2
                || suffix[0] is not ('P' or 'p')
                || !int.TryParse(
                    suffix.AsSpan(1),
                    System.Globalization.NumberStyles.None,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out pattnRevision))
            {
                return false;
            }
        }

        version = new PattNVersion(major, minor, patch, pattnRevision);
        return true;
    }

    private readonly record struct PattNVersion(int Major, int Minor, int Patch, int PattNRevision)
        : IComparable<PattNVersion>
    {
        public int CompareTo(PattNVersion other)
        {
            var value = Major.CompareTo(other.Major);
            if (value != 0) return value;
            value = Minor.CompareTo(other.Minor);
            if (value != 0) return value;
            value = Patch.CompareTo(other.Patch);
            return value != 0 ? value : PattNRevision.CompareTo(other.PattNRevision);
        }
    }
}
