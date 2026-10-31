using System.Reflection;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SuiOASIS;
using NextGenSoftware.OASIS.OASISBootLoader;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public class SuiProviderConfigurationTests
{
    [Theory]
    [InlineData("")] [InlineData("0")] [InlineData("01")] [InlineData("-1")] [InlineData("+1")]
    [InlineData(" 1")] [InlineData("1.0")] [InlineData("1e3")] [InlineData("18446744073709551616")]
    public void InvalidExplicitCeilingRejectsBeforeProviderConstruction(string value)
    {
        var settings=new SuiOASISProviderSettings {MaximumGasBudgetMist=value,RpcEndpoint="invalid-before-construction"};
        Assert.Contains("MaximumGasBudgetMist",Assert.Throws<ArgumentException>(()=>SuiProviderConfiguration.Create(settings)).Message);
    }
    [Theory]
    [InlineData(null)] [InlineData("1")] [InlineData("18446744073709551615")]
    public void ProviderCapturesOnlyTheExplicitDnaCeilingWithoutActivating(string value)
    {
        var settings=new SuiOASISProviderSettings {MaximumGasBudgetMist=value,RpcEndpoint="http://127.0.0.1:1",Network="localnet"};
        using var provider=SuiProviderConfiguration.Create(settings);
        settings.MaximumGasBudgetMist="999";
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        var client=typeof(SuiOASIS).GetField("_sdkClient",flags)!.GetValue(provider)!;
        Assert.Equal(value,client.GetType().GetField("_maximumGasBudgetMist",flags)!.GetValue(client));
        Assert.False(provider.IsProviderActivated);
    }
    [Fact]
    public void JsonRoundTripRetainsU64PrecisionAsTextAndAbsenceHasNoDefault()
    {
        const string maximum="18446744073709551615";
        var serialized=JsonConvert.SerializeObject(new SuiOASISProviderSettings {MaximumGasBudgetMist=maximum});
        Assert.Contains("\"MaximumGasBudgetMist\":\""+maximum+"\"",serialized);
        Assert.Equal(maximum,JsonConvert.DeserializeObject<SuiOASISProviderSettings>(serialized)!.MaximumGasBudgetMist);
        Assert.Null(JsonConvert.DeserializeObject<SuiOASISProviderSettings>("{}")!.MaximumGasBudgetMist);
    }
}
