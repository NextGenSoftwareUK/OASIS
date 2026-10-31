using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.Utilities;
using SQLite=NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class GoogleNearbyRoutingTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task NativeNearbyRoutingPreservesStoreIdentityAndIntegrity(bool synchronous,bool avatars)
    {
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        var path=Path.GetFullPath("TestResults/GoogleRoutingFiles/nearby.db");
        Assert.IsFalse(File.Exists(path),"Existing fixture requires reconciliation.");Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        const string project="oasis-google-native";
        var google=new GoogleCloudOASIS(projectId:project,enableStorage:false,enableFirestore:true,enableBigQuery:false);
        using var sqlite=new SQLite($"Data Source={path};Pooling=False");
        var db=new FirestoreDbBuilder {ProjectId=project,EmulatorDetection=Google.Api.Gax.EmulatorDetection.EmulatorOnly}.Build();
        var field=typeof(GoogleCloudOASIS).GetField("_firestoreDb",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        var id=Guid.NewGuid();var document=db.Collection(avatars ? "avatars":"holons").Document(id.ToString());
        var verified=false;object? healthy=null;
        try
        {
            Assert.IsFalse((await google.ActivateProviderAsync()).IsError);healthy=field.GetValue(google);
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            IHolonBase Record(string label)
            {
                IHolonBase row=avatars ? new Avatar {Username=label,Email=label.Replace(" ","")+"@example.invalid",AvatarType=new EnumValue<AvatarType>(AvatarType.User)}:new Holon();
                row.Id=id;row.Name=label;row.IsActive=true;row.HolonType=avatars ? HolonType.Avatar:HolonType.Park;
                row.MetaData=new() {{"Latitude","30.0005"},{"Longitude","20"}};return row;
            }
            async Task Save(IOASISStorageProvider store,IHolonBase row)
            {
                var error=avatars ? (await store.SaveAvatarAsync((IAvatar)row)).IsError:(await store.SaveHolonAsync((IHolon)row,saveChildren:false)).IsError;
                Assert.IsFalse(error);
            }
            string Label(IHolonBase row)=>avatars ? ((IAvatar)row).Username:row.Name;
            var native=Record("google nearby");await Save(google,native);await Save(sqlite,Record("sqlite nearby"));
            var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var types=new[]{new EnumValue<ProviderType>(ProviderType.GoogleCloudOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
            ProviderManager Manager()
            {
                var manager=new ProviderManager(null,dna);manager.RegisterProvider(google);manager.RegisterProvider(sqlite);
                Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(types).IsError);
                Assert.IsFalse(manager.SetAndReplaceAutoLoadBalanceListForProviders(types).IsError);
                Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(types).IsError);return manager;
            }
            var request=new StorageOperationRequest {Operation=avatars ? "GetAvatarsNearMe":"GetHolonsNearMe",GeoLatitude=30000000,GeoLongitude=20000000,RadiusInMeters=100,HolonType=HolonType.Park};
            async Task<NextGenSoftware.OASIS.Common.OASISResult<IEnumerable<IHolonBase>>> Route(ProviderManager manager,LoadBalancingStrategy strategy=LoadBalancingStrategy.Auto)
            {
                var router=new OASISHyperDrive(manager);
                return synchronous ? router.RouteRequest<IEnumerable<IHolonBase>>(request,strategy):await router.RouteRequestAsync<IEnumerable<IHolonBase>>(request,strategy);
            }
            foreach(var type in types)
            {
                request.PreferredProvider=type.Value;var result=await Route(Manager());Assert.IsFalse(result.IsError,result.Message);
                Assert.AreEqual(type.Value==ProviderType.GoogleCloudOASIS ? "google nearby":"sqlite nearby",Label(result.Result.Single()));
            }
            double Measure(IOASISNETProvider network)
            {
                var watch=System.Diagnostics.Stopwatch.StartNew();
                var error=avatars ? network.GetAvatarsNearMe(30000000,20000000,100).IsError:network.GetHolonsNearMe(30000000,20000000,100,HolonType.Park).IsError;
                watch.Stop();Assert.IsFalse(error);return watch.Elapsed.TotalMilliseconds;
            }
            var googleMs=Measure(google);var sqliteMs=Measure(sqlite);var balance=Manager();balance.IsAutoLoadBalanceEnabled=true;
            balance.PerformanceMonitor.RecordRequest(ProviderType.GoogleCloudOASIS,true,googleMs);balance.PerformanceMonitor.RecordRequest(ProviderType.SQLLiteDBOASIS,true,sqliteMs);
            request.PreferredProvider=ProviderType.Default;var selected=await Route(balance,LoadBalancingStrategy.Performance);
            Assert.IsFalse(selected.IsError,selected.Message);Assert.AreEqual(googleMs<=sqliteMs ? "google nearby":"sqlite nearby",Label(selected.Result.Single()));
            request.PreferredProvider=ProviderType.GoogleCloudOASIS;
            var settings=Google.Api.Gax.Grpc.CallSettingsExtensions.WithRetry(Google.Api.Gax.Grpc.CallSettings.FromExpiration(Google.Api.Gax.Expiration.FromTimeout(TimeSpan.FromSeconds(2))),Google.Api.Gax.Grpc.RetrySettings.FromConstantBackoff(1,TimeSpan.Zero,_=>false));
            var client=new Google.Cloud.Firestore.V1.FirestoreClientBuilder {Endpoint="127.0.0.1:18083",ChannelCredentials=Grpc.Core.ChannelCredentials.Insecure,Settings=new Google.Cloud.Firestore.V1.FirestoreSettings {RunQuerySettings=settings}}.Build();
            field.SetValue(google,FirestoreDb.Create(project,client));
            try
            {
                var failover=Manager();failover.IsAutoFailOverEnabled=true;var result=await Route(failover);
                Assert.IsFalse(result.IsError,result.Message);Assert.AreEqual("sqlite nearby",Label(result.Result.Single()));
                Assert.AreEqual(ProviderType.SQLLiteDBOASIS,failover.LastFailoverDiagnostic.SelectedProvider);
            }
            finally {field.SetValue(google,healthy);}
            native.MetaData.Remove("Longitude");await Save(google,native);
            var corruption=Manager();corruption.IsAutoFailOverEnabled=true;corruption.IsAutoReplicationEnabled=true;
            var before=corruption.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
            var rejected=await Route(corruption);Assert.IsTrue(rejected.IsError);Assert.IsNull(rejected.Result);Assert.IsInstanceOfType(rejected.Exception,typeof(InvalidDataException));
            Assert.AreEqual(before,corruption.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
            IHolonBase nativeRead=avatars ? (await google.LoadAvatarAsync(id)).Result:(await google.LoadHolonAsync(id,loadChildren:false)).Result;
            IHolonBase secondaryRead=avatars ? (await sqlite.LoadAvatarAsync(id)).Result:(await sqlite.LoadHolonAsync(id,loadChildren:false)).Result;
            Assert.IsFalse(nativeRead.MetaData.ContainsKey("Longitude"));Assert.AreEqual("sqlite nearby",Label(secondaryRead));verified=true;
        }
        finally
        {
            if(healthy!=null) field.SetValue(google,healthy);
            await google.DeActivateProviderAsync();await sqlite.DeActivateProviderAsync();sqlite.Dispose();
            if(verified){await document.DeleteAsync();File.Delete(path);}
        }
    }
}
