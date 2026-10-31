using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class StorageQueryTariffNearTests
{
    [Fact]
    public void ExplicitNearReadTariffRequiresPermissionAndCannotPriceSignedStorage()
    {
        var tariff = new StorageQueryTariff(ProviderType.NEAROASIS, 0, "public-read-v1");
        Assert.Single(StorageQueryTariff.Capture(new[] { tariff }, new[] { ProviderType.NEAROASIS }));
        Assert.Throws<ArgumentException>(() => StorageQueryTariff.Capture(new[] { tariff }, Array.Empty<ProviderType>()));
        Assert.Throws<ArgumentException>(() => StorageAttemptTariff.Capture(new[] {
            new StorageAttemptTariff(ProviderType.NEAROASIS, "SaveHolon", 0, "public-read-v1") }, new[] { ProviderType.NEAROASIS }));
    }
}
