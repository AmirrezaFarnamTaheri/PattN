using ServiceLib.Tests.CoreConfig;

namespace ServiceLib.Tests.Handler;

public class NodeValidatorEchOutboundTests
{
    private const string EchConfigList = "cloudflare-ech.com+https://1.1.1.1/dns-query";

    [Test]
    [Arguments("")]
    [Arguments("  \n")]
    public async Task ValidateEchOutbound_Empty_ShouldPass(string echOutbound)
    {
        await NodeValidator.ValidateEchOutbound(CreateNode(echOutbound)).Should().BeNull();
    }

    [Test]
    [Arguments("freedom")]
    [Arguments("""[{"tag": "ech-out"}]""")]
    [Arguments("{\"tag\": \"ech-out\", \"protocol\": \"freedom\"")]
    public async Task ValidateEchOutbound_NotJsonObject_ShouldFail(string echOutbound)
    {
        await NodeValidator.ValidateEchOutbound(CreateNode(echOutbound))
            .Should().BeEqualTo(string.Format(ResUI.MsgInvalidProperty, ResUI.TbEchOutbound));
    }

    [Test]
    public async Task ValidateEchOutbound_WithoutEchConfigList_ShouldFail()
    {
        var node = CreateNode("""{"tag": "ech-out", "protocol": "freedom"}""");
        node.EchConfigList = string.Empty;

        await NodeValidator.ValidateEchOutbound(node).Should().BeEqualTo(ResUI.MsgEchOutboundNeedsEchConfigList);
    }

    [Test]
    [Arguments("""{"protocol": "freedom"}""")]
    [Arguments("""{"tag": "", "protocol": "freedom"}""")]
    [Arguments("""{"tag": " ", "protocol": "freedom"}""")]
    [Arguments("""{"tag": 1, "protocol": "freedom"}""")]
    [Arguments("""{"tag": "direct", "protocol": "freedom"}""")]
    [Arguments("""{"tag": "block", "protocol": "freedom"}""")]
    [Arguments("""{"tag": "proxy", "protocol": "freedom"}""")]
    [Arguments("""{"tag": "proxy2", "protocol": "freedom"}""")]
    [Arguments("""{"tag": "proxy-ech", "protocol": "freedom"}""")]
    public async Task ValidateEchOutbound_MissingOrReservedTag_ShouldFail(string echOutbound)
    {
        await NodeValidator.ValidateEchOutbound(CreateNode(echOutbound)).Should().BeEqualTo(ResUI.MsgEchOutboundInvalidTag);
    }

    [Test]
    [Arguments("""{"tag": "ech-proxy", "protocol": "freedom"}""")]
    [Arguments("""{"tag": "Proxy", "protocol": "freedom"}""")]
    [Arguments("""{"tag": "directly", "protocol": "freedom"}""")]
    public async Task ValidateEchOutbound_OtherTags_ShouldPass(string echOutbound)
    {
        // Only the exact direct/block tags and the "proxy" prefix that balancers select by are taken.
        await NodeValidator.ValidateEchOutbound(CreateNode(echOutbound)).Should().BeNull();
    }

    [Test]
    public async Task Validate_ShouldAcceptAValidEchOutboundAndRejectAnInvalidOne()
    {
        // The checks made when saving are made again when a config is generated, for imported profiles.
        var node = CreateNode("""
            {
              "tag": "ech-out",
              "protocol": "freedom"
            }
            """);

        await NodeValidator.ValidateEchOutbound(node).Should().BeNull();
        await NodeValidator.Validate(node, ECoreType.Xray).Success.Should().BeTrue();

        node.EchOutbound = """{"tag": "proxy", "protocol": "freedom"}""";
        var result = NodeValidator.Validate(node, ECoreType.Xray);

        await result.Success.Should().BeFalse();
        await result.Errors.Should().Contain(ResUI.MsgEchOutboundInvalidTag);
    }

    private static ProfileItem CreateNode(string echOutbound)
    {
        var node = CoreConfigTestFactory.CreateVmessNode(ECoreType.Xray);
        node.StreamSecurity = Global.StreamSecurity;
        node.EchConfigList = EchConfigList;
        node.EchOutbound = echOutbound;
        return node;
    }
}
