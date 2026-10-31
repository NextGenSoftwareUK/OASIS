using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

[TestClass]
public class CqlProviderBoundaryTests
{
    private static AzureCosmosDBOASIS Inactive() => new(new Uri("https://127.0.0.1:8082"), "invalid-unused", "unused", new());
    private static CosmicQueryPlan<Holon> Plan(ProviderType provider = ProviderType.AzureCosmosDBOASIS)
        => CosmicQueryPlanner.Plan(CosmicQuery<Holon>.Create(), provider,
            new[] { new CosmicQueryTarget(provider, AzureCosmosDBOASIS.CqlCapabilities) }).Result;

    [TestMethod]
    public async Task NativeQueryNeverActivatesOrCreatesStorage()
    {
        var provider = Inactive();
        var result = await provider.ExecuteCosmicQueryAsync(Plan());
        Assert.IsTrue(result.IsError);
        Assert.AreEqual("CQL_PROVIDER_INACTIVE", result.ErrorCode);
        Assert.IsFalse(provider.IsProviderActivated);
        Assert.AreEqual(ProviderType.AzureCosmosDBOASIS, ((ICosmicQueryStorageProvider)provider).QueryProviderType);
    }

    [TestMethod]
    public async Task CancellationHasStructuredErrorBeforeStorageAccess()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var result = await Inactive().ExecuteCosmicQueryAsync(Plan(), cancellationToken: cancellation.Token);
        Assert.IsTrue(result.IsError);
        Assert.AreEqual("CQL_CANCELLED", result.ErrorCode);
    }

    [TestMethod]
    public async Task ForeignPlanFailsBeforeInactiveProviderCheck()
    {
        var result = await Inactive().ExecuteCosmicQueryAsync(Plan(ProviderType.MongoDBOASIS));
        Assert.AreEqual("CQL_PLAN_INVALID", result.ErrorCode);
        Assert.IsNull(result.Result);
    }
}
