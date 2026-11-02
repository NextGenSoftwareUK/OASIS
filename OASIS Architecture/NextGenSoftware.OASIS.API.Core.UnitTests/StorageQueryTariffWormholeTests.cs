using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class StorageQueryTariffWormholeTests
{
    [Fact]
    public void ExplicitWormholeReadTariffRequiresPermissionAndCannotPriceCrossChainDispatch()
    {
        var tariff = new StorageQueryTariff(ProviderType.WormholeOASIS, 0, "public-read-v1");
        Assert.Single(StorageQueryTariff.Capture(new[] { tariff }, new[] { ProviderType.WormholeOASIS }));
        Assert.Throws<ArgumentException>(() => StorageQueryTariff.Capture(new[] { tariff }, Array.Empty<ProviderType>()));
        Assert.Throws<ArgumentException>(() => StorageAttemptTariff.Capture(new[] {
            new StorageAttemptTariff(ProviderType.WormholeOASIS, "SaveHolon", 0, "public-read-v1") }, new[] { ProviderType.WormholeOASIS }));
    }
}
