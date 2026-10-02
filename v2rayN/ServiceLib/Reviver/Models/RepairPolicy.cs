namespace ServiceLib.Reviver.Models;

/// <summary>
/// Hard bounds for repair planning. Reviver is intentionally a constrained search, never a Cartesian fuzzer.
/// </summary>
public sealed record RepairPolicy
{
    public int MaxCandidates { get; init; } = 32;
    public int MaxCandidatesPerStrategy { get; init; } = 8;
    public int RuntimeAttempts { get; init; } = 3;
    public int MinimumRuntimeSuccesses { get; init; } = 2;
    public ERepairConfidence MaximumAutomaticConfidence { get; init; } = ERepairConfidence.EvidenceBacked;
    public bool AllowSpeculative { get; init; }

    /// <summary>
    /// Optional endpoint that accepts an HTTP POST through the temporary candidate proxy.
    /// Empty means upload-stall validation is disabled.
    /// </summary>
    public string UploadProbeUrl { get; init; } = string.Empty;
    public int UploadProbeBytes { get; init; } = 16 * 1024;
    public int UploadProbeTimeoutSeconds { get; init; } = 8;

    /// <summary>
    /// Optional operator-supplied finalMask template. PattN deliberately ships no hard-coded
    /// censorship-bypass recipe here; when present it is tried only for an UploadStall diagnosis
    /// and must pass the same real-core validation quorum as every other repair.
    /// </summary>
    public string UploadStallFinalMaskJson { get; init; } = string.Empty;

    public bool Allows(ERepairConfidence confidence)
        => confidence <= MaximumAutomaticConfidence || (AllowSpeculative && confidence == ERepairConfidence.Speculative);
}
