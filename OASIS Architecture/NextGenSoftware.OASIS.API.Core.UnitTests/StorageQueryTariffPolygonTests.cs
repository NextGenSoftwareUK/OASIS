using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class StorageQueryTariffPolygonTests
{
    [Fact]
    public void ExplicitPolygonReadTariffRequiresPermissionAndCannotPriceSignedStorage()
    {
        var tariff = new StorageQueryTariff(ProviderType.PolygonOASIS, 0, "public-read-v1");
        Assert.Single(StorageQueryTariff.Capture(new[] { tariff }, new[] { ProviderType.PolygonOASIS }));
        Assert.Throws<ArgumentException>(() => StorageQueryTariff.Capture(new[] { tariff }, Array.Empty<ProviderType>()));
        Assert.Throws<ArgumentException>(() => StorageAttemptTariff.Capture(new[] {
            new StorageAttemptTariff(ProviderType.PolygonOASIS, "SaveHolon", 0, "public-read-v1") }, new[] { ProviderType.PolygonOASIS }));
    }
}
