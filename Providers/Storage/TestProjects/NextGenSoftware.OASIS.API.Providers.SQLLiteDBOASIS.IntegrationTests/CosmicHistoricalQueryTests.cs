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
    [InlineData(false,0,1)] [InlineData(false,0,2)] [InlineData(true,0,1)] [InlineData(true,0,2)]
    [InlineData(false,1,1)] [InlineData(false,1,2)] [InlineData(true,1,1)] [InlineData(true,1,2)]
    [InlineData(false,2,1)] [InlineData(false,2,2)] [InlineData(true,2,1)] [InlineData(true,2,2)]
    [InlineData(false,3,1)] [InlineData(false,3,2)] [InlineData(true,3,1)] [InlineData(true,3,2)]
    public async Task HistoricalQueriesFilterArchivedValuesAndLoadCurrentDescendants(bool synchronous,int selector,int depth)
    {
        using var provider=new SQLLiteDBOASIS(TestConnectionString){IsVersionControlEnabled=true};
        (await provider.ActivateProviderAsync()).IsError.Should().BeFalse();
        var root=new Holon{Name="historical-query-old",Version=1,HolonType=HolonType.Planet,
            MetaData=new(){{"tag","old"},{"category","old"}}};
        var child=new Holon{Name="child-old",Version=1,HolonType=HolonType.Moon};
        var grand=new Holon{Name="grand-old",Version=1,HolonType=HolonType.Star};
        root.Children.Add(child);child.Children.Add(grand);
        (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        foreach(var value in new[]{root,child,grand}){value.Version=2;value.Name=value.Name.Replace("old","new");}
        root.MetaData["tag"]="new";root.MetaData.Remove("category");
        (await provider.SaveHolonAsync(root)).IsError.Should().BeFalse();
        IEnumerable<IHolon> selected;
        if(selector==3)
        {
            var query=new SearchParams{SearchOnlyForCurrentAvatar=false,FilterByMetaData=new(){{"tag","old"}},
                SearchGroups=new(){new SearchTextGroup{SearchHolons=true,SearchQuery="historical-query-old",
                    HolonType=HolonType.Planet,HolonSearchParams=new SearchHolonParams{Name=true}}}};
            var result=synchronous ? provider.Search(query,maxChildDepth:depth,version:1):await provider.SearchAsync(query,maxChildDepth:depth,version:1);
            result.IsError.Should().BeFalse(result.Message+result.Exception);selected=result.Result.SearchResultHolons;
            var current=await provider.SearchAsync(query,maxChildDepth:depth,version:0);
            current.IsError.Should().BeFalse();current.Result.SearchResultHolons.Should().BeEmpty();
        }
        else
        {
            var filter=selector==1 ? new Dictionary<string,string>{{"tag","old"},{"category","old"}}
                :new Dictionary<string,string>{{"tag","old"},{"missing","never"}};
            var mode=selector==1 ? MetaKeyValuePairMatchMode.All:MetaKeyValuePairMatchMode.Any;
            var result=selector==0
                ? synchronous ? provider.LoadHolonsByMetaData("tag","old",HolonType.Planet,maxChildDepth:depth,version:1)
                    :await provider.LoadHolonsByMetaDataAsync("tag","old",HolonType.Planet,maxChildDepth:depth,version:1)
                : synchronous ? provider.LoadHolonsByMetaData(filter,mode,HolonType.Planet,maxChildDepth:depth,version:1)
                    :await provider.LoadHolonsByMetaDataAsync(filter,mode,HolonType.Planet,maxChildDepth:depth,version:1);
            result.IsError.Should().BeFalse(result.Message+result.Exception);selected=result.Result;
            (await provider.LoadHolonsByMetaDataAsync("tag","old",version:0)).Result.Should().BeEmpty();
            (await provider.LoadHolonsByMetaDataAsync("tag","old",version:99)).Result.Should().BeEmpty();
        }
        var loaded=selected.Should().ContainSingle().Subject;
        loaded.Id.Should().Be(root.Id);loaded.Name.Should().Be("historical-query-old");loaded.MetaData["tag"].ToString().Should().Be("old");
        var loadedChild=loaded.Children.Should().ContainSingle().Subject;loadedChild.Name.Should().Be("child-new");
        loadedChild.Children.Count.Should().Be(depth==1 ? 0:1);
        if(depth==2)loadedChild.Children.Single().Name.Should().Be("grand-new");
    }
}
