using ServiceLib.Reviver.Models;
using ServiceLib.Reviver.Promotion;
using ServiceLib.Reviver.Services;

namespace ServiceLib.Tests.Reviver;

public class RepairPromotionServiceTests
{
    [Test]
    public async Task Prepare_ShouldCreateDetachedChild_WithoutMutatingOriginalOrCandidate()
    {
        var original = new ProfileItem
        {
            IndexId = "original-id",
            Subid = "subscription-id",
            IsSub = true,
            ConfigType = EConfigType.VLESS,
            Address = "old.example.com",
            Port = 443,
            Password = "11111111-1111-4111-8111-111111111111",
            Remarks = "Primary",
        };
        original.SetProtocolExtra(new ProtocolExtraItem { Flow = string.Empty, VlessEncryption = Global.None });
        original.SetTransportExtra(new TransportExtraItem());

        var repaired = JsonUtils.DeepCopy(original)!;
        repaired.Address = "203.0.113.20";
        var session = new RepairSession
        {
            Id = "session",
            Original = ProfileSnapshot.Capture(original),
            BaselineValidation = new RepairValidationEvidence
            {
                Attempts = 3,
                Successes = 1,
                ConsecutiveSuccesses = 1,
                MedianLatencyMs = 120,
                LossRate = 2d / 3d,
            },
        };
        var candidate = new RepairCandidate
        {
            Id = "candidate",
            SessionId = session.Id,
            Profile = repaired,
            Mutations =
            [
                new RepairMutation
                {
                    Kind = ERepairMutationKind.ReplaceEndpoint,
                    Field = nameof(ProfileItem.Address),
                    From = original.Address,
                    To = repaired.Address,
                    Reason = "Discovery evidence",
                    Confidence = ERepairConfidence.LowRisk,
                }
            ],
            State = ERepairCandidateState.RuntimeValidated,
            Validation = new RepairValidationEvidence
            {
                Attempts = 3,
                Successes = 3,
                ConsecutiveSuccesses = 3,
                MedianLatencyMs = 60,
                LossRate = 0,
            },
            Score = 96,
        };

        var plan = new RepairPromotionService().Prepare(session, candidate);

        await plan.OriginalProfileId.Should().BeEqualTo("original-id");
        await plan.OriginalProfileFingerprint.Should().NotBeEmpty();
        await plan.ChildProfile.IndexId.Should().BeEmpty();
        await plan.ChildProfile.Subid.Should().BeEmpty();
        await plan.ChildProfile.IsSub.Should().BeFalse();
        await plan.ChildProfile.Remarks.Should().BeEqualTo("Primary-revived");
        await plan.ChildProfile.Address.Should().BeEqualTo("203.0.113.20");
        await plan.BaselineValidation.Should().NotBeNull();
        await plan.OutcomeComparison.Should().NotBeNull();
        await plan.OutcomeComparison!.Verdict.Should().BeEqualTo("improved");
        await plan.OutcomeComparison.Reasons.Contains("reliability-improved").Should().BeTrue();

        // The source and validated candidate retain their original ownership metadata.
        await original.Subid.Should().BeEqualTo("subscription-id");
        await original.IsSub.Should().BeTrue();
        await candidate.Profile.Subid.Should().BeEqualTo("subscription-id");
        await candidate.Profile.IndexId.Should().BeEqualTo("original-id");
    }



