using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlWeaviateHostedConfigurationTests
{
    private const string Origin = "https://teaching.weaviate.example:443";
    private const string Search = "/weaviate.v1.Weaviate/Search";

    [Fact]
    public void RegistrationRequiresAllSettings()
    {
        var services = new ServiceCollection();
        services.AddWeaviateReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        var settings = new Dictionary<string, string?>
        {
            ["CQL_WEAVIATE_QUERY_ORIGIN"] = Origin,
            ["CQL_WEAVIATE_QUERY_READER_API_KEY"] = "restricted-reader",
            ["CQL_WEAVIATE_QUERY_PROBE_ID"] = Guid.Empty.ToString("D")
        };
        IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        Assert.Throws<InvalidOperationException>(() => services.AddWeaviateReadSource(Configuration()));
        settings["CQL_WEAVIATE_QUERY_PROBE_ID"] = "11111111-1111-4111-8111-111111111111";
        settings["CQL_WEAVIATE_QUERY_ORIGIN"] = Origin + "/v1";
        Assert.Throws<InvalidOperationException>(() => services.AddWeaviateReadSource(Configuration()));
        Assert.Empty(services);

        settings["CQL_WEAVIATE_QUERY_ORIGIN"] = Origin;
        services.AddWeaviateReadSource(Configuration());
        using var container = services.BuildServiceProvider();
        var source = Assert.Single(container.GetServices<CqlReadSourceProfile>()).Storage;
        Assert.Equal(ProviderType.WeaviateOASIS, source.ProviderType.Value);
        Assert.False(source.IsProviderActivated);
    }

    [Theory]
    [InlineData("POST", Origin + Search, 2, "application/grpc", null, true)]
    [InlineData("POST", Origin + Search, 1, "application/grpc", null, false)]
    [InlineData("POST", Origin + Search, 2, "application/json", null, false)]
    [InlineData("POST", Origin + Search, 2, "application/grpc", "other.example", false)]
    [InlineData("GET", Origin + Search, 2, "application/grpc", null, false)]
    [InlineData("POST", Origin + Search + "?other=1", 2, "application/grpc", null, false)]
    [InlineData("POST", Origin + "/weaviate.v1.Weaviate/BatchObjects", 2, "application/grpc", null, false)]
    [InlineData("POST", "https://other.example" + Search, 2, "application/grpc", null, false)]
    public async Task TransportAdmitsOnlyTheConfiguredGrpcSearch(
        string method, string address, int httpMajor, string contentType, string? host, bool admitted)
    {
        var transport = new ObservedTransport();
        using var client = new HttpClient(new CqlWeaviateReadSourceRegistration.ExactReadHandler(new Uri(Origin), transport));
        using var request = new HttpRequestMessage(new HttpMethod(method), address)
        {
            Version = new Version(httpMajor, 0),
            Content = new ByteArrayContent(Array.Empty<byte>())
        };
        request.Headers.Host = host;
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);
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
