using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class ExportSelectorVersionTests
{
    [DataTestMethod][DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)]
    [DataRow(2,false)][DataRow(2,true)][DataRow(3,false)][DataRow(3,true)]
    [DataRow(4,false)][DataRow(4,true)]
    public async Task AllVersionExportRejectsInvalidArchiveWithoutPartialResults(int mode,bool sync)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend){IsVersionControlEnabled=true};
        var row=new Holon{Id=Guid.NewGuid(),Name="first"};
        Assert.IsFalse((await provider.SaveHolonAsync(row,saveChildren:false)).IsError);
        row.Name="second";Assert.IsFalse((await provider.SaveHolonAsync(row,saveChildren:false)).IsError);
        var history=backend.Items.Keys.Single(key=>key.StartsWith("oasis/version/holon/",StringComparison.Ordinal));
        if(mode==0)backend.Items[history]=OasisJson.Serialize(new Holon{Id=Guid.NewGuid(),Version=1});
        else if(mode==1)backend.Items[history]=OasisJson.Serialize(new Holon{Id=row.Id,Version=99});
        else if(mode==2){var original=backend.Items[history];backend.Items.TryRemove(history,out _);backend.Items[history[..history.LastIndexOf('/')]+"/1"]=original;}
        else if(mode==3){backend.FailGetKey=history;backend.FailGetException=new System.IO.IOException("owned archive read failure");}
        else backend.Items[history]="";
        var puts=backend.PutCalls;var result=sync?provider.ExportAll(-1):await provider.ExportAllAsync(-1);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);
        if(mode==3)Assert.AreSame(backend.FailGetException,result.Exception);
        else Assert.IsInstanceOfType<System.IO.InvalidDataException>(result.Exception);
        Assert.AreEqual(puts,backend.PutCalls);
    }

    [DataTestMethod]
    [DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task ExportVersionSelectsHolonRootsNotIndependentIdentityHistory(bool sync,bool email)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend){IsVersionControlEnabled=true};
        var avatar=new Avatar{Id=Guid.NewGuid(),Username="old-export-owner",Email="old-export@example.invalid"};
        Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);
        avatar.Username="current-export-owner";avatar.Email="current-export@example.invalid";
        Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);Assert.AreEqual(2,avatar.Version);
        var own=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=avatar.Id};
        var foreign=new Holon{Id=Guid.NewGuid(),CreatedByAvatarId=Guid.NewGuid()};
        for(var version=1;version<=3;version++){
            own.Name="owned-version-"+version;foreign.Name="foreign-version-"+version;
            Assert.IsFalse((await provider.SaveHolonAsync(own,saveChildren:false)).IsError);
            Assert.IsFalse((await provider.SaveHolonAsync(foreign,saveChildren:false)).IsError);
        }
        var byId=await provider.ExportAllDataForAvatarByIdAsync(avatar.Id,3);
        Assert.IsFalse(byId.IsError,byId.Message);Assert.AreEqual(own.Id,byId.Result.Single().Id);
        var before=backend.Items.OrderBy(pair=>pair.Key).ToArray();var puts=backend.PutCalls;
        foreach(var version in new[]{0,1,2,3}){
            var result=email
                ?sync?provider.ExportAllDataForAvatarByEmail(avatar.Email,version):await provider.ExportAllDataForAvatarByEmailAsync(avatar.Email,version)
                :sync?provider.ExportAllDataForAvatarByUsername(avatar.Username,version):await provider.ExportAllDataForAvatarByUsernameAsync(avatar.Username,version);
            Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);Assert.AreEqual(1,result.LoadedCount);
            var selected=version==0?3:version;
            Assert.AreEqual(own.Id,result.Result.Single().Id);Assert.AreEqual(selected,result.Result.Single().Version);
            Assert.AreEqual("owned-version-"+selected,result.Result.Single().Name);
        }
        var allVersions=email
            ?sync?provider.ExportAllDataForAvatarByEmail(avatar.Email,-1):await provider.ExportAllDataForAvatarByEmailAsync(avatar.Email,-1)
            :sync?provider.ExportAllDataForAvatarByUsername(avatar.Username,-1):await provider.ExportAllDataForAvatarByUsernameAsync(avatar.Username,-1);
        Assert.IsFalse(allVersions.IsError,allVersions.Message);Assert.IsTrue(allVersions.IsLoaded);Assert.AreEqual(3,allVersions.LoadedCount);
        var versions=allVersions.Result.ToArray();CollectionAssert.AreEqual(new[]{1,2,3},versions.Select(row=>row.Version).ToArray());
        Assert.IsTrue(versions.All(row=>row.Id==own.Id&&row.CreatedByAvatarId==avatar.Id));
        var old=email?await provider.ExportAllDataForAvatarByEmailAsync("old-export@example.invalid",1)
            :await provider.ExportAllDataForAvatarByUsernameAsync("old-export-owner",1);
        Assert.IsTrue(old.IsError);Assert.IsNull(old.Result);
        CollectionAssert.AreEqual(before,backend.Items.OrderBy(pair=>pair.Key).ToArray());Assert.AreEqual(puts,backend.PutCalls);
    }

    [DataTestMethod]
    [DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NegativeExportVersionFailsBeforeIdentityStorageAccess(bool sync,bool email)
    {
        var backend=new InMemoryBackend{ThrowReads=true};var provider=new TestProvider(backend);
        var result=email
            ?sync?provider.ExportAllDataForAvatarByEmail("none@example.invalid",-2):await provider.ExportAllDataForAvatarByEmailAsync("none@example.invalid",-2)
            :sync?provider.ExportAllDataForAvatarByUsername("none",-2):await provider.ExportAllDataForAvatarByUsernameAsync("none",-2);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);
        Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);Assert.AreEqual(0,backend.ListCalls);Assert.AreEqual(0,backend.PutCalls);
    }
}
