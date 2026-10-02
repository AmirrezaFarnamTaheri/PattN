using ServiceLib.Discovery.Protocol;
using ServiceLib.Discovery.Services;

namespace ServiceLib.Tests.Reviver;

public class DiscoveryEngineServiceProcessTests
{
    [Test]
    public async Task OversizedStdoutFrame_ShouldFailPendingRequestWithoutUnboundedRead()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var helper = CreateUnixHelper(
            """
            #!/bin/sh
            IFS= read -r _ || exit 0
            dd if=/dev/zero bs=1048576 count=5 2>/dev/null | tr '\000' x
            printf '\n'
            """);
        try
        {
            await using var service = new DiscoveryEngineService(helper.Path);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            Exception? observed = null;
            try
            {
                _ = await service.GetVersionAsync(timeout.Token);
            }
            catch (Exception ex)
            {
                observed = ex;
            }

            await observed.Should().NotBeNull();
            await observed!.Message.Should().Contain("frame_too_large");
        }
        finally
        {
            DeleteHelper(helper);
        }
    }

    [Test]
    public async Task StderrFlood_ShouldNotBlockValidStdoutResponse()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var helper = CreateUnixHelper(
            """
            #!/bin/sh
            IFS= read -r line || exit 0
            id="$(printf '%s\n' "$line" | sed -n 's/.*"id":"\([^"]*\)".*/\1/p')"
            i=0
            while [ "$i" -lt 2048 ]; do
              printf 'diagnostic-%04d-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n' "$i" >&2
              i=$((i + 1))
            done
            printf '{"v":1,"id":"%s","data":{"engine":"fake","version":"1.0.0"}}\n' "$id"
            """);
        try
        {
            await using var service = new DiscoveryEngineService(helper.Path);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            var version = await service.GetVersionAsync(timeout.Token);

            await version.Engine.Should().BeEqualTo("fake");
            await version.Version.Should().BeEqualTo("1.0.0");
        }
        finally
        {
            DeleteHelper(helper);
        }
    }

    [Test]
    public async Task CrashedHelper_ShouldBackOffImmediateRestart()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var helper = CreateUnixHelper(
            """
            #!/bin/sh
            IFS= read -r _ || exit 0
            exit 42
            """);
        try
        {
            await using var service = new DiscoveryEngineService(helper.Path);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            Exception? first = null;
            try
            {
                _ = await service.GetVersionAsync(timeout.Token);
            }
            catch (Exception ex)
            {
                first = ex;
            }
            await first.Should().NotBeNull();

            Exception? second = null;
            try
            {
                _ = await service.GetVersionAsync(timeout.Token);
            }
            catch (Exception ex)
            {
                second = ex;
            }

            await second.Should().NotBeNull();
            await second!.Message.Should().Contain("backing off");
        }
        finally
        {
            DeleteHelper(helper);
        }
    }

    [Test]
    public async Task CallerCancellation_ShouldReleasePendingRequest()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var helper = CreateUnixHelper(
            """
            #!/bin/sh
            IFS= read -r _ || exit 0
            sleep 30
            """);
        try
        {
            await using var service = new DiscoveryEngineService(helper.Path);
            using var timeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

            var cancelled = false;
            try
            {
                _ = await service.GetVersionAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                cancelled = true;
            }

            await cancelled.Should().BeTrue();
        }
        finally
        {
            DeleteHelper(helper);
        }
    }

    private static HelperFixture CreateUnixHelper(string script)
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "pattn-discovery-helper-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "fake-pattn-discovery");
        File.WriteAllText(path, script.Replace("\r\n", "\n"), new UTF8Encoding(false));
        File.SetUnixFileMode(
            path,
            UnixFileMode.UserRead
            | UnixFileMode.UserWrite
            | UnixFileMode.UserExecute);
        return new HelperFixture(directory, path);
    }

    private static void DeleteHelper(HelperFixture helper)
    {
        try
        {
            Directory.Delete(helper.Directory, recursive: true);
        }
        catch
        {
            // Disposal is the behavior under test; temp cleanup must not mask it.
        }
    }

    private sealed record HelperFixture(string Directory, string Path);
}
