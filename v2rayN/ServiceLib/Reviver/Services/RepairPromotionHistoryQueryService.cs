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

    public async Task<RepairIncidentTimeline> QueryTimelineAsync(
        string profileId,
        TimeSpan? maxAge = null,
        int maxItems = 500,
        CancellationToken cancellationToken = default)
    {
        if (profileId.IsNullOrEmpty())
        {
            throw new ArgumentException("Incident timeline requires a profile ID.", nameof(profileId));
        }

        var summary = await QueryAsync(
            new RepairPromotionHistoryQuery
            {
                ProfileId = profileId,
                MaxAge = maxAge ?? TimeSpan.FromDays(180),
                MaxItems = maxItems,
            },
            cancellationToken);

        return BuildTimeline(profileId, summary.Entries);
    }

    public static RepairIncidentTimeline BuildTimeline(
        string profileId,
        IReadOnlyList<RepairPromotionHistoryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var events = entries
            .OrderBy(x => x.ObservedAt)
            .Select(entry => new RepairIncidentTimelineEvent
            {
                ObservedAt = entry.ObservedAt,
                EventKind = entry.EventKind,
                StrategyId = entry.StrategyId,
                CandidateId = entry.CandidateId,
                OutcomeVerdict = entry.OutcomeVerdict,
                MutationFields = entry.Mutations
                    .Select(x => x.Field ?? string.Empty)
                    .Where(x => x.IsNotEmpty())
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(x => x, StringComparer.Ordinal)
                    .ToArray(),
                Summary = BuildTimelineSummary(entry),
            })
            .ToArray();

        return new RepairIncidentTimeline
        {
            ProfileId = profileId,
            StartedAt = events.Length == 0 ? null : events[0].ObservedAt,
            EndedAt = events.Length == 0 ? null : events[^1].ObservedAt,
            Events = events,
        };
    }

    private static string BuildTimelineSummary(RepairPromotionHistoryEntry entry)
    {
        var strategy = entry.StrategyId.IsNullOrEmpty() ? "unknown strategy" : entry.StrategyId;
        return entry.EventKind switch
        {
            "promoted" => $"Promoted {strategy}; outcome={entry.OutcomeVerdict}.",
            "rolled-back" => $"Rolled back {strategy}; outcome={entry.OutcomeVerdict}.",
            "human-confirmed" => $"Operator feedback for {strategy}: {entry.OutcomeVerdict}.",
            _ => $"{entry.EventKind}: {strategy}; outcome={entry.OutcomeVerdict}.",
        };
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
                var humanPositive = group.Count(x =>
                    x.EventKind == "human-confirmed" && x.OutcomeVerdict == "improved");
                var humanNegative = group.Count(x =>
                    x.EventKind == "human-confirmed" && x.OutcomeVerdict == "regressed");
                var humanRated = humanPositive + humanNegative;

                double weightedSuccess = 0;
                double weightedTotal = 0;
                double weightedLearningSuccess = 0;
                double weightedLearningTotal = 0;
                foreach (var entry in group.Where(x =>
                             x.EventKind is "promoted" or "human-confirmed"
                             && x.OutcomeVerdict is "improved" or "stable" or "regressed"))
                {
                    var ageDays = Math.Max(0d, (referenceTime - entry.ObservedAt).TotalDays);
                    var freshness = Math.Pow(0.5d, ageDays / 90d);
                    var learningWeight = entry.EventKind == "human-confirmed" ? 2d : 1d;

                    weightedLearningTotal += freshness * learningWeight;
                    if (entry.OutcomeVerdict is "improved" or "stable")
                    {
                        weightedLearningSuccess += freshness * learningWeight;
                    }

                    if (entry.EventKind == "promoted")
                    {
                        weightedTotal += freshness;
                        if (entry.OutcomeVerdict is "improved" or "stable")
                        {
                            weightedSuccess += freshness;
                        }
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
                    HumanConfirmedPositive = humanPositive,
                    HumanConfirmedNegative = humanNegative,
                    SuccessRate = rated == 0
                        ? null
                        : (improved + stable) / (double)rated,
                    HumanConfirmedSuccessRate = humanRated == 0
                        ? null
                        : humanPositive / (double)humanRated,
                    RollbackRate = promotions == 0
                        ? null
                        : Math.Clamp(rollbacks / (double)promotions, 0d, 1d),
                    RecencyWeightedSuccessRate = weightedTotal <= 0
                        ? null
                        : weightedSuccess / weightedTotal,
                    RecencyWeightedLearningSuccessRate = weightedLearningTotal <= 0
                        ? null
                        : weightedLearningSuccess / weightedLearningTotal,
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
            && !string.Equals(query.EventKind, "rolled-back", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(query.EventKind, "human-confirmed", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Promotion history event kind must be 'promoted', 'rolled-back', or 'human-confirmed'.",
                nameof(query.EventKind));
        }
    }
}
