using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public sealed class CqlPlanetScaleHostedAdmissionTests
{
    [Fact]
    public void AdmissionRequiresAnExplicitQueryOnlyTariff()
    {
        var providers = new[] { ProviderType.PlanetScaleOASIS };
        var bindings = Array.Empty<NativeStorageAdmissionBinding>();
        var tariffs = new[] { new StorageQueryTariff(ProviderType.PlanetScaleOASIS, 0, "demo-read-v1") };
        var factory = new PricedStorageAdmissionFactory(null, bindings,
            Array.Empty<ProviderType>(), tariffs, null, providers);
        Assert.Equal(providers, factory.QueryProviders);
        Assert.Contains(ProviderType.PlanetScaleOASIS, factory.QueryOnlyProviders);
        Assert.Throws<ArgumentException>(() => new PricedStorageAdmissionFactory(null, bindings,
            Array.Empty<ProviderType>(), tariffs, null, Array.Empty<ProviderType>()));
    }

    [Theory]
    [InlineData("VerifyFull", true)]
    [InlineData("Required", false)]
    public void RegistrationRequiresVerifiedTls(string tls, bool accepted)
    {
        var services = new ServiceCollection();
        CqlPlanetScaleReadSourceRegistration.AddPlanetScaleReadSource(services, new ConfigurationBuilder().Build());
        Assert.Empty(services);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["CQL_PLANETSCALE_QUERY_CONNECTION_STRING"] = $"Server=example.invalid;Database=teaching;User ID=reader;SslMode={tls}",
            ["CQL_PLANETSCALE_QUERY_PROBE_ID"] = "11111111-1111-4111-8111-111111111111"
        }).Build();

        if (accepted)
        {
            CqlPlanetScaleReadSourceRegistration.AddPlanetScaleReadSource(services, configuration);
            using var container = services.BuildServiceProvider();
            Assert.False(container.GetRequiredService<CqlReadSourceProfile>().Storage.IsProviderActivated);
        }
        else
        {
            Assert.Throws<InvalidOperationException>(() =>
                CqlPlanetScaleReadSourceRegistration.AddPlanetScaleReadSource(services, configuration));
        }
    }
}
