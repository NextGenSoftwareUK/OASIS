using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlCloudinaryAndPgVectorHostedConfigurationTests
{
    private const string CloudinarySetting = "CQL_CLOUDINARY_QUERY_SOURCE_JSON";
    private const string CloudinaryCatalogue = """
        {"owner":"a5808c3e-7031-47e7-92de-48a659568f1b","assets":["sample.jpg","lady.jpg","woman.jpg"],"probe":"sample.jpg"}
        """;

    private static Dictionary<string, string?> PgVectorSettings() => new()
    {
        ["CQL_PGVECTOR_QUERY_HOST"] = "teaching.pgvector.example",
        ["CQL_PGVECTOR_QUERY_DATABASE"] = "oasis_teaching",
        ["CQL_PGVECTOR_QUERY_READER_USERNAME"] = "cosmic_reader",
        ["CQL_PGVECTOR_QUERY_READER_PASSWORD"] = "synthetic-protected-reader",
        ["CQL_PGVECTOR_QUERY_PROBE_ID"] = "aaaaaaaa-2222-4222-8222-222222222222",
    };

    [Fact]
    public void ExplicitSettingsRegisterIndependentReadProfiles()
    {
        var services = new ServiceCollection();
        var absent = new ConfigurationBuilder().Build();
        services.AddCloudinaryReadSource(absent);
        services.AddPgVectorReadSource(absent);
        Assert.Empty(services);
        var settings = PgVectorSettings();
        settings[CloudinarySetting] = CloudinaryCatalogue;
        var configured = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        services.AddCloudinaryReadSource(configured);
        services.AddPgVectorReadSource(configured);
        using var container = services.BuildServiceProvider();
        var profiles = container.GetServices<CqlReadSourceProfile>().ToArray();
        Assert.Equal(new[] { ProviderType.CloudinaryOASIS, ProviderType.PgVectorOASIS },
            profiles.Select(profile => profile.Storage.ProviderType.Value));
        Assert.All(profiles, profile => Assert.False(profile.Storage.IsProviderActivated));
    }

    [Theory]
    [InlineData("lady.jpg", "sample.jpg")]
    [InlineData("woman.jpg", "../woman.jpg")]
    [InlineData("\"probe\":\"sample.jpg\"", "\"probe\":\"foreign.jpg\"")]
    [InlineData("\"probe\":\"sample.jpg\"", "\"probe\":\"sample.jpg\",\"probe\":\"woman.jpg\"")]
    public void RejectsUnreviewedCloudinaryCatalogue(string expected, string changed)
    {
        var services = new ServiceCollection();
        var settings = new Dictionary<string, string?> { [CloudinarySetting] = CloudinaryCatalogue.Replace(expected, changed) };
        Assert.Throws<InvalidOperationException>(() => services.AddCloudinaryReadSource(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.Empty(services);
    }

    [Theory]
    [InlineData("HOST", "127.0.0.1")]
    [InlineData("HOST", "teaching.pgvector.example.")]
    [InlineData("HOST", "teaching.pgvector.example,foreign.example")]
    [InlineData("DATABASE", "oasis;Host=foreign")]
    [InlineData("READER_USERNAME", "root user")]
    [InlineData("READER_PASSWORD", null)]
    [InlineData("PROBE_ID", "00000000-0000-0000-0000-000000000000")]
    public void RejectsInvalidPgVectorSettings(string key, string? value)
    {
        var services = new ServiceCollection();
        var settings = PgVectorSettings();
        settings["CQL_PGVECTOR_QUERY_" + key] = value;
        Assert.Throws<InvalidOperationException>(() => services.AddPgVectorReadSource(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.Empty(services);
    }
}
