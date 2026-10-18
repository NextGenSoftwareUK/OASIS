using System;
using System.IO;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.IPFSOASIS.UnitTests;

public partial class IPFSOASISTests
{
    [Fact]
    public async Task CredentialSelectorsUseRetainedRecordsAndFenceStaleSecondary()
    {
        foreach(var synchronous in new[] {false,true})
        await WithOwnedProvider(async provider=>
        {
            var key="owned-credential-"+Guid.NewGuid().ToString("N");var newer=key+"-new";
            Avatar Create(string suffix,string value,int version) => new() {Id=Guid.NewGuid(),Version=version,IsActive=true,Username=key+suffix,
                AvatarType=new NextGenSoftware.Utilities.EnumValue<AvatarType>(AvatarType.User),
                VerificationToken=value,ResetToken=value,RefreshTokens=new() {new RefreshToken {Token=value}},
                ProviderWallets=new() {{ProviderType.SolanaOASIS,new() {new ProviderWallet {Id=Guid.NewGuid(),PublicKey=value,PrivateKey=value}}}}};
            var first=Create("first",key,1);var duplicate=Create("duplicate",newer,2);
            var storage=(IOASISStorageProvider)provider;
            async Task<OASISResult<IAvatar>> Read(int kind,string value,int version=0) => kind switch {
                0=>synchronous ? storage.LoadAvatarByVerificationToken(value,version):await storage.LoadAvatarByVerificationTokenAsync(value,version),
                1=>synchronous ? storage.LoadAvatarByResetToken(value,version):await storage.LoadAvatarByResetTokenAsync(value,version),
                2=>synchronous ? storage.LoadAvatarByRefreshToken(value,version):await storage.LoadAvatarByRefreshTokenAsync(value,version),
                3=>synchronous ? storage.LoadAvatarByPublicKey(value,version):await storage.LoadAvatarByPublicKeyAsync(value,version),
                _=>synchronous ? storage.LoadAvatarByPrivateKey(value,version):await storage.LoadAvatarByPrivateKeyAsync(value,version)};
            var path=Path.Combine(Path.GetDirectoryName(Environment.GetEnvironmentVariable("OASIS_IPFS_TEST_DNA_PATH"))!,"credential-routing.db");
            Assert.False(File.Exists(path));
            using(var sqlite=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False"))
            {
                Assert.False((await sqlite.ActivateProviderAsync()).IsError);
                Assert.False((await sqlite.SaveAvatarAsync(Create("stale",key,1))).IsError);
                var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
                var manager=new ProviderManager(null,dna) {IsAutoFailOverEnabled=true};manager.RegisterProvider(provider);manager.RegisterProvider(sqlite);
                Assert.False(manager.SetAndReplaceAutoFailOverListForProviders(new[] {new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.IPFSOASIS),
                    new NextGenSoftware.Utilities.EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError);
                var router=new OASISHyperDrive(manager);var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                foreach(var operation in new[] {"LoadAvatarByVerificationToken","LoadAvatarByResetToken","LoadAvatarByRefreshToken","LoadAvatarByPublicKey","LoadAvatarByPrivateKey"})
                {
                    var request=new StorageOperationRequest {Operation=operation,VerificationToken=key,ResetToken=key,RefreshToken=key,PublicKey=key,PrivateKey=key,PreferredProvider=ProviderType.IPFSOASIS};
                    var missing=synchronous ? router.RouteRequest<IAvatar>(request):await router.RouteRequestAsync<IAvatar>(request);
                    Assert.Equal(OASISResultErrorCodes.RecordNotFound,missing.ErrorCode);Assert.Null(missing.Result);
                }
                Assert.Equal(before,manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
                await sqlite.DeActivateProviderAsync();
            }
            File.Delete(path);
            await provider.SaveAvatarToFile(first);await provider.SaveAvatarDetailToFile(new AvatarDetail {Id=first.Id,Version=1,IsActive=true});
            for(var kind=0;kind<5;kind++)
            {
                var found=await Read(kind,key);Assert.False(found.IsError,found.Message);Assert.True(found.IsLoaded);Assert.Equal(first.Id,found.Result.Id);
                Assert.Equal("HYPERDRIVE_PAYLOAD_INVALID",(await Read(kind," ")).ErrorCode);
                Assert.Equal("HYPERDRIVE_PAYLOAD_INVALID",(await Read(kind,key,-1)).ErrorCode);
            }
            first.Version=2;first.VerificationToken=newer;first.ResetToken=newer;first.RefreshTokens[0].Token=newer;
            first.ProviderWallets[ProviderType.SolanaOASIS][0].PublicKey=newer;first.ProviderWallets[ProviderType.SolanaOASIS][0].PrivateKey=newer;
            await provider.SaveAvatarToFile(first);
            await provider.SaveAvatarToFile(duplicate);await provider.SaveAvatarDetailToFile(new AvatarDetail {Id=duplicate.Id,Version=2,IsActive=true});
            for(var kind=0;kind<5;kind++)
            {
                Assert.Equal(OASISResultErrorCodes.RecordNotFound,(await Read(kind,key)).ErrorCode);
                Assert.Equal(first.Id,(await Read(kind,key,1)).Result.Id);
                var ambiguous=await Read(kind,newer);Assert.Equal(OASISResultErrorCodes.AmbiguousIdentity,ambiguous.ErrorCode);Assert.Null(ambiguous.Result);
            }
            var deleted=await provider.DeleteAvatarAsync(duplicate.Id,true);Assert.False(deleted.IsError,deleted.Message);
            for(var kind=0;kind<5;kind++) Assert.Equal(first.Id,(await Read(kind,newer)).Result.Id);
        });
    }
}
