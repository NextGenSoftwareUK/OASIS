using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeCredentialSelectorsRejectAmbiguityAndStaleSecondary(bool synchronous)
    {
        var (provider,_)=await CreateActivatedProviderAsync();var storage=(IOASISStorageProvider)provider;
        var key="owned-credential-"+Guid.NewGuid().ToString("N");
        Avatar Create(string suffix) => new() {Id=Guid.NewGuid(),Username=key+suffix,IsActive=true,
            VerificationToken=key,ResetToken=key,RefreshTokens=new() {new RefreshToken {Token=key}},
            AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User),
            ProviderWallets=new() {{ProviderType.SolanaOASIS,new() {new ProviderWallet {Id=Guid.NewGuid(),PublicKey=key,PrivateKey=key}}}}};
        async Task<OASISResult<IAvatar>> Read(int kind,string value,int version=0) => kind switch {
            0=>synchronous ? storage.LoadAvatarByVerificationToken(value,version):await storage.LoadAvatarByVerificationTokenAsync(value,version),
            1=>synchronous ? storage.LoadAvatarByResetToken(value,version):await storage.LoadAvatarByResetTokenAsync(value,version),
            2=>synchronous ? storage.LoadAvatarByRefreshToken(value,version):await storage.LoadAvatarByRefreshTokenAsync(value,version),
            3=>synchronous ? storage.LoadAvatarByPublicKey(value,version):await storage.LoadAvatarByPublicKeyAsync(value,version),
            _=>synchronous ? storage.LoadAvatarByPrivateKey(value,version):await storage.LoadAvatarByPrivateKeyAsync(value,version)};
        var path=Path.GetFullPath("TestResults/EthereumCredentialRouting/owned.db");Assert.IsFalse(File.Exists(path));Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            using(var sqlite=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False"))
            {
                Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);Assert.IsFalse((await sqlite.SaveAvatarAsync(Create("stale"))).IsError);
                var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
                var manager=new ProviderManager(null,dna) {IsAutoFailOverEnabled=true};manager.RegisterProvider(provider);manager.RegisterProvider(sqlite);
                Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(new[] {new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.EthereumOASIS),
                    new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError);
                var router=new OASISHyperDrive(manager);var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                foreach(var operation in new[] {"LoadAvatarByVerificationToken","LoadAvatarByResetToken","LoadAvatarByRefreshToken","LoadAvatarByPublicKey","LoadAvatarByPrivateKey"})
                {
                    var request=new StorageOperationRequest {Operation=operation,VerificationToken=key,ResetToken=key,RefreshToken=key,PublicKey=key,PrivateKey=key,PreferredProvider=ProviderType.EthereumOASIS};
                    var absent=synchronous ? router.RouteRequest<IAvatar>(request):await router.RouteRequestAsync<IAvatar>(request);
                    Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,absent.ErrorCode);Assert.IsNull(absent.Result);
                    request.Version=1;var invalid=synchronous ? router.RouteRequest<IAvatar>(request):await router.RouteRequestAsync<IAvatar>(request);
                    Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",invalid.ErrorCode);Assert.IsInstanceOfType(invalid.Exception,typeof(NotSupportedException));
                }
                Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
                await VerifyCredentialPoliciesAsync(provider,sqlite,key+"-policy",synchronous);
                await sqlite.DeActivateProviderAsync();
            }
            File.Delete(path);
            var first=Create("first");var duplicate=Create("duplicate");
            Assert.IsFalse((await provider.SaveAvatarAsync(first)).IsError);
            for(var kind=0;kind<5;kind++)
            {
                var found=await Read(kind,key);Assert.IsFalse(found.IsError,found.Message);Assert.IsTrue(found.IsLoaded);Assert.AreEqual(first.Id,found.Result.Id);
                Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",(await Read(kind," ")).ErrorCode);
                Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",(await Read(kind,key,-1)).ErrorCode);
            }
            Assert.IsFalse((await provider.SaveAvatarAsync(duplicate)).IsError);
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail {Id=duplicate.Id,IsActive=true})).IsError);
            for(var kind=0;kind<5;kind++) {var result=await Read(kind,key);Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,result.ErrorCode);Assert.IsNull(result.Result);}
            var deleted=await provider.DeleteAvatarAsync(duplicate.Id,true);Assert.IsFalse(deleted.IsError,deleted.Message);
            for(var kind=0;kind<5;kind++) Assert.AreEqual(first.Id,(await Read(kind,key)).Result.Id);
        }
        finally {await provider.DeActivateProviderAsync();}
    }
}
