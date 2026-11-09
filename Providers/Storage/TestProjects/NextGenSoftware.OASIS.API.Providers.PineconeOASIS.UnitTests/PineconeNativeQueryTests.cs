using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.PineconeOASIS.UnitTests;

[TestClass]
public class PineconeNativeQueryTests
{
    private sealed class Transport : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> OnSend = (_, _) => throw new InvalidOperationException();
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { Calls++; return OnSend(request, token); }
    }

    private static CosmicQueryPlan<Holon> Plan(PineconeOASIS provider, Guid actor, Guid[] ids, bool paging = false)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.PineconeOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.PineconeOASIS, ids).Result).Result;
        if (paging) query = query.Skip(1).Result.Take(1).Result;
        return CosmicQueryPlanner.Plan(query, ProviderType.PineconeOASIS,
            new[] { new CosmicQueryTarget(ProviderType.PineconeOASIS, provider.QueryCapabilities) }).Result;
    }

    [DataTestMethod]
    [DataRow("success")]
    [DataRow("paging")]
    [DataRow("namespace")]
    [DataRow("foreign")]
    [DataRow("rowid")]
    [DataRow("metadataid")]
    [DataRow("payload")]
    [DataRow("vector")]
    [DataRow("shape")]
    [DataRow("http")]
    [DataRow("envelope")]
    [DataRow("docsize")]
    [DataRow("empty")]
    [DataRow("budget")]
    [DataRow("deleted")]
    [DataRow("duplicate")]
    [DataRow("cancel")]
    public async Task NativeMultiFetchValidatesNamespaceAndEveryIdentityBeforePublishing(string mode)
    {
        var actor = Guid.NewGuid();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() }.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        using var cancellation = new CancellationTokenSource();
        var transport = new Transport { OnSend = (request, token) =>
        {
            Assert.AreEqual(HttpMethod.Get, request.Method);
            Assert.AreEqual("/vectors/fetch", request.RequestUri!.AbsolutePath);
            Assert.AreEqual("?namespace=holons&" + string.Join("&", ids.Select(id => "ids=" + id.ToString("D"))), request.RequestUri.Query);
            Assert.AreEqual("2026-07", request.Headers.GetValues("X-Pinecone-API-Version").Single());
            object Row(Guid id) => new
            {
                id = mode == "rowid" ? Guid.NewGuid().ToString("D") : id.ToString("D"),
                values = mode == "vector" ? new[] { 1.0, 2.0 } : new[] { 1.0 },
                metadata = new Dictionary<string, string>
                {
                    ["id"] = mode == "metadataid" ? Guid.NewGuid().ToString("D") : id.ToString("D"),
                    ["_doc"] = mode == "docsize" ? new string('x', 40961) : JsonSerializer.Serialize(new Holon
                    { Id = mode == "payload" ? Guid.NewGuid() : id, CreatedByAvatarId = actor, DeletedDate = mode == "deleted" ? DateTime.UtcNow : default })
                }
            };
            var records = new Dictionary<string, object>
            {
                [ids[1].ToString("D")] = Row(ids[1]),
                [(mode == "foreign" ? Guid.NewGuid() : ids[0]).ToString("D")] = Row(ids[0])
            };
            var json = mode == "envelope" ? JsonSerializer.Serialize(new { padding = new string('x', 4 * 1024 * 1024 + 1) }) :
                JsonSerializer.Serialize(new { @namespace = mode == "namespace" ? "other" : "holons", vectors = mode == "shape" ? (object)Array.Empty<string>() : mode == "empty" ? new Dictionary<string, object>() : records });
            if (mode == "duplicate")
            {
                var key = JsonSerializer.Serialize(ids[1].ToString("D"));
                var row = JsonSerializer.Serialize(Row(ids[1]));
                json = "{\"namespace\":\"holons\",\"vectors\":{" + key + ":" + row + "," + key + ":" + row + "}}";
            }
            if (mode == "cancel") { cancellation.Cancel(); token.ThrowIfCancellationRequested(); }
            return Task.FromResult(new HttpResponseMessage(mode == "http" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK) { Content = new StringContent(json) });
        } };
        using var http = new HttpClient(transport);
        var provider = new PineconeOASIS(http, "http://native.invalid", "test-key") { IsProviderActivated = true };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, actor, ids, mode == "paging"),
            maximumResults: mode == "budget" ? 1 : 100, cancellationToken: cancellation.Token);
        var success = mode is "success" or "paging" or "empty" or "deleted";
        Assert.AreEqual(success, !result.IsError, result.Message);
        Assert.AreEqual(1, transport.Calls);
        if (mode == "success") CollectionAssert.AreEqual(ids, result.Result.Select(h => h.Id).ToArray());
        else if (mode == "paging") Assert.AreEqual(ids[1], result.Result.Single().Id);
        else if (success) Assert.AreEqual(0, result.Result.Count);
        else { Assert.IsNull(result.Result); Assert.IsFalse(result.IsLoaded); }
        if (mode == "cancel") Assert.AreEqual("CQL_QUERY_CANCELLED", result.ErrorCode);
    }

    [TestMethod]
    public async Task EmptyCatalogueDoesNotFetchNativeStorage()
    {
        var transport = new Transport();
        using var http = new HttpClient(transport);
        var provider = new PineconeOASIS(http, "http://native.invalid", "test-key") { IsProviderActivated = true };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Guid.NewGuid(), Array.Empty<Guid>()));
        Assert.IsFalse(result.IsError, result.Message);
        Assert.AreEqual(0, result.Result.Count);
        Assert.AreEqual(0, transport.Calls);
    }
}
