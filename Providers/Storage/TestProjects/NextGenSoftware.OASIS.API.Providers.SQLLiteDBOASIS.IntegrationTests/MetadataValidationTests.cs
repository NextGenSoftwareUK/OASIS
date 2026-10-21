using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;
public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false,MetaKeyValuePairMatchMode.All)] [InlineData(true,MetaKeyValuePairMatchMode.All)]
    [InlineData(false,MetaKeyValuePairMatchMode.Any)] [InlineData(true,MetaKeyValuePairMatchMode.Any)]
    public async Task MetadataQueriesShareInvariantSearchComparisonAndMaterializeSelection(bool synchronous,MetaKeyValuePairMatchMode mode)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var source=new Core.Holons.Holon{Name="metadata-policy",MetaData=new(){{"score",12.5m}}};
        (await provider.SaveHolonAsync(source)).IsError.Should().BeFalse();
        var previous=System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture=System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
            foreach(var empty in new[]{false,true})
            {
                var filter=empty ? new Dictionary<string,string>():new Dictionary<string,string>{{"score","12.5"}};
                var result=synchronous ? provider.LoadHolonsByMetaData(filter,mode,loadChildren:false)
                    :await provider.LoadHolonsByMetaDataAsync(filter,mode,loadChildren:false);
                result.IsError.Should().BeFalse(result.Message+result.Exception);result.IsLoaded.Should().BeTrue();
                filter["score"]="does-not-match";
                result.Result.Should().ContainSingle().Which.Id.Should().Be(source.Id);
            }
        }
        finally{System.Globalization.CultureInfo.CurrentCulture=previous;}
    }

    [Theory]
    [InlineData("key",false)] [InlineData("key",true)]
    [InlineData("filter",false)] [InlineData("filter",true)]
    [InlineData("mode",false)] [InlineData("mode",true)]
    [InlineData("version",false)] [InlineData("version",true)]
    [InlineData("depth",false)] [InlineData("depth",true)]
    [InlineData("current-depth",false)] [InlineData("current-depth",true)]
    public async Task InvalidMetadataQueryFailsBeforeActivation(string fault,bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        var filter=fault=="filter" ? null:new Dictionary<string,string>{{"tag","value"}};
        var mode=fault=="mode" ? (MetaKeyValuePairMatchMode)999:MetaKeyValuePairMatchMode.All;
        var version=fault=="version" ? -1:0;var depth=fault=="depth" ? -1:0;var initial=fault=="current-depth" ? -1:0;
        var result=fault=="key"
            ? synchronous ? provider.LoadHolonsByMetaData(null!,"value"):await provider.LoadHolonsByMetaDataAsync(null!,"value")
            : synchronous ? provider.LoadHolonsByMetaData(filter!,mode,version:version,maxChildDepth:depth,curentChildDepth:initial)
                :await provider.LoadHolonsByMetaDataAsync(filter!,mode,version:version,maxChildDepth:depth,curentChildDepth:initial);
        result.IsError.Should().BeTrue();result.Result.Should().BeNull();result.IsLoaded.Should().BeFalse();
        result.ErrorCode.Should().Be("HYPERDRIVE_PAYLOAD_INVALID");
        result.Exception.Should().BeAssignableTo<ArgumentException>();
        provider.IsProviderActivated.Should().BeFalse();File.Exists(_databasePath).Should().BeFalse();
    }
}
