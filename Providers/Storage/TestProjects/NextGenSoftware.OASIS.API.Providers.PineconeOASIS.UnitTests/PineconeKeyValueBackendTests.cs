using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.PineconeOASIS.UnitTests;

/// <summary>In-memory model of the Pinecone data-plane endpoints the backend uses, with optional response tampering.</summary>
internal sealed class FakePineconeIndex : HttpMessageHandler
{
    public readonly SortedDictionary<string, string> Vectors = new(StringComparer.Ordinal);
    public readonly List<string> Calls = new();
    public Func<string, JsonObject, JsonObject>? Tamper;
    public HttpStatusCode? Status;
    public int PageSize = 100;

    private HttpResponseMessage Reply(string path, JsonObject body)
    {
        body = Tamper?.Invoke(path, body) ?? body;
        return new HttpResponseMessage(Status ?? HttpStatusCode.OK)
        { Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json") };
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        var path = request.RequestUri!.AbsolutePath;
        Calls.Add(path);
        Assert.AreEqual("2026-07", request.Headers.GetValues("X-Pinecone-API-Version").Single());
        Assert.AreEqual("test-key", request.Headers.GetValues("Api-Key").Single());
        var query = System.Web.HttpUtility.ParseQueryString(request.RequestUri.Query);
        var input = request.Content == null ? null : JsonNode.Parse(await request.Content.ReadAsStringAsync(token))!.AsObject();
        switch (path)
        {
            case "/describe_index_stats":
                return Reply(path, new JsonObject { ["dimension"] = 1, ["indexFullness"] = 0.0, ["totalVectorCount"] = Vectors.Count,
                    ["namespaces"] = new JsonObject { ["oasis"] = new JsonObject { ["vectorCount"] = Vectors.Count } } });
            case "/vectors/upsert":
                Assert.AreEqual("oasis", input!["namespace"]!.GetValue<string>());
                var vector = input["vectors"]!.AsArray().Single()!;
                Assert.AreEqual(1.0, vector["values"]!.AsArray().Single()!.GetValue<double>());
                if (Status == null) Vectors[vector["id"]!.GetValue<string>()] = vector["metadata"]!["v"]!.GetValue<string>();
                return Reply(path, new JsonObject { ["upsertedCount"] = 1 });
            case "/vectors/fetch":
                Assert.AreEqual("oasis", query["namespace"]);
                var found = new JsonObject();
                foreach (var id in query.GetValues("ids")!)
                    if (Vectors.TryGetValue(id, out var value))
                        found[id] = new JsonObject { ["id"] = id, ["values"] = new JsonArray(1.0), ["metadata"] = new JsonObject { ["v"] = value } };
                return Reply(path, new JsonObject { ["namespace"] = "oasis", ["vectors"] = found });
            case "/vectors/delete":
                foreach (var id in input!["ids"]!.AsArray()) Vectors.Remove(id!.GetValue<string>());
                return Reply(path, new JsonObject());
            case "/vectors/list":
                var prefix = query["prefix"] ?? "";
                var after = query["paginationToken"];
                var ids = Vectors.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal) && (after == null || string.CompareOrdinal(k, after) > 0)).ToList();
                var page = new JsonObject { ["namespace"] = "oasis",
                    ["vectors"] = new JsonArray(ids.Take(PageSize).Select(id => (JsonNode)new JsonObject { ["id"] = id }).ToArray()) };
                if (ids.Count > PageSize) page["pagination"] = new JsonObject { ["next"] = ids[PageSize - 1] };
                return Reply(path, page);
            default:
                throw new InvalidOperationException("Unexpected Pinecone path " + path);
        }
    }
}

[TestClass]
public class PineconeKeyValueBackendTests
{
    private static (PineconeKeyValueBackend Backend, FakePineconeIndex Index, HttpClient Http) Create()
    {
        var index = new FakePineconeIndex();
        var http = new HttpClient(index);
        return (new PineconeKeyValueBackend(http, "http://native.invalid", "test-key"), index, http);
    }

