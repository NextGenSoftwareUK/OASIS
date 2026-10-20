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
        public OASISResult<IEnumerable<IAvatarDetail>> Rows=new();public int Calls,Version;
        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version=0) {
            Calls++;Version=version;return Task.FromResult(Rows);
        }
    }
    [DataTestMethod]
    [DataRow(false,false,"duplicate")][DataRow(false,true,"duplicate")][DataRow(true,false,"duplicate")][DataRow(true,true,"duplicate")]
    [DataRow(false,false,"upstream")][DataRow(false,true,"upstream")][DataRow(true,false,"upstream")][DataRow(true,true,"upstream")]
    [DataRow(false,false,"unique")][DataRow(false,true,"unique")][DataRow(true,false,"unique")][DataRow(true,true,"unique")]
    public async Task DetailIdentityUsesUniqueTrimmedSelectionAndRetainsOriginalFailure(bool email,bool synchronous,string scenario)
    {
        using var provider=new Provider();var cause=new IOException("owned identity collection failed");
        var row=new AvatarDetail{Id=Guid.NewGuid(),Username=" profile ",Email=" profile "};
        provider.Rows=scenario=="upstream"?new(){IsError=true,ErrorCode="OWNED_COLLECTION_FAILURE",Message="owned original",Exception=cause,ErrorCount=3}:
            new(new IAvatarDetail[]{row}.Concat(scenario=="duplicate"?new[]{new AvatarDetail{Id=Guid.NewGuid(),Username="PROFILE",Email="PROFILE"}}:Array.Empty<IAvatarDetail>()));
        var result=email?synchronous?provider.LoadAvatarDetailByEmail(" PROFILE ",7):await provider.LoadAvatarDetailByEmailAsync(" PROFILE ",7):
            synchronous?provider.LoadAvatarDetailByUsername(" PROFILE ",7):await provider.LoadAvatarDetailByUsernameAsync(" PROFILE ",7);
        Assert.AreEqual(1,provider.Calls);Assert.AreEqual(7,provider.Version);
        if(scenario=="duplicate") {Assert.IsTrue(result.IsError);Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,result.ErrorCode);Assert.IsNull(result.Result);}
        else if(scenario=="upstream") {Assert.IsTrue(result.IsError);Assert.AreEqual("OWNED_COLLECTION_FAILURE",result.ErrorCode);Assert.AreSame(cause,result.Exception);Assert.AreEqual(3,result.ErrorCount);Assert.IsNull(result.Result);}
        else {Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.AreSame(row,result.Result);Assert.IsTrue(result.IsLoaded);}
    }
}
