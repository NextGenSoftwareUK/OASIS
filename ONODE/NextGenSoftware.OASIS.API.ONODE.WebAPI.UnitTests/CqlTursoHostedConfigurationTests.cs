using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlTursoHostedConfigurationTests
{
    private const string Prefix = "CQL_TURSO_QUERY_";

    private static Dictionary<string, string?> Settings() => new()
    {
        [Prefix + "ORIGIN"] = "https://teaching.turso.io/",
        [Prefix + "READER_TOKEN"] = "synthetic-turso-reader-token",
        [Prefix + "PROBE_ID"] = "aaaaaaaa-2222-4222-8222-222222222222"
    };

    [Fact]
    public void RegistersOnlyAnExplicitInactiveTursoProfile()
    {
        var services = new ServiceCollection();
        services.AddTursoReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddTursoReadSource(new ConfigurationBuilder().AddInMemoryCollection(Settings()).Build());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.TursoOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData("ORIGIN", "http://teaching.turso.io/")]
    [InlineData("ORIGIN", "https://user@teaching.turso.io/")]
    [InlineData("ORIGIN", "https://teaching.turso.io/foreign")]
    [InlineData("ORIGIN", "https://127.0.0.1/")]
    [InlineData("READER_TOKEN", null)]
    [InlineData("READER_TOKEN", "invalid token")]
    [InlineData("PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    [InlineData("PROBE_ID", "AAAAAAAA-2222-4222-8222-222222222222")]
    public void RejectsInvalidTursoSettings(string name, string? value)
    {
        var settings = Settings();
        settings[Prefix + name] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            services.AddTursoReadSource(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.Empty(services);
    }
}
