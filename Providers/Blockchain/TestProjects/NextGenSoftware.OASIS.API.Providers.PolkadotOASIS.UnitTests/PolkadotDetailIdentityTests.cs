using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.UnitTests;

[TestClass]
public class PolkadotDetailIdentityTests
{
    private sealed class Provider():PolkadotOASIS("http://127.0.0.1:1")
    {
        public OASISResult<IAvatar> Owner=new();
        public IAvatarDetail Detail=new AvatarDetail();
        public int OwnerCalls,RootCalls,OwnerVersion,RootVersion;
        public Guid RootId;
        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string name,int version=0) {OwnerCalls++;OwnerVersion=version;return Task.FromResult(Owner);}
        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email,int version=0) {OwnerCalls++;OwnerVersion=version;return Task.FromResult(Owner);}
        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id,int version=0) {
            RootCalls++;RootId=id;RootVersion=version;return Task.FromResult(new OASISResult<IAvatarDetail>(Detail){IsLoaded=true});
        }
        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version=0)
            =>throw new AssertFailedException("Identity lookup must use the canonical owner index, not a collection fallback.");
    }
    [DataTestMethod]
    [DataRow(false,false,"duplicate")][DataRow(false,true,"duplicate")][DataRow(true,false,"duplicate")][DataRow(true,true,"duplicate")]
    [DataRow(false,false,"upstream")][DataRow(false,true,"upstream")][DataRow(true,false,"upstream")][DataRow(true,true,"upstream")]
    [DataRow(false,false,"unique")][DataRow(false,true,"unique")][DataRow(true,false,"unique")][DataRow(true,true,"unique")]
    public async Task DetailIdentityResolvesCurrentOwnerThenIndependentDetailVersion(bool email,bool synchronous,string scenario)
    {
        using var provider=new Provider();var cause=new IOException("owned identity lookup failed");
        var id=Guid.NewGuid();provider.Detail=new AvatarDetail{Id=id,Username="profile",Email="profile@example.invalid"};
        provider.Owner=scenario=="unique"?new(new Avatar{Id=id,Username="profile",Email="profile@example.invalid"}):
            new(){IsError=true,ErrorCode=scenario=="duplicate"?OASISResultErrorCodes.AmbiguousIdentity:"OWNED_LOOKUP_FAILURE",Message="owned original",Exception=cause,ErrorCount=3};
        var result=email?synchronous?provider.LoadAvatarDetailByEmail(" PROFILE ",7):await provider.LoadAvatarDetailByEmailAsync(" PROFILE ",7):
            synchronous?provider.LoadAvatarDetailByUsername(" PROFILE ",7):await provider.LoadAvatarDetailByUsernameAsync(" PROFILE ",7);
        Assert.AreEqual(1,provider.OwnerCalls);Assert.AreEqual(0,provider.OwnerVersion);
        if(scenario!="unique") {
            Assert.IsTrue(result.IsError);Assert.AreEqual(provider.Owner.ErrorCode,result.ErrorCode);Assert.AreSame(cause,result.Exception);
            Assert.AreEqual(3,result.ErrorCount);Assert.IsNull(result.Result);Assert.AreEqual(0,provider.RootCalls);
        } else {
            Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreSame(provider.Detail,result.Result);Assert.IsTrue(result.IsLoaded);
            Assert.AreEqual(1,provider.RootCalls);Assert.AreEqual(id,provider.RootId);Assert.AreEqual(7,provider.RootVersion);
        }
    }
}
