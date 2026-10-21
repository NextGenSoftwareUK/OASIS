using System.Reflection;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass][DoNotParallelize]
public class NearSearchSnapshotTests
{
    private sealed class Records(Holon[] rows,Action change):NEAROASIS("http://127.0.0.1:1","localnet","localnet","contract.local","sender.local","")
    {
        public int Reads;
        public override Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type=HolonType.All,bool loadChildren=true,bool recursive=true,int maxChildDepth=0,int curentChildDepth=0,bool continueOnError=true,bool loadChildrenFromProvider=false,int version=0)
        {Reads++;change();return Task.FromResult(new OASISResult<IEnumerable<IHolon>>{Result=rows,IsLoaded=true});}
    }

    public static IEnumerable<object[]> Cases(){foreach(var scenario in Enumerable.Range(-1,7))foreach(var sync in new[]{false,true})yield return new object[]{scenario,sync};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task SearchOwnsScopeAndReportsSuccessfulLoad(int scenario,bool synchronous)
    {
        var owner=Guid.NewGuid();var other=Guid.NewGuid();var parent=Guid.NewGuid();var alternate=Guid.NewGuid();
        var first=new Holon{Id=Guid.NewGuid(),Name="oak",Description="east",CreatedByAvatarId=owner,ParentHolonId=parent,MetaData=new(){{"zone","north"}}};
        var rows=new[]{first,new Holon{Id=Guid.NewGuid(),Name="pine",Description="oak",CreatedByAvatarId=owner,ParentHolonId=alternate,MetaData=new(){{"zone","south"}}},new Holon{Id=Guid.NewGuid(),Name="oak",CreatedByAvatarId=other,ParentHolonId=parent,MetaData=new(){{"zone","north"}}}};
        var group=new SearchTextGroup{SearchQuery="oak",SearchHolons=true,HolonSearchParams=new SearchHolonParams{Name=true}};
        var query=new SearchParams{SearchOnlyForCurrentAvatar=true,AvatarId=owner,ParentId=parent,Recursive=false,FilterByMetaData=new(){{"zone","north"}},SearchGroups=new(){group}};
        void Change(){switch(scenario){case 0:query.AvatarId=other;break;case 1:group.SearchQuery="pine";query.ParentId=alternate;query.FilterByMetaData["zone"]="south";break;case 2:group.HolonSearchParams.Name=false;group.HolonSearchParams.Description=true;break;case 3:query.FilterByMetaData["zone"]="south";break;case 4:query.ParentId=alternate;break;case 5:query.SearchOnlyForCurrentAvatar=false;break;}}
        var provider=new Records(rows,Change);
        // Existing controlled Node IPC bridge only. No SDK/native RPC or signing operation.
        var serviceType=typeof(NEAROASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.NEAROASIS.NearSdkService")!;
        var service=Activator.CreateInstance(serviceType,new object[]{"http://127.0.0.1:1","localnet","contract.local","sender.local","unused","node",Path.Combine(AppContext.BaseDirectory,"Fixtures","storage-snapshot.mjs")})!;
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        typeof(NEAROASIS).GetField("_sdk",flags)!.SetValue(provider,service);
        typeof(NEAROASIS).GetField("_isActivated",flags)!.SetValue(provider,true);
        const string setting="OASIS_NEAR_STORAGE_ENCRYPTION_KEY";var previous=Environment.GetEnvironmentVariable(setting);
        var key=RandomNumberGenerator.GetBytes(32);
        try{
            Environment.SetEnvironmentVariable(setting,Convert.ToBase64String(key));
            var result=synchronous?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
            Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(1,result.Result.NumberOfResults);
            Assert.AreEqual(first.Id,result.Result.SearchResultHolons.Single().Id);Assert.AreEqual(1,provider.Reads);Assert.IsTrue(result.IsLoaded);
        }finally{Environment.SetEnvironmentVariable(setting,previous);CryptographicOperations.ZeroMemory(key);}
    }
}
