using ServiceLib.Reviver.Models;
using ServiceLib.Reviver.Normalization;
using ServiceLib.Reviver.Strategies;

namespace ServiceLib.Tests.Reviver;

public class UploadStallMitigationStrategyTests
{
    private const string Template =
        """{"tcp":[{"type":"fragment","settings":{"packets":"tlshello","lengths":["1"],"delays":["0"],"maxSplit":"0"}}]}""";

    [Test]
    public async Task Generate_ShouldOnlyMutateFinalMaskForUploadStall()
    {
        var profile = NewVless();
        var session = new RepairSession { Original = ProfileSnapshot.Capture(profile) };
        var strategy = new UploadStallMitigationStrategy(
            new ProfileCoreCompatibility(),
            Template,
            _ => ECoreType.Xray);

        var candidates = new List<RepairCandidate>();
        await foreach (var candidate in strategy.GenerateAsync(
                           session,
                           ERepairFailureClass.UploadStall))
        {
            candidates.Add(candidate);
        }

        await candidates.Count.Should().BeEqualTo(1);
        await candidates[0].Mutations.Count.Should().BeEqualTo(1);
        await candidates[0].Mutations[0].Field.Should().BeEqualTo(nameof(ProfileItem.Finalmask));
        await candidates[0].Mutations[0].To.Should().BeEqualTo("[configured-finalmask]");
        await candidates[0].Profile.Finalmask.IsNotEmpty().Should().BeTrue();
        await ProfileMutationGuard
            .ChangesOnly(profile, candidates[0].Profile, nameof(ProfileItem.Finalmask))
            .Should().BeTrue();
    }

    [Test]
    public async Task CanApply_ShouldRequireUploadStallXrayAndValidTemplate()
    {
        var profile = NewVless();
        var compatibility = new ProfileCoreCompatibility();

        var strategy = new UploadStallMitigationStrategy(
            compatibility,
            Template,
            _ => ECoreType.Xray);
        var wrongCore = new UploadStallMitigationStrategy(
            compatibility,
            Template,
            _ => ECoreType.sing_box);
        var invalid = new UploadStallMitigationStrategy(
            compatibility,
            """{"notTcp":[]}""",
            _ => ECoreType.Xray);

        await strategy.CanApply(profile, ERepairFailureClass.UploadStall).Should().BeTrue();
        await strategy.CanApply(profile, ERepairFailureClass.ConnectionTimeout).Should().BeFalse();
        await wrongCore.CanApply(profile, ERepairFailureClass.UploadStall).Should().BeFalse();
        await invalid.CanApply(profile, ERepairFailureClass.UploadStall).Should().BeFalse();
    }

    private static ProfileItem NewVless()
        => new()
        {
            ConfigType = EConfigType.VLESS,
            CoreType = ECoreType.Xray,
            Address = "example.com",
            Port = 443,
            Password = Guid.NewGuid().ToString(),
            Network = nameof(ETransport.ws),
            StreamSecurity = "tls",
        };
}
