using System.Text.Json;
using Microsoft.Azure.Cosmos;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Infrastructure;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.IntegrationTests;

public partial class AzureCosmosDBOASISIntegrationTests
{
    [TestMethod]
    public async Task NativeCredentialsUseCurrentUniqueAuthorityWhileHistoryRemainsReadable()
    {
        var ids=new[]{Guid.NewGuid(),Guid.NewGuid()};var name="owned-credentials-"+ids[0].ToString("N");
        const string journal="C:/Source/OASIS/TestResults/CosmosDBCurrent/credentials.pending.json";Assert.IsFalse(File.Exists(journal));
        var native=new CosmosDbClient(_client.GetContainer(DatabaseName,"avatarItems"));
        foreach(var id in ids)Assert.IsNull(await native.ReadDocumentAsync(id.ToString(),new PartitionKey(id.ToString())));
        using(var stream=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
            await stream.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(new{ids,name,state="before-owned-credential-fixture"}));stream.Flush(true);}
        void Credentials(Avatar avatar,string suffix){
            avatar.VerificationToken=name+"-verify-"+suffix;avatar.ResetToken=name+"-reset-"+suffix;
            avatar.RefreshTokens=new(){new RefreshToken{Token=name+"-refresh-"+suffix,Expires=DateTime.UtcNow.AddDays(1)}};
            avatar.ProviderWallets=new(){[ProviderType.AzureCosmosDBOASIS]=new List<IProviderWallet>{new ProviderWallet{PublicKey=name+"-public-"+suffix,PrivateKey=name+"-private-"+suffix}}};
        }
        IOASISStorageProvider storage=_provider;
        async Task Check(string suffix,Guid? identity,string? error=null){
            var results=new[]{await storage.LoadAvatarByVerificationTokenAsync(name+"-verify-"+suffix),storage.LoadAvatarByVerificationToken(name+"-verify-"+suffix),
                await storage.LoadAvatarByResetTokenAsync(name+"-reset-"+suffix),storage.LoadAvatarByResetToken(name+"-reset-"+suffix),
                await storage.LoadAvatarByRefreshTokenAsync(name+"-refresh-"+suffix),storage.LoadAvatarByRefreshToken(name+"-refresh-"+suffix),
                await storage.LoadAvatarByPublicKeyAsync(name+"-public-"+suffix),storage.LoadAvatarByPublicKey(name+"-public-"+suffix),
                await storage.LoadAvatarByPrivateKeyAsync(name+"-private-"+suffix),storage.LoadAvatarByPrivateKey(name+"-private-"+suffix)};
            foreach(var result in results){Assert.AreEqual(!identity.HasValue,result.IsError,result.Exception?.GetType().Name);
                if(identity.HasValue)Assert.AreEqual(identity.Value,result.Result.Id);else {Assert.IsNull(result.Result);Assert.AreEqual(error,result.ErrorCode);}}
        }
        _provider.IsVersionControlEnabled=true;
        var first=new Avatar{Id=ids[0],Username=name,IsActive=true};Credentials(first,"old");Assert.IsFalse((await _provider.SaveAvatarAsync(first)).IsError);await Check("old",first.Id);
        Credentials(first,"new");Assert.IsFalse((await _provider.SaveAvatarAsync(first)).IsError);await Check("old",null,OASISResultErrorCodes.RecordNotFound);await Check("new",first.Id);
        var historical=await storage.LoadAvatarAsync(first.Id,1);Assert.IsFalse(historical.IsError);Assert.AreEqual(name+"-verify-old",historical.Result.VerificationToken);
        Assert.AreEqual(name+"-public-old",historical.Result.ProviderWallets[ProviderType.AzureCosmosDBOASIS][0].PublicKey);
        var forbidden=await storage.LoadAvatarByPublicKeyAsync(name+"-public-old",1);Assert.IsTrue(forbidden.IsError);Assert.IsNull(forbidden.Result);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",forbidden.ErrorCode);
        var second=new Avatar{Id=ids[1],Username=name+"-second",IsActive=true};Credentials(second,"new");Assert.IsFalse((await _provider.SaveAvatarAsync(second)).IsError);
        await Check("new",null,OASISResultErrorCodes.AmbiguousIdentity);Assert.IsFalse((await _provider.DeleteAvatarAsync(second.Id,false)).IsError);
        Assert.IsFalse((await _provider.DeleteAvatarAsync(first.Id)).IsError);await Check("new",null,OASISResultErrorCodes.RecordNotFound);
        var cleanup=await _provider.DeleteAvatarAsync(first.Id,false);Assert.IsFalse(cleanup.IsError,cleanup.Exception?.ToString());
        foreach(var id in ids)Assert.IsNull(await native.ReadDocumentAsync(id.ToString(),new PartitionKey(id.ToString())));File.Delete(journal);
    }
}