    [Test]
    public async Task Promote_ShouldRejectSourceChangedAfterValidation()
    {
        var original = new ProfileItem
        {
            IndexId = "original",
            ConfigType = EConfigType.VLESS,
            Address = "old.example",
            Port = 443,
            Password = "11111111-1111-4111-8111-111111111111",
        };
        var session = new RepairSession { Id = "session", Original = ProfileSnapshot.Capture(original) };
        var candidate = new RepairCandidate
        {
            Id = "candidate",
            SessionId = session.Id,
            Profile = JsonUtils.DeepCopy(original)!,
            Mutations = [],
            State = ERepairCandidateState.RuntimeValidated,
            Validation = new RepairValidationEvidence { Attempts = 1, Successes = 1, ConsecutiveSuccesses = 1 },
            RequiredRuntimeSuccesses = 1,
        };
        var plan = new RepairPromotionService().Prepare(session, candidate);
        var edited = JsonUtils.DeepCopy(original)!;
        edited.Password = "22222222-2222-4222-8222-222222222222";
        var service = new RepairPromotionService(
            profileLoader: _ => Task.FromResult<ProfileItem?>(edited));

        var threw = false;
        try
        {
            await service.PromoteAsync(new Config(), plan, makeDefault: false);
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("source profile changed", StringComparison.OrdinalIgnoreCase);
        }

        await threw.Should().BeTrue();
        await plan.ChildProfile.IndexId.Should().BeEmpty();
    }

    [Test]
    public async Task Promote_ShouldRemoveChildWhenSourceChangesDuringInsert()
    {
        var original = new ProfileItem
        {
            IndexId = "original",
            ConfigType = EConfigType.VLESS,
            Address = "old.example",
            Port = 443,
            Password = "11111111-1111-4111-8111-111111111111",
        };
        var session = new RepairSession { Id = "session", Original = ProfileSnapshot.Capture(original) };
        var candidate = new RepairCandidate
        {
            Id = "candidate",
            SessionId = session.Id,
            Profile = JsonUtils.DeepCopy(original)!,
            Mutations = [],
            State = ERepairCandidateState.RuntimeValidated,
            Validation = new RepairValidationEvidence { Attempts = 1, Successes = 1, ConsecutiveSuccesses = 1 },
            RequiredRuntimeSuccesses = 1,
        };
        var plan = new RepairPromotionService().Prepare(session, candidate);
        var edited = JsonUtils.DeepCopy(original)!;
        edited.Password = "22222222-2222-4222-8222-222222222222";

        var loaderCalls = 0;
        var removeCalls = 0;
        var service = new RepairPromotionService(
            profileLoader: _ =>
            {
                loaderCalls++;
                return Task.FromResult<ProfileItem?>(loaderCalls == 1 ? original : edited);
            },
            removeServers: (_, profiles) =>
            {
                removeCalls++;
                return Task.FromResult(profiles.Count == 1 && profiles[0].IndexId == "promoted" ? 0 : -1);
            },
            addServer: (_, child) =>
            {
                child.IndexId = "promoted";
                return Task.FromResult(0);
            });

        var threw = false;
        try
        {
            await service.PromoteAsync(new Config(), plan, makeDefault: false);
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("source profile changed during promotion", StringComparison.OrdinalIgnoreCase)
                    && ex.Message.Contains("child was removed", StringComparison.OrdinalIgnoreCase);
        }

        await threw.Should().BeTrue();
        await loaderCalls.Should().BeEqualTo(2);
        await removeCalls.Should().BeEqualTo(1);
    }

    [Test]
    public async Task Rollback_ShouldNotDeleteProfileSelectedAfterNonDefaultPromotion()
    {
        var promoted = new ProfileItem { IndexId = "promoted", Address = "203.0.113.10", Port = 443 };
        var config = new Config { IndexId = promoted.IndexId };
        var removeCount = 0;
        var saveCount = 0;
        var service = new RepairPromotionService(
            profileLoader: id => Task.FromResult<ProfileItem?>(id == promoted.IndexId ? promoted : null),
            removeServers: (_, _) =>
            {
                removeCount++;
                return Task.FromResult(0);
            },
            saveConfig: _ =>
            {
                saveCount++;
                return Task.FromResult(0);
            });

        var receipt = new RepairPromotionReceipt
        {
            SessionId = "session",
            CandidateId = "candidate",
            OriginalProfileId = "original",
            PromotedProfileId = promoted.IndexId,
            PreviousDefaultProfileId = "previous",
            BecameDefault = false,
        };

        var threw = false;
        try
        {
            await service.RollbackAsync(config, receipt);
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("newer user choice", StringComparison.OrdinalIgnoreCase);
        }

        await threw.Should().BeTrue();
        await removeCount.Should().BeEqualTo(0);
        await saveCount.Should().BeEqualTo(0);
        await config.IndexId.Should().BeEqualTo(promoted.IndexId);
    }

