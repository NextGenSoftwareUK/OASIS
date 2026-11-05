using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.PouchDBOASIS.UnitTests;

[TestClass]
public class PouchDBCqlTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        public List<string> Paths = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.AreEqual(HttpMethod.Get, request.Method, "Query path must never provision or write.");
            Assert.AreEqual("Basic", request.Headers.Authorization!.Scheme);
            Assert.AreEqual(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("reader:controlled")), request.Headers.Authorization.Parameter);
            if (request.RequestUri!.AbsolutePath != "/oasis_holons") Assert.AreEqual("?conflicts=true", request.RequestUri.Query);
            Paths.Add(request.RequestUri!.AbsolutePath);
            return Task.FromResult(send(request));
        }
    }

    private static HttpResponseMessage Response(string json, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(json) };
    private static string Payload(Guid id, bool deleted = false)
    {
        var node = JsonSerializer.SerializeToNode(new Holon { Id = id, Name = "Native teaching record", DeletedDate = deleted ? DateTime.UtcNow : DateTime.MinValue })!.AsObject();
        node["_id"] = id.ToString("D"); node["_rev"] = "1-1234567890abcdef1234567890abcdef";
        node["Children"] = JsonSerializer.SerializeToNode(new[] { new Holon { Id = Guid.NewGuid(), Name = "Embedded child" } });
        return node.ToJsonString();
    }
    private static CosmicQuery<Holon> Query(Guid[] ids) => CosmicQuery<Holon>.Create(
            CosmicQueryScope.Named(ProviderType.PouchDBOASIS).Result)
        .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.PouchDBOASIS, ids).Result).Result;
    private static CosmicQueryPlan<Holon> Plan(PouchDBOASIS provider, CosmicQuery<Holon> query)
        => CosmicQueryPlanner.Plan(query, ProviderType.PouchDBOASIS,
            new[] { new CosmicQueryTarget(ProviderType.PouchDBOASIS, provider.QueryCapabilities) }).Result;

    [TestMethod]
    public async Task ExactKeysReturnThreeTwoAndPageAfterExcludingDeletedAndMissing()
    {
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        var handler = new Handler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/oasis_holons") return Response("{\"db_name\":\"oasis_holons\"}");
            var id = Guid.Parse(path.Split('/').Last());
            return id == ids[4] ? Response("{\"error\":\"not_found\",\"reason\":\"missing\"}", HttpStatusCode.NotFound) : Response(Payload(id, id == ids[3]));
        });
        using var http = new HttpClient(handler);
        var provider = new PouchDBOASIS("http://127.0.0.1:1", "reader", "controlled", http);
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
        Assert.IsFalse(handler.Paths.Any(path => path.Contains("search")));
    }

    [DataTestMethod]
    [DataRow("unauthorized")]
    [DataRow("forbidden")]
    [DataRow("server-error")]
    [DataRow("wrong-id")]
    [DataRow("duplicate-id")]
    [DataRow("oversized")]
    [DataRow("missing-database")]
    [DataRow("conflict")]
    [DataRow("missing-revision")]
    [DataRow("canonical-id")]
    [DataRow("native-tombstone")]
    public async Task NativeFailuresDoNotBecomeMissingRecordsOrPartialSuccess(string fault)
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() }.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        var handler = new Handler(request =>
        {
            var id = Guid.Parse(request.RequestUri!.AbsolutePath.Split('/').Last());
            if (id == ids[0]) return Response(Payload(id));
            return fault switch
            {
                "unauthorized" => Response("{}", HttpStatusCode.Unauthorized),
                "forbidden" => Response("{}", HttpStatusCode.Forbidden),
                "server-error" => Response("{}", HttpStatusCode.InternalServerError),
                "wrong-id" => Response(Payload(Guid.NewGuid())),
                "duplicate-id" => Response("{\"id\":\"" + id + "\",\"id\":\"" + id + "\"}"),
                "missing-database" => Response("{\"error\":\"not_found\",\"reason\":\"no_db_file\"}", HttpStatusCode.NotFound),
                "conflict" => Response(Payload(id).TrimEnd('}') + ",\"_conflicts\":[\"2-foreign\"]}"),
                "missing-revision" => Response(Payload(id).Replace("\"_rev\":\"1-1234567890abcdef1234567890abcdef\"", "\"_rev\":null")),
                "native-tombstone" => Response(Payload(id).TrimEnd('}') + ",\"_deleted\":true}"),
                "canonical-id" => Response(Payload(id).Replace("\"Id\":\"" + id + "\"", "\"Id\":\"" + Guid.NewGuid() + "\"")),
                _ => Response(new string('x', 1024 * 1024 + 1))
            };
        });
        using var http = new HttpClient(handler);
        var provider = new PouchDBOASIS("http://127.0.0.1:1", "reader", "controlled", http) { IsProviderActivated = true };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(ids)));
        Assert.IsTrue(result.IsError);
        Assert.AreEqual(fault == "oversized" ? "CQL_QUERY_LIMIT" : fault == "conflict" ? "CQL_REVISION_AMBIGUOUS" : "CQL_QUERY_FAILED", result.ErrorCode);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task MissingDatabaseActivationIsReadOnlyAndClearsReadiness()
    {
        var handler = new Handler(_ => Response("{}", HttpStatusCode.NotFound));
        using var http = new HttpClient(handler);
        var provider = new PouchDBOASIS("http://127.0.0.1:1", "reader", "controlled", http) { IsProviderActivated = true };
        Assert.IsTrue((await provider.ActivateQuerySourceAsync(default)).IsError);
        Assert.IsFalse(provider.IsProviderActivated);
        CollectionAssert.AreEqual(new[] { "/oasis_holons" }, handler.Paths);
    }
}
