using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.GarnetOASIS.UnitTests;

[TestClass]
public class GarnetCqlTests
{
    private static CosmicQuery<Holon> Query(Guid[] ids)
        => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.GarnetOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(
                Guid.NewGuid(), ProviderType.GarnetOASIS, ids).Result).Result;

    [TestMethod]
    public void CapturedBusinessKeysCannotEscapeOrMutateAuthorityCatalogue()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var expected = ids[0];
        var selected = new[] { ids[0], ids[2] };
        var query = Query(ids.Take(2).ToArray()).Where(h => selected.Contains(h.Id)).Result;
        selected[0] = Guid.NewGuid();
        ids[0] = Guid.NewGuid();

        var result = GarnetCqlCompiler.Compile(query, ProviderType.GarnetOASIS);
        Assert.IsFalse(result.IsError, result.Message);
        CollectionAssert.AreEqual(new[] { expected }, result.Result.ToArray());
        Assert.IsTrue(GarnetCqlCompiler.Compile(query, ProviderType.PolygonOASIS).IsError);
        Assert.AreEqual(0, GarnetCqlCompiler.Compile(Query(Array.Empty<Guid>()), ProviderType.GarnetOASIS).Result.Count);
    }

    [DataTestMethod]
    [DataRow("name")]
    [DataRow("metadata")]
    [DataRow("order")]
    [DataRow("graph")]
    [DataRow("bound")]
    public void UnsupportedShapesRejectInsteadOfScanningNativeRecords(string shape)
    {
        var query = Query(Enumerable.Range(0, shape == "bound" ? 101 : 3).Select(_ => Guid.NewGuid()).ToArray());
        query = shape switch
        {
            "name" => query.Where(h => h.Name == "coffee").Result,
            "metadata" => query.Where(h => CosmicQueryMetadata.Value<string>(h, "category") == "coffee").Result,
            "order" => query.OrderBy(h => h.Id).Result,
            "graph" => query.WithChildren(CosmicQueryGraphOptions.Create(maxChildDepth: 1).Result).Result,
            _ => query
        };

        var result = GarnetCqlCompiler.Compile(query, ProviderType.GarnetOASIS);
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task AdapterRejectsUnsupportedQueryBeforeActivationAndHonoursPreCancellation()
    {
        using var provider = new GarnetOASIS("127.0.0.1:1", () => throw new AssertFailedException("Inactive query must never resolve a key."));
        var query = Query(new[] { Guid.NewGuid() });
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.GarnetOASIS,
            new[] { new CosmicQueryTarget(ProviderType.GarnetOASIS, provider.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);

        var inactive = await provider.ExecuteCosmicQueryAsync(plan.Result);
        Assert.AreEqual("CQL_PROVIDER_INACTIVE", inactive.ErrorCode);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var stopped = await provider.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: cancelled.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED", stopped.ErrorCode);
        Assert.AreEqual(typeof(OperationCanceledException), stopped.Exception.GetType());
        Assert.IsFalse(provider.IsProviderActivated);
    }
    [TestMethod]
    public void DerivedAliasesKeepTheirOwnIdentityAndCannotInheritRedisCertification()
    {
        using var provider = new GarnetOASIS("127.0.0.1:1", () => throw new AssertFailedException("No native key resolution allowed."));
        provider.ProviderType.Value = ProviderType.RedisOASIS;
        Assert.AreEqual(ProviderType.RedisOASIS, provider.QueryProviderType);
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.RedisOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.RedisOASIS,
                new[] { Guid.NewGuid() }).Result).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.RedisOASIS,
            new[] { new CosmicQueryTarget(ProviderType.RedisOASIS, provider.QueryCapabilities) });
        Assert.IsTrue(plan.IsError, "An unadmitted alias cannot expose Garnet query capabilities.");
    }

}
