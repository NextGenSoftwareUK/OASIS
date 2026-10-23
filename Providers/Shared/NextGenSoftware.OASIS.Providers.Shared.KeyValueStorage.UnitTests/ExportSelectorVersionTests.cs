using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class ExportSelectorVersionTests
{
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
            ?sync?provider.ExportAllDataForAvatarByEmail("none@example.invalid",-1):await provider.ExportAllDataForAvatarByEmailAsync("none@example.invalid",-1)
            :sync?provider.ExportAllDataForAvatarByUsername("none",-1):await provider.ExportAllDataForAvatarByUsernameAsync("none",-1);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);
        Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);Assert.AreEqual(0,backend.ListCalls);Assert.AreEqual(0,backend.PutCalls);
    }
}
