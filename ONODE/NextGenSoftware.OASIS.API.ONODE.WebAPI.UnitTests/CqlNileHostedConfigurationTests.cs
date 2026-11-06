using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlNileHostedConfigurationTests
{
    private const string Prefix = "CQL_NILE_QUERY_";

    private static Dictionary<string, string?> Settings(string host = "us-west-2.db.thenile.dev") => new()
    {
        [Prefix + "HOST"] = host,
        [Prefix + "DATABASE"] = "oasis_teaching",
        [Prefix + "READER_USERNAME"] = "aaaaaaaa-2222-4222-8222-222222222222",
        [Prefix + "READER_PASSWORD"] = "controlled",
        [Prefix + "PROBE_ID"] = "aaaaaaaa-1111-4111-8111-111111111111"
    };

    [Theory]
    [InlineData("db.thenile.dev")]
    [InlineData("us-west-2.db.thenile.dev")]
    public void RegistersOnlyAnExplicitInactiveNileProfile(string host)
    {
        var services = new ServiceCollection();
        services.AddNileReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddNileReadSource(new ConfigurationBuilder().AddInMemoryCollection(Settings(host)).Build());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.NileOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData("HOST", "api.thenile.dev")]
    [InlineData("HOST", "db.thenile.dev.invalid")]
    [InlineData("DATABASE", "oasis;search path=foreign")]
    [InlineData("READER_USERNAME", null)]
    [InlineData("READER_USERNAME", "reader@foreign")]
    [InlineData("READER_PASSWORD", "")]
    [InlineData("PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    public void RejectsInvalidNileSettings(string name, string? value)
    {
        var settings = Settings();
        settings[Prefix + name] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            services.AddNileReadSource(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.Empty(services);
    }
}