    [Test]
    public async Task Rollback_ShouldCompensateDefaultWhenTransactionalRemovalThrows()
    {
        var promoted = new ProfileItem { IndexId = "promoted", Address = "203.0.113.10", Port = 443 };
        var previous = new ProfileItem { IndexId = "previous", Address = "old.example", Port = 443 };
        var config = new Config { IndexId = promoted.IndexId };
        var saves = new List<string>();

        Task<ProfileItem?> Load(string id)
            => Task.FromResult<ProfileItem?>(id switch
            {
                "promoted" => promoted,
                "previous" => previous,
                _ => null,
            });

        var service = new RepairPromotionService(
            profileLoader: Load,
            removeServers: (_, _) => throw new InvalidOperationException("simulated transactional delete failure"),
            saveConfig: value =>
            {
                saves.Add(value.IndexId);
                return Task.FromResult(0);
            });

        var receipt = new RepairPromotionReceipt
        {
            SessionId = "session",
            CandidateId = "candidate",
            OriginalProfileId = "original",
            PromotedProfileId = promoted.IndexId,
            PreviousDefaultProfileId = previous.IndexId,
            BecameDefault = true,
        };

        var threw = false;
        try
        {
            await service.RollbackAsync(config, receipt);
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("remove the promoted repair profile", StringComparison.OrdinalIgnoreCase);
        }

        await threw.Should().BeTrue();
        await saves.SequenceEqual(["previous", "promoted"]).Should().BeTrue();
        await config.IndexId.Should().BeEqualTo("promoted");
    }

    [Test]
    public async Task Rollback_ShouldRejectMissingPreviousDefaultBeforeMutation()
    {
        var promoted = new ProfileItem { IndexId = "promoted", Address = "203.0.113.10", Port = 443 };
        var config = new Config { IndexId = promoted.IndexId };
        var saveCount = 0;
        var removeCount = 0;

        var service = new RepairPromotionService(
            profileLoader: id => Task.FromResult<ProfileItem?>(id == promoted.IndexId ? promoted : null),
            removeServers: (_, _) =>
            {
                removeCount++;
                return Task.FromResult(0);
            },
            saveConfig: _ =>
            {
                saveCount++;
                return Task.FromResult(0);
            });

        var receipt = new RepairPromotionReceipt
        {
            SessionId = "session",
            CandidateId = "candidate",
            OriginalProfileId = "original",
            PromotedProfileId = promoted.IndexId,
            PreviousDefaultProfileId = "missing",
            BecameDefault = true,
        };

        var threw = false;
        try
        {
            await service.RollbackAsync(config, receipt);
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("previous default profile no longer exists", StringComparison.OrdinalIgnoreCase);
        }

        await threw.Should().BeTrue();
        await config.IndexId.Should().BeEqualTo("promoted");
        await saveCount.Should().BeEqualTo(0);
        await removeCount.Should().BeEqualTo(0);
    }

