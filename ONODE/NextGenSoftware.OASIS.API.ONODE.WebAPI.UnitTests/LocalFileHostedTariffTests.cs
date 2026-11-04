using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class LocalFileHostedTariffTests
{
    [Fact]
    public void LocalFileTariffRequiresExplicitQueryOnlyEnrollment()
    {
        var provider = ProviderType.LocalFileOASIS;
        var tariffs = new[] { new StorageQueryTariff(provider, 0, "demo-read-v1") };
        var ordinary = new[] { ProviderType.MongoDBOASIS };
        var native = Array.Empty<NativeStorageAdmissionBinding>();
        var admission = new PricedStorageAdmissionFactory(null, native, ordinary,
            tariffs, null, new[] { provider });

        Assert.Contains(provider, admission.QueryProviders);
        Assert.Contains(provider, admission.QueryOnlyProviders);
        Assert.Throws<ArgumentException>(() =>
            new PricedStorageAdmissionFactory(null, native, ordinary, tariffs));
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null,
            native, ordinary, tariffs,
            new[] { new StorageAttemptTariff(provider, "SaveHolon", 0, "demo-read-v1") },
            new[] { provider }));
    }
}
