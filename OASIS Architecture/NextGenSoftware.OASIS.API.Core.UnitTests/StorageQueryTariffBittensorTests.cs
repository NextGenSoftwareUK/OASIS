using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class StorageQueryTariffBittensorTests
{
    [Fact]
    public void ExplicitBittensorReadTariffRequiresPermissionAndCannotPriceComputeProvisioning()
    {
        var tariff = new StorageQueryTariff(ProviderType.BittensorOASIS, 0, "public-read-v1");
        Assert.Single(StorageQueryTariff.Capture(new[] { tariff }, new[] { ProviderType.BittensorOASIS }));
        Assert.Throws<ArgumentException>(() => StorageQueryTariff.Capture(new[] { tariff }, Array.Empty<ProviderType>()));
        Assert.Throws<ArgumentException>(() => StorageAttemptTariff.Capture(new[] {
            new StorageAttemptTariff(ProviderType.BittensorOASIS, "SaveHolon", 0, "public-read-v1") }, new[] { ProviderType.BittensorOASIS }));
    }
}
