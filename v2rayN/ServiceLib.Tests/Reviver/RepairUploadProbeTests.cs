using ServiceLib.Reviver.Validation;

namespace ServiceLib.Tests.Reviver;

public class RepairUploadProbeTests
{
    [Test]
    public async Task ProbeAsync_ShouldPostConfiguredPayloadAndMeasureThroughput()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var receivedBody = 0;

        var server = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            await using var stream = client.GetStream();
            var data = new List<byte>();
            var buffer = new byte[4096];
            var headerEnd = -1;
            var contentLength = 0;

            while (true)
            {
                var read = await stream.ReadAsync(buffer);
                if (read <= 0)
                {
                    break;
                }
                data.AddRange(buffer.AsSpan(0, read).ToArray());

                if (headerEnd < 0)
                {
                    headerEnd = FindHeaderEnd(data);
                    if (headerEnd >= 0)
                    {
                        var header = Encoding.ASCII.GetString(data.Take(headerEnd).ToArray());
                        var lengthLine = header
                            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
                            .First(x => x.StartsWith("Content-Length:", StringComparison.OrdinalIgnoreCase));
                        contentLength = int.Parse(
                            lengthLine.Split(':', 2)[1].Trim(),
                            System.Globalization.CultureInfo.InvariantCulture);
                    }
                }

                if (headerEnd >= 0
                    && data.Count >= headerEnd + 4 + contentLength)
                {
                    receivedBody = contentLength;
                    break;
                }
            }

            var response = Encoding.ASCII.GetBytes(
                "HTTP/1.1 204 No Content\r\nContent-Length: 0\r\nConnection: close\r\n\r\n");
            await stream.WriteAsync(response);
        });

        var result = await new HttpRepairUploadProbe().ProbeAsync(
            null,
            $"http://127.0.0.1:{port}/upload",
            4096,
            TimeSpan.FromSeconds(5));

        await server.WaitAsync(TimeSpan.FromSeconds(5));
        await result.Success.Should().BeTrue();
        await result.Stalled.Should().BeFalse();
        await (result.ThroughputMbps > 0).Should().BeTrue();
        await receivedBody.Should().BeEqualTo(4096);
    }

    [Test]
    public async Task ProbeAsync_ShouldClassifyResponseTimeoutAsUploadStall()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var server = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            await Task.Delay(TimeSpan.FromSeconds(2));
        });

        var result = await new HttpRepairUploadProbe().ProbeAsync(
            null,
            $"http://127.0.0.1:{port}/upload",
            1024,
            TimeSpan.FromSeconds(1));

        await result.Success.Should().BeFalse();
        await result.Stalled.Should().BeTrue();
        await server.WaitAsync(TimeSpan.FromSeconds(4));
    }

    [Test]
    public async Task ProbeAsync_ShouldRejectNonHttpUrlWithoutNetworkWork()
    {
        var result = await new HttpRepairUploadProbe().ProbeAsync(
            null,
            "file:///tmp/test",
            1024,
            TimeSpan.FromSeconds(1));

        await result.Success.Should().BeFalse();
        await result.Stalled.Should().BeFalse();
        await result.Error.IsNotEmpty().Should().BeTrue();
    }

    private static int FindHeaderEnd(IReadOnlyList<byte> data)
    {
        for (var i = 0; i + 3 < data.Count; i++)
        {
            if (data[i] == '\r'
                && data[i + 1] == '\n'
                && data[i + 2] == '\r'
                && data[i + 3] == '\n')
            {
                return i;
            }
        }
        return -1;
    }
}
