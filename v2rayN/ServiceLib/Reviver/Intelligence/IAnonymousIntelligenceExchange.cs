namespace ServiceLib.Reviver.Intelligence;

/// <summary>
/// Optional transport boundary for collective intelligence. Implementations may exchange only
/// already-anonymized/cohort-gated records produced by NetworkAutomationService.PrepareShareBatch.
/// No default remote endpoint is provided by PattN.
/// </summary>
public interface IAnonymousIntelligenceExchange
{
    Task PublishAsync(
        IntelligenceShareBatch batch,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AnonymousIntelligenceRecord>> QueryAsync(
        string networkKey,
        string genomeKey,
        int maxItems = 100,
        CancellationToken cancellationToken = default);
}

public sealed class DisabledAnonymousIntelligenceExchange : IAnonymousIntelligenceExchange
{
    public Task PublishAsync(
        IntelligenceShareBatch batch,
        CancellationToken cancellationToken = default)
        => throw new InvalidOperationException(
            "Anonymous intelligence sharing is disabled until an explicit exchange implementation is configured.");

    public Task<IReadOnlyList<AnonymousIntelligenceRecord>> QueryAsync(
        string networkKey,
        string genomeKey,
        int maxItems = 100,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<AnonymousIntelligenceRecord>>([]);
}
