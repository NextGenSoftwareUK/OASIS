using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlMilvusHostedConfigurationTests
{
    private static Dictionary<string, string?> Settings() => new()
    {
        ["CQL_MILVUS_QUERY_ORIGIN"] = "https://milvus.example.com",
        ["CQL_MILVUS_QUERY_READER_TOKEN"] = "restricted-reader",
        ["CQL_MILVUS_QUERY_PROBE_ID"] = "aaaaaaaa-2222-4222-8222-222222222222",
    };

    [Fact]
    public void RegistersOnlyExplicitlyConfiguredProfile()
    {
        var services = new ServiceCollection();
        services.AddMilvusReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddMilvusReadSource(new ConfigurationBuilder().AddInMemoryCollection(Settings()).Build());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.MilvusOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData("ORIGIN", "http://milvus.example.com")]
    [InlineData("ORIGIN", "https://127.0.0.1")]
    [InlineData("ORIGIN", "https://milvus.example.com.")]
    [InlineData("ORIGIN", "https://milvus.example.com:443")]
    [InlineData("ORIGIN", "https://milvus.example.com/")]
    [InlineData("ORIGIN", "https://milvus.example.com/#secret")]
    [InlineData("READER_TOKEN", "invalid token")]
    [InlineData("READER_TOKEN", null)]
    [InlineData("PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    public void RejectsInvalidProtectedSettings(string key, string? value)
    {
        var settings = Settings();
        settings["CQL_MILVUS_QUERY_" + key] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddMilvusReadSource(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.Empty(services);
    }
}
