using ServiceLib.Reviver.Intelligence;

namespace ServiceLib.Tests.Reviver;

public class IntelligenceRetentionTests
{
    [Test]
    public async Task Policy_ShouldRejectAggregateRetentionShorterThanRawRetention()
    {
        var service = new IntelligenceRetentionService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
            await service.RollupAndPruneAsync(new IntelligenceRetentionPolicy
            {
                RawRetentionDays = 90,
                AggregateRetentionDays = 30,
            }));
    }
}
