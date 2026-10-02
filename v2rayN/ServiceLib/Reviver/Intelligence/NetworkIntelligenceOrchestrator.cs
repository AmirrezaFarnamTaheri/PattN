using ServiceLib.Discovery.Protocol;
using ServiceLib.Discovery.Services;

namespace ServiceLib.Reviver.Intelligence;

public sealed record NetworkProbeContext
{
    public string Carrier { get; init; } = string.Empty;
    public string Asn { get; init; } = string.Empty;
    public string CountryCode { get; init; } = string.Empty;
    public bool? IPv4Succeeded { get; init; }
    public bool? IPv6Succeeded { get; init; }
}

public sealed record NetworkIntelligenceCapture
{
    public required DiscoveryUplinkProbeResult Probe { get; init; }
    public required FailureAssessment Assessment { get; init; }
    public required NetworkFingerprint Fingerprint { get; init; }
}

public sealed class NetworkIntelligenceOrchestrator(
    NetworkIntelligenceService intelligence,
    INetworkFingerprintStore? store = null)
{
    public async Task<NetworkIntelligenceCapture> CaptureUplinkAsync(
        DiscoveryEngineService engine,
        DiscoveryUplinkProbeRequest request,
        NetworkProbeContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(request);

        var probe = await engine.ProbeUplinkAsync(request, cancellationToken);
        var observedAt = DateTimeOffset.UtcNow;
        var observation = new NetworkObservation
        {
            Carrier = context?.Carrier ?? string.Empty,
            Asn = context?.Asn ?? string.Empty,
            CountryCode = context?.CountryCode ?? string.Empty,
            TcpSucceeded = probe.Connected,
            TlsSucceeded = probe.TlsHandshakeSucceeded,
            UploadSucceeded = probe.BodyFullyRead,
            // A response on this same request is not an independent downstream-path signal;
            // do not upgrade a partial request-body read into a high-confidence UplinkStall.
            DownstreamSucceeded = null,
            IPv4Succeeded = context?.IPv4Succeeded,
            IPv6Succeeded = context?.IPv6Succeeded,
            LatencyMs = probe.DurationMs,
            ObservedAt = observedAt,
        };

        var assessment = intelligence.Classify(observation);
        var fingerprint = intelligence.BuildFingerprint(observation);
        if (store is not null)
        {
            await store.RecordAsync(fingerprint, assessment, cancellationToken);
        }

        return new NetworkIntelligenceCapture
        {
            Probe = probe,
            Assessment = assessment,
            Fingerprint = fingerprint,
        };
    }
}
