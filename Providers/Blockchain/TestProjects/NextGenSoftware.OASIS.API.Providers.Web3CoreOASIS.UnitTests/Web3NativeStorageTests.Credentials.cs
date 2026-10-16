using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

public partial class Web3NativeStorageTests
{
    private static async Task VerifyNativeCredentialSelectorsAsync(Web3CoreOASISBaseProvider provider)
    {
        foreach(var synchronous in new[] {false,true})
        {
            var storage=(IOASISStorageProvider)provider;var key="owned-credential-"+Guid.NewGuid().ToString("N");
            Avatar Create(string suffix) => new() {Id=Guid.NewGuid(),Username=key+suffix,IsActive=true,VerificationToken=key,ResetToken=key,
                AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User),
                RefreshTokens=new() {new RefreshToken {Token=key}},ProviderWallets=new() {{ProviderType.SolanaOASIS,new() {
                    new ProviderWallet {Id=Guid.NewGuid(),PublicKey=key,PrivateKey=key}}}}};
            var first=Create("first");var second=Create("second");
            async Task<OASISResult<IAvatar>> Read(int kind,string value,int version=0) => kind switch {
                0=>synchronous ? storage.LoadAvatarByVerificationToken(value,version):await storage.LoadAvatarByVerificationTokenAsync(value,version),
                1=>synchronous ? storage.LoadAvatarByResetToken(value,version):await storage.LoadAvatarByResetTokenAsync(value,version),
                2=>synchronous ? storage.LoadAvatarByRefreshToken(value,version):await storage.LoadAvatarByRefreshTokenAsync(value,version),
                3=>synchronous ? storage.LoadAvatarByPublicKey(value,version):await storage.LoadAvatarByPublicKeyAsync(value,version),
                _=>synchronous ? storage.LoadAvatarByPrivateKey(value,version):await storage.LoadAvatarByPrivateKeyAsync(value,version)};
            var path=Path.GetFullPath("TestResults/Web3CredentialRouting/owned.db");
            Assert.IsFalse(File.Exists(path),"Retained credential fixture requires inspection; refusing overwrite.");Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            using(var sqlite=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False"))
            {
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            var stale=Create("stale-secondary");
            Assert.IsFalse((await sqlite.SaveAvatarAsync(stale)).IsError);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var manager=new Core.Managers.ProviderManager(null,dna) {IsAutoFailOverEnabled=true};manager.RegisterProvider(provider);manager.RegisterProvider(sqlite);
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(new[] {new NextGenSoftware.Utilities.EnumValue<ProviderType>(provider.ProviderType.Value),
                new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError);
            var router=new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
            async Task NoStaleSecondary(string code,int version=0)
            {
                var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                foreach(var operation in new[] {"LoadAvatarByVerificationToken","LoadAvatarByResetToken","LoadAvatarByRefreshToken","LoadAvatarByPublicKey","LoadAvatarByPrivateKey"})
                {
                    var request=new Core.StorageOperationRequest {Operation=operation,VerificationToken=key,ResetToken=key,RefreshToken=key,PublicKey=key,PrivateKey=key,
                        Version=version,PreferredProvider=provider.ProviderType.Value};
                    var result=synchronous ? router.RouteRequest<IAvatar>(request):await router.RouteRequestAsync<IAvatar>(request);
                    Assert.AreEqual(code,result.ErrorCode);Assert.IsNull(result.Result);
                    Assert.AreEqual(code!=OASISResultErrorCodes.RecordNotFound,result.IsError);
                    if(version==1) Assert.IsInstanceOfType(result.Exception,typeof(NotSupportedException));
                }
                Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
            }
            await NoStaleSecondary(OASISResultErrorCodes.RecordNotFound);
            await NoStaleSecondary("HYPERDRIVE_PAYLOAD_INVALID",1);
            Assert.IsFalse((await provider.SaveAvatarAsync(first)).IsError);
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail {Id=first.Id,IsActive=true})).IsError);
            await VerifyCredentialBalanceAndOutageAsync(provider,sqlite,key,first.Id,stale.Id,synchronous);
            for(var kind=0;kind<5;kind++)
            {
                var found=await Read(kind,key);Assert.IsFalse(found.IsError,found.Message);Assert.IsTrue(found.IsLoaded);Assert.AreEqual(first.Id,found.Result.Id);
                Assert.AreEqual(first.ProviderWallets[ProviderType.SolanaOASIS][0].Id,found.Result.ProviderWallets[ProviderType.SolanaOASIS][0].Id);
                Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await Read(kind,key+"missing")).ErrorCode);
                Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",(await Read(kind," ")).ErrorCode);
                var history=await Read(kind,key,1);Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",history.ErrorCode);Assert.IsInstanceOfType(history.Exception,typeof(NotSupportedException));
            }
            Assert.IsFalse((await provider.SaveAvatarAsync(second)).IsError);
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail {Id=second.Id,IsActive=true})).IsError);
            await NoStaleSecondary(OASISResultErrorCodes.AmbiguousIdentity);
            for(var kind=0;kind<5;kind++) {var duplicate=await Read(kind,key);Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,duplicate.ErrorCode);Assert.IsNull(duplicate.Result);}
            var deleted=await provider.DeleteAvatarAsync(second.Id,true);Assert.IsFalse(deleted.IsError,deleted.Message);
            for(var kind=0;kind<5;kind++) Assert.AreEqual(first.Id,(await Read(kind,key)).Result.Id);
            Assert.IsFalse((await provider.DeleteAvatarAsync(first.Id,false)).IsError);
            Assert.IsFalse((await provider.DeleteAvatarAsync(second.Id,false)).IsError);
            await NoStaleSecondary(OASISResultErrorCodes.RecordNotFound);
            await sqlite.DeActivateProviderAsync();
            }
            File.Delete(path);
        }
    }
}
