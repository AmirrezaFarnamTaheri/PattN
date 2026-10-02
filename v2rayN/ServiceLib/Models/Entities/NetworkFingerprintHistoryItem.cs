namespace ServiceLib.Models.Entities;

[Serializable]
public class NetworkFingerprintHistoryItem
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    public string NetworkKey { get; set; } = string.Empty;
    public string CarrierKey { get; set; } = string.Empty;
    public string Asn { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public int IPv4Healthy { get; set; } = -1;
    public int IPv6Healthy { get; set; } = -1;
    public bool UploadStall { get; set; }
    public bool TlsInterferenceSuspected { get; set; }
    public bool ResetPattern { get; set; }
    public bool PacketLossPattern { get; set; }
    public double DpiSignalConfidence { get; set; }
    public int FailureClass { get; set; }
    public double FailureConfidence { get; set; }
    public long ObservedAtUnixMs { get; set; }
}
