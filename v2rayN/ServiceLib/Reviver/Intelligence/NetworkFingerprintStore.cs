using ServiceLib.Models.Entities;

namespace ServiceLib.Reviver.Intelligence;

public interface INetworkFingerprintStore
{
    Task RecordAsync(
        NetworkFingerprint fingerprint,
        FailureAssessment assessment,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NetworkFingerprint>> ListRecentAsync(
        int limit = 200,
        CancellationToken cancellationToken = default);
}

public sealed class SqliteNetworkFingerprintStore : INetworkFingerprintStore
{
    public async Task RecordAsync(
        NetworkFingerprint fingerprint,
        FailureAssessment assessment,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(fingerprint);
        ArgumentNullException.ThrowIfNull(assessment);
        cancellationToken.ThrowIfCancellationRequested();

        await SQLiteHelper.Instance.InsertAsync(new NetworkFingerprintHistoryItem
        {
            Id = Utils.GetGuid(false),
            NetworkKey = fingerprint.Key,
            CarrierKey = fingerprint.CarrierKey,
            Asn = fingerprint.Asn,
            CountryCode = fingerprint.CountryCode,
            IPv4Healthy = EncodeNullableBool(fingerprint.IPv4Healthy),
            IPv6Healthy = EncodeNullableBool(fingerprint.IPv6Healthy),
            UploadStall = fingerprint.DpiSignals.UploadStall,
            TlsInterferenceSuspected = fingerprint.DpiSignals.TlsInterferenceSuspected,
            ResetPattern = fingerprint.DpiSignals.ResetPattern,
            PacketLossPattern = fingerprint.DpiSignals.PacketLossPattern,
            DpiSignalConfidence = fingerprint.DpiSignals.Confidence,
            FailureClass = (int)assessment.FailureClass,
            FailureConfidence = assessment.Confidence,
            ObservedAtUnixMs = fingerprint.ObservedAt.ToUnixTimeMilliseconds(),
        });
    }

    public async Task<IReadOnlyList<NetworkFingerprint>> ListRecentAsync(
        int limit = 200,
        CancellationToken cancellationToken = default)
    {
        if (limit is < 1 or > 5000)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await SQLiteHelper.Instance.TableAsync<NetworkFingerprintHistoryItem>()
            .OrderByDescending(x => x.ObservedAtUnixMs)
            .Take(limit)
            .ToListAsync();

        return rows.Select(x => new NetworkFingerprint
        {
            Key = x.NetworkKey,
            CarrierKey = x.CarrierKey,
            Asn = x.Asn,
            CountryCode = x.CountryCode,
            IPv4Healthy = DecodeNullableBool(x.IPv4Healthy),
            IPv6Healthy = DecodeNullableBool(x.IPv6Healthy),
            DpiSignals = new NetworkDpiSignals
            {
                UploadStall = x.UploadStall,
                TlsInterferenceSuspected = x.TlsInterferenceSuspected,
                ResetPattern = x.ResetPattern,
                PacketLossPattern = x.PacketLossPattern,
                Confidence = x.DpiSignalConfidence,
            },
            ObservedAt = DateTimeOffset.FromUnixTimeMilliseconds(x.ObservedAtUnixMs),
        }).ToArray();
    }

    private static int EncodeNullableBool(bool? value) => value switch
    {
        true => 1,
        false => 0,
        null => -1,
    };

    private static bool? DecodeNullableBool(int value) => value switch
    {
        1 => true,
        0 => false,
        _ => null,
    };
}
