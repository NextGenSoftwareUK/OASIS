using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class StorageQueryTariffCosmosTests
{
    [Fact]
    public void FourIndependentlyPermittedDatabaseTariffsAreAdmittedTogether()
    {
        var allowed = new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS,
            ProviderType.Neo4jOASIS, ProviderType.AzureCosmosDBOASIS };
        var tariffs = allowed.Select(provider => new StorageQueryTariff(provider, 0.002m, "cloud-test-v1")).ToArray();
        Assert.Equal(tariffs, StorageQueryTariff.Capture(tariffs, allowed));
    }

    [Fact]
    public void CosmosTariffCannotGrantUnpermittedProviderAccess()
        => Assert.Throws<ArgumentException>(() => StorageQueryTariff.Capture(
            new[] { new StorageQueryTariff(ProviderType.AzureCosmosDBOASIS, 0.002m, "cloud-test-v1") },
            new[] { ProviderType.SQLLiteDBOASIS }));

    [Fact]
    public void DuplicateTariffsAreRejectedRatherThanTruncated()
    {
        var tariffs = Enumerable.Range(0, 5).Select(_ => new StorageQueryTariff(ProviderType.AzureCosmosDBOASIS, 0.002m, "cloud-test-v1"));
        Assert.Throws<ArgumentException>(() => StorageQueryTariff.Capture(tariffs, new[] { ProviderType.AzureCosmosDBOASIS }));
    }

    [Fact]
    public void OrdinaryQueryTariffRejectsChainWithoutAnUnsignedReadProfile()
        => Assert.Throws<ArgumentException>(() => StorageQueryTariff.Capture(
            new[] { new StorageQueryTariff(ProviderType.PolygonOASIS, 0.002m, "cloud-test-v1") },
            new[] { ProviderType.PolygonOASIS }));

    [Fact]
    public void ExplicitUnsignedSolanaReadDoesNotGrantSignedStorageWrites()
    {
        var allowed=new[]{ProviderType.SolanaOASIS};
        Assert.Single(StorageQueryTariff.Capture(new[]{new StorageQueryTariff(ProviderType.SolanaOASIS,0,"public-read-v1")},allowed));
        Assert.Throws<ArgumentException>(()=>StorageAttemptTariff.Capture(
            new[]{new StorageAttemptTariff(ProviderType.SolanaOASIS,"SaveHolon",0,"public-read-v1")},allowed));
    }

}
