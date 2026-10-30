using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.ChromaOASIS.UnitTests;

[TestClass]
public class ChromaNativeQueryTests
{
    private sealed class Transport : HttpMessageHandler
    {
        public const string Collection = "3bbf531e-0f7d-4e8a-9306-c901ae2c7880";
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Query = (_, _) => throw new InvalidOperationException();
        public int Queries;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (request.RequestUri!.AbsolutePath.EndsWith("/collections"))
                return Task.FromResult(Json(new { id = Collection }));
            Queries++;
            return Query(request, token);
        }
    }

    private static HttpResponseMessage Json(object body, HttpStatusCode code = HttpStatusCode.OK)
        => new(code) { Content = new StringContent(JsonSerializer.Serialize(body)) };

    private static CosmicQueryPlan<Holon> Plan(ChromaOASIS provider, Guid actor, Guid[] ids, bool paging = false)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ChromaOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.ChromaOASIS, ids).Result).Result;
        if (paging) query = query.Skip(1).Result.Take(1).Result;
        return CosmicQueryPlanner.Plan(query, ProviderType.ChromaOASIS,
            new[] { new CosmicQueryTarget(ProviderType.ChromaOASIS, provider.QueryCapabilities) }).Result;
    }

    [DataTestMethod]
    [DataRow("success")]
    [DataRow("paging")]
    [DataRow("foreign")]
    [DataRow("payload")]
    [DataRow("duplicate")]
    [DataRow("shape")]
    [DataRow("length")]
    [DataRow("null")]
    [DataRow("catalogue")]
    [DataRow("deleted")]
    [DataRow("http")]
    [DataRow("envelope")]
    [DataRow("docsize")]
    [DataRow("empty")]
    [DataRow("budget")]
    [DataRow("cancel")]
    public async Task NativeMultiGetValidatesParallelArraysAndAuthorityBeforePublication(string mode)
    {
        var actor = Guid.NewGuid();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() }.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        using var cancellation = new CancellationTokenSource();
        var transport = new Transport { Query = async (request, token) =>
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual($"/api/v2/tenants/default_tenant/databases/default_database/collections/{Transport.Collection}/get", request.RequestUri!.AbsolutePath);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            CollectionAssert.AreEqual(ids.Select(id => id.ToString("D")).ToArray(), body.RootElement.GetProperty("ids").EnumerateArray().Select(e => e.GetString()).ToArray());
            CollectionAssert.AreEqual(new[] { "documents" }, body.RootElement.GetProperty("include").EnumerateArray().Select(e => e.GetString()).ToArray());
            Assert.AreEqual(3, body.RootElement.GetProperty("limit").GetInt32());
            Assert.AreEqual(0, body.RootElement.GetProperty("offset").GetInt32());
            string Payload(Guid id, bool first) => JsonSerializer.Serialize(new Holon
            {
                Id = mode == "payload" && first ? Guid.NewGuid() : id,
                CreatedByAvatarId = mode == "catalogue" ? Guid.NewGuid() : actor,
                DeletedDate = mode == "deleted" ? DateTime.UtcNow : default,
                Name = first ? "first" : "second"
            });
            if (mode == "cancel") { cancellation.Cancel(); token.ThrowIfCancellationRequested(); }
            var keys = new[] { ids[1].ToString("D"), (mode == "foreign" ? Guid.NewGuid() : mode == "duplicate" ? ids[1] : ids[0]).ToString("D") };
            var documents = new string?[] { Payload(ids[1], false), mode == "null" ? null : mode == "docsize" ? new string('x', 1024 * 1024 + 1) : Payload(ids[0], true) };
            return Json(mode == "envelope" ? (object)new { padding = new string('x', 4 * 1024 * 1024 + 1) } : new
            {
                ids = mode == "shape" ? (object)new { } : mode == "empty" ? Array.Empty<string>() : keys,
                documents = mode == "empty" || mode == "length" ? Array.Empty<string?>() : documents
            }, mode == "http" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK);
        } };
        using var http = new HttpClient(transport);
        var provider = new ChromaOASIS(http);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, actor, ids, mode == "paging"),
            maximumResults: mode == "budget" ? 1 : 100, cancellationToken: cancellation.Token);
        var success = mode is "success" or "paging" or "empty" or "deleted" or "catalogue";
        Assert.AreEqual(success, !result.IsError, result.Message);
        Assert.AreEqual(1, transport.Queries);
        if (mode is "success" or "catalogue") CollectionAssert.AreEqual(ids, result.Result.Select(h => h.Id).ToArray());
        else if (mode == "paging") Assert.AreEqual(ids[1], result.Result.Single().Id);
        else if (success) Assert.AreEqual(0, result.Result.Count);
        else { Assert.IsNull(result.Result); Assert.IsFalse(result.IsLoaded); }
        if (mode == "cancel") Assert.AreEqual("CQL_QUERY_CANCELLED", result.ErrorCode);
    }

    [TestMethod]
    public async Task EmptyCatalogueAndUnresolvedCollectionDoNotQueryNativeStorage()
    {
        var transport = new Transport();
        using var http = new HttpClient(transport);
        var provider = new ChromaOASIS(http) { IsProviderActivated = true };
        var empty = await provider.ExecuteCosmicQueryAsync(Plan(provider, Guid.NewGuid(), Array.Empty<Guid>()));
        Assert.IsFalse(empty.IsError, empty.Message);
        var unresolved = await provider.ExecuteCosmicQueryAsync(Plan(provider, Guid.NewGuid(), new[] { Guid.NewGuid() }));
        Assert.AreEqual("CQL_PROVIDER_CONFIGURATION_REQUIRED", unresolved.ErrorCode);
        Assert.AreEqual(0, transport.Queries);
    }
}
