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
    public void EnrollmentIsOptInAndBothSourcesRetainTheirOwnAuthority()
    {
        var services = new ServiceCollection();
        services.AddRedisFamilyReadSources(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddRedisFamilyReadSources(new ConfigurationBuilder()
            .AddInMemoryCollection(Settings("REDIS").Concat(Settings("VALKEY"))).Build());
        using var container = services.BuildServiceProvider();
        var profiles = container.GetServices<CqlReadSourceProfile>().ToArray();
        Assert.Equal(new[] { ProviderType.RedisOASIS, ProviderType.ValKeyOASIS },
            profiles.Select(profile => profile.Storage.ProviderType.Value).ToArray());
        Assert.All(profiles, profile => Assert.False(profile.Storage.IsProviderActivated));
    }

    [Theory]
    [InlineData("PASSWORD", null)]
    [InlineData("ADDRESS", "127.0.0.1")]
    [InlineData("ADDRESS", "::ffff:127.0.0.1")]
    [InlineData("ADDRESS", "redis.example.test")]
    [InlineData("PORT", "65536")]
    [InlineData("TLS_HOST", "https://redis.example.test")]
    [InlineData("USERNAME", "invalid\nuser")]
    [InlineData("STORAGE_KEY", "invalid")]
    [InlineData("PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    public void InvalidConfigurationRefusesEnrollment(string field, string? value)
    {
        var settings = Settings("VALKEY");
        settings["CQL_VALKEY_QUERY_" + field] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddRedisFamilyReadSources(
            new ConfigurationBuilder().AddInMemoryCollection(Settings("REDIS").Concat(settings)).Build()));
        Assert.Empty(services);
    }
}
