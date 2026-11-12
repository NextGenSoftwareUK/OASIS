using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class StorageQueryTariffAlgorandTests
{
    [Fact]
    public void ExplicitAlgorandReadTariffRequiresPermissionAndCannotPriceSignedStorage()
    {
        var tariff = new StorageQueryTariff(ProviderType.AlgorandOASIS, 0, "public-read-v1");
        Assert.Single(StorageQueryTariff.Capture(new[] { tariff }, new[] { ProviderType.AlgorandOASIS }));
        Assert.Throws<ArgumentException>(() => StorageQueryTariff.Capture(new[] { tariff }, Array.Empty<ProviderType>()));
        Assert.Throws<ArgumentException>(() => StorageAttemptTariff.Capture(new[] {
            new StorageAttemptTariff(ProviderType.AlgorandOASIS, "SaveHolon", 0, "public-read-v1") }, new[] { ProviderType.AlgorandOASIS }));
    }
}
