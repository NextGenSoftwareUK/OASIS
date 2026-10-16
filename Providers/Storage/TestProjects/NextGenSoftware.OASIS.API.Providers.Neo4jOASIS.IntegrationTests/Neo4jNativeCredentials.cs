using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Common;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeCredentialsPreserveIdentityRejectAmbiguityAndExcludeDeleted(bool synchronous)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var session=raw.AsyncSession();
        Assert.AreEqual(0L,(await (await session.RunAsync("MATCH (n) WHERE n:Avatar OR n:AvatarDetail RETURN count(n) AS count")).SingleAsync())["count"].As<long>());
        var neo=new Provider(uri,"neo4j","oasis-local-test-only");IOASISStorageProvider store=neo;
        var key="owned-credential-"+Guid.NewGuid().ToString("N");
        Avatar Create()=>new() {Id=Guid.NewGuid(),Username=Guid.NewGuid().ToString("N"),IsActive=true,VerificationToken=key,ResetToken=key,
            RefreshTokens=new() {new RefreshToken {Token=key}},ProviderWallets=new() {{ProviderType.SolanaOASIS,new() {new ProviderWallet {Id=Guid.NewGuid(),PublicKey=key,PrivateKey=key}}}}};
        var first=Create();var second=Create();
        async Task<OASISResult<IAvatar>> Read(int kind,string value,int version=0)=>kind switch {
            0=>synchronous ? store.LoadAvatarByVerificationToken(value,version):await store.LoadAvatarByVerificationTokenAsync(value,version),
            1=>synchronous ? store.LoadAvatarByResetToken(value,version):await store.LoadAvatarByResetTokenAsync(value,version),
            2=>synchronous ? store.LoadAvatarByRefreshToken(value,version):await store.LoadAvatarByRefreshTokenAsync(value,version),
            3=>synchronous ? store.LoadAvatarByPublicKey(value,version):await store.LoadAvatarByPublicKeyAsync(value,version),
            _=>synchronous ? store.LoadAvatarByPrivateKey(value,version):await store.LoadAvatarByPrivateKeyAsync(value,version)};
        try
        {
            for(var kind=0;kind<5;kind++)
            {
                Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",(await Read(kind," ")).ErrorCode);
                var history=await Read(kind,key,1);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",history.ErrorCode);Assert.IsInstanceOfType(history.Exception,typeof(NotSupportedException));
            }
            Assert.IsFalse((await neo.ActivateProviderAsync()).IsError);
            foreach(var avatar in new[] {first,second})
            {
                Assert.IsFalse((await neo.SaveAvatarAsync(avatar)).IsError);
                Assert.IsFalse((await neo.SaveAvatarDetailAsync(new AvatarDetail {Id=avatar.Id,IsActive=true})).IsError);
                for(var kind=0;kind<5;kind++)
                {
                    var read=await Read(kind,key);
                    if(avatar==first) {Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(first.Id,read.Result.Id);Assert.AreEqual(first.ProviderWallets[ProviderType.SolanaOASIS][0].Id,read.Result.ProviderWallets[ProviderType.SolanaOASIS][0].Id);}
                    else {Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,read.ErrorCode);Assert.IsNull(read.Result);}
                    Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await Read(kind,key+"missing")).ErrorCode);
                }
            }
            Assert.IsFalse((await neo.DeleteAvatarAsync(second.Id,true)).IsError);
            for(var kind=0;kind<5;kind++) Assert.AreEqual(first.Id,(await Read(kind,key)).Result.Id);
            Assert.IsFalse((await neo.DeleteAvatarAsync(first.Id,true)).IsError);
            for(var kind=0;kind<5;kind++) {var absent=await Read(kind,key);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,absent.ErrorCode);Assert.IsNull(absent.Result);}
            foreach(var avatar in new[] {first,second}) Assert.IsFalse((await neo.DeleteAvatarAsync(avatar.Id,false)).IsError);
            await VerifyCredentialPoliciesAsync(neo,key,synchronous);
        }
        finally {await neo.DeActivateProviderAsync();}
    }
}
