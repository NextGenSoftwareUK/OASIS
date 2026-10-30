using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SupabaseOASIS.UnitTests;

[TestClass]
public class SupabaseCqlTests
{
    private static readonly Guid One = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid Two = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid Three = Guid.Parse("00000000-0000-0000-0000-000000000003");

    private sealed class Reply(Func<HttpRequestMessage, string?, HttpResponseMessage> reply) : HttpMessageHandler
    {
        public List<Uri> Reads { get; } = new();
        public string? Mutation;
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (request.RequestUri!.Query == "?select=id&limit=0") return Response("[]");
            var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(token);
            if (request.Method == HttpMethod.Get) Reads.Add(request.RequestUri);
            else Mutation = body;
            return reply(request, body);
        }
    }
    private static HttpResponseMessage Response(string json, string? range = null, HttpStatusCode status = HttpStatusCode.OK)
    {
        var result = new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        if (range != null) result.Content.Headers.TryAddWithoutValidation("Content-Range", range);
        return result;
    }
    private static object Row(Guid id, Guid? payloadId = null, bool deleted = false)
        => new { id, parent_holon_id = (string?)null, holon_type = (int)HolonType.Holon, is_deleted = false,
            data_json = JsonSerializer.Serialize(new { id = payloadId ?? id, name = "native fixture",
                deletedDate = deleted ? DateTime.UtcNow : DateTime.MinValue }) };
    private static string Rows(params object[] rows) => JsonSerializer.Serialize(rows);
    private static CosmicQuery<Holon> Query(Guid[]? ids = null)
        => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SupabaseOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.SupabaseOASIS, ids ?? new[] { One, Two, Three }).Result).Result;
    private static CosmicQueryPlan<Holon> Plan(SupabaseOASIS provider, CosmicQuery<Holon> query)
    {
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.SupabaseOASIS,
            new[] { new CosmicQueryTarget(ProviderType.SupabaseOASIS, provider.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message); return plan.Result;
    }

    [TestMethod]
    public void CompilerFreezesSelectionAndRejectsWrongAuthorityAndUnsupportedShape()
    {
        var selected = new[] { One, Three };
        var query = Query(new[] { One, Two }).Where(row => selected.Contains(row.Id)).Result;
        selected[0] = Two;
        CollectionAssert.AreEqual(new[] { One }, SupabaseCqlCompiler.Compile(query, ProviderType.SupabaseOASIS).Result.ToArray());
        Assert.IsTrue(SupabaseCqlCompiler.Compile(query, ProviderType.PocketBaseOASIS).IsError);
        Assert.IsTrue(SupabaseCqlCompiler.Compile(Query().Where(row => row.Name == "no").Result, ProviderType.SupabaseOASIS).IsError);
        Assert.IsTrue(SupabaseCqlCompiler.Compile(Query(Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToArray()), ProviderType.SupabaseOASIS).IsError);
    }

    [TestMethod]
    public async Task NativeFilterReturnsCompleteAuthorizedPageAndLifecycleIsExplicit()
    {
        using var reply = new Reply((request, _) => {
            StringAssert.Contains(string.Join(",", request.Headers.GetValues("Prefer")), "count=exact");
            return Response(Rows(Row(One), Row(Three)), "0-1/2");
        });
        using var http = new HttpClient(reply);
        var provider = new SupabaseOASIS("https://synthetic.invalid", "synthetic", http);
        var query = Query().Skip(1).Result.Take(1).Result;
        var plan = Plan(provider, query);
        Assert.AreEqual("CQL_PROVIDER_INACTIVE", (await provider.ExecuteCosmicQueryAsync(plan)).ErrorCode);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.AreEqual("CQL_QUERY_CANCELLED", (await provider.ExecuteCosmicQueryAsync(plan, cancellationToken: cancelled.Token)).ErrorCode);
        Assert.AreEqual(0, reply.Reads.Count);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        Assert.IsTrue(provider.IsProviderActivated);
        var result = await provider.ExecuteCosmicQueryAsync(plan);
        Assert.IsFalse(result.IsError, result.Exception?.ToString());
        CollectionAssert.AreEqual(new[] { Three }, result.Result.Select(row => row.Id).ToArray());
        Assert.AreEqual(1, reply.Reads.Count);
        var url = Uri.UnescapeDataString(reply.Reads.Single().Query);
        StringAssert.Contains(url, "id=in.("); StringAssert.Contains(url, "is_deleted=eq.false&order=id.asc&limit=100");
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [DataTestMethod]
    [DataRow("missing-range")][DataRow("unknown-total")][DataRow("truncated")][DataRow("offset")]
    [DataRow("duplicate")][DataRow("foreign")][DataRow("mismatch")][DataRow("deleted")]
    [DataRow("unordered")][DataRow("null")][DataRow("missing-field")][DataRow("http")]
    public async Task InvalidNativeResponsesNeverPublishPartialRows(string fault)
    {
        var json = Rows(Row(One)); string? range = "0-0/1";
        switch (fault)
        {
            case "missing-range": range = null; break;
            case "unknown-total": range = "0-0/*"; break;
            case "truncated": range = "0-0/2"; break;
            case "offset": range = "1-1/1"; break;
            case "duplicate": json = Rows(Row(One), Row(One)); range = "0-1/2"; break;
            case "foreign": json = Rows(Row(Guid.NewGuid())); break;
            case "mismatch": json = Rows(Row(One, Two)); break;
            case "deleted": json = Rows(Row(One, deleted: true)); break;
            case "unordered": json = Rows(Row(Three), Row(One)); range = "0-1/2"; break;
            case "null": json = "null"; break;
            case "missing-field": json = "[{}]"; break;
        }
        using var http = new HttpClient(new Reply((_, _) => Response(json, range, fault == "http" ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK)));
        var provider = new SupabaseOASIS("https://synthetic.invalid", "synthetic", http);
        await provider.ActivateProviderAsync();
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query()));
        Assert.IsTrue(result.IsError, fault); Assert.IsNull(result.Result);
        if (fault == "http") Assert.AreEqual(HttpStatusCode.ServiceUnavailable, ((HttpRequestException)result.Exception).StatusCode);
    }

    [TestMethod]
    public async Task PointAndAllReadsShareCompletenessAndIdentityValidation()
    {
        using var http = new HttpClient(new Reply((_, _) => Response(Rows(Row(One)), "0-0/2")));
        var provider = new SupabaseOASIS("https://synthetic.invalid", "synthetic", http);
        Assert.IsTrue((await provider.LoadHolonAsync(One, loadChildren: false)).IsError);
        Assert.IsTrue((await provider.LoadAllHolonsAsync(loadChildren: false)).IsError);
        Assert.IsTrue((await provider.LoadHolonsForParentAsync(Two, loadChildren: false)).IsError);
    }

    [TestMethod]
    public async Task EmptySelectionAndEmptyNativeResultAreCompleteWithoutInventedRows()
    {
        using var reply = new Reply((_, _) => Response("[]", "*/0"));
        using var http = new HttpClient(reply);
        var provider = new SupabaseOASIS("https://synthetic.invalid", "synthetic", http);
        await provider.ActivateProviderAsync();
        Assert.AreEqual(0, (await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(Array.Empty<Guid>())))).Result.Count);
        Assert.AreEqual(0, reply.Reads.Count);
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query()));
        Assert.IsFalse(result.IsError, result.Exception?.ToString()); Assert.AreEqual(0, result.Result.Count);
    }

    [DataTestMethod][DataRow("http")][DataRow("malformed")][DataRow("identity")][DataRow("payload")][DataRow("missing-ack")]
    public async Task DispatchedSaveFailureRetainsCallerOwnershipAndReconciliationIdentity(string fault)
    {
        using var reply = new Reply((_, body) => {
            if (fault == "http") return Response("{}", status: HttpStatusCode.ServiceUnavailable);
            if (fault == "malformed") return Response("{");
            if (fault == "missing-ack") return Response("[]");
            var ack = JsonNode.Parse(body!)!.AsObject();
            if (fault == "identity") ack["id"] = Three;
            if (fault == "payload") ack["data_json"] = "{}";
            return Response("[" + ack.ToJsonString() + "]");
        });
        using var http = new HttpClient(reply);
        var provider = new SupabaseOASIS("https://synthetic.invalid", "synthetic", http);
        var row = new Holon { Id = Guid.Empty, Name = "owned" };
        var result = await provider.SaveHolonAsync(row, saveChildren: false);
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsSaved); Assert.AreEqual(0, result.SavedCount);
        Assert.AreEqual(Guid.Empty, row.Id);
        Assert.IsFalse(row.ProviderUniqueStorageKey?.ContainsKey(ProviderType.SupabaseOASIS) ?? false);
        Assert.IsInstanceOfType(result.Exception, typeof(INativeMutationReconciliationRequired));
        using var proposed = JsonDocument.Parse(reply.Mutation!);
        Assert.AreEqual(proposed.RootElement.GetProperty("id").GetGuid(), result.Exception.GetType().GetProperty("ProposedId")!.GetValue(result.Exception));
    }

    private sealed class ActivationReply(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            => Task.FromResult(Response("[]", status: status));
    }

    [DataTestMethod][DataRow(HttpStatusCode.NotFound)][DataRow(HttpStatusCode.Unauthorized)][DataRow(HttpStatusCode.ServiceUnavailable)]
    public async Task MissingOrRejectedTableCannotActivateProvider(HttpStatusCode status)
    {
        using var http = new HttpClient(new ActivationReply(status));
        var provider = new SupabaseOASIS("https://synthetic.invalid", "synthetic", http);
        var result = await provider.ActivateProviderAsync();
        Assert.IsTrue(result.IsError); Assert.IsFalse(provider.IsProviderActivated);
        Assert.AreEqual(status, ((HttpRequestException)result.Exception).StatusCode);
    }

    [TestMethod]
    public async Task PointReadCannotReturnAnotherValidCanonicalIdentity()
    {
        using var http = new HttpClient(new Reply((_, _) => Response(Rows(Row(Two)), "0-0/1")));
        var provider = new SupabaseOASIS("https://synthetic.invalid", "synthetic", http);
        var result = await provider.LoadHolonAsync(One, loadChildren: false);
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
    }

    private sealed class ExactHolon : Holon { public decimal ExactAmount { get; set; } }
    [TestMethod]
    public async Task ExactAcknowledgementPublishesIdAndDerivedPayloadWithoutLosingPrecision()
    {
        using var reply = new Reply((request, body) => {
            StringAssert.Contains(string.Join(",", request.Headers.GetValues("Prefer")), "return=representation");
            return Response("[" + body + "]");
        });
        using var http = new HttpClient(reply);
        var provider = new SupabaseOASIS("https://synthetic.invalid", "synthetic", http);
        var row = new ExactHolon { Id = Guid.Empty, ExactAmount = 1234567890123456789012345678.1m };
        var result = await provider.SaveHolonAsync(row, saveChildren: false);
        Assert.IsFalse(result.IsError, result.Exception?.ToString()); Assert.IsTrue(result.IsSaved); Assert.AreEqual(1, result.SavedCount);
        Assert.AreNotEqual(Guid.Empty, row.Id); Assert.AreEqual(row.Id.ToString("D"), row.ProviderUniqueStorageKey[ProviderType.SupabaseOASIS]);
        using var native = JsonDocument.Parse(reply.Mutation!);
        using var payload = JsonDocument.Parse(native.RootElement.GetProperty("data_json").GetString()!);
        Assert.AreEqual(row.ExactAmount, payload.RootElement.GetProperty("exactAmount").GetDecimal());
    }

    [TestMethod]
    public async Task NativeByteAndResultBudgetsRejectWholeOperation()
    {
        var huge = true;
        using var http = new HttpClient(new Reply((_, _) => huge ? Response(new string(' ', 4 * 1024 * 1024 + 1), "*/0") : Response(Rows(Row(One), Row(Two)), "0-1/2")));
        var provider = new SupabaseOASIS("https://synthetic.invalid", "synthetic", http);
        await provider.ActivateProviderAsync();
        Assert.IsTrue((await provider.ExecuteCosmicQueryAsync(Plan(provider, Query()))).IsError);
        huge = false;
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query()), maximumResults: 1);
        Assert.AreEqual("CQL_QUERY_LIMIT", result.ErrorCode); Assert.IsNull(result.Result);
    }
}
