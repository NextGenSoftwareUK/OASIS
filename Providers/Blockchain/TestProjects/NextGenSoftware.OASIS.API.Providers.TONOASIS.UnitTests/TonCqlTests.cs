using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.TONOASIS.UnitTests;

[TestClass]
public class TonCqlTests
{
    [DataTestMethod]
    [DataRow("name")]
    [DataRow("metadata")]
    [DataRow("order")]
    public void UnsupportedShapesRejectBeforeRuntimeAccess(string shape)
    {
        using var provider = TONOASISTestFactory.Create();
        var id = Guid.NewGuid();
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.TONOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.TONOASIS, new[] { id }).Result).Result;
        query = shape switch {
            "name" => query.Where(row => row.Name == "coffee").Result,
            "metadata" => query.Where(row => CosmicQueryMetadata.Value<string>(row, "category") == "coffee").Result,
            _ => query.OrderBy(row => row.Name).Result
        };
        Assert.IsNotNull(query, "The expression must be captured before testing native capability rejection.");
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.TONOASIS,
            new[] { new CosmicQueryTarget(ProviderType.TONOASIS, provider.QueryCapabilities) });
        Assert.IsTrue(plan.IsError);
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task StorageAccessRequiresActivationBeforeAnyProcessOrFileAccess()
    {
        using var provider = TONOASISTestFactory.Create();
        var result = await provider.LoadHolonAsync(Guid.NewGuid(), loadChildren: false);
        Assert.IsTrue(result.IsError);
        Assert.IsInstanceOfType(result.Exception, typeof(TonStorageException));
        Assert.AreEqual("TON_PROVIDER_INACTIVE", ((TonStorageException)result.Exception).Code);
    }
}
