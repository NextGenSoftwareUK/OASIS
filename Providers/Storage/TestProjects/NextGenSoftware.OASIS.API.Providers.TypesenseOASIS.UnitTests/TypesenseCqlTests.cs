using System.Net;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.TypesenseOASIS.UnitTests;

[TestClass]
public class TypesenseCqlTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        public List<string> Paths = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Assert.AreEqual(HttpMethod.Get, request.Method, "Query path must never provision or write.");
            Assert.AreEqual("controlled", request.Headers.GetValues("X-TYPESENSE-API-KEY").Single());
            Paths.Add(request.RequestUri!.AbsolutePath);
            return Task.FromResult(send(request));
        }
    }

    private static HttpResponseMessage Response(string json, HttpStatusCode status = HttpStatusCode.OK)
        => new(status) { Content = new StringContent(json) };
    private static string Payload(Guid id, bool deleted = false) => JsonSerializer.Serialize(new Holon
    { Id = id, Name = "Native teaching record", DeletedDate = deleted ? DateTime.UtcNow : DateTime.MinValue },
        new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    private static CosmicQuery<Holon> Query(Guid[] ids) => CosmicQuery<Holon>.Create(
            CosmicQueryScope.Named(ProviderType.TypesenseOASIS).Result)
        .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(Guid.NewGuid(), ProviderType.TypesenseOASIS, ids).Result).Result;
    private static CosmicQueryPlan<Holon> Plan(TypesenseOASIS provider, CosmicQuery<Holon> query)
        => CosmicQueryPlanner.Plan(query, ProviderType.TypesenseOASIS,
            new[] { new CosmicQueryTarget(ProviderType.TypesenseOASIS, provider.QueryCapabilities) }).Result;

    [TestMethod]
    public async Task ExactKeysReturnThreeTwoAndPageAfterExcludingDeletedAndMissing()
    {
        var ids = Enumerable.Range(0, 5).Select(_ => Guid.NewGuid()).OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
        var handler = new Handler(request =>
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/collections/oasis_holons") return Response("{\"name\":\"oasis_holons\"}");
            var id = Guid.Parse(path.Split('/').Last());
            return id == ids[4] ? Response("{}", HttpStatusCode.NotFound) : Response(Payload(id, id == ids[3]));
        });
        using var http = new HttpClient(handler);
        var provider = new TypesenseOASIS("http://127.0.0.1:1", "controlled", http);
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
                _ => Response(new string('x', 1024 * 1024 + 1))
            };
        });
        using var http = new HttpClient(handler);
        var provider = new TypesenseOASIS("http://127.0.0.1:1", "controlled", http) { IsProviderActivated = true };
        var result = await provider.ExecuteCosmicQueryAsync(Plan(provider, Query(ids)));
        Assert.IsTrue(result.IsError);
        Assert.AreEqual(fault == "oversized" ? "CQL_QUERY_LIMIT" : "CQL_QUERY_FAILED", result.ErrorCode);
        Assert.IsNull(result.Result);
    }

    [TestMethod]
    public async Task MissingCollectionActivationIsReadOnlyAndClearsReadiness()
    {
        var handler = new Handler(_ => Response("{}", HttpStatusCode.NotFound));
        using var http = new HttpClient(handler);
        var provider = new TypesenseOASIS("http://127.0.0.1:1", "controlled", http) { IsProviderActivated = true };
        Assert.IsTrue((await provider.ActivateQuerySourceAsync(default)).IsError);
        Assert.IsFalse(provider.IsProviderActivated);
        CollectionAssert.AreEqual(new[] { "/collections/oasis_holons" }, handler.Paths);
    }
}
