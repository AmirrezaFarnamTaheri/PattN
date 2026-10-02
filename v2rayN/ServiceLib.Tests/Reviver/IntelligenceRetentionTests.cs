using ServiceLib.Reviver.Intelligence;

namespace ServiceLib.Tests.Reviver;

public class IntelligenceRetentionTests
{
    [Test]
    public async Task Policy_ShouldRejectAggregateRetentionShorterThanRawRetention()
    {
        var service = new IntelligenceRetentionService();

        var threw = false;
        try
        {
            await service.RollupAndPruneAsync(new IntelligenceRetentionPolicy
            {
                RawRetentionDays = 90,
                AggregateRetentionDays = 30,
            });
        }
        catch (ArgumentOutOfRangeException)
        {
            threw = true;
        }

        await threw.Should().BeTrue();
    }
}
