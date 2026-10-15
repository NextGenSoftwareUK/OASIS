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
            manager.IsAutoReplicationEnabled=false;manager.IsAutoFailOverEnabled=true;
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError);
            var stale=Guid.NewGuid();
            Assert.IsFalse((await sqlite.SaveAvatarAsync(new Avatar {Id=stale,IsActive=true,Username="stale",AvatarType=new EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)})).IsError);
            Assert.IsFalse((await sqlite.SaveAvatarDetailAsync(new AvatarDetail {Id=stale,IsActive=true,XP=999})).IsError);
            Assert.IsFalse((await sqlite.SaveHolonAsync(new Holon {Id=stale,IsActive=true},saveChildren:false)).IsError);
            foreach(var asynchronous in new[] {false,true})
            {
                var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests;
                var avatarRequest=new Core.StorageOperationRequest {Operation="LoadAvatar",AvatarId=stale,PreferredProvider=Core.Enums.ProviderType.PolygonOASIS};
                var detailRequest=new Core.StorageOperationRequest {Operation="LoadAvatarDetail",AvatarId=stale,PreferredProvider=Core.Enums.ProviderType.PolygonOASIS};
                var holonRequest=new Core.StorageOperationRequest {Operation="LoadHolon",HolonId=stale,LoadChildren=false,PreferredProvider=Core.Enums.ProviderType.PolygonOASIS};
                var ar=asynchronous ? await router.RouteRequestAsync<IAvatar>(avatarRequest):router.RouteRequest<IAvatar>(avatarRequest);
                var dr=asynchronous ? await router.RouteRequestAsync<IAvatarDetail>(detailRequest):router.RouteRequest<IAvatarDetail>(detailRequest);
                var hr=asynchronous ? await router.RouteRequestAsync<IHolon>(holonRequest):router.RouteRequest<IHolon>(holonRequest);
                foreach(var code in new[] {ar.ErrorCode,dr.ErrorCode,hr.ErrorCode}) Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,code);
                Assert.IsNull(ar.Result);Assert.IsNull(dr.Result);Assert.IsNull(hr.Result);
                Assert.IsNull(ar.Exception);Assert.IsNull(dr.Exception);Assert.IsNull(hr.Exception);
                var deleteRequest=new Core.StorageOperationRequest {Operation="DeleteHolon",HolonId=stale,SoftDelete=false,PreferredProvider=Core.Enums.ProviderType.PolygonOASIS};
                var absentDelete=asynchronous ? await router.RouteRequestAsync<IHolon>(deleteRequest):router.RouteRequest<IHolon>(deleteRequest);
                Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,absentDelete.ErrorCode);
                Assert.IsNull(absentDelete.Exception);Assert.IsNull(absentDelete.Result);Assert.IsFalse(absentDelete.IsSaved);
                Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests);
                Assert.AreEqual(stale,(await sqlite.LoadHolonAsync(stale,loadChildren:false)).Result.Id);
            }
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var field=typeof(Web3CoreOASISBaseProvider).GetField("_web3CoreOASIS",flags)!;
            var healthy=field.GetValue(polygon);
            var address=(string)typeof(Web3CoreOASISBaseProvider).GetField("_contractAddress",flags)!.GetValue(polygon)!;
            field.SetValue(polygon,new Web3CoreOASIS(Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_PRIVATE_KEY")!,"http://127.0.0.1:18546",address,Web3CoreOASISBaseProviderHelper.Abi,31337));
            try
            {
                foreach(var asynchronous in new[] {false,true})
                {
                    var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests;
                    var a=new Core.StorageOperationRequest {Operation="LoadAvatar",AvatarId=avatarId,PreferredProvider=Core.Enums.ProviderType.PolygonOASIS};
                    var d=new Core.StorageOperationRequest {Operation="LoadAvatarDetail",AvatarId=avatarId,PreferredProvider=Core.Enums.ProviderType.PolygonOASIS};
                    var h=new Core.StorageOperationRequest {Operation="LoadHolon",HolonId=holonId,LoadChildren=false,PreferredProvider=Core.Enums.ProviderType.PolygonOASIS};
                    var ar=asynchronous ? await router.RouteRequestAsync<IAvatar>(a):router.RouteRequest<IAvatar>(a);
                    var dr=asynchronous ? await router.RouteRequestAsync<IAvatarDetail>(d):router.RouteRequest<IAvatarDetail>(d);
                    var hr=asynchronous ? await router.RouteRequestAsync<IHolon>(h):router.RouteRequest<IHolon>(h);
                    Assert.IsFalse(ar.IsError,ar.Message);Assert.AreEqual("replicated",ar.Result.FirstName);
                    Assert.IsFalse(dr.IsError,dr.Message);Assert.AreEqual(504L,dr.Result.Karma);
                    Assert.IsFalse(hr.IsError,hr.Message);Assert.AreEqual(holonId,hr.Result.Id);
                    Assert.AreEqual(before+3,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests);
                    var failedId=Guid.NewGuid();
                    var request=new Core.StorageOperationRequest {Operation="SaveAvatarDetail",Payload=new AvatarDetail {Id=failedId,IsActive=true,XP=888},PreferredProvider=Core.Enums.ProviderType.PolygonOASIS};
                    var saved=asynchronous ? await router.RouteRequestAsync<IAvatarDetail>(request):router.RouteRequest<IAvatarDetail>(request);
                    Assert.IsTrue(saved.IsError);Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(saved.Exception));
                    Assert.AreEqual(before+3,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests);
                    Assert.IsNull((await sqlite.LoadAvatarDetailAsync(failedId)).Result);
                }
            }
            finally {field.SetValue(polygon,healthy);}
            manager.IsAutoFailOverEnabled=false;manager.IsAutoReplicationEnabled=true;
            foreach(var primary in providers)
            foreach(var asynchronous in new[] {false,true})
            {
                var id=Guid.NewGuid();
                foreach(var store in new IOASISStorageProvider[] {polygon,sqlite})
                    Assert.IsFalse((await store.SaveHolonAsync(new Holon {Id=id,IsActive=true,Name="V2 checked deletion"},saveChildren:false)).IsError);
                var request=new Core.StorageOperationRequest {Operation="DeleteHolon",HolonId=id,SoftDelete=false,PreferredProvider=primary.Value};
                var deleted=asynchronous ? await router.RouteRequestAsync<IHolon>(request):router.RouteRequest<IHolon>(request);
                Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsFalse(deleted.IsWarning,deleted.Message);Assert.AreEqual(id,deleted.Result.Id);
                var native=await ((Web3CoreOASIS)healthy!).GetHolonByIdAsync((uint)Core.Utilities.HashUtility.GetNumericHash(id.ToString()));
                Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(native));
                Assert.IsNull((await sqlite.LoadHolonAsync(id,loadChildren:false)).Result);
            }
            foreach(var primary in providers)
            foreach(var asynchronous in new[] {false,true})
            {
                var id=Guid.NewGuid();
                foreach(var store in new IOASISStorageProvider[] {polygon,sqlite})
                {
                    Assert.IsFalse((await store.SaveAvatarAsync(new Avatar {Id=id,IsActive=true,Username="V2 lifecycle "+id,Email="retained@example.invalid",AvatarType=new EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)})).IsError);
                    var detail=new AvatarDetail {Id=id,IsActive=true,XP=817,Karma=296};
                    detail.Inventory.Add(new InventoryItem {Id=Guid.NewGuid(),Name="retained lifecycle seed"});
                    Assert.IsFalse((await store.SaveAvatarDetailAsync(detail)).IsError);
                }
                var request=new Core.StorageOperationRequest {Operation="DeleteAvatar",AvatarId=id,SoftDelete=true,PreferredProvider=primary.Value};
                var deleted=asynchronous ? await router.RouteRequestAsync<bool>(request):router.RouteRequest<bool>(request);
                Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsFalse(deleted.IsWarning,deleted.Message);Assert.IsTrue(deleted.Result);
                Assert.IsTrue(Core.Helpers.AvatarTombstoneEvidence.TryGet(deleted,out var evidence));
                var pair=await ((Web3CoreOASIS)healthy!).ReadAvatarPairAsync((uint)Core.Utilities.HashUtility.GetNumericHash(id.ToString()));
                var nativeAvatar=polygon.DecodePrivateEntity<Avatar>("avatar",pair.Avatar);
                var nativeDetail=polygon.DecodePrivateEntity<AvatarDetail>("detail",pair.Detail);
                var sqlAvatar=(await sqlite.LoadAvatarAsync(id)).Result;
                var sqlDetail=(await sqlite.LoadAvatarDetailAsync(id)).Result;
                foreach(var record in new IHolonBase[] {nativeAvatar,nativeDetail,sqlAvatar,sqlDetail})
                {
                    Assert.IsNotNull(record);Assert.IsFalse(record.IsActive);
                    Assert.AreEqual(evidence.DeletedAtUtc,record.DeletedDate);Assert.AreEqual(evidence.DeletedByAvatarId,record.DeletedByAvatarId);
                }
                Assert.AreEqual("retained@example.invalid",nativeAvatar.Email);Assert.AreEqual(nativeAvatar.Email,sqlAvatar.Email);
                foreach(var detail in new IAvatarDetail[] {nativeDetail,sqlDetail})
                {Assert.AreEqual(817L,detail.XP);Assert.AreEqual(296L,detail.Karma);Assert.AreEqual("retained lifecycle seed",detail.Inventory.Single().Name);}
                Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,(await polygon.LoadAvatarAsync(id)).ErrorCode);
                Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,(await polygon.LoadAvatarDetailAsync(id)).ErrorCode);
            }
            verified=true;
        }
        finally {await sqlite.DeActivateProviderAsync();sqlite.Dispose();if(verified && File.Exists(path)) File.Delete(path);}
    }
}
