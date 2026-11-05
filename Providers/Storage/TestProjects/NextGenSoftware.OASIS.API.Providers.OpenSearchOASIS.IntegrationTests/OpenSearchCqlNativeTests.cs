using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Providers.OpenSearchOASIS.IntegrationTests;

[TestClass]
public class OpenSearchCqlNativeTests
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
    [TestCategory("OpenSearchCqlNative")]
    public async Task ExistingNativeRecordsSupportThreeTwoPagingAndSourcePreservation()
    {
        var path = Environment.GetEnvironmentVariable("COSMIC_OPENSEARCH_CQL_SOURCE");
        var username = Environment.GetEnvironmentVariable("COSMIC_OPENSEARCH_CQL_USERNAME");
        var password = Environment.GetEnvironmentVariable("COSMIC_OPENSEARCH_CQL_PASSWORD");
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
    [TestCategory("OpenSearchHostedCqlNative")]
    public async Task QualifiedHttpsSourceSupportsExactPublicQueriesWithoutWrites()
    {
        var path = Environment.GetEnvironmentVariable("COSMIC_OPENSEARCH_HOSTED_CQL_SOURCE")
            ?? throw new InvalidOperationException("Explicit hosted teaching source is required.");
        var username = Environment.GetEnvironmentVariable("CQL_OPENSEARCH_QUERY_USERNAME");
        var password = Environment.GetEnvironmentVariable("CQL_OPENSEARCH_QUERY_PASSWORD");
        Assert.IsFalse(string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password),
            "Explicit restricted credentials are required.");
        Assert.IsTrue(new FileInfo(path).Length is > 0 and <= 16384);
        var source = JsonSerializer.Deserialize<Source>(await File.ReadAllBytesAsync(path))!;
        Assert.IsNotNull(source);
        Assert.IsTrue(Uri.TryCreate(source.Endpoint, UriKind.Absolute, out var endpoint)
            && endpoint.Scheme == "https" && !endpoint.IsLoopback && endpoint.UserInfo.Length == 0
            && endpoint.AbsolutePath == "/" && endpoint.Query.Length == 0 && endpoint.Fragment.Length == 0);
        using var http = new HttpClient(new CqlSearchDocumentReadSources.ExactReadHandler(endpoint!, "oasis_holons",
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
        var provider = new OpenSearchOASIS(source.Endpoint, username, password, source.LiveIds[0], http);
        async Task<string[]> Observe()
        {
            var observations = new List<string>();
            foreach (var id in ids)
            {
                using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                using var request = new HttpRequestMessage(HttpMethod.Get,
                    source.Endpoint.TrimEnd('/') + "/oasis_holons/_doc/" + id.ToString("D") + "?realtime=true&_source=true");
                using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
                if (id == source.MissingId) Assert.AreEqual(System.Net.HttpStatusCode.NotFound,response.StatusCode);
                else response.EnsureSuccessStatusCode();
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
                Assert.AreEqual("oasis_holons",envelope.RootElement.GetProperty("_index").GetString());
                Assert.AreEqual(id.ToString("D"),envelope.RootElement.GetProperty("_id").GetString());
                if (id == source.MissingId)
                {
                    Assert.IsFalse(envelope.RootElement.GetProperty("found").GetBoolean());
                    Assert.IsFalse(envelope.RootElement.TryGetProperty("_source",out _));
                    continue;
                }
                Assert.IsTrue(envelope.RootElement.GetProperty("found").GetBoolean());
                var nativeJson = envelope.RootElement.GetProperty("_source").GetRawText();
                var row = JsonSerializer.Deserialize<Holon>(nativeJson, options)!;
                Assert.AreEqual(id, row.Id);
                Assert.AreEqual(id == source.DeletedId, row.IsDeleted);
                if (publicSource)
                {
                    Assert.AreEqual(source.Actor, row.CreatedByAvatarId);
                    Assert.IsTrue(row.IsPublic && !row.Children.Any());
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
            CosmicQuery<Holon> Query() => CosmicQuery<Holon>.Create(CosmicQueryScope.Named(ProviderType.OpenSearchOASIS).Result)
                .WithReadAccess(CosmicQueryReadAccess.AuthoritativeRecords(source.Actor, ProviderType.OpenSearchOASIS, ids).Result).Result;
            async Task<Guid[]> Read(CosmicQuery<Holon> query)
            {
                var plan = CosmicQueryPlanner.Plan(query, ProviderType.OpenSearchOASIS,
                    new[] { new CosmicQueryTarget(ProviderType.OpenSearchOASIS, provider.QueryCapabilities) });
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
