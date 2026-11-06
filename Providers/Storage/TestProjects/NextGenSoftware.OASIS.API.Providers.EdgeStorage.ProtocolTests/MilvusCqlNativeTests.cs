using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using MilvusProvider = NextGenSoftware.OASIS.API.Providers.MilvusOASIS.MilvusOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EdgeStorage.ProtocolTests;

[TestClass]
public class MilvusCqlNativeTests
{
    [TestMethod]
    [TestCategory("MilvusNativeCql")]
    public async Task RealCollectionReturnsPublicKeysAndRejectsPrivateRows()
    {
        const string endpoint = "http://127.0.0.1:19530";
        const string collection = "oasis_holons";
        const ProviderType type = ProviderType.MilvusOASIS;
        Assert.AreEqual("1", Environment.GetEnvironmentVariable("COSMIC_MILVUS_DISPOSABLE"));
        using var setup = new HttpClient { BaseAddress = new Uri(endpoint), Timeout = TimeSpan.FromSeconds(20) };
        setup.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "root:Milvus");
        var existing = await CallAsync(setup, "collections/list", new { });
        Assert.AreEqual(0, existing.GetProperty("data").GetArrayLength(),
            "A fresh disposable Milvus instance is required.");
        await CallAsync(setup, "collections/create", new
        {
            collectionName = collection,
            schema = new
            {
                autoId = false,
                fields = new object[]
                {
                    new { fieldName = "id", dataType = "VarChar", isPrimary = true, elementTypeParams = new { max_length = 64 } },
                    new { fieldName = "doc", dataType = "VarChar", elementTypeParams = new { max_length = 65535 } },
                    new { fieldName = "vector", dataType = "FloatVector", elementTypeParams = new { dim = "2" } }
                }
            },
            indexParams = new[] { new { fieldName = "vector", indexName = "vector_idx", metricType = "L2",
                @params = new { index_type = "FLAT" } } }
        });
        var ids = Enumerable.Range(0, 6).Select(_ => Guid.NewGuid())
            .OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        var actor = Guid.NewGuid();
        var rows = ids.Take(5).Select((id, position) => new
        {
            id = id.ToString("D"), vector = new[] { 0f, 1f },
            doc = JsonSerializer.Serialize(new
            {
                Id = id, CreatedByAvatarId = actor, Name = "milvus-" + id.ToString("D"),
                IsPublic = position != 3, DeletedDate = position == 4 ? DateTime.UtcNow : DateTime.MinValue,
                Children = new[] { new { Id = ids[3], IsPublic = false } }
            })
        }).ToArray();
        var inserted = await CallAsync(setup, "entities/insert", new { collectionName = collection, data = rows });
        Assert.AreEqual(5, inserted.GetProperty("data").GetProperty("insertCount").GetInt32());

        var provider = new MilvusProvider(endpoint, "root:Milvus", ids[0]);
        var active = await provider.ActivateQuerySourceAsync();
        Assert.IsFalse(active.IsError, active.Exception?.ToString());
        CosmicQuery<Holon> Query(params Guid[] keys) => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(type).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, type, keys).Result).Result;
        CosmicQueryPlan<Holon> Plan(CosmicQuery<Holon> query) =>
            CosmicQueryPlanner.Plan(query, type, new[] { new CosmicQueryTarget(type, provider.QueryCapabilities) }).Result;
        var query = Query(ids[0], ids[1], ids[2], ids[4], ids[5]);
        var all = await provider.ExecuteCosmicQueryAsync(Plan(query));
        Assert.IsFalse(all.IsError, all.Exception?.ToString());
        CollectionAssert.AreEqual(ids.Take(3).ToArray(), all.Result.Select(row => row.Id).ToArray());
        Assert.IsTrue(all.Result.All(row => !row.Children.Any()));
        var subset = new[] { ids[0], ids[2] };
        var filtered = await provider.ExecuteCosmicQueryAsync(Plan(query.Where(row => subset.Contains(row.Id)).Result));
        Assert.IsFalse(filtered.IsError, filtered.Exception?.ToString());
        CollectionAssert.AreEqual(subset, filtered.Result.Select(row => row.Id).ToArray());
        var missing = await provider.ExecuteCosmicQueryAsync(Plan(Query(ids[5])));
        Assert.IsFalse(missing.IsError, missing.Exception?.ToString());
        Assert.AreEqual(0, missing.Result.Count);
        var denied = await provider.ExecuteCosmicQueryAsync(Plan(Query(ids[0], ids[3])));
        Assert.IsTrue(denied.IsError);
        Assert.IsNull(denied.Result);
    }

    private static async Task<JsonElement> CallAsync(HttpClient client, string route, object body)
    {
        using var response = await client.PostAsJsonAsync("/v2/vectordb/" + route, body);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        Assert.AreEqual(0, document.RootElement.GetProperty("code").GetInt32(), json);
        return document.RootElement.Clone();
    }
}
