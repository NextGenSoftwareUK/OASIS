using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.MeilisearchOASIS.IntegrationTests;

[TestClass]
public class MeilisearchCqlNativeTests
{
    public sealed class Source
    {
        public string Endpoint { get; set; } = "";
        public Guid Actor { get; set; }
        public Guid[] LiveIds { get; set; } = Array.Empty<Guid>();
        public Guid DeletedId { get; set; }
        public Guid MissingId { get; set; }
    }

    [TestMethod]
    [TestCategory("MeilisearchCqlNative")]
    public async Task ExistingNativeRecordsSupportThreeTwoPagingAndSourcePreservation()
    {
        var path = Environment.GetEnvironmentVariable("COSMIC_MEILISEARCH_CQL_SOURCE");
        var key = Environment.GetEnvironmentVariable("COSMIC_MEILISEARCH_CQL_KEY");
        Assert.IsFalse(string.IsNullOrWhiteSpace(path), "Explicit retained source is required.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(key), "Explicit source credential is required.");
        var source = JsonSerializer.Deserialize<Source>(await File.ReadAllTextAsync(path!))!;
        Assert.IsNotNull(source);
        Assert.IsTrue(Uri.TryCreate(source.Endpoint, UriKind.Absolute, out var endpoint)
            && endpoint.Scheme == "http" && endpoint.IsLoopback && endpoint.UserInfo.Length == 0
            && endpoint.AbsolutePath == "/" && endpoint.Query.Length == 0 && endpoint.Fragment.Length == 0);
        Assert.AreNotEqual(Guid.Empty, source.Actor);
        Assert.AreEqual(3, source.LiveIds.Length);
        var ids = source.LiveIds.Concat(new[] { source.DeletedId, source.MissingId }).ToArray();
        Assert.AreEqual(5, ids.Distinct().Count());
        Assert.IsFalse(ids.Contains(Guid.Empty));
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(15) };
        var provider = new MeilisearchOASIS(source.Endpoint, key!, http);
        async Task<string[]> Observe()
        {
            var observations = new List<string>();
            foreach (var id in ids)
            {
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                using var request = new HttpRequestMessage(HttpMethod.Get,
                    source.Endpoint.TrimEnd('/') + "/indexes/oasis_holons/documents/" + id.ToString("D"));
                using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                if (id == source.MissingId)
                {
                    Assert.AreEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode);
                    using var failure = await response.Content.ReadAsStreamAsync(deadline.Token);
                    using var errorBody = new MemoryStream();
                    var chunk = new byte[512];
                    int size;
                    while ((size = await failure.ReadAsync(chunk, deadline.Token)) > 0)
                    {
                        Assert.IsTrue(errorBody.Length + size <= 4096);
                        errorBody.Write(chunk, 0, size);
                    }
                    using var error = JsonDocument.Parse(errorBody.ToArray());
                    Assert.AreEqual("document_not_found", error.RootElement.GetProperty("code").GetString());
                    continue;
                }
                response.EnsureSuccessStatusCode();
                using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
                using var output = new MemoryStream();
                var buffer = new byte[8192];
                int count;
                while ((count = await stream.ReadAsync(buffer, deadline.Token)) > 0)
                {
                    Assert.IsTrue(output.Length + count <= 1024 * 1024, "Native fixture document exceeds budget.");
                    output.Write(buffer, 0, count);
                }
                var bytes = output.ToArray();
                var row = JsonSerializer.Deserialize<Holon>(bytes, options)!;
                Assert.AreEqual(id, row.Id);
                Assert.AreEqual(id == source.DeletedId, row.IsDeleted);
                observations.Add(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)));
            }
            return observations.ToArray();
        }
        var activation = await provider.ActivateQuerySourceAsync(default);
        Assert.IsFalse(activation.IsError, activation.Message);
        try
        {
            var before = await Observe();
            CosmicQuery<Holon> Query() => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.MeilisearchOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(source.Actor, ProviderType.MeilisearchOASIS, ids).Result).Result;
            async Task<Guid[]> Read(CosmicQuery<Holon> query)
            {
                var plan = CosmicQueryPlanner.Plan(query, ProviderType.MeilisearchOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.MeilisearchOASIS, provider.QueryCapabilities) });
                Assert.IsFalse(plan.IsError, plan.Message);
                var result = await provider.ExecuteCosmicQueryAsync(plan.Result);
                Assert.IsFalse(result.IsError, result.Message);
                return result.Result.Select(row => row.Id).ToArray();
            }
            var ordered = source.LiveIds.OrderBy(id => id.ToString("D"), StringComparer.Ordinal).ToArray();
            CollectionAssert.AreEqual(ordered, await Read(Query()));
            var subset = new[] { ordered[0], ordered[2] };
            CollectionAssert.AreEqual(subset, await Read(Query().Where(row => subset.Contains(row.Id)).Result));
            var excluded = new[] { source.DeletedId, source.MissingId };
            Assert.AreEqual(0, (await Read(Query().Where(row => excluded.Contains(row.Id)).Result)).Length);
            CollectionAssert.AreEqual(new[] { ordered[1] }, await Read(Query().Skip(1).Result.Take(1).Result));
            CollectionAssert.AreEqual(before, await Observe(), "Native source changed during read-only qualification.");
        }
        finally { await provider.DeActivateProviderAsync(); }
    }
}
