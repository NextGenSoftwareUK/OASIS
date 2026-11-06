using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.ArangoDBOASIS.UnitTests;

[TestClass]
public class ArangoDbCqlTests
{
    [TestMethod]
    public async Task NativeKeysReturnThreeTwoAndPagingWithPublicProbe()
    {
        var ids = Keys();
        var transport = new CursorTransport(ids);
        using var client = new HttpClient(transport);
        var provider = Source(client, ids[0]);
        transport.PrivateId = ids[0];
        Assert.IsTrue((await provider.ActivateQuerySourceAsync(default)).IsError);
        Assert.IsFalse(provider.IsProviderActivated);
        transport.PrivateId = null;
        var active = await provider.ActivateQuerySourceAsync(default);
        Assert.IsFalse(active.IsError, active.Exception?.ToString() ?? active.Message);
        var query = Query(ids);
        var all = await provider.ExecuteCosmicQueryAsync(Plan(provider, query));
        Assert.IsFalse(all.IsError, all.Exception?.ToString() ?? all.Message);
        CollectionAssert.AreEqual(ids[..3], all.Result.Select(row => row.Id).ToArray());
        Assert.IsTrue(all.Result.All(row => !row.Children.Any()));
        var subset = new[] { ids[0], ids[2] };
        var filtered = await provider.ExecuteCosmicQueryAsync(Plan(provider, query.Where(row => subset.Contains(row.Id)).Result));
        Assert.IsFalse(filtered.IsError, filtered.Message);
        CollectionAssert.AreEqual(subset, filtered.Result.Select(row => row.Id).ToArray());
        var page = await provider.ExecuteCosmicQueryAsync(Plan(provider, query.Skip(1).Result.Take(1).Result));
        Assert.IsFalse(page.IsError, page.Message);
        Assert.AreEqual(ids[1], page.Result.Single().Id);
        Assert.AreEqual(5, transport.Requests);
    }

    [DataTestMethod]
    [DataRow("foreign")]
    [DataRow("wrong-id")]
    [DataRow("duplicate")]
    [DataRow("http-failure")]
    [DataRow("cursor-failure")]
    [DataRow("has-more")]
    [DataRow("private")]
    public async Task NativeFailureCannotPublishPartialRows(string mode)
    {
        var ids = Keys();
        var transport = new CursorTransport(ids);
        using var client = new HttpClient(transport);
        var provider = Source(client, ids[0]);
        Assert.IsFalse((await provider.ActivateQuerySourceAsync(default)).IsError);
        transport.Mode = mode;
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(ids)));
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        Assert.AreEqual(2, transport.Requests);
    }

    private static Guid[] Keys() => Enumerable.Range(0, 5).Select(_ => Guid.NewGuid())
        .OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();

    private static ArangoDBOASIS Source(HttpClient client, Guid probe)
        => new("https://arango.example.test", "reader", "controlled", client, probe);

    private static CosmicQuery<Holon> Query(Guid[] ids)
        => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ArangoDBOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.ArangoDBOASIS, ids).Result).Result;

    private static CosmicQueryPlan<Holon> Plan(ArangoDBOASIS provider, CosmicQuery<Holon> query) =>
        CosmicQueryPlanner.Plan(query, ProviderType.ArangoDBOASIS,
            new[] { new CosmicQueryTarget(ProviderType.ArangoDBOASIS, provider.QueryCapabilities) }).Result;

    private sealed class CursorTransport(Guid[] ids) : HttpMessageHandler
    {
        internal Guid? PrivateId { get; set; }
        internal string? Mode { get; set; }
        internal int Requests { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("https://arango.example.test/_db/oasis/_api/cursor", request.RequestUri?.AbsoluteUri);
            Assert.AreEqual("Basic", request.Headers.Authorization?.Scheme);
            using var json = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var body = json.RootElement;
            Assert.AreEqual(ArangoDBOASIS.NativeIdentityQuery, body.GetProperty("query").GetString());
            Assert.AreEqual(101, body.GetProperty("batchSize").GetInt32());
            Assert.AreEqual(15, body.GetProperty("options").GetProperty("maxRuntime").GetInt32());
            Assert.IsTrue(body.GetProperty("options").GetProperty("failOnWarning").GetBoolean());
            var selected = body.GetProperty("bindVars").GetProperty("keys").EnumerateArray().Select(value => value.GetGuid()).ToArray();
            if (Requests == 1) CollectionAssert.AreEqual(new[] { ids[0] }, selected);
            var rows = selected.Reverse().Where(id => id != ids[4]).Select(id => new
            {
                _key = Mode == "foreign" && id == ids[1] ? Guid.NewGuid() : Mode == "duplicate" ? ids[0] : id,
                id = Mode == "wrong-id" && id == ids[1] ? Guid.NewGuid() : Mode == "duplicate" ? ids[0] : id,
                isPublic = id != PrivateId && (Mode != "private" || id != ids[1]),
                deletedDate = id == ids[3] ? DateTime.UtcNow : DateTime.MinValue
            }).ToArray();
            return new HttpResponseMessage(Mode == "http-failure" ? HttpStatusCode.InternalServerError : HttpStatusCode.Created)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { error = Mode == "cursor-failure",
                    hasMore = Mode == "has-more", result = rows }))
            };
        }
    }
}
