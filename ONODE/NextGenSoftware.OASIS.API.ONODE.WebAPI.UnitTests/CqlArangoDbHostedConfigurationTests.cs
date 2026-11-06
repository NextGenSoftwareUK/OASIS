using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using ArangoProvider = NextGenSoftware.OASIS.API.Providers.ArangoDBOASIS.ArangoDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlArangoDbHostedConfigurationTests
{
    private const string Origin = "https://arango.example.test:8529";
    private const string Probe = "aaaaaaaa-1111-4111-8111-111111111111";
    private const string Route = Origin + "/_db/oasis/_api/cursor";

    [Fact]
    public void RegistrationRequiresExplicitTlsAndProbe()
    {
        var services = new ServiceCollection();
        services.AddArangoDbReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        var settings = new Dictionary<string, string?>
        {
            ["CQL_ARANGODB_QUERY_ORIGIN"] = "https://user@arango.example.test:8529",
            ["CQL_ARANGODB_QUERY_READER_USERNAME"] = "reader",
            ["CQL_ARANGODB_QUERY_READER_PASSWORD"] = "controlled",
            ["CQL_ARANGODB_QUERY_PROBE_ID"] = Probe
        };
        IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        Assert.Throws<InvalidOperationException>(() => services.AddArangoDbReadSource(Configuration()));
        settings["CQL_ARANGODB_QUERY_ORIGIN"] = Origin;
        settings["CQL_ARANGODB_QUERY_PROBE_ID"] = Guid.Empty.ToString("D");
        Assert.Throws<InvalidOperationException>(() => services.AddArangoDbReadSource(Configuration()));
        Assert.Empty(services);
        settings["CQL_ARANGODB_QUERY_PROBE_ID"] = Probe;
        services.AddArangoDbReadSource(Configuration());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.ArangoDBOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Fact]
    public async Task NativePublicProbeUsesTheGuardedCursor()
    {
        var transport = new ObservedTransport();
        using var client = GuardedClient(transport);
        var source = new ArangoProvider(Origin, "reader", "controlled", client, Guid.Parse(Probe));
        var result = await source.ActivateQuerySourceAsync(default);
        Assert.False(result.IsError, result.Exception?.ToString());
        Assert.Equal(1, transport.Requests);
    }

    [Theory]
    [InlineData("foreign-origin")]
    [InlineData("other-database")]
    [InlineData("write-route")]
    [InlineData("other-query")]
    [InlineData("extra-field")]
    [InlineData("cursor-size")]
    [InlineData("runtime")]
    [InlineData("duplicate-key")]
    [InlineData("too-many")]
    public async Task UnadmittedCommandsNeverReachTheTransport(string mutation)
    {
        var transport = new ObservedTransport();
        using var client = GuardedClient(transport);
        var keys = mutation switch
        {
            "duplicate-key" => new[] { Probe, Probe },
            "too-many" => Enumerable.Range(1, 101).Select(index => $"aaaaaaaa-1111-4111-8111-{index:D12}").ToArray(),
            _ => new[] { Probe }
        };
        var query = new Dictionary<string, object>
        {
            ["query"] = mutation == "other-query" ? "RETURN 1" : ArangoProvider.NativeIdentityQuery,
            ["batchSize"] = mutation == "cursor-size" ? 100 : 101,
            ["options"] = new { maxRuntime = mutation == "runtime" ? 16 : 15, failOnWarning = true },
            ["bindVars"] = new { keys }
        };
        if (mutation == "extra-field") query["count"] = true;
        var uri = mutation switch
        {
            "foreign-origin" => "https://other.example.test:8529/_db/oasis/_api/cursor",
            "other-database" => Origin + "/_db/foreign/_api/cursor",
            "write-route" => Origin + "/_db/oasis/_api/document/holons",
            _ => Route
        };
        using var body = new StringContent(JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.PostAsync(uri, body));
        Assert.Equal(0, transport.Requests);
    }

    private static HttpClient GuardedClient(HttpMessageHandler transport) =>
        new(new CqlArangoDbReadSourceRegistration.ExactReadHandler(new Uri(Origin), transport));

    private sealed class ObservedTransport : HttpMessageHandler
    {
        internal int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            Assert.Equal("Basic", request.Headers.Authorization?.Scheme);
            var record = new { _key = Probe, id = Probe, isPublic = true };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { error = false, hasMore = false, result = new[] { record } }))
            });
        }
    }
}
