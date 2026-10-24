using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public class MongoSearchIntentTests
{
    private sealed class Repository(Action mutate):ISearchRepository
    {
        public string? Observed;
        public OASISResult<ISearchResults> Search(ISearchParams query,int version=0)
        {
            mutate();Observed=OasisJson.Serialize(query);
            return new(new SearchResults());
        }
        public async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams query,int version=0)
        {await Task.Yield();return Search(query,version);}
    }
    public static IEnumerable<object[]> Cases(){foreach(var sync in new[]{false,true})foreach(var version in new[]{0,1})foreach(var edit in Enumerable.Range(0,4))yield return new object[]{sync,version,edit};}
    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task SearchOwnsValidatedIntentBeforeRepositoryDispatch(bool synchronous,int version,int edit)
    {
        var group=new SearchTextGroup{SearchQuery="original",SearchHolons=true};
        var query=new SearchParams{AvatarId=Guid.NewGuid(),ParentId=Guid.NewGuid(),SearchOnlyForCurrentAvatar=true,Recursive=true,
            FilterByMetaData=new(){{"tag","original"}},SearchGroups=new(){group}};
        var expected=OasisJson.Serialize((ISearchParams)query);
        var repository=new Repository(()=>{
            if(edit==0){query.AvatarId=Guid.NewGuid();query.ParentId=Guid.Empty;query.SearchOnlyForCurrentAvatar=false;query.Recursive=false;}
            else if(edit==1)query.FilterByMetaData["tag"]="changed";
            else if(edit==2)group.SearchQuery="changed";
            else query.SearchGroups.Clear();
        });
        var provider=new Provider("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1","search-intent-controls"){IsProviderActivated=true};
        typeof(Provider).GetField("_searchRepository",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,repository);
        var result=synchronous?provider.Search(query,loadChildren:false,version:version):await provider.SearchAsync(query,loadChildren:false,version:version);
        Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);
        Assert.AreNotEqual(expected,OasisJson.Serialize((ISearchParams)query),"Caller mutation must actually occur.");
        Assert.AreEqual(expected,repository.Observed,"Repository must receive the original validated query,not a live caller alias.");
    }
}
