using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using NextGenSoftware.OASIS.API.Providers.ENSOffchainOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlEnsOffchainHostedConfigurationTests
{
    private const string Setting = "CQL_ENS_OFFCHAIN_QUERY_SOURCE_JSON";
    private const string Owner = "a5808c3e-7031-47e7-92de-48a659568f1b";
    private const string Valid = """{"owner":"a5808c3e-7031-47e7-92de-48a659568f1b","names":["vitalik.eth","nick.eth","luc.eth"],"probe":"vitalik.eth"}""";

    [Fact]
    public void RegistersOnlyAnExplicitInactiveIndexProfile()
    {
        var services = new ServiceCollection();
        services.AddEnsOffchainReadSource(new ConfigurationBuilder().Build());
        Assert.Empty(services);
        services.AddEnsOffchainReadSource(Settings(Valid));
        using var container = services.BuildServiceProvider();
        var profile = Assert.Single(container.GetServices<CqlReadSourceProfile>());
        Assert.Equal(ProviderType.ENSOffchainOASIS, profile.Storage.ProviderType.Value);
        Assert.False(profile.Storage.IsProviderActivated);
    }

    [Theory]
    [InlineData("")]
    [InlineData("""{"owner":"a5808c3e-7031-47e7-92de-48a659568f1b","names":["vitalik.eth","vitalik.eth"],"probe":"vitalik.eth"}""")]
    [InlineData("""{"owner":"a5808c3e-7031-47e7-92de-48a659568f1b","names":["vitalik.eth"],"probe":"other.eth"}""")]
    [InlineData("""{"owner":"a5808c3e-7031-47e7-92de-48a659568f1b","names":["VITALIK.eth"],"probe":"VITALIK.eth"}""")]
    [InlineData("""{"owner":"a5808c3e-7031-47e7-92de-48a659568f1b","names":["vitalik.eth"],"probe":"vitalik.eth","endpoint":"https://foreign.example/"}""")]
    [InlineData("""{"owner":"a5808c3e-7031-47e7-92de-48a659568f1b","names":["vitalik.eth"],"probe":"vitalik.eth","probe":"vitalik.eth"}""")]
    public void RejectsMalformedCatalogue(string value)
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() => services.AddEnsOffchainReadSource(Settings(value)));
        Assert.Empty(services);
    }

    [Fact]
    public void RejectsEmptyOwner()
    {
        var services = new ServiceCollection();
        Assert.Throws<InvalidOperationException>(() =>
            services.AddEnsOffchainReadSource(Settings(Valid.Replace(Owner, Guid.Empty.ToString("D")))));
    }

    [Fact]
    public void ReviewedPublisherIdentityMatchesNativeMapper()
    {
        Assert.Equal(Guid.Parse("2d8b5e78-3137-8c0d-ba70-bdaa880dcdad"),
            EnsOffchainQuerySource.LogicalId(Guid.Parse(Owner), "vitalik.eth"));
    }

    private static IConfiguration Settings(string value) =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { [Setting] = value }).Build();
}
