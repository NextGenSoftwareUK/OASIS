using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlRedisFamilyHostedConfigurationTests
{
    private static Dictionary<string, string?> Settings(string name) => new()
    {
        [$"CQL_{name}_QUERY_ADDRESS"] = "1.1.1.1",
        [$"CQL_{name}_QUERY_PORT"] = "6380",
        [$"CQL_{name}_QUERY_TLS_HOST"] = $"{name.ToLowerInvariant()}.example.test",
        [$"CQL_{name}_QUERY_USERNAME"] = "teaching-reader",
        [$"CQL_{name}_QUERY_PASSWORD"] = "test, value=only",
        [$"CQL_{name}_QUERY_STORAGE_KEY"] = Convert.ToBase64String(new byte[32]),
        [$"CQL_{name}_QUERY_PROBE_ID"] = "11111111-1111-4111-8111-111111111111"
    };

    [Fact]
    public void EnrollmentIsOptInAndEachSourceRetainsItsOwnAuthority()
    {
        var services = new ServiceCollection();
        services.AddRedisFamilyReadSources(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddRedisFamilyReadSources(new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { "REDIS", "VALKEY", "GARNET", "KEYDB" }.SelectMany(Settings)).Build());
        using var container = services.BuildServiceProvider();
        var profiles = container.GetServices<CqlReadSourceProfile>().ToArray();
        Assert.Equal(new[] { ProviderType.RedisOASIS, ProviderType.ValKeyOASIS, ProviderType.GarnetOASIS, ProviderType.KeyDBOASIS },
            profiles.Select(profile => profile.Storage.ProviderType.Value).ToArray());
        Assert.All(profiles, profile => Assert.False(profile.Storage.IsProviderActivated));
    }

    [Fact]
    public void EndpointChangesAreRejectedBeforeSocketConnect()
    {
        var address = IPAddress.Parse("1.1.1.1");
        var pinned = new IPEndPoint(address, 6380);
        CqlRedisFamilyReadSources.RequirePinnedEndpoint(pinned, new IPEndPoint(address, 6380));
        Assert.Throws<InvalidOperationException>(() =>
            CqlRedisFamilyReadSources.RequirePinnedEndpoint(pinned, new IPEndPoint(address, 6381)));
        Assert.Throws<InvalidOperationException>(() =>
            CqlRedisFamilyReadSources.RequirePinnedEndpoint(pinned, new IPEndPoint(IPAddress.Loopback, 6380)));
        Assert.Throws<InvalidOperationException>(() =>
            CqlRedisFamilyReadSources.RequirePinnedEndpoint(pinned, new DnsEndPoint("redis.example.test", 6380)));
    }

    [Theory]
    [InlineData("GARNET", "PASSWORD", null)]
    [InlineData("KEYDB", "ADDRESS", "127.0.0.1")]
    [InlineData("VALKEY", "PASSWORD", null)]
    [InlineData("VALKEY", "ADDRESS", "127.0.0.1")]
    [InlineData("VALKEY", "ADDRESS", "::ffff:127.0.0.1")]
    [InlineData("VALKEY", "ADDRESS", "redis.example.test")]
    [InlineData("VALKEY", "PORT", "65536")]
    [InlineData("VALKEY", "TLS_HOST", "https://redis.example.test")]
    [InlineData("VALKEY", "USERNAME", "invalid\nuser")]
    [InlineData("VALKEY", "STORAGE_KEY", "invalid")]
    [InlineData("VALKEY", "PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    public void InvalidConfigurationRefusesEnrollment(string product, string field, string? value)
    {
        var settings = Settings(product);
        settings[$"CQL_{product}_QUERY_" + field] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddRedisFamilyReadSources(
            new ConfigurationBuilder().AddInMemoryCollection(Settings("REDIS").Concat(settings)).Build()));
        Assert.Empty(services);
    }
}
