using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.UnitTests;

[TestClass]
public class PostgreSqlIdentityTests
{
    private sealed class Provider:PostgreSQLOASIS
    {
        public Provider():base("UnsupportedConnectionKeyword=true"){}
        public int Calls;
        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version=0)
        {Calls++;return Task.FromResult(new OASISResult<IEnumerable<IAvatar>>{Result=new IAvatar[]{new Avatar{Id=Guid.NewGuid(),Username="owned",Email="owned"},new Avatar{Id=Guid.NewGuid(),Username="OWNED",Email="OWNED"}}});}
        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version=0)
        {Calls++;return Task.FromResult(new OASISResult<IEnumerable<IAvatarDetail>>{Result=new IAvatarDetail[]{new AvatarDetail{Id=Guid.NewGuid(),Username="owned",Email="owned"},new AvatarDetail{Id=Guid.NewGuid(),Username="OWNED",Email="OWNED"}}});}
    }

    [DataTestMethod]
    [DataRow(false,false,false)][DataRow(false,false,true)][DataRow(false,true,false)][DataRow(false,true,true)]
    [DataRow(true,false,false)][DataRow(true,false,true)][DataRow(true,true,false)][DataRow(true,true,true)]
    public async Task DuplicateIdentityCannotSelectAnArbitraryProfile(bool detail,bool email,bool synchronous)
    {
        var provider=new Provider();
        void Check<T>(OASISResult<T> result){Assert.IsTrue(result.IsError);Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,result.ErrorCode);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);}
        if(detail){Check(email?(synchronous?provider.LoadAvatarDetailByEmail(" owned "):await provider.LoadAvatarDetailByEmailAsync(" owned ")):(synchronous?provider.LoadAvatarDetailByUsername(" owned "):await provider.LoadAvatarDetailByUsernameAsync(" owned ")));}
        else{Check(email?(synchronous?provider.LoadAvatarByEmail(" owned "):await provider.LoadAvatarByEmailAsync(" owned ")):(synchronous?provider.LoadAvatarByUsername(" owned "):await provider.LoadAvatarByUsernameAsync(" owned ")));}
        Assert.AreEqual(1,provider.Calls,"Identity selection must use the existing provider collection owner exactly once.");
    }
}
