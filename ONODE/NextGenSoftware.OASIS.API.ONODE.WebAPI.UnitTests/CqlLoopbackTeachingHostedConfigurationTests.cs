using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlLoopbackTeachingHostedConfigurationTests
{
    private const string Probe = "aaaaaaaa-2222-4222-8222-222222222222";
    private static Dictionary<string, string?> Settings(string provider, string port) => new()
    {
        [$"CQL_{provider}_QUERY_SIDECAR"] = $"http://127.0.0.1:{port}/",
        [$"CQL_{provider}_QUERY_READER_TOKEN"] = "synthetic-teaching-reader-123456789",
        [$"CQL_{provider}_QUERY_PROBE_ID"] = Probe,
    };

    [Fact]
    public void RegistersOnlyConfiguredLoopbackTeachingProfiles()
    {
        var services = new ServiceCollection();
        services.AddLoopbackTeachingSources(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        var settings = Settings("ORBITDB", "54869").Concat(Settings("GUN", "54870"));
        services.AddLoopbackTeachingSources(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        using var container = services.BuildServiceProvider();
        var profiles = container.GetServices<CqlReadSourceProfile>().ToArray();
        Assert.Equal(new[] { ProviderType.OrbitDBOASIS, ProviderType.GUNOASIS }, profiles.Select(profile => profile.Storage.ProviderType.Value));
        Assert.All(profiles, profile => Assert.False(profile.Storage.IsProviderActivated));
    }

    [Theory]
    [InlineData("ORBITDB", "SIDECAR", "http://localhost:54869/")]
    [InlineData("ORBITDB", "SIDECAR", "http://127.1:54869/")]
    [InlineData("ORBITDB", "SIDECAR", "http://127.0.0.1:54869/foo")]
    [InlineData("GUN", "SIDECAR", "http://remote.example:54870/")]
    [InlineData("GUN", "READER_TOKEN", null)]
    [InlineData("GUN", "READER_TOKEN", "invalid token")]
    [InlineData("GUN", "PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    [InlineData("ORBITDB", "PROBE_ID", "AAAAAAAA-2222-4222-8222-222222222222")]
    public void RejectsInvalidLoopbackTeachingSettings(string provider, string name, string? value)
    {
        var settings = Settings(provider, "54869");
        settings[$"CQL_{provider}_QUERY_{name}"] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddLoopbackTeachingSources(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.Empty(services);
    }
}
