using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task OwnedNativeCredentialHistoryRequiresExplicitVersionAndPreservesCurrentRotation(bool synchronous)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var raw=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var inspection=raw.AsyncSession();
        Assert.AreEqual(0L,(await(await inspection.RunAsync("MATCH (a) WHERE a:Avatar OR a:AvatarDetail OR a:OasisRecordVersion RETURN count(a) AS count")).SingleAsync())["count"].As<long>());
        var provider=new Provider(uri,"neo4j","oasis-local-test-only"){IsVersionControlEnabled=true};
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var marker=Guid.NewGuid().ToString("N");var id=Guid.NewGuid();
            Avatar Record(string credential)=>new(){Id=id,Username="history-"+marker,IsActive=true,
                VerificationToken=credential,ResetToken=credential,RefreshTokens=new(){new RefreshToken{Token=credential}},
                ProviderWallets=new(){{ProviderType.SolanaOASIS,new(){new ProviderWallet{Id=id,PublicKey=credential,PrivateKey=credential}}}}};
            async Task Check(string credential,int version,bool found)
            {
                var reads=new OASISResult<IAvatar>[] {
                    synchronous?provider.LoadAvatarByVerificationToken(credential,version):await provider.LoadAvatarByVerificationTokenAsync(credential,version),
                    synchronous?provider.LoadAvatarByResetToken(credential,version):await provider.LoadAvatarByResetTokenAsync(credential,version),
                    synchronous?provider.LoadAvatarByRefreshToken(credential,version):await provider.LoadAvatarByRefreshTokenAsync(credential,version),
                    synchronous?provider.LoadAvatarByPublicKey(credential,version):await provider.LoadAvatarByPublicKeyAsync(credential,version),
                    synchronous?provider.LoadAvatarByPrivateKey(credential,version):await provider.LoadAvatarByPrivateKeyAsync(credential,version)};
                foreach(var read in reads)
                {
                    if(found){Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(id,read.Result.Id);Assert.AreEqual(version==0?2:version,read.Result.Version);}
                    else{Assert.IsTrue(read.IsError);Assert.IsNull(read.Result);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,read.ErrorCode);}
                }
            }
            var old="old-"+marker;var current="current-"+marker;
            Assert.IsFalse((await provider.SaveAvatarAsync(Record(old))).IsError);
            Assert.IsFalse((await provider.SaveAvatarAsync(Record(current))).IsError);
            await Check(old,1,true);await Check(old,0,false);await Check(current,0,true);
            await Check(current,1,false);await Check(old,99,false);
            Assert.IsFalse((await provider.DeleteAvatarAsync(id,true)).IsError);
            // Current lookup must not recover credentials from an archived live record.
            foreach(var read in new[]{await provider.LoadAvatarByVerificationTokenAsync(current),await provider.LoadAvatarByResetTokenAsync(current),
                await provider.LoadAvatarByRefreshTokenAsync(current),await provider.LoadAvatarByPublicKeyAsync(current),await provider.LoadAvatarByPrivateKeyAsync(current)})
            {Assert.IsTrue(read.IsError);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,read.ErrorCode);Assert.IsNull(read.Result);}
            Assert.IsFalse((await provider.DeleteAvatarAsync(id,false)).IsError);
            await Check(old,1,false);
            Assert.AreEqual(0L,(await(await inspection.RunAsync("MATCH (a) WHERE a:Avatar OR a:AvatarDetail OR a:OasisRecordVersion RETURN count(a) AS count")).SingleAsync())["count"].As<long>());
        }
        finally{await provider.DeActivateProviderAsync();}
    }
}
