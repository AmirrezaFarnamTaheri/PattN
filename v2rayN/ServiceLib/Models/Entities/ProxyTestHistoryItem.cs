namespace ServiceLib.Models.Entities;

[Serializable]
public class ProxyTestHistoryItem
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    [Indexed]
    public string ProfileIndexId { get; set; } = string.Empty;

    [Indexed]
    public long TestedAtUnixMs { get; set; }

    [Indexed]
    public string RunId { get; set; } = string.Empty;

    public string ProfileFingerprint { get; set; } = string.Empty;

    public int ActionType { get; set; }
    public int Attempt { get; set; }
    public string Phase { get; set; } = string.Empty;
    public bool IsFinalOutcome { get; set; }
    public bool Success { get; set; }
    public bool Skipped { get; set; }
    public int DelayMs { get; set; }
    public decimal Speed { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public int Port { get; set; }
    public EConfigType ConfigType { get; set; }
}
