namespace ServiceLib.Models.Entities;

[Serializable]
public class HumanFeedbackHistoryItem
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    public int Kind { get; set; }
    public string GenomeKey { get; set; } = string.Empty;
    public string NetworkKey { get; set; } = string.Empty;
    public string StrategyId { get; set; } = string.Empty;
    public long ObservedAtUnixMs { get; set; }
}
