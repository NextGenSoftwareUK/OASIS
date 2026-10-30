using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.QdrantOASIS.UnitTests;

[TestClass]
public class QdrantNativeBoundaryTests
{
    private sealed class Transport : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> OnSend = (_, _) => throw new InvalidOperationException();
        public int Calls;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        { Calls++; return OnSend(request, token); }
    }

    private static HttpResponseMessage Json(object value, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json") };

    private static CosmicQueryPlan<Holon> Plan(QdrantOASIS provider, Guid[] ids, bool page = false)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.QdrantOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.QdrantOASIS, ids).Result).Result;
        if (page) query = query.Skip(1).Result.Take(1).Result;
        return CosmicQueryPlanner.Plan(query, ProviderType.QdrantOASIS,
            new[] { new CosmicQueryTarget(ProviderType.QdrantOASIS, provider.QueryCapabilities) }).Result;
    }

    [DataTestMethod]
    [DataRow("reverse")]
    [DataRow("foreign")]
    [DataRow("duplicate")]
    [DataRow("payload")]
    [DataRow("status")]
    [DataRow("http")]
    [DataRow("missing")]
    [DataRow("deleted")]
    [DataRow("budget")]
    [DataRow("resultbudget")]
    public async Task NativeRetrievalValidatesCompleteEnvelopeAndPreservesPaging(string mode)
    {
        var ids = Enumerable.Range(0, 3).Select(_ => Guid.NewGuid()).OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        var transport = new Transport { OnSend = async (request, token) => {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("/collections/oasis_holons/points", request.RequestUri!.AbsolutePath);
            StringAssert.Contains(request.RequestUri.Query, "timeout=15");
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            CollectionAssert.AreEqual(ids, body.RootElement.GetProperty("ids").EnumerateArray().Select(v => v.GetGuid()).ToArray());
            Assert.IsTrue(body.RootElement.GetProperty("with_payload").GetBoolean());
            Assert.IsFalse(body.RootElement.GetProperty("with_vector").GetBoolean());
            var rows = ids.Reverse().Select((id, i) => new {
                id = mode == "foreign" && i == 0 ? Guid.NewGuid() : mode == "duplicate" ? ids[0] : id,
                payload = new { is_deleted = mode == "deleted",
                    data_json = mode == "budget" ? new string('x', 4 * 1024 * 1024 + 1) :
                        JsonSerializer.Serialize(new Holon { Id = mode == "payload" ? Guid.NewGuid() : id },
                            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) }
            }).ToArray();
            return Json(new { status = mode == "status" ? "error" : "ok", result = mode == "missing" ? rows.Take(0).ToArray() : rows },
                mode == "http" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK);
        }};
        using var http = new HttpClient(transport);
        var provider = new QdrantOASIS(http, "https://fixture.invalid") { IsProviderActivated = true };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, ids, page: mode != "resultbudget"), maximumResults: mode == "resultbudget" ? 1 : 100);
        if (mode == "reverse")
        { Assert.IsFalse(result.IsError, result.Message + result.Exception); Assert.AreEqual(ids[1], result.Result.Single().Id); }
        else if (mode is "missing" or "deleted")
        { Assert.IsFalse(result.IsError); Assert.AreEqual(0, result.LoadedCount); }
        else
        { Assert.IsTrue(result.IsError); Assert.IsNull(result.Result); Assert.AreEqual(mode is "budget" or "resultbudget" ? "CQL_QUERY_LIMIT" : "CQL_QUERY_FAILED", result.ErrorCode); }
    }

    [TestMethod]
    public async Task EmptyCatalogueAndHistoryAvoidNativeIo()
    {
        var transport = new Transport();
        using var http = new HttpClient(transport);
        var provider = new QdrantOASIS(http, "https://fixture.invalid") { IsProviderActivated = true };
        var empty = await provider.ExecuteCosmicQueryAsync(Plan(provider, Array.Empty<Guid>()));
        Assert.IsFalse(empty.IsError); Assert.AreEqual(0, transport.Calls);
        provider.IsVersionControlEnabled = true;
        var unsupported = await provider.ExecuteCosmicQueryAsync(Plan(provider, new[] { Guid.NewGuid() }));
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", unsupported.ErrorCode); Assert.AreEqual(0, transport.Calls);
        var save = await provider.SaveHolonAsync(new Holon(), saveChildren: false);
        Assert.IsTrue(save.IsError); Assert.AreEqual(0, transport.Calls);
    }

    [DataTestMethod]
    [DataRow("completed")]
    [DataRow("acknowledged")]
    [DataRow("missing")]
    [DataRow("error")]
    public async Task NativeCompletedOperationIsRequiredBeforeCallerIdentityChanges(string mode)
    {
        var transport = new Transport { OnSend = async (request, token) => {
            Assert.AreEqual(HttpMethod.Put, request.Method);
            StringAssert.Contains(request.RequestUri!.Query, "wait=true");
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            var point = body.RootElement.GetProperty("points")[0];
            using var payload = JsonDocument.Parse(point.GetProperty("payload").GetProperty("data_json").GetString()!);
            Assert.AreEqual(point.GetProperty("id").GetGuid(), payload.RootElement.GetProperty("id").GetGuid());
            return mode == "missing" ? Json(new { status = "ok" }) : Json(new {
                status = mode == "error" ? "error" : "ok", result = new { status = mode, operation_id = ulong.MaxValue } });
        }};
        using var http = new HttpClient(transport);
        var provider = new QdrantOASIS(http, "https://fixture.invalid");
        var row = new Holon();
        var saved = await provider.SaveHolonAsync(row, saveChildren: false);
        if (mode == "completed")
        { Assert.IsTrue(saved.IsSaved, saved.Message); Assert.AreNotEqual(Guid.Empty, row.Id); Assert.AreEqual("18446744073709551615", saved.MetaData["Qdrant.OperationId"]); }
        else { Assert.IsTrue(saved.IsError); Assert.IsFalse(saved.IsSaved); Assert.AreEqual(Guid.Empty, row.Id); }
    }

    [TestMethod]
    public void InvalidVectorDimensionsRejectBeforeNativeIo()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new QdrantOASIS("https://fixture.invalid", vectorSize: 0));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new QdrantOASIS("https://fixture.invalid", vectorSize: -1));
    }

    [DataTestMethod]
    [DataRow("existing")]
    [DataRow("missing")]
    [DataRow("http")]
    [DataRow("status")]
    [DataRow("schema")]
    [DataRow("badcreate")]
    public async Task ActivationPublishesOnlyAfterNativeSchemaOrCreationAcknowledgement(string mode)
    {
        var puts = 0;
        var transport = new Transport { OnSend = (request, _) => {
            if (request.Method == HttpMethod.Put)
            { puts++; return Task.FromResult(Json(new { status = "ok", result = mode != "badcreate" })); }
            Assert.AreEqual(HttpMethod.Get, request.Method);
            if (mode is "missing" or "badcreate") return Task.FromResult(Json(new { status = "error" }, HttpStatusCode.NotFound));
            return Task.FromResult(Json(new { status = mode == "status" ? "error" : "ok", result = new {
                config = new { @params = new { vectors = new { size = mode == "schema" ? 64 : 128, distance = "Cosine" } } }
            } }, mode == "http" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK));
        }};
        using var http = new HttpClient(transport);
        var provider = new QdrantOASIS(http, "https://fixture.invalid") { IsProviderActivated = true };
        var active = await provider.ActivateProviderAsync();
        Assert.AreEqual(mode is "existing" or "missing", !active.IsError);
        Assert.AreEqual(!active.IsError, provider.IsProviderActivated);
        Assert.AreEqual(mode == "missing" ? 3 : mode == "badcreate" ? 1 : 0, puts,
            "Only confirmed 404 may create; backend failure or schema mismatch cannot trigger replacement.");
    }

    [TestMethod]
    public async Task ActiveCancellationPublishesNoPartialRows()
    {
        var transport = new Transport { OnSend = async (_, token) => { await Task.Delay(Timeout.Infinite, token); throw new InvalidOperationException(); } };
        using var http = new HttpClient(transport);
        var provider = new QdrantOASIS(http, "https://fixture.invalid") { IsProviderActivated = true };
        using var cancellation = new CancellationTokenSource();
        var task = provider.ExecuteCosmicQueryAsync(Plan(provider, new[] { Guid.NewGuid() }), cancellationToken: cancellation.Token);
        Assert.AreEqual(1, transport.Calls);
        cancellation.Cancel();
        var result = await task;
        Assert.AreEqual("CQL_QUERY_CANCELLED", result.ErrorCode); Assert.IsNull(result.Result);
    }
}
