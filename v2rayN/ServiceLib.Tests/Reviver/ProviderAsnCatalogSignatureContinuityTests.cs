using ServiceLib.Discovery.Models;
using ServiceLib.Discovery.Services;
using ServiceLib.Models.Entities;

namespace ServiceLib.Tests.Reviver;

public class ProviderAsnCatalogSignatureContinuityTests
{
    [Test]
    public async Task Validate_ShouldRejectOlderAcceptedSignature()
    {
        var source = Source("2026-09-24T10:00:00Z", new string('a', 64));
        var validation = Valid("2026-09-24T09:00:00Z", new string('b', 64));

        var threw = false;
        try
        {
            ProviderAsnCatalogSignatureContinuity.Validate(
                source,
                validation,
                DateTimeOffset.Parse("2026-09-24T11:00:00Z"));
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("older than the last accepted", StringComparison.OrdinalIgnoreCase);
        }

        await threw.Should().BeTrue();
    }

    [Test]
    public async Task Validate_ShouldRejectDifferentContentAtSameAcceptedSignatureTime()
    {
        var source = Source("2026-09-24T10:00:00Z", new string('a', 64));
        var validation = Valid("2026-09-24T10:00:00Z", new string('b', 64));

        var threw = false;
        try
        {
            ProviderAsnCatalogSignatureContinuity.Validate(
                source,
                validation,
                DateTimeOffset.Parse("2026-09-24T11:00:00Z"));
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("different content", StringComparison.OrdinalIgnoreCase);
        }

        await threw.Should().BeTrue();
    }

    [Test]
    public async Task Validate_ShouldRejectSignatureTooFarInFuture()
    {
        var source = new ProviderAsnCatalogRemoteSourceItem();
        var validation = Valid("2026-09-24T10:11:00Z", new string('a', 64));

        var threw = false;
        try
        {
            ProviderAsnCatalogSignatureContinuity.Validate(
                source,
                validation,
                DateTimeOffset.Parse("2026-09-24T10:00:00Z"));
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("too far in the future", StringComparison.OrdinalIgnoreCase);
        }

        await threw.Should().BeTrue();
    }

    [Test]
    public async Task Validate_ShouldRejectStaleFirstAcceptedSignature()
    {
        var source = new ProviderAsnCatalogRemoteSourceItem();
        var checkedAt = DateTimeOffset.Parse("2026-09-24T10:00:00Z");
        var validation = Valid(
            checkedAt.Subtract(ProviderAsnCatalogSignatureContinuity.MaximumFirstAcceptanceAge).AddMinutes(-1).ToString("O"),
            new string('a', 64));

        var threw = false;
        try
        {
            ProviderAsnCatalogSignatureContinuity.Validate(source, validation, checkedAt);
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("first-acceptance window", StringComparison.OrdinalIgnoreCase);
        }

        await threw.Should().BeTrue();
    }

    [Test]
    public async Task Validate_ShouldAllowRecentFirstAcceptedSignature()
    {
        var source = new ProviderAsnCatalogRemoteSourceItem();
        var checkedAt = DateTimeOffset.Parse("2026-09-24T10:00:00Z");
        var validation = Valid(
            checkedAt.Subtract(ProviderAsnCatalogSignatureContinuity.MaximumFirstAcceptanceAge).AddMinutes(1).ToString("O"),
            new string('a', 64));

        ProviderAsnCatalogSignatureContinuity.Validate(source, validation, checkedAt);

        await Task.CompletedTask;
    }

    [Test]
    public async Task Validate_ShouldRejectExpiredSignature()
    {
        var checkedAt = DateTimeOffset.Parse("2026-09-24T10:00:00Z");
        var validation = Valid(
            "2026-09-24T09:00:00Z",
            new string('a', 64),
            revision: 1,
            expiresAt: checkedAt.AddSeconds(-1));

        var threw = false;
        try
        {
            ProviderAsnCatalogSignatureContinuity.Validate(
                new ProviderAsnCatalogRemoteSourceItem(),
                validation,
                checkedAt);
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("expired", StringComparison.OrdinalIgnoreCase);
        }

        await threw.Should().BeTrue();
    }

    [Test]
    public async Task Validate_ShouldRejectLowerRevisionAfterTrustReconfiguration()
    {
        var source = new ProviderAsnCatalogRemoteSourceItem
        {
            SignatureRevisionHighWatermark = 10,
            SignatureRevisionHighWatermarkCatalogSha256 = new string('a', 64),
        };
        var validation = Valid(
            "2026-09-24T12:00:00Z",
            new string('b', 64),
            revision: 9);

        var threw = false;
        try
        {
            ProviderAsnCatalogSignatureContinuity.Validate(
                source,
                validation,
                DateTimeOffset.Parse("2026-09-24T13:00:00Z"));
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("high-water", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("rollback", StringComparison.OrdinalIgnoreCase);
        }

        await threw.Should().BeTrue();
    }

    [Test]
    public async Task Validate_ShouldAllowSameRevisionOnlyForSameContent()
    {
        var hash = new string('a', 64);
        var source = new ProviderAsnCatalogRemoteSourceItem
        {
            SignatureRevisionHighWatermark = 10,
            SignatureRevisionHighWatermarkCatalogSha256 = hash,
        };
        var checkedAt = DateTimeOffset.Parse("2026-09-24T13:00:00Z");

        ProviderAsnCatalogSignatureContinuity.Validate(
            source,
            Valid("2026-09-24T12:00:00Z", hash, revision: 10),
            checkedAt);

        var threw = false;
        try
        {
            ProviderAsnCatalogSignatureContinuity.Validate(
                source,
                Valid("2026-09-24T12:01:00Z", new string('b', 64), revision: 10),
                checkedAt);
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("different content", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("equivocation", StringComparison.OrdinalIgnoreCase);
        }

        await threw.Should().BeTrue();
    }

    [Test]
    public async Task Validate_ShouldAllowForwardSignedArtifact()
    {
        var source = Source("2026-09-24T09:00:00Z", new string('a', 64));
        var validation = Valid("2026-09-24T10:00:00Z", new string('b', 64));

        ProviderAsnCatalogSignatureContinuity.Validate(
            source,
            validation,
            DateTimeOffset.Parse("2026-09-24T11:00:00Z"));

        await Task.CompletedTask;
    }

    private static ProviderAsnCatalogRemoteSourceItem Source(string signedAt, string sha256)
        => new()
        {
            LastSignatureSignedAtUnixMs = DateTimeOffset.Parse(signedAt).ToUnixTimeMilliseconds(),
            LastSignatureCatalogSha256 = sha256,
            LastSignatureRevision = 1,
        };

    private static ProviderAsnCatalogSignatureValidation Valid(
        string signedAt,
        string sha256,
        long revision = 1,
        DateTimeOffset? expiresAt = null)
    {
        var parsedSignedAt = DateTimeOffset.Parse(signedAt);
        return new ProviderAsnCatalogSignatureValidation
        {
            Attempted = true,
            Valid = true,
            PolicySatisfied = true,
            CatalogSha256 = sha256,
            Revision = revision,
            SignedAt = parsedSignedAt,
            ExpiresAt = expiresAt ?? parsedSignedAt.AddDays(400),
        };
    }
}
