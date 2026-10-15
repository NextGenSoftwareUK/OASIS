using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

public partial class Web3NativeStorageTests
{
    private static async Task VerifyPreparedReplicationAsync(PolygonOASIS.PolygonOASIS polygon,Web3CoreOASIS native,
        Web4UsageExecution executor,string user,UsageEndpointPolicy policy,MongoUsageOutboxStore store)
    {
        var path=Path.GetFullPath("TestResults/Web3CoreRouting/routing.db");
        Assert.IsFalse(File.Exists(path),"Retained SQLite fixture requires review; refusing overwrite.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var sqlite=new SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False");var verified=false;
        try
        {
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,StorageProviders=new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings(),
                SubscriptionConfig=new NextGenSoftware.OASIS.API.DNA.SubscriptionConfig {PlanType="Gold"}}};
            var manager=new Core.Managers.ProviderManager(null,dna) {IsAutoReplicationEnabled=true};
            manager.RegisterProvider(polygon);manager.RegisterProvider(sqlite);
            var types=new[] {Core.Enums.ProviderType.PolygonOASIS,Core.Enums.ProviderType.SQLLiteDBOASIS};
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(types.Select(type=>new EnumValue<Core.Enums.ProviderType>(type))).IsError);
            var router=new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);var sequence=0;
            var sdk=(Web3CoreOASIS)typeof(Web3CoreOASISBaseProvider).GetField("_web3CoreOASIS",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(polygon)!;
            foreach(var primary in types)
            foreach(var asynchronous in new[] {false,true})
            {
                var handle=await executor.BeginAsync("local-fixture-bearer",user,"prepared-replication-"+(++sequence),new string('e',64),"local prepared replication fixture",policy);
                var context=new PreparedStorageExecution(handle,"replicated-workflow",new Dictionary<Core.Enums.ProviderType,string> {{Core.Enums.ProviderType.PolygonOASIS,sdk.SigningAddress}},new[] {Core.Enums.ProviderType.SQLLiteDBOASIS});
                var id=Guid.NewGuid();var detail=new AvatarDetail {Id=id,IsActive=true,XP=611,Karma=246};
                detail.Inventory.Add(new Core.Objects.InventoryItem {Id=Guid.NewGuid(),Name="prepared replica seed"});
                var request=new Core.StorageOperationRequest {Operation="SaveAvatarDetail",Payload=detail,PreferredProvider=primary,PreparedExecution=context};
                var saved=asynchronous ? await router.RouteRequestAsync<IAvatarDetail>(request):router.RouteRequest<IAvatarDetail>(request);
                Assert.IsFalse(saved.IsError,saved.Message);Assert.IsFalse(saved.IsWarning,saved.Message);
                foreach(var storage in new IOASISStorageProvider[] {polygon,sqlite})
                {
                    var loaded=await storage.LoadAvatarDetailAsync(id);Assert.IsFalse(loaded.IsError,loaded.Message);
                    Assert.AreEqual(611L,loaded.Result.XP);Assert.AreEqual("prepared replica seed",loaded.Result.Inventory.Single().Name);
                }
                var avatar=new Avatar {Id=id,IsActive=true,Username="prepared replica "+id,AvatarType=new EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)};
                request.Operation="SaveAvatar";request.Payload=avatar;
                var profile=asynchronous ? await router.RouteRequestAsync<IAvatar>(request):router.RouteRequest<IAvatar>(request);
                Assert.IsFalse(profile.IsError,profile.Message);Assert.IsFalse(profile.IsWarning,profile.Message);
                request.Operation="DeleteAvatar";request.AvatarId=id;request.Payload=null;request.SoftDelete=true;
                var deleted=asynchronous ? await router.RouteRequestAsync<bool>(request):router.RouteRequest<bool>(request);
                Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsFalse(deleted.IsWarning,deleted.Message);Assert.IsTrue(deleted.Result);
                var pair=await native.ReadAvatarPairAsync((uint)Core.Utilities.HashUtility.GetNumericHash(id.ToString()));
                var nativeAvatar=polygon.DecodePrivateEntity<Avatar>("avatar",pair.Avatar);var nativeDetail=polygon.DecodePrivateEntity<AvatarDetail>("detail",pair.Detail);
                var backupAvatar=(await sqlite.LoadAvatarAsync(id)).Result;var backupDetail=(await sqlite.LoadAvatarDetailAsync(id)).Result;
                foreach(var record in new IHolonBase[] {nativeAvatar,nativeDetail,backupAvatar,backupDetail})
                {Assert.IsFalse(record.IsActive);Assert.AreEqual(nativeAvatar.DeletedDate,record.DeletedDate);Assert.AreEqual(nativeAvatar.DeletedByAvatarId,record.DeletedByAvatarId);}
                Assert.AreEqual(611L,nativeDetail.XP);Assert.AreEqual(246L,backupDetail.Karma);
                var operation=await store.GetAsync(handle.OperationId,default);
                Assert.AreEqual(6,operation.ProviderAttempts.Count);Assert.AreEqual(6,operation.ProviderAttempts.Select(attempt=>attempt.ClientRequestId).Distinct().Count());
                Assert.AreEqual(3,operation.ProviderAttempts.Count(attempt=>attempt.Provider=="PolygonOASIS" && attempt.NativePreparation!=null && attempt.ProviderRequestId!=null));
                Assert.AreEqual(3,operation.ProviderAttempts.Count(attempt=>attempt.Provider=="SQLLiteDBOASIS" && attempt.NativePreparation==null));
            }
            await VerifyPreparedSelectionAsync(polygon,sqlite,native,manager,router,executor,user,policy,store);
            verified=true;
        }
        finally {await sqlite.DeActivateProviderAsync();sqlite.Dispose();if(verified && File.Exists(path)) File.Delete(path);}
    }
}