    [Test]
    public async Task Rollback_ShouldRejectEmptyPreviousDefaultBeforeMutation()
    {
        var promoted = new ProfileItem { IndexId = "promoted", Address = "203.0.113.10", Port = 443 };
        var config = new Config { IndexId = promoted.IndexId };
        var saveCount = 0;
        var removeCount = 0;

        var service = new RepairPromotionService(
            profileLoader: id => Task.FromResult<ProfileItem?>(id == promoted.IndexId ? promoted : null),
            removeServers: (_, _) =>
            {
                removeCount++;
                return Task.FromResult(0);
            },
            saveConfig: _ =>
            {
                saveCount++;
                return Task.FromResult(0);
            });

        var receipt = new RepairPromotionReceipt
        {
            SessionId = "session",
            CandidateId = "candidate",
            OriginalProfileId = "original",
            PromotedProfileId = promoted.IndexId,
            PreviousDefaultProfileId = string.Empty,
            BecameDefault = true,
        };

        var threw = false;
        try
        {
            await service.RollbackAsync(config, receipt);
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("no previous default", StringComparison.OrdinalIgnoreCase);
        }

        await threw.Should().BeTrue();
        await config.IndexId.Should().BeEqualTo(promoted.IndexId);
        await saveCount.Should().BeEqualTo(0);
        await removeCount.Should().BeEqualTo(0);
    }

    [Test]
    public async Task Prepare_ShouldRejectRuntimeValidatedCandidateBelowItsRecordedQuorum()
    {
        var profile = new ProfileItem { IndexId = "original", Address = "example.com", Port = 443 };
        var session = new RepairSession { Id = "session", Original = ProfileSnapshot.Capture(profile) };
        var candidate = new RepairCandidate
        {
            SessionId = session.Id,
            Profile = JsonUtils.DeepCopy(profile)!,
            Mutations = [],
            State = ERepairCandidateState.RuntimeValidated,
            Validation = new RepairValidationEvidence { Attempts = 3, Successes = 1, ConsecutiveSuccesses = 1 },
            RequiredRuntimeSuccesses = 2,
        };

        var threw = false;
        try
        {
            _ = new RepairPromotionService().Prepare(session, candidate);
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("quorum", StringComparison.OrdinalIgnoreCase);
        }

        await threw.Should().BeTrue();
    }

    [Test]
    public async Task Prepare_ShouldRejectUndeclaredCandidateMutation()
    {
        var profile = new ProfileItem { IndexId = "original", Address = "old.example", Port = 443 };
        var session = new RepairSession { Id = "session", Original = ProfileSnapshot.Capture(profile) };
        var changed = JsonUtils.DeepCopy(profile)!;
        changed.Address = "203.0.113.7";
        var candidate = new RepairCandidate
        {
            SessionId = session.Id,
            Profile = changed,
            Mutations = [],
            State = ERepairCandidateState.RuntimeValidated,
            Validation = new RepairValidationEvidence { Attempts = 2, Successes = 2, ConsecutiveSuccesses = 2 },
            RequiredRuntimeSuccesses = 2,
        };

        var threw = false;
        try
        {
            _ = new RepairPromotionService().Prepare(session, candidate);
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("not declared", StringComparison.OrdinalIgnoreCase);
        }

        await threw.Should().BeTrue();
    }

    [Test]
    public async Task Promote_ShouldReportPartialSuccessWhenHistoryPersistenceFails()
    {
        var original = new ProfileItem
        {
            IndexId = "original",
            ConfigType = EConfigType.VLESS,
            Address = "old.example",
            Port = 443,
            Password = "11111111-1111-4111-8111-111111111111",
        };
        var session = new RepairSession { Id = "session", Original = ProfileSnapshot.Capture(original) };
        var candidate = new RepairCandidate
        {
            Id = "candidate",
            SessionId = session.Id,
            Profile = JsonUtils.DeepCopy(original)!,
            Mutations = [],
            State = ERepairCandidateState.RuntimeValidated,
            Validation = new RepairValidationEvidence { Attempts = 1, Successes = 1, ConsecutiveSuccesses = 1 },
            RequiredRuntimeSuccesses = 1,
        };
        var plan = new RepairPromotionService().Prepare(session, candidate);
        var history = new FailingPromotionHistoryStore { FailPromotion = true };
        var service = new RepairPromotionService(
            historyStore: history,
            profileLoader: _ => Task.FromResult<ProfileItem?>(original),
            addServer: (_, profile) =>
            {
                profile.IndexId = "promoted";
                return Task.FromResult(0);
            });

        var threw = false;
        try
        {
            await service.PromoteAsync(new Config(), plan, makeDefault: false);
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("promotion history", StringComparison.OrdinalIgnoreCase)
                    && ex.Message.Contains("remains installed", StringComparison.OrdinalIgnoreCase);
        }

        await threw.Should().BeTrue();
        await plan.ChildProfile.IndexId.Should().BeEqualTo("promoted");
        await history.PromotionCalls.Should().BeEqualTo(1);
    }

