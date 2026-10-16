using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static async Task VerifyWalletKeySelectorsAsync(IOASISStorageProvider mongo,IOASISStorageProvider sqlite,bool synchronous)
    {
        foreach(var store in new[] {mongo,sqlite})
        {
            var key="owned-key-"+Guid.NewGuid().ToString("N");
            Avatar Create(string suffix) => new() {Id=Guid.NewGuid(),Username=key+suffix,Email=key+suffix+"@example.invalid",IsActive=true,
                AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User),
                ProviderWallets=new() {{ProviderType.SolanaOASIS,new() {new ProviderWallet {Id=Guid.NewGuid(),PublicKey=key,PrivateKey=key}}}}};
            var first=Create("first");var second=Create("second");
            async Task<OASISResult<IAvatar>> Read(bool privateKey,string value,int version=0) => privateKey
                ? synchronous ? store.LoadAvatarByPrivateKey(value,version):await store.LoadAvatarByPrivateKeyAsync(value,version)
                : synchronous ? store.LoadAvatarByPublicKey(value,version):await store.LoadAvatarByPublicKeyAsync(value,version);
            try
            {
                (await store.SaveAvatarAsync(first)).IsError.Should().BeFalse();
                (await store.SaveAvatarDetailAsync(new AvatarDetail {Id=first.Id,Username=first.Username,Email=first.Email,IsActive=true})).IsError.Should().BeFalse();
                var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                    HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
                var manager=new Managers.ProviderManager(null,dna) {IsAutoFailOverEnabled=true};
                manager.RegisterProvider(mongo);manager.RegisterProvider(sqlite);
                manager.SetAndReplaceAutoFailOverListForProviders(new[] {new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.MongoDBOASIS),
                    new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError.Should().BeFalse();
                var router=new Managers.OASISHyperDrive.OASISHyperDrive(manager);
                var staleType=store==mongo ? ProviderType.MongoDBOASIS:ProviderType.SQLLiteDBOASIS;
                var primaryType=store==mongo ? ProviderType.SQLLiteDBOASIS:ProviderType.MongoDBOASIS;
                var before=manager.PerformanceMonitor.GetMetrics(staleType)?.TotalRequests ?? 0;
                foreach(var operation in new[] {"LoadAvatarByPublicKey","LoadAvatarByPrivateKey"})
                {
                    var request=new StorageOperationRequest {Operation=operation,PublicKey=key,PrivateKey=key,PreferredProvider=primaryType};
                    var absent=synchronous ? router.RouteRequest<IAvatar>(request):await router.RouteRequestAsync<IAvatar>(request);
                    absent.ErrorCode.Should().Be(OASISResultErrorCodes.RecordNotFound);absent.Result.Should().BeNull();
                    request.Version=-1;
                    var invalid=synchronous ? router.RouteRequest<IAvatar>(request):await router.RouteRequestAsync<IAvatar>(request);
                    invalid.ErrorCode.Should().Be("HYPERDRIVE_VERSION_INVALID");invalid.Result.Should().BeNull();
                }
                (manager.PerformanceMonitor.GetMetrics(staleType)?.TotalRequests ?? 0).Should().Be(before);
                foreach(var privateKey in new[] {false,true})
                {
                    var loaded=await Read(privateKey,key);loaded.IsError.Should().BeFalse(loaded.Message);loaded.IsLoaded.Should().BeTrue();loaded.Result.Id.Should().Be(first.Id);
                    var missing=await Read(privateKey,key+"missing");missing.ErrorCode.Should().Be(OASISResultErrorCodes.RecordNotFound);missing.Result.Should().BeNull();
                    var invalid=await Read(privateKey," ");invalid.IsError.Should().BeTrue();invalid.ErrorCode.Should().Be("HYPERDRIVE_PAYLOAD_INVALID");invalid.Exception.Should().BeAssignableTo<ArgumentException>();
                    var version=await Read(privateKey,key,-1);version.ErrorCode.Should().Be("HYPERDRIVE_PAYLOAD_INVALID");version.Exception.Should().BeAssignableTo<ArgumentException>();
                }
                (await store.SaveAvatarAsync(second)).IsError.Should().BeFalse();
                (await store.SaveAvatarDetailAsync(new AvatarDetail {Id=second.Id,Username=second.Username,Email=second.Email,IsActive=true})).IsError.Should().BeFalse();
                foreach(var privateKey in new[] {false,true})
                {
                    var ambiguous=await Read(privateKey,key);ambiguous.ErrorCode.Should().Be(OASISResultErrorCodes.AmbiguousIdentity);ambiguous.Result.Should().BeNull();
                }
                var deleted=await store.DeleteAvatarAsync(second.Id,true);deleted.IsError.Should().BeFalse(deleted.Message);
                foreach(var privateKey in new[] {false,true}) (await Read(privateKey,key)).Result.Id.Should().Be(first.Id);
            }
            finally {await store.DeleteAvatarAsync(first.Id,false);await store.DeleteAvatarAsync(second.Id,false);}
        }
    }
}
