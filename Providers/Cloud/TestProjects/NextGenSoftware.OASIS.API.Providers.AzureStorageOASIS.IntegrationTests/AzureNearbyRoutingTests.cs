using Azure.Storage.Blobs;
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

namespace NextGenSoftware.OASIS.API.Providers.AzureStorageOASIS.IntegrationTests;

public partial class AzureStorageOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task V2NearbyUsesRealStoresForBalanceFailoverAndTerminalCorruption(bool synchronous,bool avatars)
    {
        var path=Path.GetFullPath("TestResults/AzureRoutingFiles/nearby.db");
        Assert.IsFalse(File.Exists(path),"Existing fixture requires reconciliation.");Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var sqlite=new SQLite($"Data Source={path};Pooling=False");var verified=false;
        var healthy=(BlobServiceClient)ServiceField.GetValue(_provider)!;
        try
        {
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            var id=Guid.NewGuid();
            IHolonBase Record(string name)
            {
                IHolonBase row=avatars ? new Avatar {Username=name,Email=name.Replace(" ","")+"@example.invalid",AvatarType=new EnumValue<AvatarType>(AvatarType.User)}:new Holon();
                row.Id=id;row.Name=name;row.IsActive=true;row.HolonType=avatars ? HolonType.Avatar:HolonType.Park;
                row.MetaData=new() {{"Latitude","30.0005"},{"Longitude","20"}};return row;
            }
            async Task Save(IHolonBase row,bool azure)
            {
                var store=azure ? (IOASISStorageProvider)_provider:sqlite;
                var error=avatars ? (await store.SaveAvatarAsync((IAvatar)row)).IsError:(await store.SaveHolonAsync((IHolon)row,saveChildren:false)).IsError;
                Assert.IsFalse(error);
            }
            async Task<IHolonBase> Read(bool azure)
            {
                var store=azure ? (IOASISStorageProvider)_provider:sqlite;
                return avatars ? (IHolonBase)(await store.LoadAvatarAsync(id)).Result:(await store.LoadHolonAsync(id,loadChildren:false)).Result;
            }
            void Nearby(IOASISNETProvider network)
            {
                if(avatars) Assert.IsFalse(network.GetAvatarsNearMe(30000000,20000000,100).IsError);
                else Assert.IsFalse(network.GetHolonsNearMe(30000000,20000000,100,HolonType.All).IsError);
            }
            string Label(IHolonBase row)=>avatars ? ((IAvatar)row).Username:row.Name;
            var native=Record("azure nearby");await Save(native,true);
            await Save(Record("sqlite nearby"),false);
            var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var manager=new ProviderManager(null,dna) {IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true};manager.RegisterProvider(_provider);manager.RegisterProvider(sqlite);
            var types=new[] {new EnumValue<ProviderType>(ProviderType.AzureStorageOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)};
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(types).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoLoadBalanceListForProviders(types).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(types).IsError);
            var router=new OASISHyperDrive(manager);
            var request=new StorageOperationRequest {Operation=avatars ? "GetAvatarsNearMe":"GetHolonsNearMe",GeoLatitude=30000000,GeoLongitude=20000000,RadiusInMeters=100,HolonType=HolonType.Park,PreferredProvider=ProviderType.AzureStorageOASIS};
            async Task<NextGenSoftware.OASIS.Common.OASISResult<IEnumerable<IHolonBase>>> Route(LoadBalancingStrategy strategy=LoadBalancingStrategy.Auto)
                => synchronous ? router.RouteRequest<IEnumerable<IHolonBase>>(request,strategy):await router.RouteRequestAsync<IEnumerable<IHolonBase>>(request,strategy);
            foreach(var type in types)
            {
                request.PreferredProvider=type.Value;var result=await Route();Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsLoaded);
                Assert.AreEqual(type.Value==ProviderType.AzureStorageOASIS ? "azure nearby":"sqlite nearby",Label(result.Result.Single()));
            }
            var watch=System.Diagnostics.Stopwatch.StartNew();Nearby(_provider);watch.Stop();var azureMs=watch.Elapsed.TotalMilliseconds;
            watch.Restart();Nearby(sqlite);watch.Stop();var sqliteMs=watch.Elapsed.TotalMilliseconds;
            var balance=new ProviderManager(null,dna) {IsAutoLoadBalanceEnabled=true};balance.RegisterProvider(_provider);balance.RegisterProvider(sqlite);
            Assert.IsFalse(balance.SetAndReplaceAutoLoadBalanceListForProviders(types).IsError);
            balance.PerformanceMonitor.RecordRequest(ProviderType.AzureStorageOASIS,true,azureMs);balance.PerformanceMonitor.RecordRequest(ProviderType.SQLLiteDBOASIS,true,sqliteMs);
            request.PreferredProvider=ProviderType.Default;var balanceRouter=new OASISHyperDrive(balance);
            var balanced=synchronous ? balanceRouter.RouteRequest<IEnumerable<IHolonBase>>(request,LoadBalancingStrategy.Performance):await balanceRouter.RouteRequestAsync<IEnumerable<IHolonBase>>(request,LoadBalancingStrategy.Performance);
            Assert.IsFalse(balanced.IsError,balanced.Message);Assert.AreEqual(azureMs<=sqliteMs ? "azure nearby":"sqlite nearby",Label(balanced.Result.Single()));
            request.PreferredProvider=ProviderType.AzureStorageOASIS;
            var options=new BlobClientOptions();options.Retry.MaxRetries=0;options.Retry.NetworkTimeout=TimeSpan.FromSeconds(2);
            ServiceField.SetValue(_provider,new BlobServiceClient(new Uri("http://127.0.0.1:10003/devstoreaccount1"),options));
            try {var fallback=await Route();Assert.IsFalse(fallback.IsError,fallback.Message);Assert.AreEqual("sqlite nearby",Label(fallback.Result.Single()));}
            finally {ServiceField.SetValue(_provider,healthy);}
            native.MetaData.Remove("Longitude");await Save(native,true);
            var before=manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
            var corrupt=await Route();Assert.IsTrue(corrupt.IsError);Assert.IsNull(corrupt.Result);Assert.IsFalse(corrupt.IsLoaded);Assert.IsInstanceOfType(corrupt.Exception,typeof(InvalidDataException));
            Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
            Assert.AreEqual("sqlite nearby",Label(await Read(false)));
            var unchanged=await Read(true);Assert.IsFalse(unchanged.MetaData.ContainsKey("Longitude"));
            verified=true;
        }
        finally
        {
            ServiceField.SetValue(_provider,healthy);sqlite.Dispose();
            if(verified) foreach(var file in new[] {path,path+"-wal",path+"-shm"}) if(File.Exists(file)) File.Delete(file);
        }
    }
}
