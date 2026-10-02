using ServiceLib.Models.Entities;
using ServiceLib.Reviver.Models;

namespace ServiceLib.Reviver.Services;

/// <summary>
/// Persists explicit operator confirmation about a promoted repair. Feedback is structured only:
/// no free-form text, endpoint, credential, SNI, domain, or network address is stored.
/// </summary>
public sealed class RepairHumanFeedbackService
{
    public async Task RecordAsync(
        RepairPromotionReceipt receipt,
        bool worked,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(receipt);
        cancellationToken.ThrowIfCancellationRequested();

        if (receipt.PromotedProfileId.IsNullOrEmpty())
        {
            throw new ArgumentException(
                "Human repair feedback requires a persisted promoted profile.",
                nameof(receipt));
        }

        await SQLiteHelper.Instance.InsertAsync(new RepairPromotionHistoryItem
        {
            Id = Utils.GetGuid(false),
            EventKind = "human-confirmed",
            SessionId = receipt.SessionId,
            CandidateId = receipt.CandidateId,
            StrategyId = receipt.StrategyId,
            OriginalProfileId = receipt.OriginalProfileId,
            PromotedProfileId = receipt.PromotedProfileId,
            PreviousDefaultProfileId = receipt.PreviousDefaultProfileId ?? string.Empty,
            BecameDefault = receipt.BecameDefault,
            OutcomeVerdict = worked ? "improved" : "regressed",
            ObservedAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        });
    }
}
