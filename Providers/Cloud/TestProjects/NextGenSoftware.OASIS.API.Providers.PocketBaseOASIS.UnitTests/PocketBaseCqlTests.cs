using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.PocketBaseOASIS.UnitTests;

[TestClass]
public class PocketBaseCqlTests
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
            if (request.RequestUri!.AbsolutePath.EndsWith("/_superusers/auth-with-password"))
                return Response("{\"token\":\"synthetic-token\"}");
            var body = request.Content == null ? null : await request.Content.ReadAsStringAsync(token);
            if (request.Method == HttpMethod.Get) Reads.Add(request.RequestUri);
            else Mutation = body;
            return reply(request, body);
        }
    }
    private static HttpResponseMessage Response(string body, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static object Row(Guid id, Guid? payloadId = null, bool deleted = false)
        => new { id = "pb" + id.ToString("N")[19..], oasis_id = id.ToString("D"), is_deleted = false,
            data_json = JsonSerializer.Serialize(new { id = payloadId ?? id, name = "native fixture",
                deletedDate = deleted ? DateTime.UtcNow : DateTime.MinValue }) };
    private static string Page(object[] rows, int? total = null, int perPage = 100)
        => JsonSerializer.Serialize(new { page = 1, perPage, totalItems = total ?? rows.Length,
            totalPages = ((total ?? rows.Length) + perPage - 1) / perPage, items = rows });
    private static CosmicQuery<Holon> Query(Guid[] ids)
        => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.PocketBaseOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.PocketBaseOASIS, ids).Result).Result;
    private static CosmicQueryPlan<Holon> Plan(PocketBaseOASIS provider, CosmicQuery<Holon> query)
    {
        var result = CosmicQueryPlanner.Plan(query, ProviderType.PocketBaseOASIS,
            new[] { new CosmicQueryTarget(ProviderType.PocketBaseOASIS, provider.QueryCapabilities) });
        Assert.IsFalse(result.IsError, result.Message); return result.Result;
    }

    [TestMethod]
    public void FrozenSelectionCannotEscapeCatalogueOrChangeAuthority()
    {
        var selected = new[] { One, Three };
        var query = Query(new[] { One, Two }).Where(row => selected.Contains(row.Id)).Result;
        selected[0] = Two;
        CollectionAssert.AreEqual(new[] { One }, PocketBaseCqlCompiler.Compile(query, ProviderType.PocketBaseOASIS).Result.ToArray());
        Assert.IsTrue(PocketBaseCqlCompiler.Compile(query, ProviderType.MongoDBOASIS).IsError);
    }

    [DataTestMethod, DataRow("name"), DataRow("metadata"), DataRow("order"), DataRow("graph"), DataRow("bound")]
    public void UnsupportedShapesRejectBeforeRecordsAreRead(string shape)
    {
        var query = Query(shape == "bound" ? Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToArray() : new[] { One });
        query = shape switch
        {
            "name" => query.Where(row => row.Name == "coffee").Result,
            "metadata" => query.Where(row => CosmicQueryMetadata.Value<string>(row, "category") == "coffee").Result,
            "order" => query.OrderBy(row => row.Id).Result,
            "graph" => query.WithChildren(CosmicQueryGraphOptions.Create(maxChildDepth: 1).Result).Result,
            _ => query
        };
        Assert.IsTrue(PocketBaseCqlCompiler.Compile(query, ProviderType.PocketBaseOASIS).IsError);
    }

    [TestMethod]
    public async Task InactiveAndPreCancelledQueriesDoNotContactTransport()
    {
        using var http = new HttpClient(new Reply((_, _) => throw new AssertFailedException("No transport allowed.")));
        var provider = new PocketBaseOASIS("https://synthetic.invalid", "synthetic", "synthetic", http);
        var plan = Plan(provider, Query(new[] { One }));
        Assert.AreEqual("CQL_PROVIDER_INACTIVE", (await provider.ExecuteCosmicQueryAsync(plan)).ErrorCode);
        using var cancel = new CancellationTokenSource(); cancel.Cancel();
        var stopped = await provider.ExecuteCosmicQueryAsync(plan, cancellationToken: cancel.Token);
        Assert.AreEqual("CQL_QUERY_CANCELLED", stopped.ErrorCode);
        Assert.IsInstanceOfType(stopped.Exception, typeof(OperationCanceledException));
    }

    [TestMethod]
    public async Task OneNativeFilteredRequestPagesAfterMissingRowsAndPublishesLifecycle()
    {
        using var reply = new Reply((_, _) => Response(Page(new[] { Row(One), Row(Three) })));
        using var http = new HttpClient(reply);
        var provider = new PocketBaseOASIS("https://synthetic.invalid", "synthetic", "synthetic", http);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        Assert.IsTrue(provider.IsProviderActivated);
        var query = Query(new[] { One, Two, Three }).Skip(1).Result.Take(1).Result;
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, query));
        Assert.IsFalse(result.IsError, result.Message + result.Exception);
        Assert.AreEqual(Three, result.Result.Single().Id);
        Assert.AreEqual(1, reply.Reads.Count);
        var uri = Uri.UnescapeDataString(reply.Reads.Single().Query);
        StringAssert.Contains(uri, "sort=oasis_id"); StringAssert.Contains(uri, "is_deleted=false");
        foreach (var id in new[] { One, Two, Three }) StringAssert.Contains(uri, "oasis_id='" + id.ToString("D") + "'");
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
        Assert.IsFalse(provider.IsProviderActivated);
        Assert.IsFalse(http.DefaultRequestHeaders.Contains("Authorization"));
    }

    [DataTestMethod, DataRow("foreign"), DataRow("duplicate"), DataRow("projection"), DataRow("deleted"), DataRow("order"), DataRow("incomplete")]
    public async Task NativeResponseCannotPublishCorruptUnauthorizedOrPartialRows(string fault)
    {
        var body = fault switch
        {
            "foreign" => Page(new[] { Row(Three) }),
            "duplicate" => Page(new[] { Row(One), Row(One) }),
            "projection" => Page(new[] { Row(One, Two) }),
            "deleted" => Page(new[] { Row(One, deleted: true) }),
            "order" => Page(new[] { Row(Two), Row(One) }),
            _ => Page(new[] { Row(One) }, total: 2)
        };
        using var http = new HttpClient(new Reply((_, _) => Response(body)));
        var provider = new PocketBaseOASIS("https://synthetic.invalid", "synthetic", "synthetic", http);
        await provider.ActivateProviderAsync();
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(new[] { One, Two })));
        Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task CompleteResultBudgetAndNativeBodyLimitNeverPublishPartialRows()
    {
        foreach (var oversized in new[] { false, true })
        {
            using var http = new HttpClient(new Reply((_, _) => Response(oversized ? new string('x', 4 * 1024 * 1024 + 1) : Page(new[] { Row(One), Row(Two) }))));
            var provider = new PocketBaseOASIS("https://synthetic.invalid", "synthetic", "synthetic", http);
            await provider.ActivateProviderAsync();
            var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(new[] { One, Two })), maximumResults: 1);
            Assert.AreEqual("CQL_QUERY_LIMIT", result.ErrorCode); Assert.IsNull(result.Result);
        }
    }

    [TestMethod]
    public async Task NativeQueryFailurePreservesStatus()
    {
        using var http = new HttpClient(new Reply((_, _) => Response("{}", HttpStatusCode.ServiceUnavailable)));
        var provider = new PocketBaseOASIS("https://synthetic.invalid", "synthetic", "synthetic", http);
        await provider.ActivateProviderAsync();
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(new[] { One })));
        Assert.AreEqual("CQL_QUERY_FAILED", result.ErrorCode);
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, ((HttpRequestException)result.Exception).StatusCode);
        Assert.IsNull(result.Result);
    }

    [DataTestMethod, DataRow("http"), DataRow("malformed"), DataRow("identity"), DataRow("payload")]
    public async Task UnacknowledgedSaveCannotPublishCallerIdKeyOrSavedState(string fault)
    {
        using var reply = new Reply((request, body) =>
        {
            if (request.Method == HttpMethod.Get) return Response(Page(Array.Empty<object>(), perPage: 1));
            if (fault == "http") return Response("{}", HttpStatusCode.ServiceUnavailable);
            if (fault == "malformed") return Response("{}");
            var ack = JsonNode.Parse(body!)!.AsObject(); ack["id"] = "native000000001";
            if (fault == "identity") ack["oasis_id"] = Three.ToString("D");
            if (fault == "payload") ack["data_json"] = "{}";
            return Response(ack.ToJsonString());
        });
        using var http = new HttpClient(reply);
        var provider = new PocketBaseOASIS("https://synthetic.invalid", "synthetic", "synthetic", http);
        var source = new Holon { Id = Guid.Empty, Name = "caller remains owned" };
        var result = await provider.SaveHolonAsync(source, saveChildren: false);
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsSaved); Assert.AreEqual(0, result.SavedCount);
        Assert.AreEqual(Guid.Empty, source.Id);
        Assert.IsFalse(source.ProviderUniqueStorageKey?.ContainsKey(ProviderType.PocketBaseOASIS) ?? false);
        var reconciliation = result.Exception as INativeMutationReconciliationRequired;
        Assert.IsNotNull(reconciliation, result.Exception?.ToString());
        using var proposed = JsonDocument.Parse(reply.Mutation!);
        Assert.AreEqual(proposed.RootElement.GetProperty("oasis_id").GetString(), result.Exception!.GetType().GetProperty("Key")!.GetValue(result.Exception));
        if (fault == "http") Assert.AreEqual(HttpStatusCode.ServiceUnavailable, ((HttpRequestException)result.Exception.InnerException!).StatusCode);
    }

    private sealed class TaggedHolon : Holon { public decimal ExactAmount { get; set; } }

    [TestMethod]
    public async Task SuccessfulSavePublishesOnlyTheAcknowledgedProjectionAndPreservesRuntimeFields()
    {
        using var reply = new Reply((request, body) =>
        {
            if (request.Method == HttpMethod.Get) return Response(Page(Array.Empty<object>(), perPage: 1));
            var ack = JsonNode.Parse(body!)!.AsObject(); ack["id"] = "native000000001";
            return Response(ack.ToJsonString());
        });
        using var http = new HttpClient(reply);
        var provider = new PocketBaseOASIS("https://synthetic.invalid", "synthetic", "synthetic", http);
        var source = new TaggedHolon { Id = Guid.Empty, Name = "typed source", ExactAmount = 1234567890123456789012345678.1m };
        var result = await provider.SaveHolonAsync(source, saveChildren: false);
        Assert.IsFalse(result.IsError, result.Message + result.Exception);
        Assert.IsTrue(result.IsSaved); Assert.AreEqual(1, result.SavedCount);
        Assert.AreNotEqual(Guid.Empty, source.Id);
        using var request = JsonDocument.Parse(reply.Mutation!);
        using var canonical = JsonDocument.Parse(request.RootElement.GetProperty("data_json").GetString()!);
        Assert.AreEqual(source.ExactAmount, canonical.RootElement.GetProperty("exactAmount").GetDecimal());
        Assert.AreEqual(source.Id, canonical.RootElement.GetProperty("id").GetGuid());
        Assert.AreEqual(source.Id.ToString("D"), source.ProviderUniqueStorageKey[ProviderType.PocketBaseOASIS]);
    }

    [TestMethod]
    public async Task SingularLookupRejectsDuplicateNativeIdentityBeforeMutation()
    {
        using var reply = new Reply((_, _) => Response(Page(new[] { Row(One) }, total: 2, perPage: 1)));
        using var http = new HttpClient(reply);
        var provider = new PocketBaseOASIS("https://synthetic.invalid", "synthetic", "synthetic", http);
        var result = await provider.SaveHolonAsync(new Holon { Id = One }, saveChildren: false);
        Assert.IsTrue(result.IsError); Assert.IsNull(reply.Mutation);
    }
}

