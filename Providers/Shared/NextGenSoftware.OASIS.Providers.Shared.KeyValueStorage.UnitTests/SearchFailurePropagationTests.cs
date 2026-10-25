using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class SearchFailurePropagationTests
{
    [DataTestMethod]
    [DataRow(false,false,false)][DataRow(false,false,true)][DataRow(false,true,false)][DataRow(false,true,true)]
    [DataRow(true,false,false)][DataRow(true,false,true)][DataRow(true,true,false)][DataRow(true,true,true)]
    public async Task ScopedPartialSearchRedactsUnreadableIdentityWithoutHidingFailure(bool synchronous,bool scoped,bool continueOnError)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);var actor=Guid.NewGuid();
        var own=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=actor,Name="owned-search-result"};
        var unreadable=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=Guid.NewGuid(),Name="foreign-unreadable-record"};
        foreach(var row in new[]{own,unreadable})Assert.IsFalse((await provider.SaveHolonAsync(row,saveChildren:false)).IsError);
        var original=backend.Items.OrderBy(row=>row.Key).ToArray();var mutations=backend.PutCalls+backend.DeleteCalls;
        backend.FailGetKey="oasis/holon/"+unreadable.Id.ToString("N");
        backend.FailGetException=new System.IO.IOException("controlled record read outage");
        var query=new SearchParams{SearchOnlyForCurrentAvatar=scoped,AvatarId=actor,SearchGroups=new List<ISearchGroupBase>{new SearchTextGroup{SearchHolons=true,SearchQuery=own.Name,HolonSearchParams=new NextGenSoftware.OASIS.API.Core.Interfaces.SearchHolonParams{SearchAllFields=true}}}};
        var result=synchronous?provider.Search(query,loadChildren:false,continueOnError:continueOnError):await provider.SearchAsync(query,loadChildren:false,continueOnError:continueOnError);
        if(!continueOnError){
            Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);
            Assert.AreSame(backend.FailGetException,result.Exception);
        }else{
            Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsWarning);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);
            Assert.AreEqual(own.Id,result.Result.SearchResultHolons.Single().Id);Assert.AreEqual("true",result.MetaData["partialHolonReads"]);Assert.AreEqual("1",result.MetaData["failedHolonReadCount"]);
            if(scoped){
                Assert.IsFalse(result.MetaData.ContainsKey("failedHolonReads"));Assert.AreEqual("true",result.MetaData["failedHolonReadDetailsRedacted"]);
                Assert.IsFalse(result.Message.Contains(unreadable.Id.ToString(),StringComparison.Ordinal));
            }else StringAssert.Contains(result.MetaData["failedHolonReads"],unreadable.Id.ToString());
        }
        CollectionAssert.AreEqual(original,backend.Items.OrderBy(row=>row.Key).ToArray());Assert.AreEqual(mutations,backend.PutCalls+backend.DeleteCalls);
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task SearchRetainsOriginalCollectionCorruption(bool synchronous,bool avatar)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);var id=Guid.NewGuid();
        if(avatar)Assert.IsFalse((await provider.SaveAvatarAsync(new Avatar {Id=id,Username="owned-match"})).IsError);
        else Assert.IsFalse((await provider.SaveHolonAsync(new Holon {Id=id,Name="owned-match"},saveChildren:false)).IsError);
        backend.FailGetKey="oasis/"+(avatar ? "avatar/":"holon/")+id.ToString("N");
        backend.FailGetException=new System.IO.InvalidDataException("owned corrupt collection read");
        var query=new SearchParams {SearchOnlyForCurrentAvatar=false,SearchGroups=new List<ISearchGroupBase> {new SearchTextGroup {SearchQuery="owned",SearchAvatars=avatar,SearchHolons=!avatar}}};
        var result=synchronous ? provider.Search(query,continueOnError:false):await provider.SearchAsync(query,continueOnError:false);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreSame(backend.FailGetException,result.Exception);
    }
}
