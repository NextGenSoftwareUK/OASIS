using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.ArcadeDBOASIS.UnitTests;

[TestClass]
public class ArcadeDBCqlTests
{
    private sealed class Handler(Func<Guid, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<Guid> Selected = new();
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("/api/v1/query/oasis", request.RequestUri!.AbsolutePath);
            Assert.AreEqual("Basic", request.Headers.Authorization!.Scheme);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(token));
            Assert.AreEqual("sql", body.RootElement.GetProperty("language").GetString());
            Assert.AreEqual("SELECT oasisId, doc FROM Holon WHERE oasisId = :id LIMIT 2", body.RootElement.GetProperty("command").GetString());
            var id = Guid.ParseExact(body.RootElement.GetProperty("params").GetProperty("id").GetString()!, "D");
            Selected.Add(id);
            return respond(id);
        }
    }
    private static HttpResponseMessage Response(string json, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(json) };
    private static string Payload(Guid id, bool deleted = false) => JsonSerializer.Serialize(new Holon
        { Id = id, Name = "Arcade native teaching record", DeletedDate = deleted ? DateTime.UtcNow : DateTime.MinValue });
    private static string Row(Guid key, Guid? documentId = null, bool deleted = false)
        => JsonSerializer.Serialize(new { oasisId = key.ToString("D"), doc = Payload(documentId ?? key, deleted) });
    private static string Envelope(params string[] rows) => "{\"result\":[" + string.Join(",", rows) + "]}";
    private static CosmicQuery<Holon> Query(Guid[] ids) => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ArcadeDBOASIS).Result)
        .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.ArcadeDBOASIS, ids).Result).Result;
    private static CosmicQueryPlan<Holon> Plan(ArcadeDBOASIS provider, CosmicQuery<Holon> query)
        => CosmicQueryPlanner.Plan(query, ProviderType.ArcadeDBOASIS,
            new[] { new CosmicQueryTarget(ProviderType.ArcadeDBOASIS, provider.QueryCapabilities) }).Result;

    [TestMethod]
    public async Task ExactParameterizedKeysReturnThreeTwoAndPageAfterDeletedAndMissing()
    {
        var ids = Enumerable.Range(0,5).Select(_ => Guid.NewGuid()).OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        var handler = new Handler(id => Response(id == ids[4] ? Envelope() : Envelope(Row(id, deleted:id == ids[3]))));
        using var http = new HttpClient(handler);
        var provider = new ArcadeDBOASIS("http://127.0.0.1:1", "root", "controlled", ids[0], http);
        Assert.IsFalse((await provider.ActivateQuerySourceAsync(default)).IsError);
        var full = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(ids)));
        Assert.IsFalse(full.IsError, full.Message);
        CollectionAssert.AreEqual(ids.Take(3).ToArray(), full.Result.Select(row => row.Id).ToArray());
        var subset = new[] { ids[0], ids[2] };
        var filtered = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(ids).Where(row => subset.Contains(row.Id)).Result));
        Assert.IsFalse(filtered.IsError, filtered.Message);
        CollectionAssert.AreEqual(subset, filtered.Result.Select(row => row.Id).ToArray());
        var page = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(ids).Skip(1).Result.Take(1).Result));
        Assert.IsFalse(page.IsError, page.Message);
        CollectionAssert.AreEqual(new[] { ids[1] }, page.Result.Select(row => row.Id).ToArray());
    }

    [DataTestMethod]
    [DataRow("unauthorized")]
    [DataRow("forbidden")]
    [DataRow("server_error")]
    [DataRow("malformed")]
    [DataRow("missing_doc")]
    [DataRow("wrong_key")]
    [DataRow("wrong_payload_id")]
    [DataRow("duplicate_rows")]
    [DataRow("duplicate_key")]
    [DataRow("duplicate_payload_id")]
    [DataRow("error_envelope")]
    [DataRow("oversized")]
    public async Task NativeFailuresNeverPublishPartialRows(string fault)
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() }.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        var handler = new Handler(id => {
            if (id == ids[0]) return Response(Envelope(Row(id)));
            return fault switch {
                "unauthorized" => Response("{}", HttpStatusCode.Unauthorized),
                "forbidden" => Response("{}", HttpStatusCode.Forbidden),
                "server_error" => Response("{}", HttpStatusCode.InternalServerError),
                "malformed" => Response("{oops"),
                "missing_doc" => Response(Envelope(JsonSerializer.Serialize(new { oasisId = id.ToString("D") }))),
                "wrong_key" => Response(Envelope(Row(Guid.NewGuid()))),
                "wrong_payload_id" => Response(Envelope(Row(id, Guid.NewGuid()))),
                "duplicate_rows" => Response(Envelope(Row(id),Row(id))),
                "duplicate_key" => Response(Envelope("{\"oasisId\":\"" + id + "\",\"oasisId\":\"" + id + "\",\"doc\":" + JsonSerializer.Serialize(Payload(id)) + "}")),
                "duplicate_payload_id" => Response(Envelope(JsonSerializer.Serialize(new { oasisId = id.ToString("D"), doc = "{\"Id\":\"" + id + "\",\"Id\":\"" + id + "\"}" }))),
                "error_envelope" => Response("{\"result\":[],\"error\":\"query failed\"}"),
                _ => Response(new string('x', 1024*1024+1))
            };
        });
        using var http = new HttpClient(handler);
        var provider = new ArcadeDBOASIS("http://127.0.0.1:1", "root", "controlled", http) { IsProviderActivated = true };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(ids)));
        Assert.IsTrue(result.IsError);
        Assert.AreEqual(fault == "oversized" ? "CQL_QUERY_LIMIT" : "CQL_QUERY_FAILED", result.ErrorCode);
        Assert.IsNull(result.Result);
    }

    [DataTestMethod]
    [DataRow("absent")]
    [DataRow("deleted")]
    [DataRow("duplicate")]
    public async Task ActivationVerifiesExactlyOneExistingLiveTeachingRecord(string fault)
    {
        var id = Guid.NewGuid();
        var handler = new Handler(key => Response(fault switch {
            "absent" => Envelope(), "deleted" => Envelope(Row(key,deleted:true)), _ => Envelope(Row(key),Row(key)) }));
        using var http = new HttpClient(handler);
        var provider = new ArcadeDBOASIS("http://127.0.0.1:1", "root", "controlled", id, http) { IsProviderActivated = true };
        Assert.IsTrue((await provider.ActivateQuerySourceAsync(default)).IsError);
        Assert.IsFalse(provider.IsProviderActivated);
        CollectionAssert.AreEqual(new[] { id }, handler.Selected);
    }

    [TestMethod]
    public async Task MissingProbeInactiveVersionAndCancellationDoNotDispatch()
    {
        var id = Guid.NewGuid();
        var handler = new Handler(_ => throw new AssertFailedException("Native read must not dispatch."));
        using var http = new HttpClient(handler);
        var provider = new ArcadeDBOASIS("http://127.0.0.1:1", "root", "controlled", http);
        Assert.IsTrue((await provider.ActivateQuerySourceAsync(default)).IsError);
        var plan = Plan(provider, Query(new[] { id }));
        Assert.AreEqual("CQL_PROVIDER_INACTIVE", (await provider.ExecuteCosmicQueryAsync(plan)).ErrorCode);
        provider.IsProviderActivated = true;
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.AreEqual("CQL_QUERY_CANCELLED", (await provider.ExecuteCosmicQueryAsync(plan, cancellationToken:cancelled.Token)).ErrorCode);
        provider.IsVersionControlEnabled = true;
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", (await provider.ExecuteCosmicQueryAsync(plan)).ErrorCode);
        Assert.AreEqual(0, handler.Selected.Count);
    }

    [TestMethod]
    public async Task AggregateResponseBudgetFailsWithoutPublishingRows()
    {
        var ids = Enumerable.Range(0,5).Select(_ => Guid.NewGuid()).ToArray();
        var handler = new Handler(id => Response(Envelope(Row(id))[..^1] + ",\"padding\":\"" + new string('x',950000) + "\"}"));
        using var http = new HttpClient(handler);
        var provider = new ArcadeDBOASIS("http://127.0.0.1:1", "root", "controlled", http) { IsProviderActivated = true };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(ids)));
        Assert.IsTrue(result.IsError); Assert.AreEqual("CQL_QUERY_LIMIT", result.ErrorCode); Assert.IsNull(result.Result);
    }
}
