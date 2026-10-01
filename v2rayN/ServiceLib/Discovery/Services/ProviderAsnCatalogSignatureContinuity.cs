using ServiceLib.Discovery.Models;
using ServiceLib.Models.Entities;

namespace ServiceLib.Discovery.Services;

/// <summary>
/// Enforces continuity after a remote catalog signature has already been cryptographically verified.
/// This prevents replaying an older accepted artifact or presenting different content at the same
/// signing instant without changing the locally trusted source configuration.
/// </summary>
public static class ProviderAsnCatalogSignatureContinuity
{
    public static readonly TimeSpan MaximumFutureClockSkew = TimeSpan.FromMinutes(10);
    public static readonly TimeSpan MaximumFirstAcceptanceAge = TimeSpan.FromDays(180);

    public static void Validate(
        ProviderAsnCatalogRemoteSourceItem source,
        ProviderAsnCatalogSignatureValidation validation,
        DateTimeOffset checkedAt)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(validation);

        if (!validation.Attempted || !validation.Valid)
        {
            return;
        }
        if (validation.Revision is not long revision || revision <= 0)
        {
            throw new InvalidOperationException(
                "A valid provider catalog signature is missing its positive monotonic revision.");
        }
        if (validation.SignedAt is not DateTimeOffset signedAt)
        {
            throw new InvalidOperationException(
                "A valid provider catalog signature is missing its signing timestamp.");
        }
        if (validation.ExpiresAt is not DateTimeOffset expiresAt)
        {
            throw new InvalidOperationException(
                "A valid provider catalog signature is missing its expiry timestamp.");
        }
        if (signedAt > checkedAt + MaximumFutureClockSkew)
        {
            throw new InvalidOperationException(
                "Provider catalog signature timestamp is too far in the future.");
        }
        if (expiresAt <= checkedAt)
        {
            throw new InvalidOperationException(
                "Provider catalog signature has expired.");
        }

        // This high-water mark is intentionally independent from the currently configured
        // URI, TLS pins, and signing key. A trust-source or key rotation must not create a
        // fresh rollback epoch in which an older signed catalog can become current again.
        var highWatermark = source.SignatureRevisionHighWatermark;
        if (highWatermark > 0)
        {
            if (revision < highWatermark)
            {
                throw new InvalidOperationException(
                    $"Provider catalog signature revision {revision} is below the accepted high-water mark {highWatermark}; refusing rollback.");
            }
            if (revision == highWatermark
                && !source.SignatureRevisionHighWatermarkCatalogSha256.IsNullOrEmpty()
                && !string.Equals(
                    source.SignatureRevisionHighWatermarkCatalogSha256,
                    validation.CatalogSha256,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Provider catalog signature reuses the accepted revision for different content; refusing equivocation.");
            }
        }

        if (source.LastSignatureSignedAtUnixMs is not long previousUnixMs)
        {
            if (signedAt < checkedAt - MaximumFirstAcceptanceAge)
            {
                throw new InvalidOperationException(
                    $"Provider catalog signature is older than the {MaximumFirstAcceptanceAge.TotalDays:0}-day first-acceptance window; refusing stale trust baseline.");
            }
            return;
        }

        var previousSignedAt = DateTimeOffset.FromUnixTimeMilliseconds(previousUnixMs);
        if (signedAt < previousSignedAt && revision <= Math.Max(highWatermark, source.LastSignatureRevision ?? 0))
        {
            throw new InvalidOperationException(
                "Provider catalog signature is older than the last accepted signed artifact at the same or lower revision; refusing replay/downgrade.");
        }
        if (signedAt == previousSignedAt
            && !source.LastSignatureCatalogSha256.IsNullOrEmpty()
            && !string.Equals(
                source.LastSignatureCatalogSha256,
                validation.CatalogSha256,
                StringComparison.OrdinalIgnoreCase)
            && revision <= Math.Max(highWatermark, source.LastSignatureRevision ?? 0))
        {
            throw new InvalidOperationException(
                "Provider catalog signature reuses an accepted signing timestamp for different content at the same or lower revision; refusing ambiguous replay/equivocation.");
        }
    }
}
