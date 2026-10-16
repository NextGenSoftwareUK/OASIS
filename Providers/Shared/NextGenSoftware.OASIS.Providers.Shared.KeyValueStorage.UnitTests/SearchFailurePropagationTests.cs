using System;
using System.Collections.Generic;
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
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task SearchRetainsOriginalCollectionCorruption(bool synchronous,bool avatar)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);var id=Guid.NewGuid();
        if(avatar)Assert.IsFalse((await provider.SaveAvatarAsync(new Avatar {Id=id,Username="owned-match"})).IsError);
        else Assert.IsFalse((await provider.SaveHolonAsync(new Holon {Id=id,Name="owned-match"},saveChildren:false)).IsError);
        backend.FailGetKey="oasis/"+(avatar ? "avatar/":"holon/")+id.ToString("N");
        backend.FailGetException=new System.IO.InvalidDataException("owned corrupt collection read");
        var query=new SearchParams {SearchGroups=new List<ISearchGroupBase> {new SearchTextGroup {SearchQuery="owned",SearchAvatars=avatar,SearchHolons=!avatar}}};
        var result=synchronous ? provider.Search(query,continueOnError:false):await provider.SearchAsync(query,continueOnError:false);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreSame(backend.FailGetException,result.Exception);
    }
}
