using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MarqoOASIS.UnitTests;

[TestClass]
public class MarqoCqlTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<string> Paths { get; } = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.AreEqual(HttpMethod.Get, request.Method, "Query activation/execution must never provision or mutate.");
            Assert.IsNull(request.Content);
            Paths.Add(request.RequestUri!.AbsolutePath);
            return Task.FromResult(respond(request));
        }
    }

    private static HttpResponseMessage Reply(string json, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(json) };
    private static string Payload(Guid id, bool deleted = false, string? name = null, bool children = false)
        => JsonSerializer.Serialize(new Holon { Id = id, Name = name ?? "Native Marqo root",
            DeletedDate = deleted ? DateTime.UtcNow : DateTime.MinValue,
            Children = children ? new[] { new Holon { Id = Guid.NewGuid(), Name = "Native child" } } : Array.Empty<IHolon>() });
    private static string Envelope(Guid id, string? payload = null)
        => JsonSerializer.Serialize(new { _id = id.ToString("D"), doc = payload ?? Payload(id) });
    private static CosmicQuery<Holon> Query(IEnumerable<Guid> ids)
        => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MarqoOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.MarqoOASIS, ids).Result).Result;
    private static CosmicQueryPlan<Holon> Plan(MarqoOASIS provider, CosmicQuery<Holon> query)
        => CosmicQueryPlanner.Plan(query, ProviderType.MarqoOASIS,
            new[] { new CosmicQueryTarget(ProviderType.MarqoOASIS, provider.QueryCapabilities) }).Result;

    [TestMethod]
    public async Task NativeIdentitySelectionExcludesDeletedMissingAndPagesAfterFiltering()
    {
        var ids = Enumerable.Range(1, 5).Select(i => new Guid(i, 0, 0, new byte[8])).ToArray();
        var handler = new Handler(request => {
            if (request.RequestUri!.AbsolutePath.EndsWith("/settings")) return Reply("{\"type\":\"unstructured\",\"model\":\"hf/e5-base-v2\"}");
            var id = Guid.Parse(request.RequestUri.Segments.Last());
            return id == ids[4] ? Reply("{\"code\":\"document_not_found\"}", HttpStatusCode.NotFound)
                : Reply(Envelope(id, Payload(id, deleted: id == ids[3], children: id == ids[0])));
        });
        using var http = new HttpClient(handler);
        var provider = new MarqoOASIS("http://127.0.0.1:1", http);
        Assert.IsFalse((await provider.ActivateQuerySourceAsync(default)).IsError);
        async Task<Guid[]> Read(CosmicQuery<Holon> query)
        {
            var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, query));
            Assert.IsFalse(result.IsError, result.Message);
            Assert.IsTrue(result.Result.All(row => !row.Children.Any()));
            return result.Result.Select(row => row.Id).ToArray();
        }
        CollectionAssert.AreEqual(ids.Take(3).ToArray(), await Read(Query(ids)));
        var subset = new[] { ids[0], ids[2] };
        CollectionAssert.AreEqual(subset, await Read(Query(ids).Where(row => subset.Contains(row.Id)).Result));
        var excluded = ids.Skip(3).ToArray();
        Assert.AreEqual(0, (await Read(Query(ids).Where(row => excluded.Contains(row.Id)).Result)).Length);
        CollectionAssert.AreEqual(new[] { ids[1] }, await Read(Query(ids).Skip(1).Result.Take(1).Result));
        Assert.IsTrue(handler.Paths.All(path => path.StartsWith("/indexes/oasis-holons/")));
        Assert.IsFalse(handler.Paths.Any(path => path.Contains("search")));
    }

    [DataTestMethod]
    [DataRow("native-id")]
    [DataRow("canonical-id")]
    [DataRow("duplicate-native")]
    [DataRow("duplicate-canonical")]
    [DataRow("invalid-json")]
    [DataRow("wrong-payload-case")]
    [DataRow("index-missing")]
    [DataRow("oversized")]
    public async Task InvalidSecondDocumentNeverPublishesPartialRows(string fault)
    {
        var ids = Enumerable.Range(1, 2).Select(i => new Guid(i, 0, 0, new byte[8])).ToArray();
        var handler = new Handler(request => {
            var id = Guid.Parse(request.RequestUri!.Segments.Last());
            if (id == ids[0]) return Reply(Envelope(id));
            return fault switch {
                "native-id" => Reply(Envelope(Guid.NewGuid(), Payload(id))),
                "canonical-id" => Reply(Envelope(id, Payload(Guid.NewGuid()))),
                "duplicate-native" => Reply("{\"_id\":\"" + id + "\",\"_id\":\"" + id + "\",\"doc\":\"{}\"}"),
                "duplicate-canonical" => Reply(Envelope(id, "{\"Id\":\"" + id + "\",\"Id\":\"" + id + "\"}")),
                "invalid-json" => Reply("not-json"),
                "wrong-payload-case" => Reply(Envelope(id, "{\"id\":\"" + id + "\"}")),
                "index-missing" => Reply("{\"code\":\"index_not_found\"}", HttpStatusCode.NotFound),
                _ => Reply(new string('x', 1024 * 1024 + 1))
            };
        });
        using var http = new HttpClient(handler);
        var provider = new MarqoOASIS("http://127.0.0.1:1", http) { IsProviderActivated = true };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(ids)));
        Assert.IsTrue(result.IsError);
        Assert.IsNull(result.Result);
        Assert.AreEqual(fault == "oversized" ? "CQL_QUERY_LIMIT" : "CQL_QUERY_FAILED", result.ErrorCode);
        Assert.AreEqual(2, handler.Paths.Count);
    }

    [DataTestMethod]
    [DataRow(401)]
    [DataRow(403)]
    [DataRow(429)]
    [DataRow(500)]
    public async Task NativeHttpFailuresRetainTypedStatusAndNeverBecomeAbsence(int status)
    {
        var id = Guid.NewGuid();
        var handler = new Handler(_ => Reply("not-a-success-payload", (HttpStatusCode)status));
        using var http = new HttpClient(handler);
        var provider = new MarqoOASIS("http://127.0.0.1:1", http) { IsProviderActivated = true };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(new[] { id })));
        Assert.AreEqual("CQL_QUERY_FAILED", result.ErrorCode);
        Assert.IsNull(result.Result);
        Assert.AreEqual((HttpStatusCode)status, ((HttpRequestException)result.Exception).StatusCode);
        Assert.AreEqual(1, handler.Paths.Count);
    }

    [TestMethod]
    public async Task MissingIndexActivationFailsWithoutProvisioningAndClearsReadiness()
    {
        var handler = new Handler(_ => Reply("{\"code\":\"index_not_found\"}", HttpStatusCode.NotFound));
        using var http = new HttpClient(handler);
        var provider = new MarqoOASIS("http://127.0.0.1:1", http) { IsProviderActivated = true };
        var result = await provider.ActivateQuerySourceAsync(default);
        Assert.IsTrue(result.IsError);
        Assert.IsFalse(provider.IsProviderActivated);
        CollectionAssert.AreEqual(new[] { "/indexes/oasis-holons/settings" }, handler.Paths.ToArray());
    }

    [TestMethod]
    public async Task PreCancellationAndInactiveSourceDoNotSendNativeRequests()
    {
        var handler = new Handler(_ => throw new AssertFailedException("No request expected."));
        using var http = new HttpClient(handler);
        var provider = new MarqoOASIS("http://127.0.0.1:1", http);
        var plan = Plan(provider, Query(new[] { Guid.NewGuid() }));
        Assert.AreEqual("CQL_PROVIDER_INACTIVE", (await provider.ExecuteCosmicQueryAsync(plan)).ErrorCode);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.AreEqual("CQL_QUERY_CANCELLED", (await provider.ExecuteCosmicQueryAsync(plan, cancellationToken: cancelled.Token)).ErrorCode);
        Assert.AreEqual("CQL_QUERY_CANCELLED", (await provider.ActivateQuerySourceAsync(cancelled.Token)).ErrorCode);
        Assert.AreEqual(0, handler.Paths.Count);
    }

    [TestMethod]
    public async Task AggregateWireAndCompletePageBudgetsRejectWithoutPartialRows()
    {
        var ids = Enumerable.Range(1, 5).Select(i => new Guid(i, 0, 0, new byte[8])).ToArray();
        var handler = new Handler(request => {
            var id = Guid.Parse(request.RequestUri!.Segments.Last());
            return Reply(Envelope(id, Payload(id, name: new string('x', 900000))));
        });
        using var http = new HttpClient(handler);
        var provider = new MarqoOASIS("http://127.0.0.1:1", http) { IsProviderActivated = true };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(ids)));
        Assert.AreEqual("CQL_QUERY_LIMIT", result.ErrorCode);
        Assert.IsNull(result.Result);
        result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(ids.Take(2))), maximumResults: 1);
        Assert.AreEqual("CQL_QUERY_LIMIT", result.ErrorCode);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task CapturedSelectionIntersectsTrustedCatalogueBeforeNativeReads()
    {
        var known = Guid.NewGuid(); var foreign = Guid.NewGuid();
        var selected = new[] { known, foreign };
        var query = Query(new[] { known }).Where(row => selected.Contains(row.Id)).Result;
        selected[0] = Guid.NewGuid();
        var handler = new Handler(request => {
            Assert.AreEqual(known.ToString("D"), request.RequestUri!.Segments.Last());
            return Reply(Envelope(known));
        });
        using var http = new HttpClient(handler);
        var provider = new MarqoOASIS("http://127.0.0.1:1", http) { IsProviderActivated = true };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, query));
        Assert.IsFalse(result.IsError, result.Message);
        Assert.AreEqual(1, result.Result.Count);
        Assert.AreEqual(known, result.Result[0].Id);
        Assert.AreEqual(1, handler.Paths.Count);
    }

    [TestMethod]
    public async Task UnsupportedContentAndHistoricalReadsRejectBeforeTransport()
    {
        var handler = new Handler(_ => throw new AssertFailedException("No native read expected."));
        using var http = new HttpClient(handler);
        var provider = new MarqoOASIS("http://127.0.0.1:1", http) { IsProviderActivated = true };
        var query = Query(new[] { Guid.NewGuid() });
        var content = CosmicQueryPlanner.Plan(query.Where(row => row.Name == "Unsupported").Result, ProviderType.MarqoOASIS,
            new[] { new CosmicQueryTarget(ProviderType.MarqoOASIS, provider.QueryCapabilities) });
        Assert.IsTrue(content.IsError);
        provider.IsVersionControlEnabled = true;
        Assert.AreEqual("CQL_CAPABILITY_UNSUPPORTED", (await provider.ExecuteCosmicQueryAsync(Plan(provider, query))).ErrorCode);
        Assert.AreEqual(0, handler.Paths.Count);
    }

    [DataTestMethod]
    [DataRow("{\"type\":\"structured\",\"model\":\"hf/e5-base-v2\"}")]
    [DataRow("{\"type\":\"unstructured\",\"model\":\"\"}")]
    [DataRow("{\"type\":\"unstructured\",\"type\":\"unstructured\",\"model\":\"hf/e5-base-v2\"}")]
    public async Task InvalidIndexSettingsDoNotPublishReadiness(string settings)
    {
        var handler = new Handler(_ => Reply(settings));
        using var http = new HttpClient(handler);
        var provider = new MarqoOASIS("http://127.0.0.1:1", http);
        Assert.IsTrue((await provider.ActivateQuerySourceAsync(default)).IsError);
        Assert.IsFalse(provider.IsProviderActivated);
        Assert.AreEqual(1, handler.Paths.Count);
    }
}
