using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class StorageQueryTariffStacksTests
{
    [Fact]
    public void ExplicitStacksReadTariffRequiresPermissionAndCannotPriceAuthenticatedStorage()
    {
        var tariff = new StorageQueryTariff(ProviderType.BlockStackOASIS, 0, "public-read-v1");
        Assert.Single(StorageQueryTariff.Capture(new[] { tariff }, new[] { ProviderType.BlockStackOASIS }));
        Assert.Throws<ArgumentException>(() => StorageQueryTariff.Capture(new[] { tariff }, Array.Empty<ProviderType>()));
        Assert.Throws<ArgumentException>(() => StorageAttemptTariff.Capture(new[] {
            new StorageAttemptTariff(ProviderType.BlockStackOASIS, "SaveHolon", 0, "public-read-v1") }, new[] { ProviderType.BlockStackOASIS }));
    }
}
