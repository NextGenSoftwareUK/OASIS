using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData("null",false)] [InlineData("null",true)]
    [InlineData("owner",false)] [InlineData("owner",true)]
    [InlineData("match-mode",false)] [InlineData("match-mode",true)]
    [InlineData("blank-text",false)] [InlineData("blank-text",true)]
    [InlineData("group-operator",false)] [InlineData("group-operator",true)]
    [InlineData("version",false)] [InlineData("version",true)]
    [InlineData("depth",false)] [InlineData("depth",true)]
    [InlineData("unknown-group",false)] [InlineData("unknown-group",true)]
    public async Task InvalidSearchFailsBeforeActivationOrDatabaseCreation(string fault,bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        var text=new SearchTextGroup {SearchQuery="owned validation request",SearchHolons=true};
        var query=new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=new() {text},MetaKeyValuePairMatchMode=MetaKeyValuePairMatchMode.All};
        if(fault=="owner") query.SearchOnlyForCurrentAvatar=true;
        if(fault=="match-mode") query.MetaKeyValuePairMatchMode=(MetaKeyValuePairMatchMode)999;
        if(fault=="blank-text") text.SearchQuery=" ";
        if(fault=="group-operator") text.PreviousSearchGroupOperator=(SearchParamGroupOperator)999;
        if(fault=="unknown-group") query.SearchGroups=new() {null!};
        var version=fault=="version" ? -1:0;var depth=fault=="depth" ? -1:0;
        var result=synchronous ? provider.Search(fault=="null" ? null!:query,maxChildDepth:depth,version:version)
            :await provider.SearchAsync(fault=="null" ? null!:query,maxChildDepth:depth,version:version);
        result.IsError.Should().BeTrue();result.IsLoaded.Should().BeFalse();result.Result.Should().BeNull();
        result.ErrorCode.Should().Be("HYPERDRIVE_PAYLOAD_INVALID");
        var expected=fault switch {"null"=>typeof(ArgumentNullException),"match-mode" or "version" or "depth"=>typeof(ArgumentOutOfRangeException),
            "unknown-group"=>typeof(NotSupportedException),_=>typeof(ArgumentException)};
        result.Exception.Should().BeOfType(expected);
        provider.IsProviderActivated.Should().BeFalse("invalid search must not activate storage");
        File.Exists(_databasePath).Should().BeFalse("invalid search must not create a database");
    }
}
