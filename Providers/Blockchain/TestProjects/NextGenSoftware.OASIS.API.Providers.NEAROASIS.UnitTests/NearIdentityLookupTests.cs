using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass]
public class NearIdentityLookupTests
{
    private sealed class Rows(bool duplicate):NEAROASIS("http://127.0.0.1:1","owned","owned","unused")
    {
        public int Reads;public int Version;
        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version=0){
            Reads++;Version=version;return Task.FromResult(new OASISResult<IEnumerable<IAvatar>>(Enumerable.Range(0,duplicate?2:1).Select(_=>(IAvatar)new Avatar{Id=Guid.NewGuid(),Username="owned",Email="owned@example.invalid",Version=version}).ToArray()));
        }
        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version=0){
            Reads++;Version=version;return Task.FromResult(new OASISResult<IEnumerable<IAvatarDetail>>(Enumerable.Range(0,duplicate?2:1).Select(_=>(IAvatarDetail)new AvatarDetail{Id=Guid.NewGuid(),Username="owned",Email="owned@example.invalid",Version=version}).ToArray()));
        }
    }
    [DataTestMethod]
    [DataRow(0,false)][DataRow(0,true)][DataRow(1,false)][DataRow(1,true)]
    [DataRow(2,false)][DataRow(2,true)][DataRow(3,false)][DataRow(3,true)]
    public async Task PublicProfileSelectorsRejectDuplicatesAndPreserveVersion(int kind,bool sync)
    {
        foreach(var duplicate in new[]{false,true}){
            var provider=new Rows(duplicate);
            void Check<T>(OASISResult<T> result){Assert.AreEqual(duplicate,result.IsError);if(duplicate){Assert.IsNull(result.Result);Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,result.ErrorCode);}else{Assert.IsNotNull(result.Result);Assert.IsTrue(result.IsLoaded);}}
            if(kind==0)Check(sync?provider.LoadAvatarByUsername(" OWNED ",3):await provider.LoadAvatarByUsernameAsync(" OWNED ",3));
            if(kind==1)Check(sync?provider.LoadAvatarByEmail(" OWNED@EXAMPLE.INVALID ",3):await provider.LoadAvatarByEmailAsync(" OWNED@EXAMPLE.INVALID ",3));
            if(kind==2)Check(sync?provider.LoadAvatarDetailByUsername(" OWNED ",3):await provider.LoadAvatarDetailByUsernameAsync(" OWNED ",3));
            if(kind==3)Check(sync?provider.LoadAvatarDetailByEmail(" OWNED@EXAMPLE.INVALID ",3):await provider.LoadAvatarDetailByEmailAsync(" OWNED@EXAMPLE.INVALID ",3));
            Assert.AreEqual(1,provider.Reads);Assert.AreEqual(3,provider.Version);Assert.IsFalse(provider.IsProviderActivated);
        }
    }
}
