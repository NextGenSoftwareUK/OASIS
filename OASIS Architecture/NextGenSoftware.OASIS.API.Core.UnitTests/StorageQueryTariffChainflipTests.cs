using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class StorageQueryTariffChainflipTests
{
    [Fact]
    public void ExplicitChainflipReadTariffRequiresPermissionAndCannotPriceCrossChainDispatch()
    {
        var tariff = new StorageQueryTariff(ProviderType.ChainflipOASIS, 0, "public-read-v1");
        Assert.Single(StorageQueryTariff.Capture(new[] { tariff }, new[] { ProviderType.ChainflipOASIS }));
        Assert.Throws<ArgumentException>(() => StorageQueryTariff.Capture(new[] { tariff }, Array.Empty<ProviderType>()));
        Assert.Throws<ArgumentException>(() => StorageAttemptTariff.Capture(new[] {
            new StorageAttemptTariff(ProviderType.ChainflipOASIS, "SaveHolon", 0, "public-read-v1") }, new[] { ProviderType.ChainflipOASIS }));
    }
}
