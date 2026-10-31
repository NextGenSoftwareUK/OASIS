using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

public partial class Web3NativeStorageTests
{
    private static async Task VerifyCredentialReplicationAsync(Web3CoreOASISBaseProvider provider,
        SQLLiteDBOASIS.SQLLiteDBOASIS sqlite,string key,bool synchronous)
    {
        var dna=new DNA.OASISDNA {OASIS=new DNA.OASIS {HyperDriveMode=DNA.HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
        var manager=new Core.Managers.ProviderManager(null,dna) {IsAutoReplicationEnabled=true,IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=false};
        manager.RegisterProvider(provider);manager.RegisterProvider(sqlite);
        var types=new[] {provider.ProviderType.Value,ProviderType.SQLLiteDBOASIS};
        Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(types.Select(type=>new NextGenSoftware.Utilities.EnumValue<ProviderType>(type))).IsError);
        var router=new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
        var id=Guid.NewGuid();var walletId=Guid.NewGuid();string? previous=null;
        async Task<OASISResult<IAvatar>> Read(IOASISStorageProvider store,int kind,string value) => kind switch {
            0=>synchronous ? store.LoadAvatarByVerificationToken(value):await store.LoadAvatarByVerificationTokenAsync(value),
            1=>synchronous ? store.LoadAvatarByResetToken(value):await store.LoadAvatarByResetTokenAsync(value),
            2=>synchronous ? store.LoadAvatarByRefreshToken(value):await store.LoadAvatarByRefreshTokenAsync(value),
            3=>synchronous ? store.LoadAvatarByPublicKey(value):await store.LoadAvatarByPublicKeyAsync(value),
            _=>synchronous ? store.LoadAvatarByPrivateKey(value):await store.LoadAvatarByPrivateKeyAsync(value)};
        foreach(var primary in types)
        {
            var credential=key+"-replica-"+primary;
            var avatar=new Avatar {Id=id,Username=key+"-replicated",IsActive=true,
                AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User),VerificationToken=credential,ResetToken=credential,
                RefreshTokens=new() {new RefreshToken {Token=credential}},ProviderWallets=new() {{ProviderType.SolanaOASIS,new() {
                    new ProviderWallet {Id=walletId,PublicKey=credential,PrivateKey=credential}}}}};
            var request=new StorageOperationRequest {Operation="SaveAvatar",Payload=avatar,PreferredProvider=primary};
            var saved=synchronous ? router.RouteRequest<IAvatar>(request):await router.RouteRequestAsync<IAvatar>(request);
            Assert.IsFalse(saved.IsError,saved.Message);Assert.IsFalse(saved.IsWarning,saved.Message);
            var detailRequest=new StorageOperationRequest {Operation="SaveAvatarDetail",Payload=new AvatarDetail {Id=id,IsActive=true},PreferredProvider=primary};
            var detail=synchronous ? router.RouteRequest<IAvatarDetail>(detailRequest):await router.RouteRequestAsync<IAvatarDetail>(detailRequest);
            Assert.IsFalse(detail.IsError,detail.Message);Assert.IsFalse(detail.IsWarning,detail.Message);
            foreach(var store in new IOASISStorageProvider[] {provider,sqlite})
            for(var kind=0;kind<5;kind++)
            {
                var read=await Read(store,kind,credential);Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(id,read.Result?.Id);
                Assert.AreEqual(walletId,read.Result!.ProviderWallets[ProviderType.SolanaOASIS].Single().Id);
                Assert.AreEqual(credential,read.Result.VerificationToken);Assert.AreEqual(credential,read.Result.ResetToken);
                Assert.AreEqual(credential,read.Result.RefreshTokens.Single().Token);
                if(previous!=null) {var old=await Read(store,kind,previous);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,old.ErrorCode,$"{store.ProviderType.Value} selector {kind} rotation");Assert.IsNull(old.Result);Assert.IsFalse(old.IsLoaded);}
            }
            previous=credential;
        }
        // Verify replicated tombstones remove every credential identity from both native stores.
        var deletion=new StorageOperationRequest {Operation="DeleteAvatar",AvatarId=id,SoftDelete=true,PreferredProvider=provider.ProviderType.Value};
        var deleted=synchronous ? router.RouteRequest<bool>(deletion):await router.RouteRequestAsync<bool>(deletion);
        Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsFalse(deleted.IsWarning,deleted.Message);
        foreach(var store in new IOASISStorageProvider[] {provider,sqlite})
        {
            for(var kind=0;kind<5;kind++) {var absent=await Read(store,kind,previous!);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,absent.ErrorCode,$"{store.ProviderType.Value} selector {kind} deletion");Assert.IsNull(absent.Result);Assert.IsFalse(absent.IsLoaded);}
            var removed=await store.DeleteAvatarAsync(id,false);Assert.IsFalse(removed.IsError,removed.Message);
        }
    }
}
