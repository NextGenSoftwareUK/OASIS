using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

public partial class AzureStorageOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(0,false)] [DataRow(0,true)]
    [DataRow(1,false)] [DataRow(1,true)]
    [DataRow(2,false)] [DataRow(2,true)]
    [DataRow(3,false)] [DataRow(3,true)]
    public async Task V2MalformedBlobsDoNotReadStaleSecondary(int kind,bool synchronous)
    {
        var path=Path.GetFullPath("TestResults/AzureRoutingFiles/lifecycle.db");
        if(File.Exists(path)) Assert.Fail("Retained SQLite Azure evidence exists; refusing overwrite.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var sqlite=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False");
        var id=Guid.NewGuid();var container=kind==0 ? _containers[1]:kind==1 ? _containers[2]:_containers[0];
        var blob=_service.GetBlobContainerClient(container).GetBlobClient(id.ToString());var uploaded=false;var verified=false;
        try
        {
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            if(kind==0) Assert.IsFalse((await sqlite.SaveAvatarAsync(new Avatar {Id=id,Username="owned-stale",IsActive=true,
                ProviderWallets=new() {{Core.Enums.ProviderType.SolanaOASIS,new() {new Core.Objects.ProviderWallet {Id=Guid.NewGuid(),PublicKey="owned-stale",PrivateKey="owned-stale"}}}},
                VerificationToken="owned-stale",ResetToken="owned-stale",RefreshTokens=new() {new Core.Objects.RefreshToken {Token="owned-stale"}},
                AvatarType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)})).IsError);
            else if(kind==1) Assert.IsFalse((await sqlite.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username="owned-stale",IsActive=true,XP=999})).IsError);
            else Assert.IsFalse((await sqlite.SaveHolonAsync(new Holon {Id=id,Name="owned-stale",IsActive=true},saveChildren:false)).IsError);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var manager=new Core.Managers.ProviderManager(null,dna) {IsAutoFailOverEnabled=true};
            manager.RegisterProvider(_provider);manager.RegisterProvider(sqlite);
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(new[] {
                new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.AzureStorageOASIS),
                new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS)}).IsError);
            var router=new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
            var tokenOperations=new[] {"LoadAvatarByVerificationToken","LoadAvatarByResetToken","LoadAvatarByRefreshToken","LoadAvatarByPublicKey","LoadAvatarByPrivateKey"};
            Core.StorageOperationRequest TokenRequest(string operation,int version=0) => new() {
                Operation=operation,VerificationToken="owned-stale",ResetToken="owned-stale",RefreshToken="owned-stale",PublicKey="owned-stale",PrivateKey="owned-stale",
                Version=version,PreferredProvider=Core.Enums.ProviderType.AzureStorageOASIS};
            if(kind==0)
            {
                var secondaryBefore=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                foreach(var operation in tokenOperations)
                {
                    var missing=synchronous ? router.RouteRequest<IAvatar>(TokenRequest(operation)):await router.RouteRequestAsync<IAvatar>(TokenRequest(operation));
                    Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,missing.ErrorCode);Assert.IsNull(missing.Result);
                    var invalid=synchronous ? router.RouteRequest<IAvatar>(TokenRequest(operation,-1)):await router.RouteRequestAsync<IAvatar>(TokenRequest(operation,-1));
                    Assert.AreEqual("HYPERDRIVE_VERSION_INVALID",invalid.ErrorCode);Assert.IsNull(invalid.Result);
                }
                Assert.AreEqual(secondaryBefore,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
            }
            var json=kind==3 ? NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Serialize(
                new Holon {Id=id,CustomKey="different-owned-key",Name="owned-key-mismatch",IsActive=true}):"{ invalid canonical JSON";
            Assert.AreEqual(201,(await blob.UploadAsync(BinaryData.FromString(json))).GetRawResponse().Status);uploaded=true;
            void Corrupt<T>(OASISResult<T> result)
            {
                Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
                Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
                if(kind!=3) Assert.IsInstanceOfType(result.Exception.InnerException,typeof(Newtonsoft.Json.JsonException));
            }
            var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
            foreach(var collection in new[] {false,true})
            {
                var request=new Core.StorageOperationRequest {Operation=kind==0 ? collection ? "LoadAllAvatars":"LoadAvatar":kind==1
                    ? collection ? "LoadAllAvatarDetails":"LoadAvatarDetail":collection ? "LoadAllHolons":"LoadHolon",
                    AvatarId=id,HolonId=id,LoadChildren=false,PreferredProvider=Core.Enums.ProviderType.AzureStorageOASIS};
                if(kind==0)
                {
                    if(collection) Corrupt(synchronous ? router.RouteRequest<System.Collections.Generic.IEnumerable<IAvatar>>(request):await router.RouteRequestAsync<System.Collections.Generic.IEnumerable<IAvatar>>(request));
                    else Corrupt(synchronous ? router.RouteRequest<IAvatar>(request):await router.RouteRequestAsync<IAvatar>(request));
                }
                else if(kind==1)
                {
                    if(collection) Corrupt(synchronous ? router.RouteRequest<System.Collections.Generic.IEnumerable<IAvatarDetail>>(request):await router.RouteRequestAsync<System.Collections.Generic.IEnumerable<IAvatarDetail>>(request));
                    else Corrupt(synchronous ? router.RouteRequest<IAvatarDetail>(request):await router.RouteRequestAsync<IAvatarDetail>(request));
                }
                else
                {
                    if(collection) Corrupt(synchronous ? router.RouteRequest<System.Collections.Generic.IEnumerable<IHolon>>(request):await router.RouteRequestAsync<System.Collections.Generic.IEnumerable<IHolon>>(request));
                    else Corrupt(synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request));
                }
            }
            if(kind==0)
                foreach(var operation in tokenOperations)
                    Corrupt(synchronous ? router.RouteRequest<IAvatar>(TokenRequest(operation)):await router.RouteRequestAsync<IAvatar>(TokenRequest(operation)));
            Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
            Assert.AreEqual(json,(await blob.DownloadContentAsync()).Value.Content.ToString());
            if(kind==0) Assert.AreEqual(id,(await sqlite.LoadAvatarAsync(id)).Result.Id);
            else if(kind==1) Assert.AreEqual(999L,(await sqlite.LoadAvatarDetailAsync(id)).Result.XP);
            else Assert.AreEqual("owned-stale",(await sqlite.LoadHolonAsync(id,loadChildren:false)).Result.Name);
            verified=true;
        }
        finally
        {
            if(uploaded) await blob.DeleteAsync();await sqlite.DeActivateProviderAsync();
            if(verified) foreach(var file in new[] {path,path+"-wal",path+"-shm"}) if(File.Exists(file)) File.Delete(file);
        }
    }

    [DataTestMethod]
    [DataRow(0,false)] [DataRow(0,true)]
    [DataRow(1,false)] [DataRow(1,true)]
    [DataRow(2,false)] [DataRow(2,true)]
    public async Task NativeMalformedBlobsRemainTypedIntegrityFailures(int kind,bool synchronous)
    {
        var id=Guid.NewGuid();var container=kind==0 ? _containers[1]:kind==1 ? _containers[2]:_containers[0];
        var blob=_service.GetBlobContainerClient(container).GetBlobClient(id.ToString());
        const string json="{ invalid canonical JSON";
        var write=await blob.UploadAsync(BinaryData.FromString(json));
        Assert.AreEqual(201,write.GetRawResponse().Status);
        try
        {
            Assert.AreEqual(json,(await blob.DownloadContentAsync()).Value.Content.ToString());
            void Corrupt<T>(OASISResult<T> result)
            {
                Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
                Assert.IsInstanceOfType(result.Exception,typeof(InvalidDataException));
                Assert.IsInstanceOfType(result.Exception.InnerException,typeof(Newtonsoft.Json.JsonException));
            }
            if(kind==0)
            {
                Corrupt(synchronous ? _provider.LoadAvatar(id):await _provider.LoadAvatarAsync(id));
                Corrupt(synchronous ? _provider.LoadAllAvatars():await _provider.LoadAllAvatarsAsync());
            }
            else if(kind==1)
            {
                Corrupt(synchronous ? _provider.LoadAvatarDetail(id):await _provider.LoadAvatarDetailAsync(id));
                Corrupt(synchronous ? _provider.LoadAllAvatarDetails():await _provider.LoadAllAvatarDetailsAsync());
            }
            else
            {
                Corrupt(synchronous ? _provider.LoadHolon(id,loadChildren:false):await _provider.LoadHolonAsync(id,loadChildren:false));
                Corrupt(synchronous ? _provider.LoadAllHolons(loadChildren:false):await _provider.LoadAllHolonsAsync(loadChildren:false));
            }
        }
        finally {await blob.DeleteAsync();}
    }
}
