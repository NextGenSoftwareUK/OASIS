using System.Reflection;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;
using NextGenSoftware.OASIS.OASISBootLoader;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public class CosmosProviderConfigurationTests
{
    [Theory]
    [InlineData(null)] [InlineData("9007199254740993")]
    public void DnaExplicitCeilingsAreCapturedWithoutActivationOrDefaultGuessing(string amount)
    {
        var settings=new CosmosBlockChainOASISProviderSettings {RpcEndpoint="http://127.0.0.1:1",MaximumFeeUnitsByDenomination=amount==null?null:new(){{"stake",amount}}};
        using var provider=CosmosProviderConfiguration.Create(settings);
        if(amount!=null)settings.MaximumFeeUnitsByDenomination["stake"]="1";
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;var backend=typeof(CosmosBlockChainOASIS).GetField("_sdkBackend",flags)!.GetValue(provider)!;
        var selected=(IReadOnlyDictionary<string,string>?)backend.GetType().GetField("_maximumFeeUnitsByDenomination",flags)!.GetValue(backend);
        if(amount==null)Assert.Null(selected);else Assert.Equal(amount,selected!["stake"]);
        Assert.False(provider.IsProviderActivated);
    }
    [Theory]
    [InlineData("")] [InlineData("0")] [InlineData("-1")] [InlineData("1.0")]
    public void InvalidExplicitMapRejectsBeforeRpcConstruction(string amount)
    {
        var settings=new CosmosBlockChainOASISProviderSettings {RpcEndpoint="invalid",MaximumFeeUnitsByDenomination=new(){{"stake",amount}}};
        Assert.Contains("Cosmos",Assert.Throws<ArgumentException>(()=>CosmosProviderConfiguration.Create(settings)).Message);
    }
    [Fact]
    public void DnaRoundTripKeepsLargeIntegerTextAndAbsenceIsUnconfigured()
    {
        var settings=new CosmosBlockChainOASISProviderSettings {MaximumFeeUnitsByDenomination=new(){{"stake","9007199254740993"}}};
        var json=JsonConvert.SerializeObject(settings);Assert.Contains("\"stake\":\"9007199254740993\"",json);
        Assert.Equal("9007199254740993",JsonConvert.DeserializeObject<CosmosBlockChainOASISProviderSettings>(json)!.MaximumFeeUnitsByDenomination["stake"]);
        Assert.Null(JsonConvert.DeserializeObject<CosmosBlockChainOASISProviderSettings>("{}")!.MaximumFeeUnitsByDenomination);
    }
}
