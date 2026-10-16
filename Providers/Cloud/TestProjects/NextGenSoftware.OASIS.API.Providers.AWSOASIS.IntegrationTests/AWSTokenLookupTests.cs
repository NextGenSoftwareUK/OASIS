using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public partial class AWSOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeTokenLookupsResolveCanonicalIdentityAndRejectAmbiguity(bool synchronous)
    {
        var token=Guid.NewGuid().ToString("N");
        var first=new Avatar {Id=Guid.NewGuid(),Username="owned-token-"+token,Email=token+"@example.invalid",
            VerificationToken=token,ResetToken=token,RefreshTokens=new() {new RefreshToken {Token=token}}};
        var second=new Avatar {Id=Guid.NewGuid(),Username="owned-token-duplicate-"+token,Email="duplicate-"+token+"@example.invalid",
            VerificationToken=token,ResetToken=token,RefreshTokens=new() {new RefreshToken {Token=token}}};
        async Task<OASISResult<IAvatar>> Read(int kind,string value,int version=0) => kind switch {
            0 => synchronous ? _provider.LoadAvatarByVerificationToken(value,version):await _provider.LoadAvatarByVerificationTokenAsync(value,version),
            1 => synchronous ? _provider.LoadAvatarByResetToken(value,version):await _provider.LoadAvatarByResetTokenAsync(value,version),
            _ => synchronous ? _provider.LoadAvatarByRefreshToken(value,version):await _provider.LoadAvatarByRefreshTokenAsync(value,version)};
        try
        {
            Assert.IsFalse((await _provider.SaveAvatarAsync(first)).IsError);
            for(var kind=0;kind<3;kind++)
            {
                var found=await Read(kind,token);
                Assert.IsFalse(found.IsError,found.Message);Assert.IsTrue(found.IsLoaded);Assert.AreEqual(first.Id,found.Result?.Id);
                var missing=await Read(kind,token+"missing");
                Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,missing.ErrorCode);Assert.IsNull(missing.Result);
                var invalid=await Read(kind," ");
                Assert.IsTrue(invalid.IsError);Assert.IsInstanceOfType(invalid.Exception,typeof(ArgumentException));
                var unsupported=await Read(kind,token,1);
                Assert.IsTrue(unsupported.IsError);Assert.IsInstanceOfType(unsupported.Exception,typeof(NotSupportedException));
            }
            Assert.IsFalse((await _provider.SaveAvatarAsync(second)).IsError);
            for(var kind=0;kind<3;kind++)
            {
                var duplicate=await Read(kind,token);
                Assert.IsTrue(duplicate.IsError);Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,duplicate.ErrorCode);
                Assert.IsNull(duplicate.Result);
            }
            Assert.IsFalse((await _provider.DeleteAvatarAsync(second.Id,softDelete:true)).IsError);
            for(var kind=0;kind<3;kind++) Assert.AreEqual(first.Id,(await Read(kind,token)).Result?.Id);
        }
        finally
        {
            await Client.DeleteItemAsync(new Amazon.DynamoDBv2.Model.DeleteItemRequest {TableName="OasisAvatars",Key=Key(first.Id)});
            await Client.DeleteItemAsync(new Amazon.DynamoDBv2.Model.DeleteItemRequest {TableName="OasisAvatars",Key=Key(second.Id)});
        }
    }
}
