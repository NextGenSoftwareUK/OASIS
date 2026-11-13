using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class StorageQueryTariffClickHouseTests
{
    [Fact]
    public void ExplicitClickHouseReadTariffRequiresPermissionAndCannotPriceWalletWrites()
    {
        var tariff = new StorageQueryTariff(ProviderType.ClickHouseOASIS, 0, "public-read-v1");
        Assert.Single(StorageQueryTariff.Capture(new[] { tariff }, new[] { ProviderType.ClickHouseOASIS }));
        Assert.Throws<ArgumentException>(() => StorageQueryTariff.Capture(new[] { tariff }, Array.Empty<ProviderType>()));
        Assert.Throws<ArgumentException>(() => StorageAttemptTariff.Capture(new[] {
            new StorageAttemptTariff(ProviderType.ClickHouseOASIS, "SaveHolon", 0, "public-read-v1") }, new[] { ProviderType.ClickHouseOASIS }));
    }
}
