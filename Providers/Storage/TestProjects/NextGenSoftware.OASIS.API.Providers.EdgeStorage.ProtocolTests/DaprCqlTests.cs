using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Providers.DaprOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests;

[TestClass]
public class DaprCqlTests
{
    [TestMethod]
    public async Task Exact_query_rejects_private_records_before_paging_and_never_writes_the_index()
    {
        const ProviderType authority = ProviderType.DaprOASIS;
        const string store = "statestore";
        var ids = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
        var api = new FakeDaprStateApi();
        await Seed(ids[0], true);
        await Seed(ids[1], true);
        await Seed(ids[2], true, deleted: true);
        using var source = new DaprQuerySource(store, ids[0], api);
        Assert.IsFalse((await source.ActivateQuerySourceAsync(default)).IsError);
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(authority).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), authority, ids).Result).Result
            .Where(row => ids.Contains(row.Id)).Result;
        var targets = new[] { new CosmicQueryTarget(authority, source.QueryCapabilities) };
        var full = await source.ExecuteCosmicQueryAsync(CosmicQueryPlanner.Plan(query, authority, targets).Result);
        Assert.IsFalse(full.IsError, full.Message);
        CollectionAssert.AreEquivalent(ids[..2], full.Result.Select(row => row.Id).ToArray());

        await Seed(ids[1], false);
        var paged = await source.ExecuteCosmicQueryAsync(CosmicQueryPlanner.Plan(query.Take(1).Result, authority, targets).Result);
        Assert.IsTrue(paged.IsError);
        Assert.IsNull(paged.Result);

        using var backend = new DaprStateBackend(api, store, queryOnly: true);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => backend.PutAsync($"oasis/holon/{ids[3]:N}", "new"));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => backend.DeleteAsync($"oasis/holon/{ids[0]:N}"));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => backend.ListKeysAsync("oasis/holon/"));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => backend.GetAsync($"oasis/avatar/{ids[0]:N}"));
        Assert.AreEqual(3, api.Count);

        Task Seed(Guid id, bool isPublic, bool deleted = false) => api.SaveAsync(store, $"oasis/holon/{id:N}",
            OasisJson.Serialize(new Holon { Id = id, IsPublic = isPublic, DeletedDate = deleted ? DateTime.UtcNow : DateTime.MinValue }), default);
    }

    [TestMethod]
    [DataRow("http://localhost:50001/")]
    [DataRow("https://example.com:50001/")]
    [DataRow("http://127.0.0.1:50001/path")]
    public void Query_source_requires_an_explicit_loopback_sidecar_origin(string address) =>
        Assert.ThrowsException<ArgumentException>(() => new DaprQuerySource("statestore", new Uri(address), "fixture-token", Guid.NewGuid()));
}