    [TestMethod]
    public async Task PutGetListDeleteRoundTripFollowsPaginationToTheEnd()
    {
        var (backend, index, http) = Create();
        using var _ = http;
        index.PageSize = 7;
        for (var i = 0; i < 20; i++) await backend.PutAsync($"oasis/holon/{i:D2}", "value" + i);
        await backend.PutAsync("oasis/avatar/x", "a");

        var keys = await backend.ListKeysAsync("oasis/holon/");
        CollectionAssert.AreEqual(Enumerable.Range(0, 20).Select(i => $"oasis/holon/{i:D2}").ToArray(), keys.ToArray());
        Assert.AreEqual(3, index.Calls.Count(c => c == "/vectors/list"));
        Assert.AreEqual("value3", await backend.GetAsync("oasis/holon/03"));
        Assert.IsNull(await backend.GetAsync("oasis/holon/missing"));
        await backend.DeleteAsync("oasis/holon/03");
        await backend.DeleteAsync("oasis/holon/never-existed");
        Assert.IsNull(await backend.GetAsync("oasis/holon/03"));
        Assert.AreEqual(19, (await backend.ListKeysAsync("oasis/holon/")).Count);
    }

    [DataTestMethod]
    [DataRow("oversized")]
    [DataRow("longid")]
    [DataRow("unicodeid")]
    [DataRow("emptyid")]
    public async Task LimitViolationsRejectBeforeAnyRequest(string mode)
    {
        var (backend, index, http) = Create();
        using var _ = http;
        var key = mode switch { "longid" => new string('k', 513), "unicodeid" => "oasis/é", "emptyid" => "", _ => "oasis/k" };
        var value = mode == "oversized" ? new string('x', 40961) : "v";
        await Assert.ThrowsExceptionAsync<ArgumentException>(async () =>
        {
            try { await backend.PutAsync(key, value); }
            catch (InvalidDataException cause) { throw new ArgumentException("limit", cause); }
        });
        Assert.AreEqual(0, index.Calls.Count);
    }

    [DataTestMethod]
    [DataRow("count")]
    [DataRow("missing")]
    [DataRow("type")]
    [DataRow("http")]
    public async Task UpsertRequiresExactlyOneAcknowledgedVector(string mode)
    {
        var (backend, index, http) = Create();
        using var _ = http;
        if (mode == "http") index.Status = HttpStatusCode.ServiceUnavailable;
        index.Tamper = (path, body) => mode switch
        {
            "count" => new JsonObject { ["upsertedCount"] = 0 },
            "missing" => new JsonObject(),
            "type" => new JsonObject { ["upsertedCount"] = "1" },
            _ => body
        };
        await Assert.ThrowsExceptionAsync<Exception>(async () =>
        {
            try { await backend.PutAsync("oasis/k", "v"); }
            catch (Exception cause) when (cause is InvalidDataException or HttpRequestException) { throw new Exception("rejected", cause); }
        });
        Assert.AreEqual(1, index.Calls.Count, "Unknown outcomes must never cause replay.");
    }

    [DataTestMethod]
    [DataRow("namespace")]
    [DataRow("foreign")]
    [DataRow("rowid")]
    [DataRow("vector")]
    [DataRow("valuetype")]
    public async Task FetchRejectsResponsesThatDifferFromTheRequestedRecord(string mode)
    {
        var (backend, index, http) = Create();
        using var _ = http;
        await backend.PutAsync("oasis/k", "v");
        index.Tamper = (path, body) =>
        {
            if (path != "/vectors/fetch") return body;
            var row = body["vectors"]!["oasis/k"]!.AsObject();
            switch (mode)
            {
                case "namespace": body["namespace"] = "other"; break;
                case "foreign": body["vectors"]!.AsObject().Remove("oasis/k"); body["vectors"]!["oasis/other"] = row; break;
                case "rowid": row["id"] = "oasis/other"; break;
                case "vector": row["values"] = new JsonArray(1.0, 2.0); break;
                case "valuetype": row["metadata"] = new JsonObject { ["v"] = 5 }; break;
            }
            return body;
        };
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => backend.GetAsync("oasis/k"));
    }

    [DataTestMethod]
    [DataRow("stall")]
    [DataRow("prefix")]
    public async Task ListRejectsNonAdvancingPagesAndForeignIds(string mode)
    {
        var (backend, index, http) = Create();
        using var _ = http;
        await backend.PutAsync("oasis/a/1", "v");
        index.Tamper = (path, body) =>
        {
            if (path != "/vectors/list") return body;
            if (mode == "stall") body["pagination"] = new JsonObject { ["next"] = "same" };
            else body["vectors"]!.AsArray().Add(new JsonObject { ["id"] = "oasis/b/1" });
            return body;
        };
        await Assert.ThrowsExceptionAsync<InvalidDataException>(() => backend.ListKeysAsync("oasis/a/"));
    }

    [TestMethod]
    public void ConstructorRequiresExplicitHostAndKey()
    {
        using var http = new HttpClient();
        Assert.ThrowsException<ArgumentException>(() => new PineconeKeyValueBackend(http, "not a host", "k"));
        Assert.ThrowsException<ArgumentException>(() => new PineconeKeyValueBackend(http, "http://host?x=1", "k"));
        Assert.ThrowsException<ArgumentException>(() => new PineconeKeyValueBackend(http, "http://host", " "));
        Assert.AreEqual(0, http.DefaultRequestHeaders.Count(), "The caller's client headers are never changed.");
    }
}

