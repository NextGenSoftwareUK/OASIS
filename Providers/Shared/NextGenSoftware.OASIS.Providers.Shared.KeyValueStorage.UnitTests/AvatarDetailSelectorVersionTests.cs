using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class AvatarDetailSelectorVersionTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task CurrentIdentitySelectsIndependentlyVersionedDetailRoot(bool synchronous,bool email)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend){IsVersionControlEnabled=true};
        var avatar=new Avatar{Id=Guid.NewGuid(),Username="old-name",Email="old@example.invalid"};
        Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);
        avatar.Username="current-name";avatar.Email="current@example.invalid";
        Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);Assert.AreEqual(2,avatar.Version);
        var detail=new AvatarDetail{Id=avatar.Id};
        for(var version=1;version<=3;version++){
            detail.Karma=version*100;detail.XP=version*200;
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(detail)).IsError);Assert.AreEqual(version,detail.Version);
        }
        var before=backend.Items.OrderBy(pair=>pair.Key).ToArray();var writes=backend.PutCalls;
        foreach(var version in new[]{0,1,2,3}){
            var result=email
                ?synchronous?provider.LoadAvatarDetailByEmail(avatar.Email,version):await provider.LoadAvatarDetailByEmailAsync(avatar.Email,version)
                :synchronous?provider.LoadAvatarDetailByUsername(avatar.Username,version):await provider.LoadAvatarDetailByUsernameAsync(avatar.Username,version);
            Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual(avatar.Id,result.Result.Id);
            var selected=version==0?3:version;Assert.AreEqual(selected,result.Result.Version);
            Assert.AreEqual((long)selected*100,result.Result.Karma);Assert.AreEqual((long)selected*200,result.Result.XP);
        }
        // Version is NOT a historical identity-alias selector or an ancestor snapshot.
        var old=email?await provider.LoadAvatarDetailByEmailAsync("old@example.invalid",1):await provider.LoadAvatarDetailByUsernameAsync("old-name",1);
        Assert.IsTrue(old.IsError);Assert.IsNull(old.Result);
        CollectionAssert.AreEqual(before,backend.Items.OrderBy(pair=>pair.Key).ToArray());Assert.AreEqual(writes,backend.PutCalls);
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task InvalidDetailSelectorVersionFailsBeforeIdentityBackendRead(bool synchronous,bool email)
    {
        var backend=new InMemoryBackend{ThrowReads=true};var provider=new TestProvider(backend);
        var result=email
            ?synchronous?provider.LoadAvatarDetailByEmail("none@example.invalid",-1):await provider.LoadAvatarDetailByEmailAsync("none@example.invalid",-1)
            :synchronous?provider.LoadAvatarDetailByUsername("none",-1):await provider.LoadAvatarDetailByUsernameAsync("none",-1);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",result.ErrorCode);
        Assert.IsInstanceOfType<ArgumentOutOfRangeException>(result.Exception);Assert.AreEqual(0,backend.ListCalls);Assert.AreEqual(0,backend.PutCalls);
    }
}
