namespace ServiceLib.Reviver.Models;

public sealed record DnsRepairFamilyHistory
{
    public required string Family { get; init; }
    public int Samples { get; init; }
    public int RuntimeQuorumPasses { get; init; }
    public double PassRate => Samples <= 0 ? 0.5d : (double)RuntimeQuorumPasses / Samples;
    public double? MedianLatencyMs { get; init; }
}

/// <summary>
/// Historical family evidence keeps strict single-family trials separate from dual-stack
/// first-family ordering trials. PreferredFamily may use either mature evidence class, but
/// never pools their pass rates; conflicting mature classes remain neutral.
/// </summary>
public sealed record DnsRepairHistorySummary
{
    public required string Host { get; init; }
    public DnsRepairFamilyHistory IPv4 { get; init; } = new() { Family = "ipv4" };
    public DnsRepairFamilyHistory IPv6 { get; init; } = new() { Family = "ipv6" };
    public DnsRepairFamilyHistory IPv4First { get; init; } = new() { Family = "ipv4-first" };
    public DnsRepairFamilyHistory IPv6First { get; init; } = new() { Family = "ipv6-first" };

    public int TotalSamples => IPv4.Samples + IPv6.Samples + IPv4First.Samples + IPv6First.Samples;

    public string? PreferredFamily(int minimumSamplesPerFamily = 3, double minimumPassRateDelta = 0.20d)
    {
        var strict = PreferredBetween(IPv4, IPv6, minimumSamplesPerFamily, minimumPassRateDelta);
        var ordered = PreferredBetween(IPv4First, IPv6First, minimumSamplesPerFamily, minimumPassRateDelta);

        if (strict is not null && ordered is not null && !string.Equals(strict, ordered, StringComparison.Ordinal))
        {
            return null;
        }

        return strict ?? ordered;
    }

    private static string? PreferredBetween(
        DnsRepairFamilyHistory ipv4,
        DnsRepairFamilyHistory ipv6,
        int minimumSamplesPerFamily,
        double minimumPassRateDelta)
    {
        if (ipv4.Samples < minimumSamplesPerFamily || ipv6.Samples < minimumSamplesPerFamily)
        {
            return null;
        }

        var delta = ipv4.PassRate - ipv6.PassRate;
        const double epsilon = 1e-9;
        if (Math.Abs(delta) <= minimumPassRateDelta + epsilon)
        {
            return null;
        }

        return delta > 0 ? "ipv4" : "ipv6";
    }
}
