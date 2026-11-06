using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.RedisOASIS.UnitTests;

[TestClass]
public class RedisCqlTests
{
    [DataTestMethod]
    [DataRow("public", true, 1)]
    [DataRow("private", false, 1)]
    [DataRow("missing", false, 1)]
    [DataRow("foreign-id", false, 1)]
    [DataRow("alias", false, 0)]
    public async Task QueryActivationUsesOnlyTheEncryptedExactKey(string fixture, bool admitted, int reads)
    {
        var store = new RedisControlledStore();
        var probe = Guid.NewGuid();
        using var provider = new QueryProvider(store, probe);
        if (fixture != "missing")
            await provider.StoreProbe(probe, new Holon { Id = fixture == "foreign-id" ? Guid.NewGuid() : probe,
                Name = "Controlled public probe", IsPublic = fixture != "private" });
        if (fixture == "alias") provider.ProviderType.Value = ProviderType.KeyDBOASIS;
        store.Reads = 0;
        var before = store.Items.ToArray();
        var result = await provider.ActivateQuerySourceAsync(default);
        Assert.AreEqual(admitted, provider.IsProviderActivated);
        Assert.AreEqual(!admitted, result.IsError);
        Assert.AreEqual(reads, store.Reads);
        CollectionAssert.AreEqual(before, store.Items.ToArray());
    }

    [TestMethod]
    public async Task PublicQueryRefusesAChangedVisibilityWithoutPublishingAnyRows()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() }.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        var store = new RedisControlledStore();
        using var provider = new QueryProvider(store, ids[0]);
        await provider.StoreProbe(ids[0], new Holon { Id = ids[0], IsPublic = true });
        await provider.StoreProbe(ids[1], new Holon { Id = ids[1], IsPublic = true });
        Assert.IsFalse((await provider.ActivateQuerySourceAsync(default)).IsError);
        var plan = CosmicQueryPlanner.Plan(Query(ids), ProviderType.RedisOASIS, new[]
        {
            new CosmicQueryTarget(ProviderType.RedisOASIS, provider.QueryCapabilities)
        });
        Assert.IsFalse(plan.IsError, plan.Message);
        Assert.AreEqual(2, (await provider.ExecuteCosmicQueryAsync(plan.Result)).Result.Count);

        await provider.StoreProbe(ids[1], new Holon { Id = ids[1], IsPublic = false });
        var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
    }

    private sealed class QueryProvider : RedisOASIS
    {
        internal QueryProvider(RedisControlledStore store, Guid probe)
            : base(store, () => Enumerable.Repeat((byte)11, 32).ToArray()) => BindQueryProbe(probe);

        internal Task StoreProbe(Guid id, Holon row) => Backend.PutAsync(RecordKey("holon", id), OasisJson.Serialize(row));
    }

    private static CosmicQuery<Holon> Query(Guid[] ids)
        => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.RedisOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(
                Guid.NewGuid(), ProviderType.RedisOASIS, ids).Result).Result;

    [TestMethod]
    public void CapturedBusinessKeysCannotEscapeOrMutateAuthorityCatalogue()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var expected = ids[0];
        var selected = new[] { ids[0], ids[2] };
        var query = Query(ids.Take(2).ToArray()).Where(h => selected.Contains(h.Id)).Result;
        selected[0] = Guid.NewGuid();
        ids[0] = Guid.NewGuid();

        var result = RedisCqlCompiler.Compile(query, ProviderType.RedisOASIS);
        Assert.IsFalse(result.IsError, result.Message);
        CollectionAssert.AreEqual(new[] { expected }, result.Result.ToArray());
        Assert.IsTrue(RedisCqlCompiler.Compile(query, ProviderType.PolygonOASIS).IsError);
        Assert.AreEqual(0, RedisCqlCompiler.Compile(Query(Array.Empty<Guid>()), ProviderType.RedisOASIS).Result.Count);
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

        var result = RedisCqlCompiler.Compile(query, ProviderType.RedisOASIS);
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task AdapterRejectsUnsupportedQueryBeforeActivationAndHonoursPreCancellation()
    {
        using var provider = new RedisOASIS("127.0.0.1:1", () => throw new AssertFailedException("Inactive query must never resolve a key."));
        var query = Query(new[] { Guid.NewGuid() });
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.RedisOASIS,
            new[] { new CosmicQueryTarget(ProviderType.RedisOASIS, provider.QueryCapabilities) });
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
        using var provider = new RedisOASIS("127.0.0.1:1", () => throw new AssertFailedException("No native key resolution allowed."));
        provider.ProviderType.Value = ProviderType.KeyDBOASIS;
        Assert.AreEqual(ProviderType.KeyDBOASIS, provider.QueryProviderType);
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.KeyDBOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.KeyDBOASIS,
                new[] { Guid.NewGuid() }).Result).Result;
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.KeyDBOASIS,
            new[] { new CosmicQueryTarget(ProviderType.KeyDBOASIS, provider.QueryCapabilities) });
        Assert.IsTrue(plan.IsError, "An unadmitted alias cannot expose Redis query capabilities.");
    }

}
