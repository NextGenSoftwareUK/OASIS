using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class GoogleIntegrityTests
{
    [DataTestMethod]
    [DataRow(0,false)] [DataRow(0,true)]
    [DataRow(1,false)] [DataRow(1,true)]
    [DataRow(2,false)] [DataRow(2,true)]
    public async Task V2CorruptPrimaryDoesNotReadStaleSecondary(int kind,bool synchronous)
    {
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        var path=Path.GetFullPath("TestResults/GoogleRoutingFiles/tombstone-routing.db");
        if(File.Exists(path)) Assert.Fail("Retained Google SQLite evidence exists; refusing overwrite.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        const string project="oasis-google-native";
        var db=new FirestoreDbBuilder {ProjectId=project,EmulatorDetection=Google.Api.Gax.EmulatorDetection.EmulatorOnly}.Build();
        var provider=new GoogleCloudOASIS(projectId:project,enableStorage:false,enableFirestore:true,enableBigQuery:false);
        var sqlite=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False");
        var id=Guid.NewGuid();
        var document=db.Collection(kind==0 ? "avatars":kind==1 ? "avatarDetails":"holons").Document(id.ToString());
        const string json="{ invalid canonical JSON";
        var acknowledged=false;var verified=false;
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
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
            manager.RegisterProvider(provider);manager.RegisterProvider(sqlite);
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(new[] {
                new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.GoogleCloudOASIS),
                new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS)}).IsError);
            var router=new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
            var tokenOperations=new[] {"LoadAvatarByVerificationToken","LoadAvatarByResetToken","LoadAvatarByRefreshToken","LoadAvatarByPublicKey","LoadAvatarByPrivateKey"};
            Core.StorageOperationRequest TokenRequest(string operation,int version=0) => new() {
                Operation=operation,VerificationToken="owned-stale",ResetToken="owned-stale",RefreshToken="owned-stale",PublicKey="owned-stale",PrivateKey="owned-stale",
                Version=version,PreferredProvider=Core.Enums.ProviderType.GoogleCloudOASIS};
            if(kind==0)
            {
                var secondaryBefore=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
                foreach(var operation in tokenOperations)
                {
                    var missing=synchronous ? router.RouteRequest<IAvatar>(TokenRequest(operation)):await router.RouteRequestAsync<IAvatar>(TokenRequest(operation));
                    Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,missing.ErrorCode);Assert.IsNull(missing.Result);
                    var invalid=synchronous ? router.RouteRequest<IAvatar>(TokenRequest(operation,1)):await router.RouteRequestAsync<IAvatar>(TokenRequest(operation,1));
                    Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID",invalid.ErrorCode);Assert.IsNull(invalid.Result);
                    Assert.IsInstanceOfType(invalid.Exception,typeof(NotSupportedException));
                }
                Assert.AreEqual(secondaryBefore,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
            }
            await document.CreateAsync(new Dictionary<string,object> {{"id",id.ToString()},{"data_json",json},{"isActive",true},{"isDeleted",false}});
            acknowledged=true;
            var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
            foreach(var collection in new[] {false,true})
            {
                var request=new Core.StorageOperationRequest {Operation=kind==0 ? collection ? "LoadAllAvatars":"LoadAvatar":kind==1
                    ? collection ? "LoadAllAvatarDetails":"LoadAvatarDetail":collection ? "LoadAllHolons":"LoadHolon",
                    AvatarId=id,HolonId=id,LoadChildren=false,PreferredProvider=Core.Enums.ProviderType.GoogleCloudOASIS};
                if(kind==0)
                {
                    if(collection) Corrupt(synchronous ? router.RouteRequest<IEnumerable<IAvatar>>(request):await router.RouteRequestAsync<IEnumerable<IAvatar>>(request));
                    else Corrupt(synchronous ? router.RouteRequest<IAvatar>(request):await router.RouteRequestAsync<IAvatar>(request));
                }
                else if(kind==1)
                {
                    if(collection) Corrupt(synchronous ? router.RouteRequest<IEnumerable<IAvatarDetail>>(request):await router.RouteRequestAsync<IEnumerable<IAvatarDetail>>(request));
                    else Corrupt(synchronous ? router.RouteRequest<IAvatarDetail>(request):await router.RouteRequestAsync<IAvatarDetail>(request));
                }
                else
                {
                    if(collection) Corrupt(synchronous ? router.RouteRequest<IEnumerable<IHolon>>(request):await router.RouteRequestAsync<IEnumerable<IHolon>>(request));
                    else Corrupt(synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request));
                }
            }
            if(kind==0)
                foreach(var operation in tokenOperations)
                    Corrupt(synchronous ? router.RouteRequest<IAvatar>(TokenRequest(operation)):await router.RouteRequestAsync<IAvatar>(TokenRequest(operation)));
            Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
            Assert.AreEqual(json,(await document.GetSnapshotAsync()).GetValue<string>("data_json"));
            if(kind==0) Assert.AreEqual(id,(await sqlite.LoadAvatarAsync(id)).Result.Id);
            else if(kind==1) Assert.AreEqual(999L,(await sqlite.LoadAvatarDetailAsync(id)).Result.XP);
            else Assert.AreEqual("owned-stale",(await sqlite.LoadHolonAsync(id,loadChildren:false)).Result.Name);
            verified=true;
        }
        finally
        {
            if(acknowledged) await document.DeleteAsync();
            await provider.DeActivateProviderAsync();await sqlite.DeActivateProviderAsync();
            if(verified) foreach(var file in new[] {path,path+"-wal",path+"-shm"}) if(File.Exists(file)) File.Delete(file);
        }
    }

    [DataTestMethod]
    [DataRow(0,false)] [DataRow(0,true)]
    [DataRow(1,false)] [DataRow(1,true)]
    [DataRow(2,false)] [DataRow(2,true)]
    public async Task NativeMalformedDocumentsRemainIntegrityFailures(int kind,bool synchronous)
    {
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        const string project="oasis-google-native";
        var db=new FirestoreDbBuilder {ProjectId=project,EmulatorDetection=Google.Api.Gax.EmulatorDetection.EmulatorOnly}.Build();
        var provider=new GoogleCloudOASIS(projectId:project,enableStorage:false,enableFirestore:true,enableBigQuery:false);
        var id=Guid.NewGuid();
        var document=db.Collection(kind==0 ? "avatars":kind==1 ? "avatarDetails":"holons").Document(id.ToString());
        const string json="{ invalid canonical JSON";
        var acknowledged=false;
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            await document.CreateAsync(new Dictionary<string,object> {{"id",id.ToString()},{"data_json",json},{"isActive",true},{"isDeleted",false}});
            acknowledged=true;
            Assert.AreEqual(json,(await document.GetSnapshotAsync()).GetValue<string>("data_json"));
            if(kind==0)
            {
                Corrupt(synchronous ? provider.LoadAvatar(id):await provider.LoadAvatarAsync(id));
                Corrupt(synchronous ? provider.LoadAllAvatars():await provider.LoadAllAvatarsAsync());
            }
            else if(kind==1)
            {
                Corrupt(synchronous ? provider.LoadAvatarDetail(id):await provider.LoadAvatarDetailAsync(id));
                Corrupt(synchronous ? provider.LoadAllAvatarDetails():await provider.LoadAllAvatarDetailsAsync());
            }
            else
            {
                Corrupt(synchronous ? provider.LoadHolon(id,loadChildren:false):await provider.LoadHolonAsync(id,loadChildren:false));
                Corrupt(synchronous ? provider.LoadAllHolons(loadChildren:false):await provider.LoadAllHolonsAsync(loadChildren:false));
            }
            Assert.AreEqual(json,(await document.GetSnapshotAsync()).GetValue<string>("data_json"));
        }
        finally
        {
            if(acknowledged) await document.DeleteAsync();
            await provider.DeActivateProviderAsync();
        }
    }

    private static void Corrupt<T>(OASISResult<T> result)
    {
        Assert.IsTrue(result.IsError,result.Message);Assert.IsNull(result.Result);Assert.IsFalse(result.IsLoaded);
        Assert.IsInstanceOfType(result.Exception,typeof(System.IO.InvalidDataException));
        Assert.IsInstanceOfType(result.Exception.InnerException,typeof(Newtonsoft.Json.JsonException));
    }
}
