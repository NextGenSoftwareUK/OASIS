using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.IntegrationTests;

public partial class PostgreSqlNativeAcceptanceTests
{
    [DataTestMethod]
    [DataRow(0,false)][DataRow(1,false)][DataRow(2,false)]
    [DataRow(0,true)][DataRow(1,true)][DataRow(2,true)]
    public async Task NativeCredentialSelectorsRejectRotationAmbiguityAndDeletedAuthority(int kind,bool synchronous)
    {
        var first=Guid.NewGuid();var second=Guid.NewGuid();
        await using var fixture=new AvatarFixture($"native-credential-{kind}-{synchronous}",first,second);
        await fixture.Begin();await fixture.Seed(first,fixture.Marker+"-first",includeDetail:false);
        await fixture.Seed(second,fixture.Marker+"-second",includeDetail:false);
        var oldToken=fixture.Marker+"-old";var currentToken=fixture.Marker+"-current";
        void Set(IAvatar avatar,string token){
            if(kind==0)avatar.VerificationToken=token;
            else if(kind==1)avatar.ResetToken=token;
            else avatar.RefreshTokens=new(){new RefreshToken{Token=token}};
        }
        async Task Save(IAvatar avatar){
            fixture.BeforeMutation();var result=synchronous?fixture.Provider.SaveAvatar(avatar):await fixture.Provider.SaveAvatarAsync(avatar);
            Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);await fixture.Observe();
        }
        async Task<OASISResult<IAvatar>> Lookup(string token,int version=0){
            if(kind==0)return synchronous?fixture.Provider.LoadAvatarByVerificationToken(token,version):await fixture.Provider.LoadAvatarByVerificationTokenAsync(token,version);
            if(kind==1)return synchronous?fixture.Provider.LoadAvatarByResetToken(token,version):await fixture.Provider.LoadAvatarByResetTokenAsync(token,version);
            return synchronous?fixture.Provider.LoadAvatarByRefreshToken(token,version):await fixture.Provider.LoadAvatarByRefreshTokenAsync(token,version);
        }
        static void Missing(OASISResult<IAvatar> result){
            Assert.IsTrue(result.IsError);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,result.ErrorCode);
            Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);Assert.AreEqual(0,result.LoadedCount);
        }
        var loaded=await fixture.Provider.LoadAvatarAsync(first);Assert.IsFalse(loaded.IsError,loaded.Message);
        Set(loaded.Result,oldToken);await Save(loaded.Result);
        var original=await Lookup(oldToken);Assert.IsFalse(original.IsError,original.Message);Assert.AreEqual(first,original.Result.Id);
        Set(loaded.Result,currentToken);await Save(loaded.Result);
        Missing(await Lookup(oldToken));
        var historical=await Lookup(oldToken,2);Assert.IsTrue(historical.IsError);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",historical.ErrorCode);
        Assert.IsInstanceOfType<ArgumentOutOfRangeException>(historical.Exception);Assert.IsNull(historical.Result);
        var current=await Lookup(currentToken);Assert.IsFalse(current.IsError,current.Message);Assert.AreEqual(first,current.Result.Id);Assert.AreEqual(1,current.LoadedCount);
        var duplicate=await fixture.Provider.LoadAvatarAsync(second);Assert.IsFalse(duplicate.IsError,duplicate.Message);
        Set(duplicate.Result,currentToken);await Save(duplicate.Result);
        var ambiguous=await Lookup(currentToken);Assert.IsTrue(ambiguous.IsError);Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,ambiguous.ErrorCode);
        Assert.IsNull(ambiguous.Result);Assert.IsFalse(ambiguous.IsLoaded);Assert.AreEqual(0,ambiguous.LoadedCount);
        fixture.BeforeMutation();var removed=await fixture.Provider.DeleteAvatarAsync(second,true);Assert.IsFalse(removed.IsError,removed.Message);await fixture.Observe();
        current=await Lookup(currentToken);Assert.IsFalse(current.IsError,current.Message);Assert.AreEqual(first,current.Result.Id);
        fixture.BeforeMutation();removed=await fixture.Provider.DeleteAvatarAsync(first,true);Assert.IsFalse(removed.IsError,removed.Message);await fixture.Observe();
        Missing(await Lookup(currentToken));Missing(await Lookup(oldToken));
    }
}
