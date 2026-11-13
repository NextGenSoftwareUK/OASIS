using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class StorageQueryTariffZcashTests
{
    [Fact]
    public void ExplicitZcashReadTariffRequiresPermissionAndCannotPriceWalletWrites()
    {
        var tariff = new StorageQueryTariff(ProviderType.ZcashOASIS, 0, "public-read-v1");
        Assert.Single(StorageQueryTariff.Capture(new[] { tariff }, new[] { ProviderType.ZcashOASIS }));
        Assert.Throws<ArgumentException>(() => StorageQueryTariff.Capture(new[] { tariff }, Array.Empty<ProviderType>()));
        Assert.Throws<ArgumentException>(() => StorageAttemptTariff.Capture(new[] {
            new StorageAttemptTariff(ProviderType.ZcashOASIS, "SaveHolon", 0, "public-read-v1") }, new[] { ProviderType.ZcashOASIS }));
    }
}
