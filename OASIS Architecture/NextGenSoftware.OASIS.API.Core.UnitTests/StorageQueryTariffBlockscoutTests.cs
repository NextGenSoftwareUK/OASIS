using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class StorageQueryTariffBlockscoutTests
{
    [Fact]
    public void ExplicitBlockscoutReadTariffRequiresPermissionAndCannotPriceTransactionSubmission()
    {
        var tariff = new StorageQueryTariff(ProviderType.BlockscoutOASIS, 0, "public-read-v1");
        Assert.Single(StorageQueryTariff.Capture(new[] { tariff }, new[] { ProviderType.BlockscoutOASIS }));
        Assert.Throws<ArgumentException>(() => StorageQueryTariff.Capture(new[] { tariff }, Array.Empty<ProviderType>()));
        Assert.Throws<ArgumentException>(() => StorageAttemptTariff.Capture(new[] {
            new StorageAttemptTariff(ProviderType.BlockscoutOASIS, "SaveHolon", 0, "public-read-v1") }, new[] { ProviderType.BlockscoutOASIS }));
    }
}
