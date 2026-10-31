using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class StorageQueryTariffBnbTests
{
    [Fact]
    public void ExplicitBnbReadTariffRequiresPermissionAndCannotPriceSignedStorage()
    {
        var tariff = new StorageQueryTariff(ProviderType.BNBChainOASIS, 0, "public-read-v1");
        Assert.Single(StorageQueryTariff.Capture(new[] { tariff }, new[] { ProviderType.BNBChainOASIS }));
        Assert.Throws<ArgumentException>(() => StorageQueryTariff.Capture(new[] { tariff }, Array.Empty<ProviderType>()));
        Assert.Throws<ArgumentException>(() => StorageAttemptTariff.Capture(new[] {
            new StorageAttemptTariff(ProviderType.BNBChainOASIS, "SaveHolon", 0, "public-read-v1") }, new[] { ProviderType.BNBChainOASIS }));
    }
}
