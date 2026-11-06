using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlPineconeHostedConfigurationTests
{
    private const string Origin = "https://teaching.svc.pinecone.io";
    private const string Probe = "aaaaaaaa-1111-4111-8111-111111111111";
    private const string Fetch = "/vectors/fetch?namespace=holons&ids=" + Probe;

    [Fact]
    public void RegistersOnlyAnExplicitNativeIndex()
    {
        var services = new ServiceCollection();
        services.AddPineconeReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        var values = new Dictionary<string, string?>
        {
            ["CQL_PINECONE_QUERY_ORIGIN"] = "https://api.pinecone.io",
            ["CQL_PINECONE_QUERY_READER_KEY"] = "controlled",
            ["CQL_PINECONE_QUERY_PROBE_ID"] = Probe
        };
        IConfiguration Settings() => new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        Assert.Throws<InvalidOperationException>(() => services.AddPineconeReadSource(Settings()));
        values["CQL_PINECONE_QUERY_ORIGIN"] = Origin;
        services.AddPineconeReadSource(Settings());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.PineconeOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Fact]
    public async Task GuardAllowsExactNativeFetch()
    {
        var observed = new ObservedTransport();
        using var client = new HttpClient(new CqlPineconeReadSourceRegistration.ExactReadHandler(new Uri(Origin), observed));
        using var result = await client.GetAsync(Origin + Fetch);
        Assert.Equal(1, observed.Requests);
    }

    [Theory]
    [InlineData("POST", Origin + Fetch)]
    [InlineData("GET", "https://other.svc.pinecone.io" + Fetch)]
    [InlineData("GET", Origin + "/vectors/fetch?namespace=avatars&ids=" + Probe)]
    [InlineData("GET", Origin + Fetch + "&ids=" + Probe)]
    [InlineData("GET", Origin + Fetch + "&filter=everything")]
    [InlineData("GET", Origin + "/vectors/fetch?namespace=holons&ids=00000000-0000-0000-0000-000000000000")]
    public async Task UnadmittedRequestsNeverReachTransport(string method, string uri)
    {
        var observed = new ObservedTransport();
        using var client = new HttpClient(new CqlPineconeReadSourceRegistration.ExactReadHandler(new Uri(Origin), observed));
        using var request = new HttpRequestMessage(new HttpMethod(method), uri);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
        Assert.Equal(0, observed.Requests);
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
