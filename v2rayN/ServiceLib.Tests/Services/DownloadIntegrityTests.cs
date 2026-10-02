namespace ServiceLib.Tests.Services;

public class DownloadIntegrityTests
{
    [Test]
    public async Task NormalizeSha256_ShouldAcceptGitHubDigestPrefix()
    {
        var expected = new string('a', 64);

        var ok = DownloadIntegrity.TryNormalizeSha256($"sha256:{expected.ToUpperInvariant()}", out var actual);

        await ok.Should().BeTrue();
        await actual.Should().BeEqualTo(expected);
    }

    [Test]
    public async Task ParseGitHubReleaseDownloadUrl_ShouldExtractReleaseIdentity()
    {
        var ok = DownloadIntegrity.TryParseGitHubReleaseDownloadUrl(
            "https://github.com/example/project/releases/download/v1.2.3/app-linux-x64.zip",
            out var owner,
            out var repository,
            out var tag,
            out var assetName);

        await ok.Should().BeTrue();
        await owner.Should().BeEqualTo("example");
        await repository.Should().BeEqualTo("project");
        await tag.Should().BeEqualTo("v1.2.3");
        await assetName.Should().BeEqualTo("app-linux-x64.zip");
    }

    [Test]
    public async Task ParseGitHubReleaseDownloadUrl_ShouldRejectNonReleaseUrl()
    {
        var ok = DownloadIntegrity.TryParseGitHubReleaseDownloadUrl(
            "https://example.com/project/releases/download/v1.2.3/app.zip",
            out _,
            out _,
            out _,
            out _);

        await ok.Should().BeFalse();
    }

    [Test]
    public async Task VerifySha256_ShouldAcceptExpectedBytes_AndRejectMismatch()
    {
        var path = Path.Combine(Path.GetTempPath(), $"pattn-integrity-{Guid.NewGuid():N}.bin");
        try
        {
            var bytes = Encoding.UTF8.GetBytes("pattn-update-integrity");
            await File.WriteAllBytesAsync(path, bytes);
            var digest = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

            DownloadIntegrity.VerifySha256(path, digest);
            await File.Exists(path).Should().BeTrue();

            var rejected = false;
            try
            {
                DownloadIntegrity.VerifySha256(path, new string('0', 64));
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }

            await rejected.Should().BeTrue();
            await File.Exists(path).Should().BeFalse();
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
