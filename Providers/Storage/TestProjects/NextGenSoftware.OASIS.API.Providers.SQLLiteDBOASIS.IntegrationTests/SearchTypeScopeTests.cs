using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.IntegrationTests;

public partial class SQLLiteDBOASISIntegrationTests
{
    [Theory]
    [InlineData(false,false)] [InlineData(false,true)]
    [InlineData(true,false)] [InlineData(true,true)]
    public async Task NumericAndDateSearchHonorEachGroupsHolonType(bool date,bool synchronous)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString);
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var created=new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc);
        var park=new Holon {Id=Guid.NewGuid(),Version=7,CreatedDate=created,HolonType=HolonType.Park,Name="owned type-scoped park"};
        var quest=new Holon {Id=Guid.NewGuid(),Version=7,CreatedDate=created,HolonType=HolonType.Quest,Name="owned type-scoped quest"};
        foreach(var row in new[] {park,quest})
            (await provider.SaveHolonAsync(row,saveChildren:false)).IsError.Should().BeFalse();
        foreach(var type in new[] {HolonType.Park,HolonType.Quest,HolonType.All})
        {
            ISearchGroupBase group=date
                ? new SearchDateGroup {SearchHolons=true,HolonType=type,Date=new DateTime(2000,1,1,0,0,0,DateTimeKind.Utc),
                    DateOperator=SearchOperatorType.GreaterThan,HolonSearchParams=new SearchHolonParams {CreatedDate=true}}
                : new SearchNumberGroup {SearchHolons=true,HolonType=type,Number=7,NumberOperator=SearchOperatorType.EqualTo,
                    HolonSearchParams=new SearchHolonParams {Version=true}};
            var query=new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=new() {group}};
            var result=synchronous ? provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
            result.IsError.Should().BeFalse(result.Message);
            var expected=type==HolonType.All ? new[] {park.Id,quest.Id}:new[] {type==HolonType.Park ? park.Id:quest.Id};
            result.Result.SearchResultHolons.Select(row=>row.Id).Should().BeEquivalentTo(expected);
            result.Result.SearchResultAvatars.Should().BeEmpty();
            result.Result.NumberOfResults.Should().Be(expected.Length);
        }
    }
}
