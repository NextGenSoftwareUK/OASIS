using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public sealed class GoogleSearchSnapshotTests
{
    private sealed class Records(Holon[] rows,Action change):GoogleCloudOASIS(enableStorage:false,enableFirestore:false,enableBigQuery:false)
    {
        public int Reads;
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type=HolonType.All,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
        {Reads++;change();return Task.FromResult(new OASISResult<IEnumerable<IHolon>>{Result=rows,IsLoaded=true});}
    }
    public static IEnumerable<object[]> Cases(){foreach(var scenario in Enumerable.Range(0,6))foreach(var sync in new[]{false,true})yield return new object[]{scenario,sync};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task SearchOwnsScopeAndNestedMasksBeforeCollectionRead(int scenario,bool sync)
    {
        var owner=Guid.NewGuid();var other=Guid.NewGuid();var parent=Guid.NewGuid();var alternateParent=Guid.NewGuid();
        var first=new Holon{Id=Guid.NewGuid(),Name="oak",Description="east",CreatedByAvatarId=owner,ParentHolonId=parent,MetaData=new(){{"zone","north"}}};
        var rows=new[]{first,new Holon{Id=Guid.NewGuid(),Name="pine",Description="oak",CreatedByAvatarId=owner,ParentHolonId=alternateParent,MetaData=new(){{"zone","south"}}},new Holon{Id=Guid.NewGuid(),Name="oak",CreatedByAvatarId=other,ParentHolonId=parent,MetaData=new(){{"zone","north"}}}};
        var group=new SearchTextGroup{SearchQuery="oak",SearchHolons=true,HolonSearchParams=new SearchHolonParams{Name=true}};
        var query=new SearchParams{SearchOnlyForCurrentAvatar=true,AvatarId=owner,ParentId=parent,Recursive=false,FilterByMetaData=new(){{"zone","north"}},SearchGroups=new List<ISearchGroupBase>{group}};
        void Change(){switch(scenario){case 0:query.AvatarId=other;break;case 1:group.SearchQuery="pine";query.ParentId=alternateParent;query.FilterByMetaData["zone"]="south";break;case 2:group.HolonSearchParams.Name=false;group.HolonSearchParams.Description=true;break;case 3:query.FilterByMetaData["zone"]="south";break;case 4:query.ParentId=alternateParent;break;case 5:query.SearchOnlyForCurrentAvatar=false;break;}}
        var provider=new Records(rows,Change);var result=sync?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.Result.NumberOfResults);Assert.AreEqual(1,result.LoadedCount);Assert.AreEqual(first.Id,result.Result.SearchResultHolons.Single().Id);Assert.AreEqual(1,provider.Reads);
    }
}