    [Test]
    public async Task Rollback_ShouldReportPartialSuccessWhenHistoryPersistenceFails()
    {
        var promoted = new ProfileItem { IndexId = "promoted", Address = "203.0.113.10", Port = 443 };
        var history = new FailingPromotionHistoryStore { FailRollback = true };
        var removeCount = 0;
        var service = new RepairPromotionService(
            historyStore: history,
            profileLoader: id => Task.FromResult<ProfileItem?>(id == promoted.IndexId ? promoted : null),
            removeServers: (_, _) =>
            {
                removeCount++;
                return Task.FromResult(0);
            });
        var receipt = new RepairPromotionReceipt
        {
            SessionId = "session",
            CandidateId = "candidate",
            OriginalProfileId = "original",
            PromotedProfileId = promoted.IndexId,
            PreviousDefaultProfileId = "previous",
            BecameDefault = false,
        };

        var threw = false;
        try
        {
            await service.RollbackAsync(new Config { IndexId = "other" }, receipt);
        }
        catch (InvalidOperationException ex)
        {
            threw = ex.Message.Contains("rollback history", StringComparison.OrdinalIgnoreCase)
                    && ex.Message.Contains("already changed", StringComparison.OrdinalIgnoreCase);
        }

        await threw.Should().BeTrue();
        await removeCount.Should().BeEqualTo(1);
        await history.RollbackCalls.Should().BeEqualTo(1);
    }

    [Test]
    public async Task Prepare_ShouldRejectCandidateFromAnotherSession()
    {
        var profile = new ProfileItem { IndexId = "original" };
        var session = new RepairSession { Id = "one", Original = ProfileSnapshot.Capture(profile) };
        var candidate = new RepairCandidate
        {
            SessionId = "two",
            Profile = new ProfileItem(),
            Mutations = [],
            State = ERepairCandidateState.RuntimeValidated,
            Validation = new RepairValidationEvidence { Attempts = 1, Successes = 1, ConsecutiveSuccesses = 1 },
        };

        var threw = false;
        try
        {
            _ = new RepairPromotionService().Prepare(session, candidate);
        }
        catch (InvalidOperationException)
        {
            threw = true;
        }

        await threw.Should().BeTrue();
    }

    private sealed class FailingPromotionHistoryStore : IRepairPromotionHistoryStore
    {
        public bool FailPromotion { get; init; }
        public bool FailRollback { get; init; }
        public int PromotionCalls { get; private set; }
        public int RollbackCalls { get; private set; }

        public Task RecordPromotedAsync(
            RepairPromotionPlan plan,
            RepairPromotionReceipt receipt,
            CancellationToken cancellationToken = default)
        {
            PromotionCalls++;
            return FailPromotion
                ? Task.FromException(new InvalidOperationException("simulated promotion history failure"))
                : Task.CompletedTask;
        }

        public Task RecordRolledBackAsync(
            RepairPromotionReceipt receipt,
            CancellationToken cancellationToken = default)
        {
            RollbackCalls++;
            return FailRollback
                ? Task.FromException(new InvalidOperationException("simulated rollback history failure"))
                : Task.CompletedTask;
        }
    }
}
