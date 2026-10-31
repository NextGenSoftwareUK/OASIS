using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.UnitTests;

[TestClass]
public class NeoSearchSnapshotTests
{
    private sealed class Probe(IHolon[] holons,IAvatar[] avatars,Action change):Provider("bolt://127.0.0.1:17688","neo4j","owned",()=>DispatchProxy.Create<IDriver,Neo4jLifecycleTests.DriverProbe>())
    {
        public int Reads;
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type=HolonType.All,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
        {Reads++;change();return Task.FromResult(new OASISResult<IEnumerable<IHolon>>(holons){IsLoaded=true,LoadedCount=holons.Length});}
        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version=0)
        {Reads++;change();return Task.FromResult(new OASISResult<IEnumerable<IAvatar>>(avatars){IsLoaded=true,LoadedCount=avatars.Length});}
    }
    public static IEnumerable<object[]> ScopeCases(){foreach(var kind in Enumerable.Range(0,6))foreach(var sync in new[]{false,true})yield return new object[]{kind,sync};}
    [DataTestMethod][DynamicData(nameof(ScopeCases),DynamicDataSourceType.Method)]
    public async Task SearchCapturesOwnerParentMetadataAndMaskBeforeRead(int kind,bool sync)
    {
        var owner=Guid.NewGuid();var other=Guid.NewGuid();var parent=Guid.NewGuid();var alternative=Guid.NewGuid();
        var first=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=owner,ParentHolonId=parent,Name="oak",Description="east",MetaData=new(){{"zone","north"}}};
        IHolon[] rows={first,new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=owner,ParentHolonId=alternative,Name="pine",Description="oak",MetaData=new(){{"zone","south"}}},new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=other,ParentHolonId=parent,Name="oak",MetaData=new(){{"zone","north"}}}};
        var group=new SearchTextGroup{SearchHolons=true,HolonType=HolonType.All,SearchQuery="oak",HolonSearchParams=new SearchHolonParams{Name=true}};
        var query=new SearchParams{SearchOnlyForCurrentAvatar=true,AvatarId=owner,ParentId=parent,Recursive=false,FilterByMetaData=new(){{"zone","north"}},SearchGroups=new List<ISearchGroupBase>{group}};
        void Change(){switch(kind){case 0:query.AvatarId=other;break;case 1:group.SearchQuery="pine";query.ParentId=alternative;query.FilterByMetaData["zone"]="south";break;case 2:group.HolonSearchParams.Name=false;group.HolonSearchParams.Description=true;break;case 3:query.FilterByMetaData["zone"]="south";break;case 4:query.ParentId=alternative;break;case 5:query.SearchOnlyForCurrentAvatar=false;break;}}
        var provider=new Probe(rows,Array.Empty<IAvatar>(),Change);
        try{var result=sync?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(first.Id,result.Result.SearchResultHolons.Single().Id);Assert.AreEqual(1,provider.Reads);}
        finally{await provider.DeActivateProviderAsync();}
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task SearchCountDescribesSelectedResultsNotSourceCollection(bool avatar,bool sync)
    {
        var provider=new Probe(new IHolon[]{new Holon{Id=Guid.NewGuid(),Name="oak"},new Holon{Id=Guid.NewGuid(),Name="pine"}},
            new IAvatar[]{new Avatar{Id=Guid.NewGuid(),Username="oak"},new Avatar{Id=Guid.NewGuid(),Username="pine"}},()=>{});
        var query=new SearchParams{SearchOnlyForCurrentAvatar=false,SearchGroups=new List<ISearchGroupBase>{new SearchTextGroup{SearchAvatars=avatar,SearchHolons=!avatar,HolonType=HolonType.All,SearchQuery="oak"}}};
        try{var result=sync?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(1,result.Result.NumberOfResults);Assert.AreEqual(1,result.LoadedCount);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,provider.Reads);}
        finally{await provider.DeActivateProviderAsync();}
    }
}
