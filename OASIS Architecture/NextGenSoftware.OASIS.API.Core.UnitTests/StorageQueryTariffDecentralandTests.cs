using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class StorageQueryTariffDecentralandTests
{
    [Fact]
    public void DecentralandReadTariffRequiresPermissionAndDoesNotEnableWrites()
    {
        var provider = ProviderType.DecentralandOASIS;
        var allowed = new[] { provider };
        var queryTariffs = new[] { new StorageQueryTariff(provider, 0, "public-read-v1") };
        var writeTariffs = new[] { new StorageAttemptTariff(provider, "SaveHolon", 0, "public-read-v1") };

        Assert.Equal(queryTariffs[0], Assert.Single(StorageQueryTariff.Capture(queryTariffs, allowed)));
        Assert.Throws<ArgumentException>(() => StorageQueryTariff.Capture(queryTariffs, Array.Empty<ProviderType>()));
        Assert.Throws<ArgumentException>(() => StorageAttemptTariff.Capture(writeTariffs, allowed));
    }
}
