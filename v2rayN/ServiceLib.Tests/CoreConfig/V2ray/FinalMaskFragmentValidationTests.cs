namespace ServiceLib.Tests.CoreConfig.V2ray;

public class FinalMaskFragmentValidationTests
{
    [Test]
    public async Task GenerateClientConfigContent_NegativeFragmentLength_ShouldFailClosed()
    {
        var (service, config) = CreateService();
        config.Fragment4RayItem.Lengths = ["-5"];

        var result = service.GenerateClientConfigContent();

        await result.Success.Should().BeFalse();
    }

    [Test]
    public async Task GenerateClientConfigContent_NegativeFragmentDelay_ShouldFailClosed()
    {
        var (service, config) = CreateService();
        config.Fragment4RayItem.Delays = ["-1"];

        var result = service.GenerateClientConfigContent();

        await result.Success.Should().BeFalse();
    }

    [Test]
    public async Task GenerateClientConfigContent_NegativeFragmentMaxSplit_ShouldFailClosed()
    {
        var (service, config) = CreateService();
        config.Fragment4RayItem.MaxSplit = "-1";

        var result = service.GenerateClientConfigContent();

        await result.Success.Should().BeFalse();
    }

    [Test]
    public async Task GenerateClientConfigContent_ValidFragmentRanges_ShouldStillGenerate()
    {
        var (service, config) = CreateService();
        config.Fragment4RayItem.Lengths = ["0", "104", "1"];
        config.Fragment4RayItem.Delays = ["0"];
        config.Fragment4RayItem.MaxSplit = "0";

        var result = service.GenerateClientConfigContent();

        await result.Success.Should().BeTrue();
    }

    private static (CoreConfigV2rayService Service, Config Config) CreateService()
    {
        var config = CoreConfigTestFactory.CreateConfig(ECoreType.Xray);
        config.CoreBasicItem.EnableFragment = true;
        CoreConfigTestFactory.BindAppManagerConfig(config);

        var node = CoreConfigTestFactory.CreateVmessNode(ECoreType.Xray);
        node.StreamSecurity = Global.StreamSecurity;
        node.Sni = "example.com";

        var context = CoreConfigTestFactory.CreateContext(config, node, ECoreType.Xray);
        return (new CoreConfigV2rayService(context), config);
    }
}
