using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class StorageQueryTariffThreeFoldTests
{
    [Fact]
    public void ExplicitThreeFoldReadTariffRequiresPermissionAndCannotPriceProvisioningWrites()
    {
        var tariff = new StorageQueryTariff(ProviderType.ThreeFoldOASIS, 0, "public-read-v1");
        Assert.Single(StorageQueryTariff.Capture(new[] { tariff }, new[] { ProviderType.ThreeFoldOASIS }));
        Assert.Throws<ArgumentException>(() => StorageQueryTariff.Capture(new[] { tariff }, Array.Empty<ProviderType>()));
        Assert.Throws<ArgumentException>(() => StorageAttemptTariff.Capture(new[] {
            new StorageAttemptTariff(ProviderType.ThreeFoldOASIS, "SaveHolon", 0, "public-read-v1") }, new[] { ProviderType.ThreeFoldOASIS }));
    }
}
