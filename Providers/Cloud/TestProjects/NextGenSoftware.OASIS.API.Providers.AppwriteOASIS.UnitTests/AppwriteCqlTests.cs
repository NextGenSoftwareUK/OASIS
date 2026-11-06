using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.AppwriteOASIS.UnitTests;

[TestClass]
public class AppwriteCqlTests
{
    [TestMethod]
    public async Task Exact_documents_support_existing_physical_ids_and_reject_private_records_before_paging()
    {
        const ProviderType provider = ProviderType.AppwriteOASIS;
        var ids = Enumerable.Range(0, 4).Select(_ => Guid.NewGuid()).ToArray();
        var privateSecond = false;
        using var transport = new Fixture(request =>
        {
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("appwrite.example", request.RequestUri!.Host);
            Assert.AreEqual("/v1/documentsdb/database/collections/oasis_holons/documents", request.RequestUri.AbsolutePath);
            Assert.AreEqual("project", request.Headers.GetValues("X-Appwrite-Project").Single());
            Assert.AreEqual("reader-token", request.Headers.GetValues("X-Appwrite-Key").Single());
            var queries = request.RequestUri.Query.TrimStart('?').Split('&')
                .Select(parameter => JsonSerializer.Deserialize<JsonElement>(Uri.UnescapeDataString(parameter.Split('=', 2)[1]))).ToArray();
            Assert.AreEqual(2, queries.Length);
            Assert.AreEqual("equal", queries[0].GetProperty("method").GetString());
            Assert.AreEqual("oasis_id", queries[0].GetProperty("attribute").GetString());
            Assert.AreEqual("limit", queries[1].GetProperty("method").GetString());
            Assert.AreEqual(2, queries[1].GetProperty("values")[0].GetInt32());
            var id = Guid.Parse(queries[0].GetProperty("values").EnumerateArray().Single().GetString()!);
            Assert.IsTrue(ids.Contains(id));
            if (id == ids[3]) return Reply(new { total = 0, documents = Array.Empty<object>() });
            return Reply(new { total = 1, documents = new[] { Document(id, id == ids[2], id != ids[1] || !privateSecond) } });
        });
        using var source = Source(ids[0], transport);
        var activation = await source.ActivateQuerySourceAsync();
        Assert.IsFalse(activation.IsError, activation.Exception?.Message);
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(provider).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), provider, ids).Result).Result
            .Where(row => ids.Contains(row.Id)).Result;
        var targets = new[] { new CosmicQueryTarget(provider, source.QueryCapabilities) };
        var result = await source.ExecuteCosmicQueryAsync(CosmicQueryPlanner.Plan(query, provider, targets).Result);
        Assert.IsFalse(result.IsError, result.Message);
        CollectionAssert.AreEquivalent(ids[..2], result.Result.Select(row => row.Id).ToArray());

        privateSecond = true;
        var denied = await source.ExecuteCosmicQueryAsync(CosmicQueryPlanner.Plan(query.Take(1).Result, provider, targets).Result);
        Assert.IsTrue(denied.IsError);
        Assert.IsNull(denied.Result);
    }

    [DataTestMethod]
    [DataRow("duplicate"), DataRow("incomplete"), DataRow("foreign"), DataRow("missing-delete-state")]
    public async Task Query_mode_denies_ordinary_operations_and_rejects_invalid_pages(string variant)
    {
        var id = Guid.NewGuid();
        using var transport = new Fixture(_ => variant switch
        {
            "duplicate" => Reply(new { total = 2, documents = new[] { Document(id), Document(id) } }),
            "incomplete" => Reply(new { total = 1, documents = Array.Empty<object>() }),
            "foreign" => Reply(new { total = 1, documents = new[] { Document(Guid.NewGuid()) } }),
            "missing-delete-state" => Reply(new { total = 1, documents = new[] { new { oasis_id = id, data_json = "{}" } } }),
            _ => throw new ArgumentOutOfRangeException(nameof(variant))
        });
        using var source = Source(id, transport);
        Assert.IsTrue((await source.SaveHolonAsync(new Holon { Id = id })).IsError);
        Assert.IsTrue((await source.LoadAllHolonsAsync(loadChildren: false)).IsError);
        Assert.AreEqual(0, transport.Requests);
        var result = await source.ActivateQuerySourceAsync();
        Assert.IsTrue(result.IsError);
        Assert.IsInstanceOfType(result.Exception, typeof(JsonException));
        Assert.AreEqual(1, transport.Requests);
    }

    private static AppwriteQuerySource Source(Guid probe, HttpMessageHandler handler) =>
        new(new Uri("https://appwrite.example/v1"), "project", "database", "reader-token", probe, handler);

    private static object Document(Guid id, bool deleted = false, bool visible = true) => new Dictionary<string, object>
    {
        ["$id"] = "earlier-physical-id",
        ["oasis_id"] = id.ToString("D"),
        ["is_deleted"] = deleted,
        ["data_json"] = OasisJson.Serialize(new Holon { Id = id, IsPublic = visible })
    };

    private static HttpResponseMessage Reply(object body) => new(HttpStatusCode.OK) { Content = JsonContent.Create(body) };

    private sealed class Fixture(Func<HttpRequestMessage, HttpResponseMessage> reply) : HttpMessageHandler
    {
        public int Requests { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            return Task.FromResult(reply(request));
        }
    }
}
