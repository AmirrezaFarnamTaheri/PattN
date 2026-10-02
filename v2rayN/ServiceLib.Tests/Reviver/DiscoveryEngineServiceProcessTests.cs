using ServiceLib.Discovery.Protocol;
using ServiceLib.Discovery.Services;

namespace ServiceLib.Tests.Reviver;

public class DiscoveryEngineServiceProcessTests
{
    [Test]
    public async Task OversizedStdoutFrame_ShouldFailGenerationWithFrameTooLarge()
    {
        await WithFakeHelperAsync(
            """
            IFS= read -r request || exit 0
            head -c 4194305 /dev/zero | tr '\000' x
            printf '\n'
            """,
            async service =>
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var error = await CaptureAsync(() => service.GetVersionAsync(timeout.Token));
                await (error is DiscoveryRpcException { Code: "frame_too_large" }).Should().BeTrue();
            });
    }

    [Test]
    public async Task StderrFlood_ShouldNotBlockValidProtocolResponse()
    {
        await WithFakeHelperAsync(
            """
            IFS= read -r request || exit 0
            id="$(printf '%s\n' "$request" | sed -n 's/.*"id":"\([^"]*\)".*/\1/p')"
            i=0
            while [ "$i" -lt 4000 ]; do
              printf 'diagnostic-%s-xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\n' "$i" >&2
              i=$((i + 1))
            done
            printf '{"v":1,"id":"%s","data":{"engine":"pattn-discovery","version":"fake"}}\n' "$id"
            """,
            async service =>
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                var version = await service.GetVersionAsync(timeout.Token);
                await version.Engine.Should().BeEqualTo("pattn-discovery");
                await version.Version.Should().BeEqualTo("fake");
            });
    }

    [Test]
    public async Task CrashedHelper_ShouldBackOffBeforeRestarting()
    {
        string? startsPath = null;
        await WithFakeHelperAsync(
            """
            starts="$0.starts"
            printf x >> "$starts"
            IFS= read -r request || exit 0
            exit 17
            """,
            async (service, helperPath) =>
            {
                startsPath = helperPath + ".starts";
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                var first = await CaptureAsync(() => service.GetVersionAsync(timeout.Token));
                await (first is IOException).Should().BeTrue();

                var second = await CaptureAsync(() => service.GetVersionAsync(timeout.Token));
                await (second is IOException
                       && second.Message.Contains("backing off", StringComparison.OrdinalIgnoreCase))
                    .Should().BeTrue();

                var starts = await File.ReadAllTextAsync(startsPath, timeout.Token);
                await starts.Should().BeEqualTo("x");
            });

        _ = startsPath;
    }

    [Test]
    public async Task CallerCancellation_ShouldCancelRequestWithoutHangingHelperShutdown()
    {
        await WithFakeHelperAsync(
            """
            IFS= read -r request || exit 0
            # The client sends request.cancel after caller cancellation. Reading it
            # proves the redirected stdin remains usable while the first RPC is pending.
            IFS= read -r cancel || exit 0
            exit 0
            """,
            async service =>
            {
                using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(150));
                var error = await CaptureAsync(() => service.GetVersionAsync(cancellation.Token));
                await (error is OperationCanceledException).Should().BeTrue();
            });
    }

    private static async Task WithFakeHelperAsync(
        string body,
        Func<DiscoveryEngineService, Task> test)
        => await WithFakeHelperAsync(body, async (service, _) => await test(service));

    private static async Task WithFakeHelperAsync(
        string body,
        Func<DiscoveryEngineService, string, Task> test)
    {
        // The production helper is a native executable. These process-level tests use
        // a POSIX script on CI to exercise the exact redirected stdio/supervision path.
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var root = Path.Combine(Path.GetTempPath(), "pattn-discovery-fake-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var helperPath = Path.Combine(root, "pattn-discovery-fake");
        await File.WriteAllTextAsync(
            helperPath,
            "#!/usr/bin/env bash\nset -eu\n" + body.Trim() + "\n");
        File.SetUnixFileMode(
            helperPath,
            UnixFileMode.UserRead
            | UnixFileMode.UserWrite
            | UnixFileMode.UserExecute);

        try
        {
            await using var service = new DiscoveryEngineService(helperPath);
            await test(service, helperPath);
        }
        finally
        {
            try
            {
                Directory.Delete(root, recursive: true);
            }
            catch
            {
                // Test cleanup must not mask the behavior assertion.
            }
        }
    }

    private static async Task<Exception?> CaptureAsync(Func<Task> action)
    {
        try
        {
            await action();
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }
}
