using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlLanceDbHostedConfigurationTests
{
    private const string Origin = "https://teaching.api.lancedb.com";
    private const string Probe = "aaaaaaaa-1111-4111-8111-111111111111";
    private const string Route = "/v1/table/oasis_holons/query/";

    [Fact]
    public void RegistersOnlyAnExplicitDatabase()
    {
        var services = new ServiceCollection();
        services.AddLanceDbReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        var settings = new Dictionary<string, string?>
        {
            ["CQL_LANCEDB_QUERY_ORIGIN"] = "https://api.lancedb.com",
            ["CQL_LANCEDB_QUERY_DATABASE"] = "oasis",
            ["CQL_LANCEDB_QUERY_READER_KEY"] = "controlled",
            ["CQL_LANCEDB_QUERY_PROBE_ID"] = Probe
        };
        IConfiguration Config() => new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        Assert.Throws<InvalidOperationException>(() => services.AddLanceDbReadSource(Config()));
        settings["CQL_LANCEDB_QUERY_ORIGIN"] = Origin;
        services.AddLanceDbReadSource(Config());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.LanceDBOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Fact]
    public async Task ExactQueryReachesTransport()
    {
        var transport = new ObservedTransport();
        using var client = new HttpClient(new CqlLanceDbReadSourceRegistration.ExactReadHandler(new Uri(Origin), "oasis", transport));
        using var request = Request("");
        using var response = await client.SendAsync(request);
        Assert.Equal(1, transport.Requests);
    }

    [Theory]
    [InlineData("foreign-origin")]
    [InlineData("foreign-database")]
    [InlineData("write-route")]
    [InlineData("extra-field")]
    [InlineData("vector")]
    [InlineData("version")]
    [InlineData("limit")]
    [InlineData("predicate")]
    [InlineData("duplicate")]
    public async Task UnadmittedCommandsNeverReachTransport(string mutation)
    {
        var transport = new ObservedTransport();
        using var client = new HttpClient(new CqlLanceDbReadSourceRegistration.ExactReadHandler(new Uri(Origin), "oasis", transport));
        using var request = Request(mutation);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
        Assert.Equal(0, transport.Requests);
    }

    private static HttpRequestMessage Request(string mutation)
    {
        var origin = mutation == "foreign-origin" ? "https://foreign.api.lancedb.com" : Origin;
        var route = mutation == "write-route" ? "/v1/table/oasis_holons/delete/" : Route;
        var predicate = mutation switch
        {
            "predicate" => "id IS NOT NULL",
            "duplicate" => $"id IN ('{Probe}','{Probe}')",
            _ => $"id IN ('{Probe}')"
        };
        var body = new Dictionary<string, object?>
        {
            ["k"] = mutation is "limit" or "duplicate" ? 3 : 2,
            ["offset"] = 0,
            ["columns"] = new[] { "id", "doc" },
            ["prefilter"] = true,
            ["vector"] = mutation == "vector" ? new[] { 1 } : Array.Empty<int>(),
            ["version"] = mutation == "version" ? 1 : null,
            ["filter"] = predicate
        };
        if (mutation == "extra-field") body["full_text_query"] = new { query = "coffee" };
        var request = new HttpRequestMessage(HttpMethod.Post, origin + route)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("x-lancedb-database", mutation == "foreign-database" ? "other" : "oasis");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.apache.arrow.file"));
        return request;
    }

    private sealed class ObservedTransport : HttpMessageHandler
    {
        public int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
