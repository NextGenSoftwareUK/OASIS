using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlTypesenseHostedConfigurationTests
{
    private const string Origin = "https://typesense.example.test";
    private const string Key = "0123456789abcdef0123456789abcdef";
    private const string Collection = "/collections/oasis_holons";
    private const string Document = Collection + "/documents/11111111-1111-4111-8111-111111111111";

    private static IConfiguration Settings(string? origin, string? key) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?> {
            ["CQL_TYPESENSE_QUERY_ORIGIN"] = origin,
            ["CQL_TYPESENSE_QUERY_API_KEY"] = key
        }).Build();

    [Fact]
    public void EnrollsOnlyAnExplicitInactiveReadProfile()
    {
        var services = new ServiceCollection();
        services.AddTypesenseReadSource(Settings(null, null));
        Assert.Empty(services);
        services.AddTypesenseReadSource(Settings(Origin, Key));
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.TypesenseOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData(null, Key)]
    [InlineData(Origin, null)]
    [InlineData(Origin, "invalid")]
    [InlineData("http://typesense.example.test", Key)]
    [InlineData("https://localhost", Key)]
    [InlineData("https://user@typesense.example.test", Key)]
    [InlineData(Origin + "/index", Key)]
    [InlineData(Origin + "?q=x", Key)]
    public void RefusesInvalidConfigurationBeforeRegistration(string? origin, string? key)
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddTypesenseReadSource(Settings(origin, key)));
        Assert.Empty(services);
    }

    [Theory]
    [InlineData("POST", Origin + Collection)]
    [InlineData("GET", Origin + Collection + "/documents")]
    [InlineData("GET", Origin + Collection + "/documents/search")]
    [InlineData("GET", Origin + "/collections/foreign")]
    [InlineData("GET", Origin + Document + "?fields=id")]
    [InlineData("GET", "https://different.example.test" + Collection)]
    public async Task RefusesUnadmittedRequestsBeforeTransport(string method, string uri)
    {
        var transport = new ObservedTransport();
        using var client = new HttpClient(new CqlTypesenseReadSourceRegistration.ExactReadHandler(new Uri(Origin), transport));
        using var request = new HttpRequestMessage(new HttpMethod(method), uri);
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.SendAsync(request));
        Assert.Empty(transport.Requests);
    }

    [Fact]
    public async Task ForwardsOnlyCollectionMetadataAndExactDocumentReads()
    {
        var transport = new ObservedTransport();
        using var client = new HttpClient(new CqlTypesenseReadSourceRegistration.ExactReadHandler(new Uri(Origin), transport));
        using var index = await client.GetAsync(Origin + Collection);
        using var document = await client.GetAsync(Origin + Document);
        Assert.Equal(new[] { Collection, Document }, transport.Requests);
        Assert.Equal(HttpStatusCode.NotFound, document.StatusCode);
    }

    private sealed class ObservedTransport : HttpMessageHandler
    {
        internal List<string> Requests { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!.AbsolutePath);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
