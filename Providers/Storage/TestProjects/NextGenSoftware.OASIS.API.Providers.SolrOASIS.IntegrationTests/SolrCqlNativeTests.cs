using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Providers.SolrOASIS.IntegrationTests;

[TestClass]
public class SolrCqlNativeTests
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
    [TestCategory("SolrCqlNative")]
    public async Task ExistingNativeRecordsSupportThreeTwoPagingAndSourcePreservation()
    {
        var path = Environment.GetEnvironmentVariable("COSMIC_SOLR_CQL_SOURCE");
        var username = Environment.GetEnvironmentVariable("COSMIC_SOLR_CQL_USERNAME");
        var password = Environment.GetEnvironmentVariable("COSMIC_SOLR_CQL_PASSWORD");
        Assert.IsFalse(string.IsNullOrWhiteSpace(path), "Explicit retained source is required.");
        Assert.AreEqual(username == null, password == null, "Basic authentication requires both values.");
        var source = JsonSerializer.Deserialize<Source>(await File.ReadAllTextAsync(path!))!;
        Assert.IsNotNull(source);
        Assert.IsTrue(Uri.TryCreate(source.Endpoint, UriKind.Absolute, out var endpoint)
            && endpoint.Scheme == "http" && endpoint.IsLoopback && endpoint.UserInfo.Length == 0
            && endpoint.AbsolutePath == "/" && endpoint.Query.Length == 0 && endpoint.Fragment.Length == 0);
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(15) };
        await RunNativeAsync(source, username, password, http, publicSource: false);
    }

    [TestMethod]
    [TestCategory("SolrHostedCqlNative")]
    public async Task QualifiedHttpsSourceSupportsExactPublicQueriesWithoutWrites()
    {
        var path = Environment.GetEnvironmentVariable("COSMIC_SOLR_HOSTED_CQL_SOURCE")
            ?? throw new InvalidOperationException("Explicit hosted teaching source is required.");
        var username = Environment.GetEnvironmentVariable("CQL_SOLR_QUERY_USERNAME");
        var password = Environment.GetEnvironmentVariable("CQL_SOLR_QUERY_PASSWORD");
        Assert.IsFalse(string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password),
            "Explicit restricted credentials are required.");
        Assert.IsTrue(new FileInfo(path).Length is > 0 and <= 16384);
        var source = JsonSerializer.Deserialize<Source>(await File.ReadAllBytesAsync(path))!;
        Assert.IsNotNull(source);
        Assert.IsTrue(Uri.TryCreate(source.Endpoint, UriKind.Absolute, out var endpoint)
            && endpoint.Scheme == "https" && !endpoint.IsLoopback && endpoint.UserInfo.Length == 0
            && endpoint.AbsolutePath == "/" && endpoint.Query.Length == 0 && endpoint.Fragment.Length == 0);
        using var http = new HttpClient(new CqlSolrReadSourceRegistration.ExactReadHandler(endpoint!,
            NativeConnectionGuard.CreateGuardedHttpHandler())) { Timeout = TimeSpan.FromSeconds(15) };
        await RunNativeAsync(source, username, password, http, publicSource: true);
    }

    private static async Task RunNativeAsync(Source source, string? username, string? password, HttpClient http, bool publicSource)
    {
        Assert.AreNotEqual(Guid.Empty, source.Actor);
        Assert.AreEqual(3, source.LiveIds.Length);
        var ids = source.LiveIds.Concat(new[] { source.DeletedId, source.MissingId }).ToArray();
        Assert.AreEqual(5, ids.Distinct().Count());
        Assert.IsFalse(ids.Contains(Guid.Empty));
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var provider = new SolrOASIS(source.Endpoint, username, password, http);
        async Task<string[]> Observe()
        {
            var observations = new List<string>();
            foreach (var id in ids)
            {
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                using var request = new HttpRequestMessage(HttpMethod.Get,
                    source.Endpoint.TrimEnd('/') + "/solr/oasis_holons/get?id=" + id.ToString("D") + "&wt=json&omitHeader=false");
                using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
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
                using var envelope = JsonDocument.Parse(bytes);
                Assert.AreEqual(0, envelope.RootElement.GetProperty("responseHeader").GetProperty("status").GetInt32());
                var native = envelope.RootElement.GetProperty("doc");
                if (id == source.MissingId)
                {
                    Assert.AreEqual(JsonValueKind.Null, native.ValueKind);
                    continue;
                }
                var nativeJson = native.GetRawText();
                var row = JsonSerializer.Deserialize<Holon>(nativeJson, options)!;
                Assert.AreEqual(id, row.Id);
                Assert.AreEqual(id == source.DeletedId, row.IsDeleted);
                if (publicSource)
                {
                    Assert.AreEqual(source.Actor, row.CreatedByAvatarId);
                    Assert.IsTrue(row.IsPublic && !row.Children.Any(), "Hosted source must be public and root-only.");
                }
                observations.Add(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(nativeJson))));
            }
            return observations.ToArray();
        }
        var activation = await provider.ActivateQuerySourceAsync(default);
        Assert.IsFalse(activation.IsError, activation.Message);
        try
        {
            var before = await Observe();
            CosmicQuery<Holon> Query() => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.SolrOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(source.Actor, ProviderType.SolrOASIS, ids).Result).Result;
            async Task<Guid[]> Read(CosmicQuery<Holon> query)
            {
                var plan = CosmicQueryPlanner.Plan(query, ProviderType.SolrOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.SolrOASIS, provider.QueryCapabilities) });
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
