using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.ElasticsearchOASIS.UnitTests;

[TestClass]
public class ElasticsearchNativeBoundaryTests
{
    private sealed class Handler : HttpMessageHandler
    {
        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Respond = (_, _) => throw new InvalidOperationException("Unexpected I/O");
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => Respond(request, token);
    }

    private static HttpResponseMessage Response(object value, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json") };

    private static object Doc(Guid id, Guid actor, bool found = true)
        => found ? new { _index = "oasis-holons", _id = id.ToString("D"), found = true, _version = 1,
            _source = new { isDeleted = false, dataJson = JsonSerializer.Serialize(new Holon { Id = id, CreatedByAvatarId = actor },
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) } } :
            new { _index = "oasis-holons", _id = id.ToString("D"), found = false };

    private static object Ack(string id, string index = "oasis-holons", int failed = 0)
        => new { _index = index, _id = id, _version = 1, _seq_no = 0, _primary_term = 1,
            result = "created", _shards = new { failed, successful = 1 } };

    private static CosmicQueryPlan<Holon> Plan(ElasticsearchOASIS provider, Guid actor, Guid[] ids, bool page = false)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.ElasticsearchOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.ElasticsearchOASIS, ids).Result).Result;
        if (page) query = query.Skip(1).Result.Take(1).Result;
        return CosmicQueryPlanner.Plan(query, ProviderType.ElasticsearchOASIS,
            new[] { new CosmicQueryTarget(ProviderType.ElasticsearchOASIS, provider.QueryCapabilities) }).Result;
    }

    [TestMethod]
    public async Task MultiGetUsesOnlyAdmittedIdsAndRestoresOrderBeforePaging()
    {
        var actor = Guid.NewGuid();
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() }.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        var handler = new Handler { Respond = async (request, _) =>
        {
            Assert.AreEqual(HttpMethod.Post, request.Method);
            Assert.AreEqual("/oasis-holons/_mget", request.RequestUri!.AbsolutePath);
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            CollectionAssert.AreEqual(ids, body.RootElement.GetProperty("ids").EnumerateArray().Select(v => v.GetGuid()).ToArray());
            return Response(new { docs = ids.Reverse().Select(id => Doc(id, actor)) });
        }};
        using var http = new HttpClient(handler);
        var provider = new ElasticsearchOASIS(http, "https://native.invalid") { IsProviderActivated = true };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, actor, ids, page: true));
        Assert.IsFalse(result.IsError, result.Message);
        Assert.AreEqual(ids[1], result.Result.Single().Id);
        var limited = await provider.ExecuteCosmicQueryAsync(Plan(provider, actor, ids), maximumResults: 1);
        Assert.AreEqual("CQL_QUERY_LIMIT", limited.ErrorCode);
        Assert.IsNull(limited.Result);
    }

    [DataTestMethod]
    [DataRow("duplicate")]
    [DataRow("foreign")]
    [DataRow("payloadId")]
    [DataRow("omitted")]
    [DataRow("itemError")]
    [DataRow("wrongIndex")]
    [DataRow("revision")]
    public async Task InvalidMultiGetNeverPublishesPartialRows(string fault)
    {
        var actor = Guid.NewGuid(); var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        object second = fault switch {
            "duplicate" => Doc(ids[0], actor),
            "foreign" => Doc(Guid.NewGuid(), actor),
            "payloadId" => new { _id = ids[1].ToString("D"), _index = "oasis-holons", found = true, _version = 1,
                _source = new { isDeleted = false, dataJson = JsonSerializer.Serialize(new Holon { Id = Guid.NewGuid() }) } },
            "itemError" => new { _id = ids[1].ToString("D"), _index = "oasis-holons", error = new { type = "unavailable_shards_exception" } },
            "wrongIndex" => new { _id = ids[1].ToString("D"), _index = "other", found = false },
            "revision" => new { _id = ids[1].ToString("D"), _index = "oasis-holons", found = true, _version = 0 },
            _ => Doc(ids[1], actor)
        };
        var docs = fault == "omitted" ? new[] { Doc(ids[0], actor) } : new[] { Doc(ids[0], actor), second };
        using var http = new HttpClient(new Handler { Respond = (_, _) => Task.FromResult(Response(new { docs })) });
        var provider = new ElasticsearchOASIS(http, "https://native.invalid") { IsProviderActivated = true };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, actor, ids));
        Assert.AreEqual("CQL_QUERY_FAILED", result.ErrorCode);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task MissingRecordsAndEmptyCatalogueAreSuccessfulWithoutInventedRows()
    {
        var actor = Guid.NewGuid(); var id = Guid.NewGuid(); var calls = 0;
        using var http = new HttpClient(new Handler { Respond = (_, _) => { calls++; return Task.FromResult(Response(new { docs = new[] { Doc(id, actor, false) } })); } });
        var provider = new ElasticsearchOASIS(http, "https://native.invalid") { IsProviderActivated = true };
        var missing = await provider.ExecuteCosmicQueryAsync(Plan(provider, actor, new[] { id }));
        Assert.IsFalse(missing.IsError, missing.Message); Assert.AreEqual(0, missing.LoadedCount);
        var empty = await provider.ExecuteCosmicQueryAsync(Plan(provider, actor, Array.Empty<Guid>()));
        Assert.IsFalse(empty.IsError); Assert.AreEqual(1, calls);
    }

    [TestMethod]
    public async Task WireBudgetAndActiveCancellationAreEnforcedAtTransport()
    {
        using var cancelled = new CancellationTokenSource(); var oversized = true;
        using var http = new HttpClient(new Handler { Respond = (_, token) => {
            if (oversized) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(new string(' ', 4 * 1024 * 1024 + 1)) });
            cancelled.Cancel(); return Task.FromCanceled<HttpResponseMessage>(token);
        }});
        var provider = new ElasticsearchOASIS(http, "https://native.invalid") { IsProviderActivated = true };
        var plan = Plan(provider, Guid.NewGuid(), new[] { Guid.NewGuid() });
        var large = await provider.ExecuteCosmicQueryAsync(plan);
        Assert.AreEqual("CQL_QUERY_LIMIT", large.ErrorCode); Assert.IsNull(large.Result);
        oversized = false;
        var stopped = await provider.ExecuteCosmicQueryAsync(plan, cancellationToken: cancelled.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED", stopped.ErrorCode); Assert.IsNull(stopped.Result);
    }

    [DataTestMethod]
    [DataRow("http")]
    [DataRow("id")]
    [DataRow("index")]
    [DataRow("shards")]
    [DataRow("malformed")]
    public async Task FailedWriteAcknowledgementPreservesCaller(string fault)
    {
        var row = new Holon(); var keys = row.ProviderUniqueStorageKey;
        using var http = new HttpClient(new Handler { Respond = (request, _) => Task.FromResult(fault switch {
            "http" => Response(new { error = "offline" }, HttpStatusCode.ServiceUnavailable),
            "id" => Response(Ack(Guid.NewGuid().ToString("D"))),
            "index" => Response(Ack(request.RequestUri!.Segments.Last(), "other")),
            "shards" => Response(Ack(request.RequestUri!.Segments.Last(), failed: 1)),
            _ => Response(new { result = "created" })
        }) });
        var provider = new ElasticsearchOASIS(http, "https://native.invalid");
        var result = await provider.SaveHolonAsync(row, saveChildren: false);
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsSaved); Assert.AreEqual(0, result.SavedCount);
        Assert.AreEqual(Guid.Empty, row.Id); Assert.AreSame(keys, row.ProviderUniqueStorageKey);
    }

    [TestMethod]
    public async Task SavePublishesMatchingStagedIdentityOnlyAfterNativeAcknowledgement()
    {
        var row = new Holon(); var pending = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        string? key = null;
        using var http = new HttpClient(new Handler { Respond = async (request, _) => {
            key = request.RequestUri!.Segments.Last();
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync());
            using var holon = JsonDocument.Parse(body.RootElement.GetProperty("dataJson").GetString()!);
            Assert.AreEqual(Guid.Parse(key), holon.RootElement.GetProperty("id").GetGuid());
            return await pending.Task;
        }});
        var provider = new ElasticsearchOASIS(http, "https://native.invalid");
        var save = provider.SaveHolonAsync(row, saveChildren: false);
        Assert.AreEqual(Guid.Empty, row.Id); Assert.IsFalse(save.IsCompleted);
        pending.SetResult(Response(Ack(key!)));
        var result = await save;
        Assert.IsFalse(result.IsError, result.Message); Assert.IsTrue(result.IsSaved); Assert.AreEqual(1, result.SavedCount);
        Assert.AreEqual(key, row.ProviderUniqueStorageKey[ProviderType.ElasticsearchOASIS]);
    }

    [DataTestMethod]
    [DataRow(false, 1, 0)]
    [DataRow(true, 3, 2)]
    public async Task BatchStopContinueCountsOnlyAcknowledgedWrites(bool keepGoing, int attempts, int saves)
    {
        var count = 0;
        using var http = new HttpClient(new Handler { Respond = (request, _) => Task.FromResult(++count == 1
            ? Response(new { error = "offline" }, HttpStatusCode.ServiceUnavailable)
            : Response(Ack(request.RequestUri!.Segments.Last()))) });
        var provider = new ElasticsearchOASIS(http, "https://native.invalid");
        var result = await provider.SaveHolonsAsync(new[] { new Holon(), new Holon(), new Holon() },
            saveChildren: false, continueOnError: keepGoing);
        Assert.IsTrue(result.IsError); Assert.AreEqual(1, result.ErrorCount);
        Assert.AreEqual(attempts, count); Assert.AreEqual(saves, result.SavedCount);
        Assert.AreEqual(saves != 0, result.IsSaved); Assert.AreEqual(saves, result.Result.Count());
    }

    [TestMethod]
    public async Task FailedActivationClearsStaleState()
    {
        using var http = new HttpClient(new Handler { Respond = (_, _) => Task.FromResult(Response(new { error = "offline" }, HttpStatusCode.ServiceUnavailable)) });
        var provider = new ElasticsearchOASIS(http, "https://native.invalid") { IsProviderActivated = true };
        Assert.IsTrue((await provider.ActivateProviderAsync()).IsError);
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
