using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass]
public class GoogleNativeDetailTests
{
    [TestMethod]
    public async Task OfficialFirestoreSdkRetainsCanonicalDetailsAndRejectsDuplicateSelectors()
    {
        Assert.AreEqual("127.0.0.1:18080",Environment.GetEnvironmentVariable("FIRESTORE_EMULATOR_HOST"));
        const string project="oasis-google-native";
        var db=new FirestoreDbBuilder {ProjectId=project,EmulatorDetection=Google.Api.Gax.EmulatorDetection.EmulatorOnly}.Build();
        Assert.AreEqual(0,(await db.Collection("avatarDetails").Limit(1).GetSnapshotAsync()).Count,"Refusing existing test data.");
        var provider=new GoogleCloudOASIS(projectId:project,enableStorage:false,enableFirestore:true,enableBigQuery:false);
        var ids=new List<Guid>();
        try {
            var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Message);
            var empty=await provider.LoadAllAvatarDetailsAsync();Assert.IsFalse(empty.IsError,empty.Message);Assert.AreEqual(0,empty.Result.Count());
            var detail=new AvatarDetail {Id=Guid.NewGuid(),IsActive=true,Username="owned-user",Email="owned@example.invalid",XP=321,Karma=29,
                MetaData=new Dictionary<string,object> {{"owned-field","owned-value"}}};
            detail.Inventory.Add(new InventoryItem {Id=Guid.NewGuid(),Name="owned seed"});ids.Add(detail.Id);
            var saved=await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(saved.IsError,saved.Message);Assert.IsTrue(saved.IsSaved);
            var loaded=await provider.LoadAvatarDetailAsync(detail.Id);Assert.IsFalse(loaded.IsError,loaded.Message);
            Assert.AreEqual(321,loaded.Result.XP);Assert.AreEqual(29L,loaded.Result.Karma);
            Assert.AreEqual(detail.Inventory[0].Id,loaded.Result.Inventory.Single().Id);Assert.AreEqual("owned-value",loaded.Result.MetaData["owned-field"]);
            Assert.AreEqual(detail.Id,(await provider.LoadAvatarDetailByUsernameAsync(detail.Username)).Result.Id);
            Assert.AreEqual(detail.Id,provider.LoadAvatarDetailByEmail(detail.Email).Result.Id);
            var duplicate=new AvatarDetail {Id=Guid.NewGuid(),IsActive=true,Username=detail.Username,Email=detail.Email};ids.Add(duplicate.Id);
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(duplicate)).IsError);
            Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,(await provider.LoadAvatarDetailByEmailAsync(detail.Email)).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,(await provider.LoadAvatarDetailByUsernameAsync(detail.Username)).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await provider.LoadAvatarDetailAsync(Guid.NewGuid())).ErrorCode);
            Assert.AreEqual(2,(await provider.LoadAllAvatarDetailsAsync()).Result.Count());
            await VerifyV2Async(provider,detail);
        }
        finally {foreach(var id in ids) await db.Collection("avatarDetails").Document(id.ToString()).DeleteAsync();await provider.DeActivateProviderAsync();}
    }

    private static async Task VerifyV2Async(GoogleCloudOASIS google,AvatarDetail detail)
    {
        var directory=Path.GetFullPath("TestResults/GoogleRoutingFiles");
        if(Directory.Exists(directory) && Directory.EnumerateFiles(directory,"*",SearchOption.AllDirectories).Any()) Assert.Fail("Owned routing files already exist.");
        var local=new NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.LocalFileOASIS(storageDirectory:directory);
        Assert.IsFalse((await local.ActivateProviderAsync()).IsError);
        try {
            Assert.IsFalse((await local.SaveAvatarDetailAsync(new AvatarDetail {Id=detail.Id,IsActive=true,Karma=999})).IsError);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,
                StorageProviders=new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings(),
                SubscriptionConfig=new NextGenSoftware.OASIS.API.DNA.SubscriptionConfig {PlanType="Gold"}}};
            var manager=new NextGenSoftware.OASIS.API.Core.Managers.ProviderManager(null,dna) {IsAutoLoadBalanceEnabled=true,IsAutoFailOverEnabled=false,IsAutoReplicationEnabled=false};
            manager.RegisterProvider(google);manager.RegisterProvider(local);
            var providers=new[] {new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.GoogleCloudOASIS),
                new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.LocalFileOASIS)};
            Assert.IsFalse(manager.SetAndReplaceAutoLoadBalanceListForProviders(providers).IsError);
            var router=new NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
            var counts=new Dictionary<long,int>();
            for(int i=0;i<6;i++) {
                var request=new NextGenSoftware.OASIS.API.Core.StorageOperationRequest {Operation="LoadAvatarDetail",AvatarId=detail.Id};
                var read=i%2==0 ? router.RouteRequest<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarDetail>(request,Core.Enums.LoadBalancingStrategy.RoundRobin)
                    :await router.RouteRequestAsync<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarDetail>(request,Core.Enums.LoadBalancingStrategy.RoundRobin);
                Assert.IsFalse(read.IsError,read.Message);counts[read.Result.Karma]=counts.GetValueOrDefault(read.Result.Karma)+1;
            }
            Assert.AreEqual(3,counts[29]);Assert.AreEqual(3,counts[999]);
            foreach(var strategy in Enum.GetValues<Core.Enums.LoadBalancingStrategy>())
            foreach(var asynchronous in new[] {false,true}) {
                var googleBefore=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.GoogleCloudOASIS).TotalRequests;
                var localBefore=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.LocalFileOASIS).TotalRequests;
                var request=new NextGenSoftware.OASIS.API.Core.StorageOperationRequest {Operation="LoadAvatarDetail",AvatarId=detail.Id};
                var read=asynchronous ? await router.RouteRequestAsync<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarDetail>(request,strategy)
                    :router.RouteRequest<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarDetail>(request,strategy);
                Assert.IsFalse(read.IsError,read.Message);
                var decision=manager.LastProviderSelectionDiagnostic;
                Assert.AreEqual(strategy,decision.RequestedStrategy);Assert.AreEqual(2,decision.Candidates.Count);
                Assert.IsTrue(decision.SelectedProvider==Core.Enums.ProviderType.GoogleCloudOASIS || decision.SelectedProvider==Core.Enums.ProviderType.LocalFileOASIS);
                var choseGoogle=decision.SelectedProvider==Core.Enums.ProviderType.GoogleCloudOASIS;
                Assert.AreEqual(choseGoogle ? 29L:999L,read.Result.Karma);
                Assert.AreEqual(googleBefore+(choseGoogle ? 1:0),manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.GoogleCloudOASIS).TotalRequests);
                Assert.AreEqual(localBefore+(choseGoogle ? 0:1),manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.LocalFileOASIS).TotalRequests);
            }
            manager.IsAutoLoadBalanceEnabled=false;manager.IsAutoReplicationEnabled=true;
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(providers).IsError);
            int amount=400;
            foreach(var primary in providers)
            foreach(var asynchronous in new[] {false,true}) {
                var value=new AvatarDetail {Id=detail.Id,IsActive=true,Username=detail.Username,Email=detail.Email,Karma=++amount,XP=321};
                value.Inventory.Add(detail.Inventory[0]);
                var request=new NextGenSoftware.OASIS.API.Core.StorageOperationRequest {Operation="SaveAvatarDetail",Payload=value,PreferredProvider=primary.Value};
                var saved=asynchronous ? await router.RouteRequestAsync<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarDetail>(request)
                    :router.RouteRequest<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarDetail>(request);
                Assert.IsFalse(saved.IsError,saved.Message);Assert.IsFalse(saved.IsWarning,saved.Message);
                var native=await google.LoadAvatarDetailAsync(detail.Id);var secondary=await local.LoadAvatarDetailAsync(detail.Id);
                Assert.AreEqual(value.Karma,native.Result.Karma);Assert.AreEqual(value.Karma,secondary.Result.Karma);
                Assert.AreEqual(detail.Inventory[0].Id,native.Result.Inventory.Single().Id);Assert.AreEqual(detail.Inventory[0].Id,secondary.Result.Inventory.Single().Id);
            }
            manager.IsAutoReplicationEnabled=false;manager.IsAutoFailOverEnabled=true;
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(providers).IsError);
            var missing=Guid.NewGuid();
            try {
                Assert.IsFalse((await local.SaveAvatarDetailAsync(new AvatarDetail {Id=missing,IsActive=true,Karma=999})).IsError);
                var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.LocalFileOASIS).TotalRequests;
                foreach(var asynchronous in new[] {false,true}) {
                    var request=new NextGenSoftware.OASIS.API.Core.StorageOperationRequest {Operation="LoadAvatarDetail",AvatarId=missing,PreferredProvider=Core.Enums.ProviderType.GoogleCloudOASIS};
                    var read=asynchronous ? await router.RouteRequestAsync<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarDetail>(request)
                        :router.RouteRequest<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarDetail>(request);
                    Assert.IsNull(read.Result);Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,read.ErrorCode);
                }
                Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.LocalFileOASIS).TotalRequests);
            }
            finally {await local.DeleteAvatarDetailAsync(missing,softDelete:false);}
            var activated=manager.SetAndActivateCurrentStorageProvider(google);
            Assert.IsFalse(activated.IsError,activated.Message);
            var field=typeof(GoogleCloudOASIS).GetField("_firestoreDb",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
            var healthy=field.GetValue(google);
            var settings=Google.Api.Gax.Grpc.CallSettingsExtensions.WithRetry(
                Google.Api.Gax.Grpc.CallSettings.FromExpiration(Google.Api.Gax.Expiration.FromTimeout(TimeSpan.FromSeconds(2))),
                Google.Api.Gax.Grpc.RetrySettings.FromConstantBackoff(1,TimeSpan.Zero,_=>false));
            var client=new Google.Cloud.Firestore.V1.FirestoreClientBuilder {
                Endpoint="127.0.0.1:18083",ChannelCredentials=Grpc.Core.ChannelCredentials.Insecure,
                Settings=new Google.Cloud.Firestore.V1.FirestoreSettings {GetDocumentSettings=settings}}.Build();
            var unreachable=FirestoreDb.Create("oasis-google-native",client);
            try {
                field.SetValue(google,unreachable);
                foreach(var asynchronous in new[] {false,true}) {
                    var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.LocalFileOASIS).TotalRequests;
                    var request=new NextGenSoftware.OASIS.API.Core.StorageOperationRequest {Operation="LoadAvatarDetail",AvatarId=detail.Id,PreferredProvider=Core.Enums.ProviderType.GoogleCloudOASIS};
                    var read=asynchronous ? await router.RouteRequestAsync<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarDetail>(request)
                        :router.RouteRequest<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarDetail>(request);
                    Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(detail.Id,read.Result.Id);
                    Assert.AreEqual(404L,read.Result.Karma);
                    Assert.AreEqual(Core.Enums.ProviderType.LocalFileOASIS,manager.LastFailoverDiagnostic.SelectedProvider);
                    Assert.AreEqual(before+1,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.LocalFileOASIS).TotalRequests);
                }
            }
            finally {field.SetValue(google,healthy);}
        }
        finally {await local.DeleteAvatarDetailAsync(detail.Id,softDelete:false);await local.DeActivateProviderAsync();}
    }
}
