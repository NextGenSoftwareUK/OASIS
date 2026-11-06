using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using MilvusProvider = NextGenSoftware.OASIS.API.Providers.MilvusOASIS.MilvusOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests;

[TestClass]
public class MilvusCqlTests
{
    private const ProviderType Type = ProviderType.MilvusOASIS;

    [TestMethod]
    public async Task NativeExactReadsEnforcePublicAuthorityAndProjectOutChildren()
    {
        var ids = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid())
            .OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        using var service = new FakeService((request, body) =>
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("/v2/vectordb/entities/get", request.RequestUri.AbsolutePath);
            Assert.AreEqual("reader", request.Headers.Authorization.Parameter);
            using var parsed = JsonDocument.Parse(body);
            var root = parsed.RootElement;
            Assert.AreEqual("oasis_holons", root.GetProperty("collectionName").GetString());
            Assert.AreEqual("Strong", root.GetProperty("consistencyLevel").GetString());
            CollectionAssert.AreEqual(new[] { "id", "doc" },
                root.GetProperty("outputFields").EnumerateArray().Select(field => field.GetString()).ToArray());
            var id = root.GetProperty("id").EnumerateArray().Single().GetGuid();
            return id == ids[5]
                ? FakeService.Json(new { code = 0, data = Array.Empty<object>() })
                : Entity(id, id != ids[3], id == ids[4]);
        });
        var provider = new MilvusProvider("https://milvus.invalid", "reader", ids[0], service);
        var activated = await provider.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Exception?.ToString());

        var query = Query(ids[0], ids[1], ids[2], ids[4], ids[5]);
        var all = await provider.ExecuteCosmicQueryAsync(Plan(provider, query));
        Assert.IsFalse(all.IsError, all.Exception?.ToString());
        CollectionAssert.AreEqual(ids.Take(3).ToArray(), all.Result.Select(row => row.Id).ToArray());
        Assert.IsTrue(all.Result.All(row => !row.Children.Any()));

        var subset = new[] { ids[0], ids[2] };
        var filtered = await provider.ExecuteCosmicQueryAsync(Plan(provider, query.Where(row => subset.Contains(row.Id)).Result));
        Assert.IsFalse(filtered.IsError, filtered.Exception?.ToString());
        CollectionAssert.AreEqual(subset, filtered.Result.Select(row => row.Id).ToArray());

        var refused = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(ids[0], ids[3])));
        Assert.IsTrue(refused.IsError);
        Assert.IsNull(refused.Result);
    }

    [TestMethod]
    [DataRow("native-error")]
    [DataRow("wrong-key")]
    [DataRow("private")]
    [DataRow("missing")]
    public async Task RejectedProbeCannotActivate(string failure)
    {
        var id = Guid.NewGuid();
        using var service = new FakeService((_, _) => failure switch
        {
            "native-error" => FakeService.Json(new { code = 1100, data = Array.Empty<object>() }),
            "wrong-key" => Entity(Guid.NewGuid()),
            "private" => Entity(id, isPublic: false),
            "missing" => FakeService.Json(new { code = 0, data = Array.Empty<object>() }),
            _ => throw new AssertFailedException("Unknown probe case.")
        });
        var provider = new MilvusProvider("https://milvus.invalid", "reader", id, service);
        Assert.IsTrue((await provider.ActivateQuerySourceAsync()).IsError);
        Assert.AreEqual("CQL_PROVIDER_INACTIVE",
            (await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(id)))).ErrorCode);
    }

    private static HttpResponseMessage Entity(Guid id, bool isPublic = true, bool deleted = false) =>
        FakeService.Json(new
        {
            code = 0,
            data = new[] { new
            {
                id,
                doc = JsonSerializer.Serialize(new
                {
                    Id = id, IsPublic = isPublic, DeletedDate = deleted ? DateTime.UtcNow : DateTime.MinValue,
                    Children = new[] { new { Id = Guid.NewGuid() } }
                })
            } }
        });

    private static CosmicQuery<Holon> Query(params Guid[] ids) => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(Type).Result)
        .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), Type, ids).Result).Result;

    private static CosmicQueryPlan<Holon> Plan(MilvusProvider provider, CosmicQuery<Holon> query) =>
        CosmicQueryPlanner.Plan(query, Type, new[] { new CosmicQueryTarget(Type, provider.QueryCapabilities) }).Result;
}
