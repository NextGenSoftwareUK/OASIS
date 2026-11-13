using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.ChromaOASIS;

namespace NextGenSoftware.OASIS.API.Providers.ChromaOASIS.UnitTests;

[TestClass]
public class ChromaCqlTests
{
    private static CosmicQuery<Holon> Query(Guid[] ids)
        => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ChromaOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(
                Guid.NewGuid(), ProviderType.ChromaOASIS, ids).Result).Result;

    [TestMethod]
    public void CapturedBusinessKeysCannotEscapeOrMutateAuthorityCatalogue()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var expected = ids[0];
        var selected = new[] { ids[0], ids[2] };
        var query = Query(ids.Take(2).ToArray()).Where(h => selected.Contains(h.Id)).Result;
        selected[0] = Guid.NewGuid();
        ids[0] = Guid.NewGuid();

        var result = ChromaCqlCompiler.Compile(query, ProviderType.ChromaOASIS);
        Assert.IsFalse(result.IsError, result.Message);
        CollectionAssert.AreEqual(new[] { expected }, result.Result.ToArray());
        Assert.IsTrue(ChromaCqlCompiler.Compile(query, ProviderType.EthereumOASIS).IsError);
        Assert.AreEqual(0, ChromaCqlCompiler.Compile(Query(Array.Empty<Guid>()), ProviderType.ChromaOASIS).Result.Count);
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

        var result = ChromaCqlCompiler.Compile(query, ProviderType.ChromaOASIS);
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task AdapterRejectsUnsupportedQueryBeforeActivationAndHonoursPreCancellation()
    {
        var provider = new ChromaOASIS("http://127.0.0.1:1");
        var query = Query(new[] { Guid.NewGuid() });
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.ChromaOASIS,
            new[] { new CosmicQueryTarget(ProviderType.ChromaOASIS, provider.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message);

        var inactive = await provider.ExecuteCosmicQueryAsync(plan.Result);
        Assert.AreEqual("CQL_PROVIDER_INACTIVE", inactive.ErrorCode);
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var stopped = await provider.ExecuteCosmicQueryAsync(plan.Result, cancellationToken: cancelled.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED", stopped.ErrorCode);
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task DeactivationClearsPreviouslyActiveState()
    {
        var provider = new ChromaOASIS("http://127.0.0.1:1")
        { IsProviderActivated = true };

        var result = await provider.DeActivateProviderAsync();

        Assert.IsFalse(result.IsError);
        Assert.IsTrue(result.Result);
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public async Task MissingPlanAndInvalidBudgetsRejectWithoutConnection()
    {
        var provider = new ChromaOASIS("http://127.0.0.1:1");
        var missing = await provider.ExecuteCosmicQueryAsync<Holon>(null!);
        Assert.AreEqual("CQL_PLAN_TARGET_UNAVAILABLE", missing.ErrorCode);
        var plan = CosmicQueryPlanner.Plan(Query(new[] { Guid.NewGuid() }),
            ProviderType.ChromaOASIS,
            new[] { new CosmicQueryTarget(ProviderType.ChromaOASIS, provider.QueryCapabilities) });
        foreach (var budget in new[] { 0, -1, 10001 })
        {
            var result = await provider.ExecuteCosmicQueryAsync(plan.Result, budget);
            Assert.AreEqual("CQL_QUERY_LIMIT", result.ErrorCode);
            Assert.IsNull(result.Result);
        }
    }
}
