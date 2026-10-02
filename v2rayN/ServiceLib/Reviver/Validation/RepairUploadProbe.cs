using ServiceLib.Reviver.Models;

namespace ServiceLib.Reviver.Validation;

public sealed record RepairUploadProbeResult
{
    public bool Success { get; init; }
    public bool Stalled { get; init; }
    public double? ThroughputMbps { get; init; }
    public int? StatusCode { get; init; }
    public string? Error { get; init; }
}

public interface IRepairUploadProbe
{
    Task<RepairUploadProbeResult> ProbeAsync(
        IWebProxy? proxy,
        string url,
        int payloadBytes,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Sends random, non-secret bytes through the candidate proxy to an operator-configured HTTP endpoint.
/// It never labels a timeout as censorship/DPI by itself; callers only receive the narrower UploadStall signal.
/// </summary>
public sealed class HttpRepairUploadProbe : IRepairUploadProbe
{
    public async Task<RepairUploadProbeResult> ProbeAsync(
        IWebProxy? proxy,
        string url,
        int payloadBytes,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https"))
        {
            return new RepairUploadProbeResult { Error = "Upload probe URL must be an absolute HTTP(S) URL." };
        }

        var boundedBytes = Math.Clamp(payloadBytes, 1024, 1024 * 1024);
        var boundedTimeout = TimeSpan.FromSeconds(Math.Clamp(timeout.TotalSeconds, 1d, 60d));
        var payload = new byte[boundedBytes];
        RandomNumberGenerator.Fill(payload);

        using var handler = new SocketsHttpHandler
        {
            Proxy = proxy,
            UseProxy = proxy is not null,
            ConnectTimeout = boundedTimeout,
            AutomaticDecompression = DecompressionMethods.None,
        };
        using var client = new HttpClient(handler)
        {
            Timeout = Timeout.InfiniteTimeSpan,
        };
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(boundedTimeout);
        using var content = new ByteArrayContent(payload);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

        var started = Stopwatch.GetTimestamp();
        try
        {
            using var response = await client.PostAsync(uri, content, linked.Token);
            var elapsed = Stopwatch.GetElapsedTime(started);
            var seconds = Math.Max(elapsed.TotalSeconds, 0.001d);
            var throughputMbps = boundedBytes * 8d / seconds / 1_000_000d;

            return new RepairUploadProbeResult
            {
                Success = response.IsSuccessStatusCode,
                Stalled = false,
                ThroughputMbps = response.IsSuccessStatusCode ? throughputMbps : null,
                StatusCode = (int)response.StatusCode,
                Error = response.IsSuccessStatusCode
                    ? null
                    : $"Upload probe returned HTTP {(int)response.StatusCode}.",
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return new RepairUploadProbeResult
            {
                Success = false,
                Stalled = true,
                Error = "Upload probe timed out after application reachability succeeded.",
            };
        }
        catch (Exception ex)
        {
            return new RepairUploadProbeResult
            {
                Success = false,
                Stalled = false,
                Error = ex.Message,
            };
        }
    }
}
