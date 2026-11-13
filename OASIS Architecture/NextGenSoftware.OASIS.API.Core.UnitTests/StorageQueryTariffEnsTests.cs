using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class StorageQueryTariffEnsTests
{
    [Fact]
    public void ExplicitEnsReadTariffRequiresPermissionAndCannotPriceTransactionSubmission()
    {
        var tariff = new StorageQueryTariff(ProviderType.ENSOASIS, 0, "public-read-v1");
        Assert.Single(StorageQueryTariff.Capture(new[] { tariff }, new[] { ProviderType.ENSOASIS }));
        Assert.Throws<ArgumentException>(() => StorageQueryTariff.Capture(new[] { tariff }, Array.Empty<ProviderType>()));
        Assert.Throws<ArgumentException>(() => StorageAttemptTariff.Capture(new[] {
            new StorageAttemptTariff(ProviderType.ENSOASIS, "SaveHolon", 0, "public-read-v1") }, new[] { ProviderType.ENSOASIS }));
    }
}
