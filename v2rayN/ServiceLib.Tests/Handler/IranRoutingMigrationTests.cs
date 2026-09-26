using ServiceLib.Handler;

namespace ServiceLib.Tests.Handler;

public class IranRoutingMigrationTests
{
    private static RulesItem DirectRule(params string[] domains) => new()
    {
        OutboundTag = Global.DirectTag,
        Domain = [.. domains],
    };

    private static string Domains(RulesItem rule) => string.Join(",", rule.Domain ?? []);

    [Test]
    public async Task MigrateIranDirectDomains_ShouldRewriteGeositeIr()
    {
        var rules = new List<RulesItem> { DirectRule("geosite:private"), DirectRule("geosite:ir") };

        var changed = ConfigHandler.MigrateIranDirectDomains(rules);

        await changed.Should().BeTrue();
        await Domains(rules[0]).Should().BeEqualTo("geosite:private");
        await Domains(rules[1]).Should().BeEqualTo("domain:ir,geosite:category-ir");
    }

    [Test]
    public async Task MigrateIranDirectDomains_ShouldBeIdempotent()
    {
        var rules = new List<RulesItem> { DirectRule("domain:ir", "geosite:category-ir") };

        var changed = ConfigHandler.MigrateIranDirectDomains(rules);

        await changed.Should().BeFalse();
        await Domains(rules[0]).Should().BeEqualTo("domain:ir,geosite:category-ir");
    }

    [Test]
    public async Task MigrateIranDirectDomains_ShouldNotDuplicateExistingEntries()
    {
        var rules = new List<RulesItem> { DirectRule("domain:ir", "geosite:ir", "geosite:category-ir") };

        var changed = ConfigHandler.MigrateIranDirectDomains(rules);

        await changed.Should().BeTrue();
        await Domains(rules[0]).Should().BeEqualTo("domain:ir,geosite:category-ir");
    }

    [Test]
    public async Task MigrateIranDirectDomains_ShouldLeaveNonDirectRulesAlone()
    {
        var proxyRule = new RulesItem { OutboundTag = Global.ProxyTag, Domain = ["geosite:ir"] };
        var rules = new List<RulesItem> { proxyRule };

        var changed = ConfigHandler.MigrateIranDirectDomains(rules);

        await changed.Should().BeFalse();
        await Domains(proxyRule).Should().BeEqualTo("geosite:ir");
    }

    // The last rule of custom_routing_white_iran up to 7.25.2-P28, exactly as it shipped
    private const string ShippedProxyCatchAll = """
        [{"port":"0-65535","outboundTag":"proxy","enabled":true,"remarks":"\u0633\u0627\u06cc\u0631\u0020\u0645\u0648\u0627\u0631\u062f\u0020\u002d\u0020\u067e\u0631\u0627\u06a9\u0633\u06cc"}]
        """;

    private static RulesItem ShippedProxyCatchAllRule() => JsonUtils.Deserialize<List<RulesItem>>(ShippedProxyCatchAll)![0];

    [Test]
    public async Task RemoveIranProxyCatchAll_ShouldRemoveTheShippedRule()
    {
        var rules = new List<RulesItem> { DirectRule("domain:ir", "geosite:category-ir"), ShippedProxyCatchAllRule() };

        var removed = ConfigHandler.RemoveIranProxyCatchAll(rules);

        await removed.Should().BeEqualTo(1);
        await rules.Count.Should().BeEqualTo(1);
        await Domains(rules[0]).Should().BeEqualTo("domain:ir,geosite:category-ir");
        await ConfigHandler.RemoveIranProxyCatchAll(rules).Should().BeEqualTo(0);
    }

    [Test]
    public async Task RemoveIranProxyCatchAll_ShouldLeaveEditedRulesAlone()
    {
        var rules = new List<RulesItem>();
        foreach (var edit in new Action<RulesItem>[]
        {
            t => t.Domain = ["geosite:google"],
            t => t.Ip = ["1.1.1.1"],
            t => t.Network = "udp",
            t => t.Port = "443",
            t => t.OutboundTag = Global.DirectTag,
            t => t.Remarks = "proxy",
        })
        {
            var rule = ShippedProxyCatchAllRule();
            edit(rule);
            rules.Add(rule);
        }

        var removed = ConfigHandler.RemoveIranProxyCatchAll(rules);

        await removed.Should().BeEqualTo(0);
        await rules.Count.Should().BeEqualTo(6);
    }

    [Test]
    public async Task CustomRoutingWhiteIran_ShouldNotShipTheProxyCatchAll()
    {
        // Fresh installs get the same rules as updaters after the migration
        var rules = JsonUtils.Deserialize<List<RulesItem>>(EmbedUtils.GetEmbedText(Global.CustomRoutingFileName + "white_iran")) ?? [];

        await (rules.Count > 0).Should().BeTrue();
        await ConfigHandler.RemoveIranProxyCatchAll(rules).Should().BeEqualTo(0);
        await rules.Any(t => t.OutboundTag == Global.ProxyTag).Should().BeFalse();
    }
}
