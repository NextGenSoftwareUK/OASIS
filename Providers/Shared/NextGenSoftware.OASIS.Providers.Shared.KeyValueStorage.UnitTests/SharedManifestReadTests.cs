using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class SharedManifestReadTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(true,false)][DataRow(false,true)][DataRow(true,true)]
    public async Task SelectedMemberAbsenceIsCorruptionButExplicitReadContinuationRetainsHealthyMembers(bool sync,bool unavailable)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);
        var healthy=new Holon{Id=Guid.NewGuid()};var missing=new Holon{Id=Guid.NewGuid()};
        var root=new Holon{Id=Guid.NewGuid(),Children=new List<IHolon>{healthy,missing}};
        Assert.IsFalse((await provider.SaveHolonAsync(root)).IsError);var key="oasis/holon/"+missing.Id.ToString("N");
        if(unavailable)backend.FailGetKey=key;else Assert.IsTrue(backend.Items.TryRemove(key,out _));
        var result=sync?provider.LoadHolon(root.Id,continueOnError:true):await provider.LoadHolonAsync(root.Id,continueOnError:true);
        if(unavailable){Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsWarning);Assert.AreEqual(healthy.Id,result.Result.Children.Single().Id);Assert.AreEqual("1",result.MetaData["failedHolonReadCount"]);}
        else{Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsInstanceOfType<System.IO.InvalidDataException>(result.Exception);}
    }

    [DataTestMethod]
    [DataRow(false,"id")][DataRow(true,"id")]
    [DataRow(false,"all")][DataRow(true,"all")]
    [DataRow(false,"metadata")][DataRow(true,"metadata")]
    public async Task ExplicitEmptyMembershipDoesNotResurrectAnIndependentlyRetainedChild(bool sync,string route)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);
        var child=new Holon{Id=Guid.NewGuid()};var root=new Holon{Id=Guid.NewGuid(),Children=new List<IHolon>{child}};
        root.MetaData["selected-owner"]="yes";
        Assert.IsFalse((await provider.SaveHolonAsync(root)).IsError);
        root.Children.Clear();Assert.IsFalse((await provider.SaveHolonAsync(root)).IsError);
        var before=backend.Items.OrderBy(pair=>pair.Key).ToArray();IHolon loaded;
        if(route=="id"){
            var result=sync?provider.LoadHolon(root.Id):await provider.LoadHolonAsync(root.Id);Assert.IsFalse(result.IsError,result.Message);loaded=result.Result;
        }else if(route=="all"){
            var result=sync?provider.LoadAllHolons():await provider.LoadAllHolonsAsync();Assert.IsFalse(result.IsError,result.Message);
            Assert.IsTrue(result.Result.Any(row=>row.Id==child.Id),"Unlinking is not deletion or hidden root filtering.");loaded=result.Result.Single(row=>row.Id==root.Id);
        }else{
            var result=sync?provider.LoadHolonsByMetaData("selected-owner","yes"):await provider.LoadHolonsByMetaDataAsync("selected-owner","yes");Assert.IsFalse(result.IsError,result.Message);loaded=result.Result.Single();
        }
        Assert.AreEqual(0,loaded.Children.Count);Assert.IsTrue(backend.Items.ContainsKey("oasis/holon/"+child.Id.ToString("N")));
        CollectionAssert.AreEqual(before,backend.Items.OrderBy(pair=>pair.Key).ToArray(),"Read must not repair/delete native records or indexes.");
    }
}
