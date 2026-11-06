using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlPocketBaseHostedConfigurationTests
{
    private const string Origin = "https://pocketbase.example.test";
    private const string Id = "11111111-1111-4111-8111-111111111111";
    private const string Path = "/api/collections/oasis_holons/records";
    private const string Filter = "(oasis_id='" + Id + "')&&is_deleted=false";
    private const string Parameters = "?perPage=100&page=1&sort=oasis_id&skipTotal=false&filter=";

    [Fact]
    public void RegistrationIsExplicitAndDoesNotActivateTheSource()
    {
        var services = new ServiceCollection();
        services.AddPocketBaseReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        var settings = new Dictionary<string, string?>
        {
            ["CQL_POCKETBASE_QUERY_ORIGIN"] = Origin,
            ["CQL_POCKETBASE_QUERY_TOKEN"] = "restricted-teaching-token"
        };
        IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        Assert.Throws<InvalidOperationException>(() => services.AddPocketBaseReadSource(Configuration()));
        Assert.Empty(services);
        settings["CQL_POCKETBASE_QUERY_PROBE_ID"] = Id;
        services.AddPocketBaseReadSource(Configuration());
        using var container = services.BuildServiceProvider();
        var source = Assert.Single(container.GetServices<CqlReadSourceProfile>()).Storage;
        Assert.Equal(ProviderType.PocketBaseOASIS, source.ProviderType.Value);
        Assert.False(source.IsProviderActivated);
    }

    [Theory]
    [InlineData("GET", Origin, Path, Parameters, Filter, true)]
    [InlineData("POST", Origin, Path, Parameters, Filter, false)]
    [InlineData("GET", "https://elsewhere.example.test", Path, Parameters, Filter, false)]
    [InlineData("GET", Origin, "/api/collections/oasis_avatars/records", Parameters, Filter, false)]
    [InlineData("GET", Origin, Path, "?perPage=100&page=2&sort=oasis_id&skipTotal=false&filter=", Filter, false)]
    [InlineData("GET", Origin, Path, Parameters, Filter + "||is_deleted=true", false)]
    public async Task GuardForwardsOnlyBoundedCanonicalReads(string method, string origin, string path,
        string parameters, string filter, bool admitted)
    {
        var transport = new ObservedTransport();
        using var client = new HttpClient(new CqlPocketBaseReadSourceRegistration.ExactReadHandler(new Uri(Origin), transport));
        using var request = new HttpRequestMessage(new HttpMethod(method), origin + path + parameters + Uri.EscapeDataString(filter));
        if (admitted)
        {
            using var result = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.OK, result.StatusCode);
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
