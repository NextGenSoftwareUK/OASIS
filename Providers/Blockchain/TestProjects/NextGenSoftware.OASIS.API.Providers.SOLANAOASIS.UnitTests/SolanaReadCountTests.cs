using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.Models;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.Common;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.UnitTests;

[TestClass]
public class SolanaReadCountTests
{
    private sealed class Records(SolanaBaseDto[] rows):ISolanaRepository
    {
        public int Reads;public Action? AfterRead;public SolanaBaseDto[]? CurrentRows;public List<int> RequestedVersions=new();
        public Task<IReadOnlyList<(T Record,string Signature)>> GetAllAsync<T>(int version=0) where T:SolanaBaseDto,new(){Reads++;RequestedVersions.Add(version);var captured=(version==0?CurrentRows??rows:rows).OfType<T>().Select(row=>(row,row.Id.ToString())).ToArray();var change=AfterRead;AfterRead=null;change?.Invoke();return Task.FromResult<IReadOnlyList<(T,string)>>(captured);}
        public Task<T> GetAsync<T>(string reference) where T:SolanaBaseDto,new(){Reads++;return Task.FromResult(rows.OfType<T>().Single(row=>row.Id.ToString()==reference));}
        public Task<string> CreateAsync<T>(T value)where T:SolanaBaseDto,new()=>throw new InvalidOperationException("Read-only fixture");
        public Task<string> UpdateAsync<T>(T value)where T:SolanaBaseDto,new()=>throw new InvalidOperationException("Read-only fixture");
        public Task<bool> DeleteAsync(string reference)=>throw new InvalidOperationException("Read-only fixture");
        public Task<string> CreateTombstoneAsync(string reference)=>throw new InvalidOperationException("Read-only fixture");
    }
    public static IEnumerable<object[]> Cases(){foreach(var route in Enumerable.Range(0,11))foreach(var sync in new[]{false,true})foreach(var version in new[]{0,1})yield return new object[]{route,sync,version};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task PublicReadCountsDescribeReturnedRoots(int route,bool sync,int version)
    {
        var first=Guid.NewGuid();var other=Guid.NewGuid();var parent=Guid.NewGuid();
        SolanaBaseDto[] rows={new SolanaAvatarDto{Id=first,AvatarId=first,UserName="oak"},new SolanaAvatarDto{Id=other,AvatarId=other,UserName="pine"},new SolanaAvatarDetailDto{Id=first,Username="oak"},new SolanaAvatarDetailDto{Id=other,Username="pine"},new SolanaHolonDto{Id=first,Name="oak",ParentHolonId=parent},new SolanaHolonDto{Id=other,Name="pine",ParentHolonId=Guid.NewGuid()}};
        foreach(var row in rows)row.Version=version==0?2:1;
        var records=new Records(rows);var account=new Account();var provider=new SolanaOASIS("http://127.0.0.1:1",account.PrivateKey.Key,account.PublicKey.Key){IsProviderActivated=true};
        typeof(SolanaOASIS).GetField("_solanaRepository",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,records);
        void Check<T>(OASISResult<T> read,int count){Assert.IsFalse(read.IsError,read.Exception?.ToString());Assert.IsTrue(read.IsLoaded);Assert.AreEqual(count,read.LoadedCount);Assert.IsNotNull(read.Result);if(read.Result is IEnumerable<IHolonBase> selected)Assert.AreEqual(count,selected.Count());else if(read.Result is ISearchResults search)Assert.AreEqual(count,search.NumberOfResults);else if(read.Result is IHolonBase selectedRoot)Assert.AreEqual(first,selectedRoot.Id);}
        switch(route){
            case 0:Check(sync?provider.LoadAvatar(first,version):await provider.LoadAvatarAsync(first,version),1);break;
            case 1:Check(sync?provider.LoadAvatarDetail(first,version):await provider.LoadAvatarDetailAsync(first,version),1);break;
            case 2:Check(sync?provider.LoadHolon(first,loadChildren:false,version:version):await provider.LoadHolonAsync(first,loadChildren:false,version:version),1);break;
            case 3:Check(sync?provider.LoadAllAvatars(version):await provider.LoadAllAvatarsAsync(version),2);break;
            case 4:Check(sync?provider.LoadAllAvatarDetails(version):await provider.LoadAllAvatarDetailsAsync(version),2);break;
            case 5:Check(sync?provider.LoadAllHolons(loadChildren:false,version:version):await provider.LoadAllHolonsAsync(loadChildren:false,version:version),2);break;
            case 6:Check(sync?provider.LoadHolonsForParent(parent,loadChildren:false,version:version):await provider.LoadHolonsForParentAsync(parent,loadChildren:false,version:version),1);break;
            case 7:var selected=rows.OfType<SolanaHolonDto>().First();selected.MetaData=new(){{"zone","park"}};Check(sync?provider.LoadHolonsByMetaData("zone","park",loadChildren:false,version:version):await provider.LoadHolonsByMetaDataAsync("zone","park",loadChildren:false,version:version),1);break;
            case 8:Check(sync?provider.LoadAvatarByProviderKey(first.ToString(),version):await provider.LoadAvatarByProviderKeyAsync(first.ToString(),version),1);break;
            case 9:Check(sync?provider.LoadHolon(first.ToString(),loadChildren:false,version:version):await provider.LoadHolonAsync(first.ToString(),loadChildren:false,version:version),1);break;
            case 10:var query=new SearchParams{SearchOnlyForCurrentAvatar=false,SearchGroups=new List<ISearchGroupBase>{new SearchTextGroup{SearchHolons=true,SearchQuery="oak",HolonSearchParams=new SearchHolonParams{Name=true}}}};Check(sync?provider.Search(query,loadChildren:false,version:version):await provider.SearchAsync(query,loadChildren:false,version:version),1);break;
        }
        Assert.IsTrue(records.Reads>0);
    }
    [DataTestMethod][DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)][DataRow(2,false)][DataRow(2,true)]
    public async Task SearchOwnsOriginalOwnerMetadataAndNestedQueryBeforeRead(int mutation,bool sync)
    {
        var owner=Guid.NewGuid();var first=new SolanaHolonDto{Id=Guid.NewGuid(),Name="oak",CreatedByAvatarId=owner,MetaData=new(){{"zone","north"}}};
        var other=new SolanaHolonDto{Id=Guid.NewGuid(),Name="oak",CreatedByAvatarId=Guid.NewGuid(),MetaData=new(){{"zone","north"}}};
        var records=new Records([first,other]);var account=new Account();var provider=new SolanaOASIS("http://127.0.0.1:1",account.PrivateKey.Key,account.PublicKey.Key){IsProviderActivated=true};
        typeof(SolanaOASIS).GetField("_solanaRepository",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,records);
        var group=new SearchTextGroup{SearchHolons=true,SearchQuery="oak",HolonSearchParams=new SearchHolonParams{Name=true}};
        var query=new SearchParams{SearchOnlyForCurrentAvatar=true,AvatarId=owner,FilterByMetaData=new(){{"zone","north"}},SearchGroups=new List<ISearchGroupBase>{group}};
        records.AfterRead=()=>{switch(mutation){case 0:query.AvatarId=other.CreatedByAvatarId;break;case 1:query.FilterByMetaData["zone"]="south";break;default:group.SearchQuery="pine";break;}};
        var result=sync?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(first.Id,result.Result.SearchResultHolons.Single().Id);Assert.IsNull(records.AfterRead);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task HistoricalSearchSelectsRootVersionButHydratesIndependentCurrentChildren(bool sync)
    {
        var root=Guid.NewGuid();var child=Guid.NewGuid();
        var records=new Records([new SolanaHolonDto{Id=root,Name="oak",Version=1},new SolanaHolonDto{Id=child,ParentHolonId=root,Name="old child",Version=1}]){
            CurrentRows=[new SolanaHolonDto{Id=root,Name="current root",Version=2},new SolanaHolonDto{Id=child,ParentHolonId=root,Name="independent current child",Version=7}]};
        var account=new Account();var provider=new SolanaOASIS("http://127.0.0.1:1",account.PrivateKey.Key,account.PublicKey.Key){IsProviderActivated=true};
        typeof(SolanaOASIS).GetField("_solanaRepository",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,records);
        var query=new SearchParams{SearchOnlyForCurrentAvatar=false,SearchGroups=new List<ISearchGroupBase>{new SearchTextGroup{SearchHolons=true,SearchQuery="oak",HolonSearchParams=new SearchHolonParams{Name=true}}}};
        var result=sync?provider.Search(query,version:1):await provider.SearchAsync(query,version:1);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreEqual(1,result.LoadedCount);var selected=result.Result.SearchResultHolons.Single();Assert.AreEqual(root,selected.Id);Assert.AreEqual(1,selected.Version);Assert.AreEqual(7,selected.Children.Single().Version);Assert.AreEqual("independent current child",selected.Children.Single().Name);
        CollectionAssert.AreEqual(new[]{1,0},records.RequestedVersions);
    }
}
