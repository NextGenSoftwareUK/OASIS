using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public sealed class SearchRequestSnapshotTests
{
    private sealed class ObservedBackend : IKeyValueBackend
    {
        public readonly InMemoryBackend Store=new();public Action OnList;
        public Task<string> GetAsync(string key,CancellationToken token=default)=>Store.GetAsync(key,token);
        public Task PutAsync(string key,string value,CancellationToken token=default)=>Store.PutAsync(key,value,token);
        public Task DeleteAsync(string key,CancellationToken token=default)=>Store.DeleteAsync(key,token);
        public Task VerifyAsync(CancellationToken token=default)=>Store.VerifyAsync(token);
        public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix,CancellationToken token=default){var rows=await Store.ListKeysAsync(prefix,token);var change=OnList;OnList=null;change?.Invoke();return rows;}
    }
    public static IEnumerable<object[]> Cases(){foreach(var scenario in Enumerable.Range(0,6))foreach(var sync in new[]{false,true})yield return new object[]{scenario,sync};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task SharedSearchOwnsScopeAndNestedMasksBeforeBackendRead(int scenario,bool sync)
    {
        var backend=new ObservedBackend();var provider=new TestProvider(backend);var owner=Guid.NewGuid();var other=Guid.NewGuid();var parent=Guid.NewGuid();var alternateParent=Guid.NewGuid();
        var first=new Holon{Id=Guid.NewGuid(),Name="oak",Description="east",CreatedByAvatarId=owner,ParentHolonId=parent,MetaData=new(){{"zone","north"}}};
        var rows=new[]{first,new Holon{Id=Guid.NewGuid(),Name="pine",Description="oak",CreatedByAvatarId=owner,ParentHolonId=alternateParent,MetaData=new(){{"zone","south"}}},new Holon{Id=Guid.NewGuid(),Name="oak",CreatedByAvatarId=other,ParentHolonId=parent,MetaData=new(){{"zone","north"}}}};
        foreach(var row in rows){var saved=await provider.SaveHolonAsync(row,saveChildren:false);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());}
        var original=backend.Store.Items.ToDictionary(pair=>pair.Key,pair=>pair.Value);var writes=backend.Store.PutCalls;
        var group=new SearchTextGroup{SearchQuery="oak",SearchHolons=true,HolonSearchParams=new SearchHolonParams{Name=true}};
        var query=new SearchParams{SearchOnlyForCurrentAvatar=true,AvatarId=owner,ParentId=parent,Recursive=false,FilterByMetaData=new(){{"zone","north"}},SearchGroups=new List<ISearchGroupBase>{group}};
        backend.OnList=()=>{switch(scenario){case 0:query.AvatarId=other;break;case 1:group.SearchQuery="pine";query.ParentId=alternateParent;query.FilterByMetaData["zone"]="south";break;case 2:group.HolonSearchParams.Name=false;group.HolonSearchParams.Description=true;break;case 3:query.FilterByMetaData["zone"]="south";break;case 4:query.ParentId=alternateParent;break;case 5:query.SearchOnlyForCurrentAvatar=false;break;}};
        var result=sync?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.Result.NumberOfResults);Assert.AreEqual(first.Id,result.Result.SearchResultHolons.Single().Id);
        Assert.IsNull(backend.OnList);Assert.AreEqual(writes,backend.Store.PutCalls);Assert.AreEqual(original.Count,backend.Store.Items.Count);foreach(var pair in original)Assert.AreEqual(pair.Value,backend.Store.Items[pair.Key]);
    }
}
