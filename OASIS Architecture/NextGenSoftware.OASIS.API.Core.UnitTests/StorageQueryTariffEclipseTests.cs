using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class StorageQueryTariffEclipseTests
{
    [Fact]
    public void ExplicitEclipseReadTariffRequiresPermissionAndCannotPriceTransactionSubmission()
    {
        var tariff = new StorageQueryTariff(ProviderType.EclipseOASIS, 0, "public-read-v1");
        Assert.Single(StorageQueryTariff.Capture(new[] { tariff }, new[] { ProviderType.EclipseOASIS }));
        Assert.Throws<ArgumentException>(() => StorageQueryTariff.Capture(new[] { tariff }, Array.Empty<ProviderType>()));
        Assert.Throws<ArgumentException>(() => StorageAttemptTariff.Capture(new[] {
            new StorageAttemptTariff(ProviderType.EclipseOASIS, "SaveHolon", 0, "public-read-v1") }, new[] { ProviderType.EclipseOASIS }));
    }
}
