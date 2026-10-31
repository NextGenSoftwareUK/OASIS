using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class StorageQueryTariffAbstractTests
{
    [Fact]
    public void ExplicitAbstractReadTariffRequiresPermissionAndCannotPriceSignedStorage()
    {
        var tariff = new StorageQueryTariff(ProviderType.AbstractOASIS, 0, "public-read-v1");
        Assert.Single(StorageQueryTariff.Capture(new[] { tariff }, new[] { ProviderType.AbstractOASIS }));
        Assert.Throws<ArgumentException>(() => StorageQueryTariff.Capture(new[] { tariff }, Array.Empty<ProviderType>()));
        Assert.Throws<ArgumentException>(() => StorageAttemptTariff.Capture(new[] {
            new StorageAttemptTariff(ProviderType.AbstractOASIS, "SaveHolon", 0, "public-read-v1") }, new[] { ProviderType.AbstractOASIS }));
    }
}
