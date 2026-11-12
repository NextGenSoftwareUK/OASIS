using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlPublicReadCatalogueTests
{
    [Fact]
    public void EverySupportedReadProviderCanBeEnrolledTogether()
    {
        var providers = Enum.GetValues<ProviderType>().Distinct()
            .Where(CqlV1Compiler.SupportedReadProfile).ToArray();
        Assert.True(providers.Length > 16);
        var json = JsonSerializer.Serialize(providers.ToDictionary(
            provider => provider.ToString(), _ => new[] { Guid.NewGuid() }));
        var services = new ServiceCollection();
        CqlPublicReadCatalogue.Enroll(services, Configuration(json));
        using var container = services.BuildServiceProvider();
        foreach (var provider in providers)
            Assert.Equal(provider, Assert.IsType<CqlPublicReadCatalogue>(
                container.GetRequiredKeyedService<ICqlReadAccessPolicy>(provider)).Provider);
    }

    [Theory]
    [InlineData("{\"WakuOASIS\":[],\"WakuOASIS\":[]}")]
    [InlineData("{\"UnknownOASIS\":[]}")]
    [InlineData("{\"WakuOASIS\":[\"00000000-0000-0000-0000-000000000000\"]}")]
    public void InvalidConfigurationAddsNoPolicies(string json)
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentException>(() => CqlPublicReadCatalogue.Enroll(services, Configuration(json)));
        Assert.Empty(services);
    }

    [Fact]
    public void ConfigurationByteBudgetStillApplies()
    {
        var services = new ServiceCollection();
        Assert.Throws<ArgumentException>(() => CqlPublicReadCatalogue.Enroll(
            services, Configuration(new string(' ', 32769) + "{}")));
        Assert.Empty(services);
    }

    private static IConfiguration Configuration(string json) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
            { [CqlPublicReadCatalogue.ConfigurationKey] = json }).Build();
}
