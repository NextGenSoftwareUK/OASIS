using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.FirebaseOASIS.UnitTests;

[TestClass]
public class FirebaseCqlTests
{
    private static readonly Guid One = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid Two = Guid.Parse("00000000-0000-0000-0000-000000000002");
    private static readonly Guid Three = Guid.Parse("00000000-0000-0000-0000-000000000003");
    private sealed class Reply(Func<Guid, HttpResponseMessage> reply) : HttpMessageHandler
    {
        public List<Guid> Reads { get; } = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            if (request.RequestUri!.AbsolutePath.EndsWith(Guid.Empty.ToString("D") + ".json"))
                return Task.FromResult(Response("null"));
            var query = request.RequestUri.Query.TrimStart('?').Split('&').Select(part => part.Split('=', 2))
                .ToDictionary(part => part[0], part => Uri.UnescapeDataString(part[1]));
            Assert.AreEqual("$key", JsonSerializer.Deserialize<string>(query["orderBy"]));
            var id = Guid.Parse(JsonSerializer.Deserialize<string>(query["equalTo"])!);
            Reads.Add(id);
            return Task.FromResult(reply(id));
        }
    }
    private static HttpResponseMessage Response(string json, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    private static string Row(Guid key, Guid? payload = null, bool deleted = false)
        => JsonSerializer.Serialize(new Dictionary<string, object> { [key.ToString("D")] = new {
            id = payload ?? key, name = "Firebase native row", deletedDate = deleted ? DateTime.UtcNow : DateTime.MinValue } });
    private static CosmicQuery<Holon> Query(Guid[]? ids = null)
        => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.FirebaseOASIS).Result)
            .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.FirebaseOASIS, ids ?? new[] { One, Two, Three }).Result).Result;
    private static CosmicQueryPlan<Holon> Plan(FirebaseOASIS provider, CosmicQuery<Holon> query)
    {
        var plan = CosmicQueryPlanner.Plan(query, ProviderType.FirebaseOASIS,
            new[] { new CosmicQueryTarget(ProviderType.FirebaseOASIS, provider.QueryCapabilities) });
        Assert.IsFalse(plan.IsError, plan.Message); return plan.Result;
    }
    [TestMethod]
    public void CompilerFreezesSelectionAndCannotChangeAuthorityOrEscapeCatalogue()
    {
        var selected = new[] { One, Three };
        var query = Query(new[] { One, Two }).Where(row => selected.Contains(row.Id)).Result;
        selected[0] = Two;
        CollectionAssert.AreEqual(new[] { One }, FirebaseCqlCompiler.Compile(query, ProviderType.FirebaseOASIS).Result.ToArray());
        Assert.IsTrue(FirebaseCqlCompiler.Compile(query, ProviderType.GoogleCloudOASIS).IsError);
        Assert.IsTrue(FirebaseCqlCompiler.Compile(Query().Where(row => row.Name == "no").Result, ProviderType.FirebaseOASIS).IsError);
        Assert.IsTrue(FirebaseCqlCompiler.Compile(Query(Enumerable.Range(0,101).Select(_ => Guid.NewGuid()).ToArray()), ProviderType.FirebaseOASIS).IsError);
    }
    [TestMethod]
    public async Task NativeKeyUnionCompletesBeforePagingAndLifecycleAndCancellationAreExplicit()
    {
        using var reply = new Reply(id => Response(id == Two ? "null" : Row(id)));
        using var http = new HttpClient(reply);
        var provider = new FirebaseOASIS("https://synthetic.invalid", null, http);
        var plan = Plan(provider, Query().Skip(1).Result.Take(1).Result);
        Assert.AreEqual("CQL_PROVIDER_INACTIVE", (await provider.ExecuteCosmicQueryAsync(plan)).ErrorCode);
        using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
        Assert.AreEqual("CQL_QUERY_CANCELLED", (await provider.ExecuteCosmicQueryAsync(plan, cancellationToken: cancelled.Token)).ErrorCode);
        Assert.AreEqual(0, reply.Reads.Count);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError); Assert.IsTrue(provider.IsProviderActivated);
        var result = await provider.ExecuteCosmicQueryAsync(plan);
        Assert.IsFalse(result.IsError, result.Exception?.ToString());
        CollectionAssert.AreEqual(new[] { Three }, result.Result.Select(row => row.Id).ToArray());
        CollectionAssert.AreEqual(new[] { One, Two, Three }, reply.Reads);
        await provider.DeActivateProviderAsync(); Assert.IsFalse(provider.IsProviderActivated);
    }
    [DataTestMethod][DataRow("foreign")][DataRow("mismatch")][DataRow("multiple")][DataRow("array")][DataRow("scalar")][DataRow("bad-json")][DataRow("http")]
    public async Task InvalidLaterNativeResultDoesNotPublishEarlierRows(string fault)
    {
        using var reply = new Reply(id => {
            if (id == One) return Response(Row(id));
            return fault switch {
                "foreign" => Response(Row(Three)), "mismatch" => Response(Row(Two, Three)),
                "multiple" => Response("{" + Row(Two)[1..^1] + "," + Row(Three)[1..^1] + "}"),
                "array" => Response("[]"), "scalar" => Response("1"), "bad-json" => Response("{"),
                _ => Response("{}", HttpStatusCode.ServiceUnavailable)
            };
        });
        using var http = new HttpClient(reply);
        var provider = new FirebaseOASIS("https://synthetic.invalid", null, http);
        await provider.ActivateProviderAsync();
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query()));
        Assert.IsTrue(result.IsError, fault); Assert.IsNull(result.Result); Assert.AreEqual(2, reply.Reads.Count);
        if (fault == "http") Assert.AreEqual(HttpStatusCode.ServiceUnavailable, ((HttpRequestException)result.Exception).StatusCode);
    }
    [TestMethod]
    public async Task EmptySelectionDoesNotReadAndDeletedAndMissingRecordsDoNotConsumePages()
    {
        using var reply = new Reply(id => Response(id == One ? Row(id, deleted: true) : id == Two ? "{}" : Row(id)));
        using var http = new HttpClient(reply);
        var provider = new FirebaseOASIS("https://synthetic.invalid", null, http);
        await provider.ActivateProviderAsync();
        Assert.AreEqual(0, (await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(Array.Empty<Guid>())))).Result.Count);
        Assert.AreEqual(0, reply.Reads.Count);
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query().Take(1).Result));
        Assert.IsFalse(result.IsError, result.Exception?.ToString()); Assert.AreEqual(Three, result.Result.Single().Id);
    }
    [TestMethod]
    public async Task AggregateByteAndResultBudgetsFailWithoutPartialRows()
    {
        var huge = true;
        using var reply = new Reply(id => Response(huge ? new string(' ', 2100000) + "null" : Row(id)));
        using var http = new HttpClient(reply);
        var provider = new FirebaseOASIS("https://synthetic.invalid", null, http);
        await provider.ActivateProviderAsync();
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query()));
        Assert.AreEqual("CQL_QUERY_LIMIT", result.ErrorCode); Assert.IsNull(result.Result); Assert.AreEqual(2, reply.Reads.Count);
        huge = false;
        result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query()), maximumResults: 1);
        Assert.AreEqual("CQL_QUERY_LIMIT", result.ErrorCode); Assert.IsNull(result.Result);
    }
}
