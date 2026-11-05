using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using QdrantProvider = NextGenSoftware.OASIS.API.Providers.QdrantOASIS.QdrantOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlQdrantHostedConfigurationTests
{
    private const string Origin = "https://qdrant.example.test";
    private const string Id = "11111111-1111-4111-8111-111111111111";
    private const string Route = "/collections/oasis_holons/points?timeout=15";

    [Fact]
    public void EnrollmentRequiresAnExplicitHttpsSourceAndProbe()
    {
        var services = new ServiceCollection();
        services.AddQdrantReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        var settings = new Dictionary<string, string?> {
            ["CQL_QDRANT_QUERY_ORIGIN"] = Origin,
            ["CQL_QDRANT_QUERY_API_KEY"] = "restricted"
        };
        IConfiguration Build() => new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        Assert.Throws<InvalidOperationException>(() => services.AddQdrantReadSource(Build()));
        Assert.Empty(services);
        settings["CQL_QDRANT_QUERY_PROBE_ID"] = Id;
        services.AddQdrantReadSource(Build());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.QdrantOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData(Route, true, false)]
    [InlineData(Route, false, true)]
    [InlineData(Route, false, false)]
    [InlineData("/collections/oasis_holons/points/scroll", true, false)]
    public async Task RefusesVectorsDuplicateIdsEmptyIdsAndScans(string route, bool duplicate, bool vector)
    {
        var transport = new ObservedTransport();
        using var client = new HttpClient(new CqlQdrantReadSourceRegistration.ExactReadHandler(new Uri(Origin), transport));
        var ids = duplicate ? new[] { Id, Id } : vector ? new[] { Id } : Array.Empty<string>();
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.PostAsJsonAsync(Origin + route,
            new { ids, with_payload = true, with_vector = vector }));
        Assert.Equal(0, transport.Requests);
    }

    [Fact]
    public async Task ActualProviderProbeReachesTheOwnedPointRead()
    {
        var transport = new ObservedTransport();
        using var client = new HttpClient(new CqlQdrantReadSourceRegistration.ExactReadHandler(new Uri(Origin), transport));
        var provider = new QdrantProvider(client, Origin, "restricted", Guid.Parse(Id));
        Assert.True((await provider.ActivateQuerySourceAsync(default)).IsError);
        Assert.Equal(1, transport.Requests);
    }

    private sealed class ObservedTransport : HttpMessageHandler
    {
        internal int Requests { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
