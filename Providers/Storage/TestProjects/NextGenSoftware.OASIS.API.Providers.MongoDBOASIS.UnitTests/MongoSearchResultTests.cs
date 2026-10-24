using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces;
using NextGenSoftware.OASIS.Common;
using Provider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.UnitTests;

[TestClass]
public class MongoSearchResultTests
{
    private sealed class Repository(bool malformed,bool failed=false):ISearchRepository
    {
        internal readonly Exception NativeCause=new IOException("owned-native-search-failure");
        public OASISResult<ISearchResults> Search(ISearchParams query,int version=0)
        {
            var found=new SearchResults();
            if(malformed)found.SearchResultHolons=null!;
            else found.SearchResultHolons.Add(new Holon{Id=Guid.NewGuid(),Name="owned"});
            return new(found){IsLoaded=true,LoadedCount=99,IsError=failed,ErrorCode=failed?"OWNED_NATIVE_FAILURE":null,Exception=failed?NativeCause:null};
        }
        public Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams query,int version=0)=>Task.FromResult(Search(query,version));
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task RepositoryFailureCannotPublishStalePayloadOrCounts(bool synchronous)
    {
        var provider=new Provider("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1","owned-search-controls"){IsProviderActivated=true};
        var repository=new Repository(false,true);typeof(Provider).GetField("_searchRepository",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,repository);
        var query=new SearchParams{SearchOnlyForCurrentAvatar=false};
        var result=synchronous?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
        Assert.IsTrue(result.IsError);Assert.AreSame(repository.NativeCause,result.Exception);Assert.AreEqual("OWNED_NATIVE_FAILURE",result.ErrorCode);
        Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);
    }
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task SearchCountsDescribePublishedResultsNotRepositoryEnvelope(bool malformed,bool synchronous)
    {
        var provider=new Provider("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=1","owned-search-controls"){IsProviderActivated=true};
        typeof(Provider).GetField("_searchRepository",BindingFlags.NonPublic|BindingFlags.Instance)!.SetValue(provider,new Repository(malformed));
        var query=new SearchParams{SearchOnlyForCurrentAvatar=false,SearchGroups=new(){new SearchTextGroup{SearchQuery="owned",SearchHolons=true}}};
        var result=synchronous?provider.Search(query,loadChildren:false):await provider.SearchAsync(query,loadChildren:false);
        Assert.AreEqual(malformed,result.IsError);Assert.AreEqual(!malformed,result.IsLoaded);
        Assert.AreEqual(malformed?0:1,result.LoadedCount);
        if(malformed)Assert.IsNull(result.Result);else Assert.AreEqual(1,result.Result.NumberOfResults);
    }
}
