using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlRegistryAndObjectHostedConfigurationTests
{
    private const string RegistrySetting = "CQL_PROOF_OF_HUMANITY_QUERY_SOURCE_JSON";
    private const string ObjectSetting = "CQL_ORACLE_OBJECT_STORAGE_QUERY_SOURCE_JSON";
    private const string Registry = """
        {"owner":"a5808c3e-7031-47e7-92de-48a659568f1b","addresses":["0x16044e1063c08670f8653055a786b7cc2034d2b0","0xa478095886659168e8812154fb0de39f103e74b2"],"probe":"0x16044e1063c08670f8653055a786b7cc2034d2b0"}
        """;
    private const string Object = """
        {"owner":"a5808c3e-7031-47e7-92de-48a659568f1b","keys":["bds-livelabs/env.sh","bds-livelabs/download-all-objstore.sh"],"probe":"bds-livelabs/env.sh"}
        """;

    [Fact]
    public void ExplicitSettingsRegisterBothSourcesWithoutActivatingThem()
    {
        var services = new ServiceCollection();
        services.AddRegistryAndObjectReadSources(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        var settings = new Dictionary<string, string?> { [RegistrySetting] = Registry, [ObjectSetting] = Object };
        services.AddRegistryAndObjectReadSources(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        using var container = services.BuildServiceProvider();
        var profiles = container.GetServices<CqlReadSourceProfile>().ToArray();
        Assert.Equal(new[] { ProviderType.ProofOfHumanityOASIS, ProviderType.OracleObjectStorageOASIS },
            profiles.Select(profile => profile.Storage.ProviderType.Value));
        Assert.All(profiles, profile => Assert.False(profile.Storage.IsProviderActivated));
    }

    [Theory]
    [InlineData(RegistrySetting, "0xa478", "0xA478")]
    [InlineData(RegistrySetting, "0xa478095886659168e8812154fb0de39f103e74b2", "0x16044e1063c08670f8653055a786b7cc2034d2b0")]
    [InlineData(RegistrySetting, "\"addresses\"", "\"keys\"")]
    [InlineData(ObjectSetting, "bds-livelabs/env.sh", "bds-livelabs/../env.sh")]
    [InlineData(ObjectSetting, "\"probe\":\"bds-livelabs/env.sh\"", "\"probe\":\"bds-livelabs/other.sh\"")]
    [InlineData(ObjectSetting, "\"probe\"", "\"endpoint\":\"https://foreign.example\",\"probe\"")]
    public void InvalidSettingsFailClosed(string key, string before, string after)
    {
        var services = new ServiceCollection();
        var original = key == RegistrySetting ? Registry : Object;
        var settings = new Dictionary<string, string?> { [key] = original.Replace(before, after) };
        Assert.Throws<InvalidOperationException>(() => services.AddRegistryAndObjectReadSources(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build()));
        Assert.Empty(services);
    }
}
