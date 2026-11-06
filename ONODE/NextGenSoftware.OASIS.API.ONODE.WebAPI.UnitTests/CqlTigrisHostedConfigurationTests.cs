using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlTigrisHostedConfigurationTests
{
    private const string Origin = "https://teaching.t3.storage.dev";
    private const string Key = "/oasis/holon/11111111111141118111111111111111";

    [Fact]
    public void SourceIsOptInAndBoundToTheNamedTigrisBucket()
    {
        var services = new ServiceCollection();
        services.AddTigrisReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        var settings = new Dictionary<string, string?>
        {
            ["CQL_TIGRIS_QUERY_ORIGIN"] = "https://t3.storage.dev.other.example",
            ["CQL_TIGRIS_QUERY_BUCKET"] = "teaching",
            ["CQL_TIGRIS_QUERY_ACCESS_KEY"] = "controlled",
            ["CQL_TIGRIS_QUERY_SECRET_KEY"] = "controlled",
            ["CQL_TIGRIS_QUERY_PROBE_ID"] = "11111111-1111-4111-8111-111111111111"
        };
        IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        Assert.Throws<InvalidOperationException>(() => services.AddTigrisReadSource(Configuration()));
        settings["CQL_TIGRIS_QUERY_ORIGIN"] = "https://t3.storage.dev";
        settings["CQL_TIGRIS_QUERY_BUCKET"] = "other.teaching";
        Assert.Throws<InvalidOperationException>(() => services.AddTigrisReadSource(Configuration()));
        Assert.Empty(services);

        settings["CQL_TIGRIS_QUERY_BUCKET"] = "teaching";
        services.AddTigrisReadSource(Configuration());
        using var container = services.BuildServiceProvider();
        var source = Assert.Single(container.GetServices<CqlReadSourceProfile>()).Storage;
        Assert.Equal(ProviderType.TigrisOASIS, source.ProviderType.Value);
        Assert.False(source.IsProviderActivated);
    }

    [Theory]
    [InlineData("GET", Origin + Key, null, true)]
    [InlineData("GET", Origin + Key, "teaching.t3.storage.dev", true)]
    [InlineData("PUT", Origin + Key, null, false)]
    [InlineData("HEAD", Origin + Key, null, false)]
    [InlineData("GET", Origin + Key + "?versionId=other", null, false)]
    [InlineData("GET", Origin + "/?list-type=2", null, false)]
    [InlineData("GET", Origin + "/oasis/avatar/11111111111141118111111111111111", null, false)]
    [InlineData("GET", "https://other.t3.storage.dev" + Key, null, false)]
    [InlineData("GET", Origin + Key, "other.t3.storage.dev", false)]
    public async Task TransportAdmitsOnlyCanonicalHolonReads(string method, string address, string? host, bool admitted)
    {
        var transport = new ObservedTransport();
        using var client = new HttpClient(new CqlTigrisReadSourceRegistration.ExactReadHandler(new Uri(Origin), transport));
        using var request = new HttpRequestMessage(new HttpMethod(method), address);
        request.Headers.Host = host;
        if (admitted)
        {
            using var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
        else
            await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
        Assert.Equal(admitted ? 1 : 0, transport.Requests);
    }

    private sealed class ObservedTransport : HttpMessageHandler
    {
        internal int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
