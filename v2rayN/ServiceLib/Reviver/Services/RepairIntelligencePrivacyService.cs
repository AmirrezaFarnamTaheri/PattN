using ServiceLib.Reviver.Models;

namespace ServiceLib.Reviver.Services;

/// <summary>
/// Constructs the only payload shape approved for any future intelligence sharing.
/// LocalOnly is the default and returns no payload at all.
/// </summary>
public static class RepairIntelligencePrivacyService
{
    public static AnonymousRepairLearningEvent? BuildLearningEvent(
        IntelligencePrivacyPolicy policy,
        ProxyGenome genome,
        NetworkObservationFingerprint network,
        RepairFailureAssessment assessment,
        string? strategyId,
        string? outcomeVerdict,
        bool humanConfirmed,
        DateTimeOffset? observedAt = null)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(genome);
        ArgumentNullException.ThrowIfNull(network);
        ArgumentNullException.ThrowIfNull(assessment);

        if (policy.SharingMode != EIntelligenceSharingMode.AnonymousAggregate)
        {
            return null;
        }

        var observed = observedAt ?? DateTimeOffset.UtcNow;
        return new AnonymousRepairLearningEvent
        {
            ProxyGenomeFingerprint = genome.Fingerprint,
            NetworkFingerprint = policy.IncludeNetworkFingerprint
                ? network.Fingerprint
                : string.Empty,
            FailureClass = assessment.FailureClass,
            FailureConfidenceBucket = Math.Round(
                Math.Clamp(assessment.Confidence, 0d, 1d) * 10d,
                MidpointRounding.AwayFromZero) / 10d,
            StrategyId = SafeLabel(strategyId),
            OutcomeVerdict = NormalizeOutcome(outcomeVerdict),
            HumanConfirmed = humanConfirmed,
            ObservedDayUtc = DateOnly.FromDateTime(observed.UtcDateTime),
        };
    }

    private static string NormalizeOutcome(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        return normalized is "improved" or "stable" or "regressed"
            ? normalized
            : "unknown";
    }

    private static string SafeLabel(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalized.Length is > 0 and <= 64
            && normalized.All(ch => char.IsAsciiLetterOrDigit(ch) || ch is '-' or '_' or '.'))
        {
            return normalized;
        }

        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        var digest = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(normalized)))
            .ToLowerInvariant();
        return $"custom:{digest[..16]}";
    }
}
