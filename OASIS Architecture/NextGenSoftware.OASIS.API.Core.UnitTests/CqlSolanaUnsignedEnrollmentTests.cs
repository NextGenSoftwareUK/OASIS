using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CqlSolanaUnsignedEnrollmentTests
{
    [Fact]
    public void ExplicitUnsignedReadTariffDoesNotGrantOrdinaryStorageOrImplicitProviderSelection()
    {
        var provider = ProviderType.SolanaOASIS;
        var tariff = new StorageQueryTariff(provider, 0, "public-account-test");
        Assert.Single(StorageQueryTariff.Capture(new[] { tariff }, new[] { provider }));
        Assert.Throws<ArgumentException>(() => StorageQueryTariff.Capture(new[] { tariff }, Array.Empty<ProviderType>()));
        Assert.False(CqlV1Compiler.Supported(provider));
        Assert.Throws<ArgumentException>(() => CqlV1Compiler.RequestedProvider("SolanaOASIS"));
        Assert.Equal(provider, CqlV1Compiler.RequestedProvider("SolanaOASIS", true));
        foreach (var operation in StorageAttemptTariff.SupportedOperations)
            Assert.Throws<ArgumentException>(() => StorageAttemptTariff.Capture(
                new[] { new StorageAttemptTariff(provider, operation, 0, "public-account-test") }, new[] { provider }));
    }
}
