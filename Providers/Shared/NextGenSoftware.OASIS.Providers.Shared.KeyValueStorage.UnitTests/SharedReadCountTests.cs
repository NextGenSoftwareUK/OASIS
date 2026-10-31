using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public sealed class SharedReadCountTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(true,false)][DataRow(false,true)][DataRow(true,true)]
    public async Task PartialChildReadCountsTheReturnedRootAndFailedReadCountsNothing(bool sync,bool continueOnError)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);
        var healthy=new Holon{Id=Guid.NewGuid()};var unavailable=new Holon{Id=Guid.NewGuid()};
        var root=new Holon{Id=Guid.NewGuid(),Children=new List<IHolon>{healthy,unavailable}};
        Assert.IsFalse((await provider.SaveHolonAsync(root)).IsError);backend.FailGetKey="oasis/holon/"+unavailable.Id.ToString("N");
        var result=sync?provider.LoadHolon(root.Id,continueOnError:continueOnError):await provider.LoadHolonAsync(root.Id,continueOnError:continueOnError);
        if(continueOnError){Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsWarning);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);Assert.AreEqual(healthy.Id,result.Result.Children.Single().Id);}
        else{Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsLoaded);Assert.IsNull(result.Result);Assert.AreEqual(0,result.LoadedCount);}
    }

    public static IEnumerable<object[]> Cases(){foreach(var route in Enumerable.Range(0,12))foreach(var sync in new[]{false,true})yield return new object[]{route,sync};}

    [DataTestMethod][DynamicData(nameof(Cases),DynamicDataSourceType.Method)]
    public async Task AcknowledgedReadCountsReturnedRootsNotDescendants(int route,bool sync)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);
        var avatar=new Avatar{Id=Guid.NewGuid(),Username="owned-count",Email="owned-count@example.invalid",FirstName="count-match"};
        var detail=new AvatarDetail{Id=avatar.Id,Username=avatar.Username,Email=avatar.Email};
        var child=new Holon{Id=Guid.NewGuid(),Name="child",CreatedByAvatarId=avatar.Id};
        var root=new Holon{Id=Guid.NewGuid(),Name="count-match",CreatedByAvatarId=avatar.Id,Children=new List<IHolon>{child}};
        root.MetaData["owned.count"]="selected";
        Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);
        Assert.IsFalse((await provider.SaveAvatarDetailAsync(detail)).IsError);
        Assert.IsFalse((await provider.SaveHolonAsync(root)).IsError);
        void Check<T>(OASISResult<T> result,int expected)
        {
            Assert.IsFalse(result.IsError,result.Message);Assert.IsNotNull(result.Result);
            Assert.IsTrue(result.IsLoaded);Assert.AreEqual(expected,result.LoadedCount);Assert.IsFalse(result.IsSaved);
        }
        if(route==0)Check(sync?provider.LoadAvatar(avatar.Id):await provider.LoadAvatarAsync(avatar.Id),1);
        else if(route==1)Check(sync?provider.LoadAvatarDetail(avatar.Id):await provider.LoadAvatarDetailAsync(avatar.Id),1);
        else if(route==2){var result=sync?provider.LoadHolon(root.Id):await provider.LoadHolonAsync(root.Id);Check(result,1);Assert.AreEqual(1,result.Result.Children.Count());}
        else if(route==3)Check(sync?provider.LoadAllAvatars():await provider.LoadAllAvatarsAsync(),1);
        else if(route==4)Check(sync?provider.LoadAllAvatarDetails():await provider.LoadAllAvatarDetailsAsync(),1);
        else if(route==5)Check(sync?provider.LoadAllHolons():await provider.LoadAllHolonsAsync(),2);
        else if(route==6)Check(sync?provider.LoadHolonsForParent(root.Id):await provider.LoadHolonsForParentAsync(root.Id),1);
        else if(route==7)Check(sync?provider.LoadHolonsByMetaData("owned.count","selected"):await provider.LoadHolonsByMetaDataAsync("owned.count","selected"),1);
        else if(route==8){var query=new SearchParams{SearchOnlyForCurrentAvatar=false};Check(sync?provider.Search(query):await provider.SearchAsync(query),3);}
        else if(route==9)Check(sync?provider.ExportAll():await provider.ExportAllAsync(),2);
        else if(route==10)Check(sync?provider.ExportAllDataForAvatarById(avatar.Id):await provider.ExportAllDataForAvatarByIdAsync(avatar.Id),2);
        else Check(sync?provider.LoadHolonsByMetaData("owned.count","absent"):await provider.LoadHolonsByMetaDataAsync("owned.count","absent"),0);
    }
}
