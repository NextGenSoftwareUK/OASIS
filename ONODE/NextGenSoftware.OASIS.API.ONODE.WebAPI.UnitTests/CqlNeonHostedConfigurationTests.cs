using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlNeonHostedConfigurationTests
{
    private const string Prefix = "CQL_NEON_QUERY_";
    private const string Host = "ep-demo.us-east-2.aws.neon.tech";

    private static Dictionary<string, string?> Settings() => new()
    {
        [Prefix + "HOST"] = Host,
        [Prefix + "READER_PASSWORD"] = "controlled",
        [Prefix + "PROBE_ID"] = "aaaaaaaa-1111-4111-8111-111111111111"
    };

    [Fact]
    public void RegistersOnlyAnExplicitInactiveNeonSource()
    {
        var services = new ServiceCollection();
        services.AddNeonReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddNeonReadSource(new ConfigurationBuilder().AddInMemoryCollection(Settings()).Build());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.NeonOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData("HOST", "ep-demo-pooler.us-east-2.aws.neon.tech")]
    [InlineData("HOST", "pg.neon.tech")]
    [InlineData("HOST", "ep-demo.neon.tech.invalid")]
    [InlineData("HOST", "ep-demo..neon.tech")]
    [InlineData("HOST", "localhost")]
    [InlineData("READER_PASSWORD", "")]
    [InlineData("PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    public void RejectsInvalidNeonSettings(string name, string value)
    {
        var settings = Settings();
        settings[Prefix + name] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            services.AddNeonReadSource(new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.Empty(services);
    }
}
