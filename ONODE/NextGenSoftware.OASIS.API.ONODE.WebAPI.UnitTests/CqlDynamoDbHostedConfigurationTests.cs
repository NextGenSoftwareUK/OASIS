using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlDynamoDbHostedConfigurationTests
{
    private static Dictionary<string, string?> Settings() => new()
    {
        ["CQL_DYNAMODB_QUERY_REGION"] = "us-east-1",
        ["CQL_DYNAMODB_QUERY_ACCESS_KEY_ID"] = "controlled-access",
        ["CQL_DYNAMODB_QUERY_SECRET_ACCESS_KEY"] = "controlled-secret",
        ["CQL_DYNAMODB_QUERY_SESSION_TOKEN"] = "controlled-token",
        ["CQL_DYNAMODB_QUERY_PROBE_ID"] = "11111111-1111-4111-8111-111111111111"
    };

    [Fact]
    public void EnrollmentUsesOnlyTheExplicitDynamoDbAuthority()
    {
        var services = new ServiceCollection();
        services.AddDynamoDbReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddDynamoDbReadSource(new ConfigurationBuilder().AddInMemoryCollection(Settings()).Build());
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.DynamoDBOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData("REGION", "https://unexpected.example.test")]
    [InlineData("ACCESS_KEY_ID", null)]
    [InlineData("SECRET_ACCESS_KEY", "invalid secret")]
    [InlineData("SESSION_TOKEN", "")]
    [InlineData("PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    public void PartialOrInvalidConfigurationRefusesEnrollment(string field, string? value)
    {
        var settings = Settings();
        settings["CQL_DYNAMODB_QUERY_" + field] = value;
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddDynamoDbReadSource(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.Empty(services);
    }
}
