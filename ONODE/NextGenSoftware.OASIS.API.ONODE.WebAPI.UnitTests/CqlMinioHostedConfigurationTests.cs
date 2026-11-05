using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlMinioHostedConfigurationTests
{
    private static Dictionary<string, string?> Settings() => new() {
        ["CQL_MINIO_QUERY_ORIGIN"] = "https://minio.example",
        ["CQL_MINIO_QUERY_BUCKET"] = "owned-teaching",
        ["CQL_MINIO_QUERY_REGION"] = "us-east-1",
        ["CQL_MINIO_QUERY_PROBE_ID"] = Guid.NewGuid().ToString("D"),
        ["CQL_MINIO_QUERY_ACCESS_KEY"] = "controlled",
        ["CQL_MINIO_QUERY_SECRET_KEY"] = "controlled"
    };
    private static IConfiguration Config(Dictionary<string, string?> values)
        => new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void AbsentConfigurationDoesNotEnrollMinio()
    {
        var services = new ServiceCollection();
        services.AddMinioReadSource(Config(new()));
        Assert.Empty(services);
    }

    [Theory]
    [InlineData("ORIGIN", null)]
    [InlineData("ACCESS_KEY", "")]
    [InlineData("SECRET_KEY", " ")]
    [InlineData("REGION", "")]
    [InlineData("ORIGIN", "http://minio.example")]
    [InlineData("ORIGIN", "https://localhost")]
    [InlineData("ORIGIN", "https://minio.example/bucket")]
    [InlineData("ORIGIN", "https://user:secret@minio.example")]
    [InlineData("ORIGIN", "https://minio.example?x=1")]
    [InlineData("ORIGIN", "https://minio.example#fragment")]
    [InlineData("BUCKET", "../foreign")]
    [InlineData("PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    [InlineData("PROBE_ID", "invalid")]
    [InlineData("REGION", "region/path")]
    public void IncompleteOrInvalidConfigurationFailsBeforeServiceCreation(string setting, string? value)
    {
        var values = Settings(); values["CQL_MINIO_QUERY_" + setting] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddMinioReadSource(Config(values)));
        Assert.Empty(services);
    }

    [Fact]
    public void CompleteConfigurationConstructsOneInactiveReadOnlyProfileWithoutNetwork()
    {
        var services = new ServiceCollection(); services.AddMinioReadSource(Config(Settings()));
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.MinIOOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
        Assert.Same(profile, container.GetRequiredService<CqlReadSourceProfile>());
    }
}
