using ServiceLib.Models.Entities;
using ServiceLib.Reviver.Models;

namespace ServiceLib.Reviver.Services;

public sealed class RepairPromotionHistoryQueryService
{
    public async Task<RepairPromotionHistorySummary> QueryAsync(
        RepairPromotionHistoryQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        query ??= new RepairPromotionHistoryQuery();
        Validate(query);

        var cutoff = DateTimeOffset.UtcNow.Subtract(query.MaxAge).ToUnixTimeMilliseconds();
        var where = new List<string> { "ObservedAtUnixMs >= ?" };
        var args = new List<object> { cutoff };

        if (!query.EventKind.IsNullOrEmpty())
        {
            where.Add("EventKind = ? COLLATE NOCASE");
            args.Add(query.EventKind);
        }
        if (!query.SessionId.IsNullOrEmpty())
        {
            where.Add("SessionId = ?");
            args.Add(query.SessionId);
        }
        if (!query.CandidateId.IsNullOrEmpty())
        {
            where.Add("CandidateId = ?");
            args.Add(query.CandidateId);
        }
        if (!query.StrategyId.IsNullOrEmpty())
        {
            where.Add("StrategyId = ?");
            args.Add(query.StrategyId);
        }
        if (!query.ProfileId.IsNullOrEmpty())
        {
            where.Add("(OriginalProfileId = ? OR PromotedProfileId = ?)");
            args.Add(query.ProfileId);
            args.Add(query.ProfileId);
        }

        args.Add(query.MaxItems);
        var rows = await SQLiteHelper.Instance.QueryAsync<RepairPromotionHistoryItem>(
            $"""
            SELECT *
            FROM RepairPromotionHistoryItem
            WHERE {string.Join(" AND ", where)}
            ORDER BY ObservedAtUnixMs DESC
            LIMIT ?
            """,
            args.ToArray());

        cancellationToken.ThrowIfCancellationRequested();
        return Summarize(rows);
    }

    public static RepairPromotionHistorySummary Summarize(
        IReadOnlyList<RepairPromotionHistoryItem> rows,
        DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(rows);
        var referenceTime = now ?? DateTimeOffset.UtcNow;

        var entries = rows
            .OrderByDescending(x => x.ObservedAtUnixMs)
            .Select(Project)
            .ToArray();

        var strategies = entries
            .Where(x => x.StrategyId.IsNotEmpty())
            .GroupBy(x => x.StrategyId, StringComparer.Ordinal)
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var promotions = group.Count(x => x.EventKind == "promoted");
                var rollbacks = group.Count(x => x.EventKind == "rolled-back");
                var improved = group.Count(x => x.EventKind == "promoted" && x.OutcomeVerdict == "improved");
                var stable = group.Count(x => x.EventKind == "promoted" && x.OutcomeVerdict == "stable");
                var regressed = group.Count(x => x.EventKind == "promoted" && x.OutcomeVerdict == "regressed");
                var rated = improved + stable + regressed;

                double weightedSuccess = 0;
                double weightedTotal = 0;
                foreach (var entry in group.Where(x =>
                             x.EventKind == "promoted"
                             && x.OutcomeVerdict is "improved" or "stable" or "regressed"))
                {
                    var ageDays = Math.Max(0d, (referenceTime - entry.ObservedAt).TotalDays);
                    var weight = Math.Pow(0.5d, ageDays / 90d);
                    weightedTotal += weight;
                    if (entry.OutcomeVerdict is "improved" or "stable")
                    {
                        weightedSuccess += weight;
                    }
                }

                return new RepairStrategyHistorySummary
                {
                    StrategyId = group.Key,
                    Promotions = promotions,
                    Rollbacks = rollbacks,
                    Improved = improved,
                    Stable = stable,
                    Regressed = regressed,
                    RatedPromotions = rated,
                    SuccessRate = rated == 0
                        ? null
                        : (improved + stable) / (double)rated,
                    RollbackRate = promotions == 0
                        ? null
                        : Math.Clamp(rollbacks / (double)promotions, 0d, 1d),
                    RecencyWeightedSuccessRate = weightedTotal <= 0
                        ? null
                        : weightedSuccess / weightedTotal,
                    LatestEventAt = group.Max(x => x.ObservedAt),
                };
            })
            .ToArray();

        return new RepairPromotionHistorySummary
        {
            TotalEvents = entries.Length,
            Promotions = entries.Count(x => x.EventKind == "promoted"),
            Rollbacks = entries.Count(x => x.EventKind == "rolled-back"),
            Improved = entries.Count(x => x.OutcomeVerdict == "improved"),
            Stable = entries.Count(x => x.OutcomeVerdict == "stable"),
            Regressed = entries.Count(x => x.OutcomeVerdict == "regressed"),
            Unknown = entries.Count(x => x.OutcomeVerdict is "" or "unknown"),
            LatestEventAt = entries.Length == 0 ? null : entries[0].ObservedAt,
            Strategies = strategies,
            Entries = entries,
        };
    }

    private static RepairPromotionHistoryEntry Project(RepairPromotionHistoryItem row)
        => new()
        {
            Id = row.Id,
            EventKind = row.EventKind,
            SessionId = row.SessionId,
            CandidateId = row.CandidateId,
            StrategyId = row.StrategyId,
            OriginalProfileId = row.OriginalProfileId,
            PromotedProfileId = row.PromotedProfileId,
            PreviousDefaultProfileId = row.PreviousDefaultProfileId,
            BecameDefault = row.BecameDefault,
            Score = row.Score,
            OutcomeVerdict = (row.OutcomeVerdict.NullIfEmpty() ?? "unknown").Trim().ToLowerInvariant(),
            Mutations = DeserializeOrDefault<List<RepairMutation>>(row.MutationsJson, nameof(row.MutationsJson)) ?? [],
            BaselineValidation = DeserializeOrDefault<RepairValidationEvidence>(row.BaselineValidationJson, nameof(row.BaselineValidationJson)),
            CandidateValidation = DeserializeOrDefault<RepairValidationEvidence>(row.CandidateValidationJson, nameof(row.CandidateValidationJson)),
            OutcomeComparison = DeserializeOrDefault<RepairOutcomeComparison>(row.OutcomeComparisonJson, nameof(row.OutcomeComparisonJson)),
            ObservedAt = DateTimeOffset.FromUnixTimeMilliseconds(row.ObservedAtUnixMs),
        };

    private static T? DeserializeOrDefault<T>(string json, string fieldName)
    {
        if (json.IsNullOrEmpty())
        {
            return default;
        }
        try
        {
            return JsonUtils.DeserializeStrict<T>(json);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            throw new InvalidOperationException(
                $"Stored repair promotion history field '{fieldName}' contains invalid JSON.",
                ex);
        }
    }

    private static void Validate(RepairPromotionHistoryQuery query)
    {
        if (query.MaxAge <= TimeSpan.Zero || query.MaxAge > TimeSpan.FromDays(3650))
        {
            throw new ArgumentOutOfRangeException(nameof(query.MaxAge), "Promotion history age must be greater than zero and at most 10 years.");
        }
        if (query.MaxItems is < 1 or > 1000)
        {
            throw new ArgumentOutOfRangeException(nameof(query.MaxItems), "Promotion history item limit must be between 1 and 1000.");
        }
        if (!query.EventKind.IsNullOrEmpty()
            && !string.Equals(query.EventKind, "promoted", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(query.EventKind, "rolled-back", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Promotion history event kind must be 'promoted' or 'rolled-back'.", nameof(query.EventKind));
        }
    }
}
