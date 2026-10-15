using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

public partial class Web3NativeStorageTests
{
    private static async Task VerifyV2Async(PolygonOASIS.PolygonOASIS polygon,Guid avatarId)
    {
        var path=Path.GetFullPath("TestResults/Web3CoreRouting/routing.db");
        if(File.Exists(path)) Assert.Fail("Retained Web3Core SQLite evidence exists; refusing reset.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var sqlite=new SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False");var verified=false;
        try
        {
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await sqlite.SaveAvatarAsync(new Avatar {Id=avatarId,IsActive=true,Username="owned-private",AvatarType=new EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)})).IsError);
            Assert.IsFalse((await sqlite.SaveAvatarDetailAsync(new AvatarDetail {Id=avatarId,IsActive=true,XP=999,Karma=999})).IsError);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,StorageProviders=new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings(),
                SubscriptionConfig=new NextGenSoftware.OASIS.API.DNA.SubscriptionConfig {PlanType="Gold"}}};
            var manager=new Core.Managers.ProviderManager(null,dna) {IsAutoLoadBalanceEnabled=true,IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false};
            manager.RegisterProvider(polygon);manager.RegisterProvider(sqlite);
            var providers=new[] {new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.PolygonOASIS),new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS)};
            Assert.IsFalse(manager.SetAndReplaceAutoLoadBalanceListForProviders(providers).IsError);
            var router=new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
            var counts=new Dictionary<long,int>();
            for(var index=0;index<6;index++)
            {
                var request=new Core.StorageOperationRequest {Operation="LoadAvatarDetail",AvatarId=avatarId};
                var read=index%2==0 ? router.RouteRequest<IAvatarDetail>(request,Core.Enums.LoadBalancingStrategy.RoundRobin)
                    :await router.RouteRequestAsync<IAvatarDetail>(request,Core.Enums.LoadBalancingStrategy.RoundRobin);
                Assert.IsFalse(read.IsError,read.Message);counts[read.Result.Karma]=counts.GetValueOrDefault(read.Result.Karma)+1;
            }
            Assert.AreEqual(3,counts[29]);Assert.AreEqual(3,counts[999]);
            foreach(var strategy in Enum.GetValues<Core.Enums.LoadBalancingStrategy>())
            foreach(var asynchronous in new[] {false,true})
            {
                var beforePolygon=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.PolygonOASIS).TotalRequests;
                var beforeSqlite=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests;
                var request=new Core.StorageOperationRequest {Operation="LoadAvatarDetail",AvatarId=avatarId};
                var read=asynchronous ? await router.RouteRequestAsync<IAvatarDetail>(request,strategy):router.RouteRequest<IAvatarDetail>(request,strategy);
                Assert.IsFalse(read.IsError,read.Message);var choice=manager.LastProviderSelectionDiagnostic;
                Assert.AreEqual(strategy,choice.RequestedStrategy);Assert.AreEqual(2,choice.Candidates.Count);
                var selected=choice.SelectedProvider==Core.Enums.ProviderType.PolygonOASIS;
                Assert.AreEqual(selected ? 29L:999L,read.Result.Karma);
                Assert.AreEqual(beforePolygon+(selected ? 1:0),manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.PolygonOASIS).TotalRequests);
                Assert.AreEqual(beforeSqlite+(selected ? 0:1),manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests);
            }
            manager.IsAutoLoadBalanceEnabled=false;manager.IsAutoReplicationEnabled=true;
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError);
            var amount=500;var holonId=Guid.NewGuid();
            foreach(var primary in providers)
            foreach(var asynchronous in new[] {false,true})
            {
                var detail=new AvatarDetail {Id=avatarId,IsActive=true,XP=600,Karma=++amount};detail.Inventory.Add(new InventoryItem {Id=Guid.NewGuid(),Name="routed seed"});
                var request=new Core.StorageOperationRequest {Operation="SaveAvatarDetail",Payload=detail,PreferredProvider=primary.Value};
                var saved=asynchronous ? await router.RouteRequestAsync<IAvatarDetail>(request):router.RouteRequest<IAvatarDetail>(request);
                Assert.IsFalse(saved.IsError,saved.Message);Assert.IsFalse(saved.IsWarning,saved.Message);
                var avatar=new Avatar {Id=avatarId,IsActive=true,Username="routed profile",FirstName="replicated",AvatarType=new EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)};
                var profileRequest=new Core.StorageOperationRequest {Operation="SaveAvatar",Payload=avatar,PreferredProvider=primary.Value};
                var profile=asynchronous ? await router.RouteRequestAsync<IAvatar>(profileRequest):router.RouteRequest<IAvatar>(profileRequest);
                Assert.IsFalse(profile.IsError,profile.Message);Assert.IsFalse(profile.IsWarning,profile.Message);
                var holon=new Holon {Id=holonId,IsActive=true,Name="replicated holon",ParentHolonId=avatarId,CreatedByAvatarId=avatarId,MetaData=new Dictionary<string,object> {{"owned-field","owned-value"}}};
                var holonRequest=new Core.StorageOperationRequest {Operation="SaveHolon",Payload=holon,SaveChildren=false,PreferredProvider=primary.Value};
                var written=asynchronous ? await router.RouteRequestAsync<IHolon>(holonRequest):router.RouteRequest<IHolon>(holonRequest);
                Assert.IsFalse(written.IsError,written.Message);Assert.IsFalse(written.IsWarning,written.Message);
                foreach(var store in new IOASISStorageProvider[] {polygon,sqlite})
                {
                    var loaded=await store.LoadAvatarDetailAsync(avatarId);Assert.IsFalse(loaded.IsError,loaded.Message);
                    Assert.AreEqual((long)amount,loaded.Result.Karma);Assert.AreEqual(600L,loaded.Result.XP);Assert.AreEqual("routed seed",loaded.Result.Inventory.Single().Name);
                    Assert.AreEqual("replicated",(await store.LoadAvatarAsync(avatarId)).Result.FirstName);
                    var record=await store.LoadHolonAsync(holonId,loadChildren:false);Assert.IsFalse(record.IsError,record.Message);
                    Assert.AreEqual(avatarId,record.Result.ParentHolonId);Assert.AreEqual("owned-value",record.Result.MetaData["owned-field"].ToString());
                }
            }
            verified=true;
        }
        finally {await sqlite.DeActivateProviderAsync();sqlite.Dispose();if(verified && File.Exists(path)) File.Delete(path);}
    }
}
