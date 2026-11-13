using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlSupabaseHostedConfigurationTests
{
    private const string Origin = "https://supabase.example.test";
    private const string Id = "11111111-1111-4111-8111-111111111111";
    private const string Path = "/rest/v1/oasis_holons";
    private const string Filter = Id + ")&is_deleted=eq.false&order=id.asc&limit=100&offset=0";
    private const string Parameters = "?id=in.(";

    [Fact]
    public void SupabaseRequiresAnExplicitQueryOnlyTariff()
    {
        var providers = new[] { ProviderType.SupabaseOASIS };
        var tariff = new StorageQueryTariff(ProviderType.SupabaseOASIS, 0, "demo-read-v1");
        var bindings = Array.Empty<NativeStorageAdmissionBinding>();
        var factory = new PricedStorageAdmissionFactory(null, bindings,
            Array.Empty<ProviderType>(), new[] { tariff }, null, providers);

        Assert.Equal(providers, factory.QueryProviders);
        Assert.Contains(ProviderType.SupabaseOASIS, factory.QueryOnlyProviders);
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null, bindings,
            Array.Empty<ProviderType>(), new[] { tariff }, null, Array.Empty<ProviderType>()));
    }

    [Fact]
    public void RegistrationIsExplicitAndDoesNotActivateTheSource()
    {
        var services = new ServiceCollection();
        services.AddSupabaseReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        var settings = new Dictionary<string, string?>
        {
            ["CQL_SUPABASE_QUERY_ORIGIN"] = Origin,
            ["CQL_SUPABASE_QUERY_GATEWAY_KEY"] = "sb_publishable_controlled-key",
            ["CQL_SUPABASE_QUERY_READER_TOKEN"] = Jwt("authenticated")
        };
        IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        Assert.Throws<InvalidOperationException>(() => services.AddSupabaseReadSource(Configuration()));
        Assert.Empty(services);
        settings["CQL_SUPABASE_QUERY_PROBE_ID"] = Id;
        settings["CQL_SUPABASE_QUERY_READER_TOKEN"] = Jwt("service_role");
        Assert.Throws<InvalidOperationException>(() => services.AddSupabaseReadSource(Configuration()));
        settings["CQL_SUPABASE_QUERY_READER_TOKEN"] = Jwt("authenticated");
        settings["CQL_SUPABASE_QUERY_GATEWAY_KEY"] = "sb_secret_controlled-key";
        Assert.Throws<InvalidOperationException>(() => services.AddSupabaseReadSource(Configuration()));
        settings["CQL_SUPABASE_QUERY_GATEWAY_KEY"] = "sb_publishable_controlled-key";
        services.AddSupabaseReadSource(Configuration());
        using var container = services.BuildServiceProvider();
        var source = Assert.Single(container.GetServices<CqlReadSourceProfile>()).Storage;
        Assert.Equal(ProviderType.SupabaseOASIS, source.ProviderType.Value);
        Assert.False(source.IsProviderActivated);
    }

    private static string Jwt(string role) => "header." + Convert.ToBase64String(
        System.Text.Encoding.UTF8.GetBytes($"{{\"role\":\"{role}\"}}")).TrimEnd('=').Replace('+', '-').Replace('/', '_') + ".signature";

    [Theory]
    [InlineData("GET", Origin, Path, Parameters, Filter, true)]
    [InlineData("GET", Origin, Path, Parameters, Id + ")&is_deleted=eq.false&order=id.asc&limit=100&offset=1", true)]
    [InlineData("GET", Origin, Path, Parameters, Id + ")&is_deleted=eq.false&order=id.asc&limit=100&offset=100", false)]
    [InlineData("POST", Origin, Path, Parameters, Filter, false)]
    [InlineData("GET", "https://elsewhere.example.test", Path, Parameters, Filter, false)]
    [InlineData("GET", Origin, "/rest/v1/oasis_avatars", Parameters, Filter, false)]
    [InlineData("GET", Origin, Path, "?id=not.in.(", Filter, false)]
    [InlineData("GET", Origin, Path, Parameters, Filter + "||is_deleted=true", false)]
    public async Task GuardForwardsOnlyBoundedCanonicalReads(string method, string origin, string path,
        string parameters, string filter, bool admitted)
    {
        var transport = new ObservedTransport();
        using var client = new HttpClient(new CqlSupabaseReadSourceRegistration.ExactReadHandler(new Uri(Origin), transport));
        using var request = new HttpRequestMessage(new HttpMethod(method), origin + path + parameters + filter);
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
