using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyCredentialReplicationAsync(IOASISStorageProvider mongo,IOASISStorageProvider sqlite,bool synchronous,bool mongoPrimary)
    {
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
        var manager=new ProviderManager(null,dna) {IsAutoReplicationEnabled=true,IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=false};
        manager.RegisterProvider(mongo);manager.RegisterProvider(sqlite);
        manager.SetAndReplaceAutoReplicationListForProviders(new[] {new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError.Should().BeFalse();
        var router=new OASISHyperDrive(manager);
        var id=Guid.NewGuid();var walletId=Guid.NewGuid();var primary=mongoPrimary ? ProviderType.MongoDBOASIS:ProviderType.SQLLiteDBOASIS;
        var key="owned-replicated-credential-"+id.ToString("N");
        async Task Check(IOASISStorageProvider store,string value,bool absent)
        {
            var reads=new[] {
                synchronous ? store.LoadAvatarByVerificationToken(value):await store.LoadAvatarByVerificationTokenAsync(value),
                synchronous ? store.LoadAvatarByResetToken(value):await store.LoadAvatarByResetTokenAsync(value),
                synchronous ? store.LoadAvatarByRefreshToken(value):await store.LoadAvatarByRefreshTokenAsync(value),
                synchronous ? store.LoadAvatarByPublicKey(value):await store.LoadAvatarByPublicKeyAsync(value),
                synchronous ? store.LoadAvatarByPrivateKey(value):await store.LoadAvatarByPrivateKeyAsync(value)};
            foreach(var read in reads)
            {
                if(absent) {read.ErrorCode.Should().Be(OASISResultErrorCodes.RecordNotFound);read.Result.Should().BeNull();read.IsLoaded.Should().BeFalse();}
                else
                {
                    read.IsError.Should().BeFalse(read.Message);read.Result.Id.Should().Be(id);
                    read.Result.VerificationToken.Should().Be(value);read.Result.ResetToken.Should().Be(value);
                    read.Result.RefreshTokens.Single().Token.Should().Be(value);
                    var wallet=read.Result.ProviderWallets[ProviderType.SolanaOASIS].Single();
                    wallet.Id.Should().Be(walletId);wallet.PublicKey.Should().Be(value);wallet.PrivateKey.Should().Be(value);
                }
            }
        }
        string? previous=null;
        foreach(var stage in new[] {1,2})
        {
            var value=key+stage;
            var avatar=new Avatar {Id=id,Username=key,IsActive=true,AvatarType=new EnumValue<AvatarType>(AvatarType.User),
                VerificationToken=value,ResetToken=value,RefreshTokens=new() {new RefreshToken {Token=value}},
                ProviderWallets=new() {{ProviderType.SolanaOASIS,new() {new ProviderWallet {Id=walletId,PublicKey=value,PrivateKey=value}}}}};
            var request=new StorageOperationRequest {Operation="SaveAvatar",Payload=avatar,PreferredProvider=primary};
            var saved=synchronous ? router.RouteRequest<IAvatar>(request):await router.RouteRequestAsync<IAvatar>(request);
            saved.IsError.Should().BeFalse(saved.Message);saved.IsWarning.Should().BeFalse(saved.Message);
            var detailRequest=new StorageOperationRequest {Operation="SaveAvatarDetail",Payload=new AvatarDetail {Id=id,Username=key,IsActive=true},PreferredProvider=primary};
            var detail=synchronous ? router.RouteRequest<IAvatarDetail>(detailRequest):await router.RouteRequestAsync<IAvatarDetail>(detailRequest);
            detail.IsError.Should().BeFalse(detail.Message);detail.IsWarning.Should().BeFalse(detail.Message);
            foreach(var store in new[] {mongo,sqlite}) {await Check(store,value,false);if(previous!=null) await Check(store,previous,true);}
            previous=value;
        }
        var deletion=new StorageOperationRequest {Operation="DeleteAvatar",AvatarId=id,SoftDelete=true,PreferredProvider=primary};
        var deleted=synchronous ? router.RouteRequest<bool>(deletion):await router.RouteRequestAsync<bool>(deletion);
        deleted.IsError.Should().BeFalse(deleted.Message);deleted.IsWarning.Should().BeFalse(deleted.Message);
        foreach(var store in new[] {mongo,sqlite})
        {
            await Check(store,previous!,true);
            var removed=await store.DeleteAvatarAsync(id,false);removed.IsError.Should().BeFalse(removed.Message);
        }
    }
}
