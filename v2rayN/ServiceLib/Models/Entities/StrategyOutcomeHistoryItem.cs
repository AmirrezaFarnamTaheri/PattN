namespace ServiceLib.Models.Entities;

[Serializable]
public class StrategyOutcomeHistoryItem
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    public string StrategyId { get; set; } = string.Empty;
    public string GenomeKey { get; set; } = string.Empty;
    public string NetworkKey { get; set; } = string.Empty;
    public bool Succeeded { get; set; }
    public bool RolledBack { get; set; }
    public bool HumanConfirmed { get; set; }
    public double? LatencyMs { get; set; }
    public long ObservedAtUnixMs { get; set; }
}
