using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

[TestClass]
public class EvmSearchOwnershipControlledTests
{
    private sealed class Collection(Holon[] rows,Action change)
    {
        public int Reads;
        public Task<OASISResult<IEnumerable<IHolon>>> Read(){Reads++;change();return Task.FromResult(new OASISResult<IEnumerable<IHolon>>{Result=rows,IsLoaded=true});}
    }
    private sealed class EthereumRecords(Collection collection):EthereumOASIS("http://127.0.0.1:1","0x"+new string('1',64),31337,"0x"+new string('3',40))
    {
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type=HolonType.All,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)=>collection.Read();
    }
    private sealed class Web3Records(Collection collection):Web3CoreOASISBaseProvider("http://127.0.0.1:1","0x"+new string('1',64),"0x"+new string('3',40),31337)
    {
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type=HolonType.All,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)=>collection.Read();
    }
    public static IEnumerable<object[]> Cases(){foreach(var web3 in new[]{false,true})foreach(var scenario in Enumerable.Range(0,6))foreach(var sync in new[]{false,true})yield return new object[]{web3,scenario,sync};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task SearchCapturesOriginalScopeBeforeCollectionRead(bool web3,int scenario,bool synchronous)
    {
        var owner=Guid.NewGuid();var other=Guid.NewGuid();var parent=Guid.NewGuid();var alternate=Guid.NewGuid();
        var first=new Holon{Id=Guid.NewGuid(),Name="oak",Description="east",CreatedByAvatarId=owner,ParentHolonId=parent,MetaData=new(){{"zone","north"}}};
        var rows=new[]{first,new Holon{Id=Guid.NewGuid(),Name="pine",Description="oak",CreatedByAvatarId=owner,ParentHolonId=alternate,MetaData=new(){{"zone","south"}}},new Holon{Id=Guid.NewGuid(),Name="oak",CreatedByAvatarId=other,ParentHolonId=parent,MetaData=new(){{"zone","north"}}}};
        var group=new SearchTextGroup{SearchQuery="oak",SearchHolons=true,HolonSearchParams=new SearchHolonParams{Name=true}};
        var query=new SearchParams{SearchOnlyForCurrentAvatar=true,AvatarId=owner,ParentId=parent,Recursive=false,FilterByMetaData=new(){{"zone","north"}},SearchGroups=new(){group}};
        void Change(){switch(scenario){case 0:query.AvatarId=other;break;case 1:group.SearchQuery="pine";query.ParentId=alternate;query.FilterByMetaData["zone"]="south";break;case 2:group.HolonSearchParams.Name=false;group.HolonSearchParams.Description=true;break;case 3:query.FilterByMetaData["zone"]="south";break;case 4:query.ParentId=alternate;break;case 5:query.SearchOnlyForCurrentAvatar=false;break;}}
        var collection=new Collection(rows,Change);
        OASISStorageProviderBase provider=web3?new Web3Records(collection):new EthereumRecords(collection);
        var result=synchronous?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);
        Assert.AreEqual(1,result.Result.NumberOfResults);Assert.AreEqual(first.Id,result.Result.SearchResultHolons.Single().Id);Assert.AreEqual(1,collection.Reads);
    }
}
