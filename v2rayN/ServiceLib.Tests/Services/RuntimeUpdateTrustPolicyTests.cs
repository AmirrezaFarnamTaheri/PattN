using ServiceLib.Helper;
using ServiceLib.Services;

namespace ServiceLib.Tests.Services;

public class RuntimeUpdateTrustPolicyTests
{
    [Test]
    public async Task LegacyRuntimeUpdater_ShouldRemainFailClosedWithoutPinnedTrustRoot()
    {
        await RuntimeUpdateTrustPolicy.BlockUnauthenticatedLegacyUpdater.Should().BeTrue();

        var service = new UpdateService(
            new Config(),
            (_, _) => Task.CompletedTask);

        var result = await service.CheckHasUpdateOnly(
            ECoreType.v2rayN,
            preRelease: false,
            blProxy: false);

        await result.Success.Should().BeFalse();
        await result.Msg.Should().BeEqualTo(ResUI.MsgRuntimeUpdateAuthenticationRequired);
    }
}