[TestClass]
public class PineconeProviderStoreTests
{
    private static CosmicQueryPlan<Holon> Plan(PineconeOASIS provider, Guid actor, Guid[] ids, bool paging = false)
    {
        var query = CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.PineconeOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(actor, ProviderType.PineconeOASIS, ids).Result).Result;
        if (paging) query = query.Skip(1).Result.Take(1).Result;
        return CosmicQueryPlanner.Plan(query, ProviderType.PineconeOASIS,
            new[] { new CosmicQueryTarget(ProviderType.PineconeOASIS, provider.QueryCapabilities) }).Result;
    }

    [TestMethod]
    public async Task SavedHolonsLoadAndAnswerCqlThroughTheStore()
    {
        var index = new FakePineconeIndex();
        using var http = new HttpClient(index);
        var provider = new PineconeOASIS(http, "http://native.invalid", "test-key");
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var actor = Guid.NewGuid();
        var saved = new List<Guid>();
        for (var i = 0; i < 2; i++)
        {
            var result = await provider.SaveHolonAsync(new Holon { Name = "coffee" + i, CreatedByAvatarId = actor }, saveChildren: false);
            Assert.IsFalse(result.IsError, result.Message);
            Assert.AreEqual(1, result.SavedCount);
            saved.Add(result.Result.Id);
        }
        Assert.AreEqual("coffee1", (await provider.LoadHolonAsync(saved[1], loadChildren: false)).Result.Name);

        var ids = new[] { saved[0], Guid.NewGuid(), saved[1] };
        var all = await provider.ExecuteCosmicQueryAsync(Plan(provider, actor, ids));
        Assert.IsFalse(all.IsError, all.Message);
        CollectionAssert.AreEquivalent(saved, all.Result.Select(h => h.Id).ToArray(), "Missing identities are skipped, not invented.");
        var page = await provider.ExecuteCosmicQueryAsync(Plan(provider, actor, saved.ToArray(), paging: true));
        Assert.AreEqual(1, page.Result.Count);
        var budget = await provider.ExecuteCosmicQueryAsync(Plan(provider, actor, saved.ToArray()), maximumResults: 1);
        Assert.AreEqual("CQL_QUERY_LIMIT", budget.ErrorCode);
        index.Status = HttpStatusCode.ServiceUnavailable;
        var failed = await provider.ExecuteCosmicQueryAsync(Plan(provider, actor, saved.ToArray()));
        Assert.AreEqual("CQL_QUERY_FAILED", failed.ErrorCode);
        Assert.IsNull(failed.Result);
    }

    [TestMethod]
    public async Task FailedWriteLeavesTheCallerHolonUntouched()
    {
        var index = new FakePineconeIndex { Status = HttpStatusCode.ServiceUnavailable };
        using var http = new HttpClient(index);
        var provider = new PineconeOASIS(http, "http://native.invalid", "test-key") { IsProviderActivated = true };
        var holon = new Holon { Name = "coffee" };
        var result = await provider.SaveHolonAsync(holon, saveChildren: false);
        Assert.IsTrue(result.IsError);
        Assert.AreEqual(0, result.SavedCount);
        Assert.AreEqual(Guid.Empty, holon.Id);
        Assert.IsFalse(holon.ProviderUniqueStorageKey?.ContainsKey(ProviderType.PineconeOASIS) == true);
    }

    [TestMethod]
    public async Task OversizedRecordIsRejectedWithoutWriting()
    {
        var index = new FakePineconeIndex();
        using var http = new HttpClient(index);
        var provider = new PineconeOASIS(http, "http://native.invalid", "test-key") { IsProviderActivated = true };
        var result = await provider.SaveHolonAsync(new Holon { Name = new string('x', 41000) }, saveChildren: false);
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(index.Calls.Contains("/vectors/upsert"));
        Assert.AreEqual(0, index.Vectors.Count);
    }
}
