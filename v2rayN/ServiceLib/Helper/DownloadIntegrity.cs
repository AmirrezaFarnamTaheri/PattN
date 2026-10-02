namespace ServiceLib.Helper;

/// <summary>
/// Integrity helpers shared by runtime downloads and updater metadata binding.
/// </summary>
public static class DownloadIntegrity
{
    public static bool TryNormalizeSha256(string? digest, out string sha256)
    {
        sha256 = string.Empty;
        if (digest.IsNullOrEmpty())
        {
            return false;
        }

        var value = digest!.Trim();
        const string prefix = "sha256:";
        if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            value = value[prefix.Length..];
        }

        if (value.Length != 64 || value.Any(ch => !Uri.IsHexDigit(ch)))
        {
            return false;
        }

        sha256 = value.ToLowerInvariant();
        return true;
    }

    public static bool TryParseGitHubReleaseDownloadUrl(
        string? value,
        out string owner,
        out string repository,
        out string tag,
        out string assetName)
    {
        owner = string.Empty;
        repository = string.Empty;
        tag = string.Empty;
        assetName = string.Empty;

        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(uri.Host, "github.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var segments = uri.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length < 6
            || !string.Equals(segments[2], "releases", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(segments[3], "download", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        owner = Uri.UnescapeDataString(segments[0]);
        repository = Uri.UnescapeDataString(segments[1]);
        tag = Uri.UnescapeDataString(segments[4]);
        assetName = string.Join(
            "/",
            segments.Skip(5).Select(Uri.UnescapeDataString));

        return owner.IsNotEmpty()
               && repository.IsNotEmpty()
               && tag.IsNotEmpty()
               && assetName.IsNotEmpty();
    }

    public static void VerifySha256(string filePath, string expectedDigest)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!TryNormalizeSha256(expectedDigest, out var expected))
        {
            throw new InvalidDataException("Expected SHA-256 digest is malformed.");
        }
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("Downloaded file does not exist for integrity verification.", filePath);
        }

        using var stream = File.OpenRead(filePath);
        var actual = Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(stream))
            .ToLowerInvariant();

        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            try
            {
                File.Delete(filePath);
            }
            catch
            {
                // A failed cleanup must not turn an integrity failure into success.
            }

            throw new InvalidDataException(
                $"Downloaded file SHA-256 mismatch: expected {expected}, got {actual}.");
        }
    }
}
