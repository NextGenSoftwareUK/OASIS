using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.CouchbaseOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlCouchbaseHostedConfigurationTests
{
    private const string Origin = "https://couchbase.example.test:18093";
    private const string Bucket = "teaching";
    private const string Probe = "aaaaaaaa-1111-4111-8111-111111111111";
    private const string Route = Origin + "/query/service";

    [Fact]
    public void RegistrationRequiresExplicitTlsAndAValidBucket()
    {
        var services = new ServiceCollection();
        services.AddCouchbaseReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        var settings = new Dictionary<string, string?>
        {
            ["CQL_COUCHBASE_QUERY_ORIGIN"] = "https://user@couchbase.example.test:18093",
            ["CQL_COUCHBASE_QUERY_BUCKET"] = Bucket,
            ["CQL_COUCHBASE_QUERY_READER_USERNAME"] = "reader",
            ["CQL_COUCHBASE_QUERY_READER_PASSWORD"] = "controlled",
            ["CQL_COUCHBASE_QUERY_PROBE_ID"] = Probe
        };
        IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        Assert.Throws<InvalidOperationException>(() => services.AddCouchbaseReadSource(Configuration()));
        settings["CQL_COUCHBASE_QUERY_ORIGIN"] = Origin;
        settings["CQL_COUCHBASE_QUERY_BUCKET"] = "teaching`foreign";
        Assert.Throws<ArgumentException>(() => services.AddCouchbaseReadSource(Configuration()));
        Assert.Empty(services);
        settings["CQL_COUCHBASE_QUERY_BUCKET"] = Bucket;
        services.AddCouchbaseReadSource(Configuration());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.CouchbaseOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Fact]
    public async Task NativePublicProbeUsesTheGuardedQueryService()
    {
        var transport = new ObservedTransport();
        using var client = GuardedClient(transport);
        var source = new CouchbaseQuerySource(new Uri(Origin), Bucket, "reader", "controlled", client, Guid.Parse(Probe));
        var result = await source.ActivateQuerySourceAsync(default);
        Assert.False(result.IsError, result.Exception?.ToString());
        Assert.Equal(1, transport.Requests);
    }

    [Theory]
    [InlineData("foreign-origin")]
    [InlineData("wrong-route")]
    [InlineData("other-bucket")]
    [InlineData("false-readonly")]
    [InlineData("other-statement")]
    [InlineData("extra-field")]
    [InlineData("duplicate-key")]
    [InlineData("too-many")]
    public async Task UnadmittedCommandsNeverReachTheTransport(string mutation)
    {
        var transport = new ObservedTransport();
        using var client = GuardedClient(transport);
        var query = new Dictionary<string, object>
        {
            ["statement"] = CouchbaseCqlCompiler.NativeIdentityStatement(mutation == "other-bucket" ? "foreign" : Bucket),
            ["readonly"] = mutation != "false-readonly",
            ["timeout"] = "15s",
            ["$keys"] = new[] { Probe }
        };
        if (mutation == "other-statement") query["statement"] = "SELECT 1";
        if (mutation == "extra-field") query["creds"] = new[] { "foreign" };
        if (mutation == "duplicate-key") query["$keys"] = new[] { Probe, Probe };
        if (mutation == "too-many") query["$keys"] = Enumerable.Range(1, 101)
            .Select(index => $"aaaaaaaa-1111-4111-8111-{index:D12}").ToArray();
        var uri = mutation switch
        {
            "foreign-origin" => "https://other.example.test:18093/query/service",
            "wrong-route" => Origin + "/admin/active_requests",
            _ => Route
        };
        using var body = new StringContent(JsonSerializer.Serialize(query), Encoding.UTF8, "application/json");
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.PostAsync(uri, body));
        Assert.Equal(0, transport.Requests);
    }

    private static HttpClient GuardedClient(HttpMessageHandler transport) => new(new CqlCouchbaseReadSourceRegistration.ExactReadHandler(
        new Uri(Origin), CouchbaseCqlCompiler.NativeIdentityStatement(Bucket), transport));

    private sealed class ObservedTransport : HttpMessageHandler
    {
        internal int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            Assert.Equal("Basic", request.Headers.Authorization?.Scheme);
            var result = new { Key = Probe, Id = Probe, IsDeleted = false,
                DataJson = JsonSerializer.Serialize(new { id = Probe, isPublic = true }) };
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(new { status = "success", results = new[] { result } }))
            });
        }
    }
}
