namespace ServiceLib.Models.Entities;

[Serializable]
public class StrategyOutcomeAggregateItem
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    public string StrategyId { get; set; } = string.Empty;
    public string GenomeKey { get; set; } = string.Empty;
    public string NetworkKey { get; set; } = string.Empty;
    public long DayBucketUnixSeconds { get; set; }
    public int Samples { get; set; }
    public int Successes { get; set; }
    public int Rollbacks { get; set; }
    public int HumanConfirmed { get; set; }
    public int HumanConfirmedSuccesses { get; set; }
    public double LatencySumMs { get; set; }
    public int LatencySamples { get; set; }
}
