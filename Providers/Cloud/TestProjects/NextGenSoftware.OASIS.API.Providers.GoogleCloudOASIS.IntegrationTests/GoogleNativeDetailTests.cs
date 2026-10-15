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
        var holonId=Guid.NewGuid();
        var childId=Guid.NewGuid();
        var avatarId=Guid.NewGuid();var duplicateAvatarId=Guid.NewGuid();
        var importedIds=new[] {Guid.NewGuid(),Guid.NewGuid()};
        try {
            var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Message);
            Assert.AreEqual(0,(await provider.LoadAllAvatarsAsync()).Result.Count());
            var avatar=new Avatar {Id=avatarId,IsActive=true,Username="owned-avatar",Email="owned-avatar@example.invalid",FirstName="Owned",
                MetaData=new Dictionary<string,object> {{"profile-field","profile-value"}}};
            var avatarSaved=await provider.SaveAvatarAsync(avatar);Assert.IsFalse(avatarSaved.IsError,avatarSaved.Message);Assert.IsTrue(avatarSaved.IsSaved);
            var avatarRead=await provider.LoadAvatarAsync(avatarId);Assert.IsFalse(avatarRead.IsError,avatarRead.Message);
            Assert.AreEqual("Owned",avatarRead.Result.FirstName);Assert.AreEqual("profile-value",avatarRead.Result.MetaData["profile-field"]);
            Assert.AreEqual(avatarId,(await provider.LoadAvatarByUsernameAsync("OWNED-AVATAR")).Result.Id);
            Assert.AreEqual(avatarId,provider.LoadAvatarByEmail("OWNED-AVATAR@EXAMPLE.INVALID").Result.Id);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await provider.LoadAvatarAsync(Guid.NewGuid())).ErrorCode);
            Assert.IsFalse((await provider.SaveAvatarAsync(new Avatar {Id=duplicateAvatarId,IsActive=true,Username=avatar.Username,Email=avatar.Email})).IsError);
            Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,(await provider.LoadAvatarByUsernameAsync(avatar.Username)).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,(await provider.LoadAvatarByEmailAsync(avatar.Email)).ErrorCode);
            Assert.AreEqual(2,(await provider.LoadAllAvatarsAsync()).Result.Count());
            Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,(await provider.DeleteAvatarByUsernameAsync(avatar.Username)).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,(await provider.DeleteAvatarByEmailAsync(avatar.Email,softDelete:false)).ErrorCode);
            Assert.AreEqual(2,(await provider.LoadAllAvatarsAsync()).Result.Count());
            var emptyHolons=await provider.LoadAllHolonsAsync();Assert.IsFalse(emptyHolons.IsError,emptyHolons.Message);Assert.AreEqual(0,emptyHolons.Result.Count());
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
            var holon=new Holon {Id=holonId,IsActive=true,Name="owned holon",ParentHolonId=detail.Id,CreatedByAvatarId=avatarId,
                MetaData=new Dictionary<string,object> {{"owned-field","owned-value"}}};
            var written=await provider.SaveHolonAsync(holon,saveChildren:false);Assert.IsFalse(written.IsError,written.Message);
            var read=await provider.LoadHolonAsync(holonId,loadChildren:false);Assert.IsFalse(read.IsError,read.Message);
            var keyRead=await provider.LoadHolonAsync(holonId.ToString(),loadChildren:false);
            Assert.IsFalse(keyRead.IsError,keyRead.Message);Assert.AreEqual(holonId,keyRead.Result.Id);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await provider.LoadHolonAsync(Guid.NewGuid().ToString(),loadChildren:false)).ErrorCode);
            Assert.AreEqual(holonId,read.Result.Id);Assert.AreEqual(detail.Id,read.Result.ParentHolonId);Assert.AreEqual(avatarId,read.Result.CreatedByAvatarId);
            Assert.AreEqual("owned-value",read.Result.MetaData["owned-field"]);
            var metadata=await provider.LoadHolonsByMetaDataAsync("owned-field","owned-value",loadChildren:false);
            Assert.IsFalse(metadata.IsError,metadata.Message);Assert.AreEqual(holonId,metadata.Result.Single().Id);
            var pairs=new Dictionary<string,string> {{"owned-field","owned-value"},{"missing","no"}};
            var any=await provider.LoadHolonsByMetaDataAsync(pairs,NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.Any);
            Assert.IsFalse(any.IsError,any.Message);Assert.AreEqual(holonId,any.Result.Single().Id);
            var all=provider.LoadHolonsByMetaData(pairs,NextGenSoftware.OASIS.API.Core.Enums.MetaKeyValuePairMatchMode.All);
            Assert.IsFalse(all.IsError,all.Message);Assert.AreEqual(0,all.Result.Count());
            var search=await provider.SearchAsync(new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchParams {
                SearchOnlyForCurrentAvatar=false,FilterByMetaData=new Dictionary<string,string> {{"owned-field","owned-value"}},
                SearchGroups=new() {new NextGenSoftware.OASIS.API.Core.Objects.Search.SearchTextGroup {SearchQuery="owned holon",SearchHolons=true}}});
            Assert.IsFalse(search.IsError,search.Message);Assert.AreEqual(1,search.Result.NumberOfResults);
            Assert.AreEqual(holonId,search.Result.SearchResultHolons.Single().Id);Assert.AreEqual(0,search.Result.SearchResultAvatars.Count);
            var allHolons=await provider.LoadAllHolonsAsync(loadChildren:false);
            Assert.IsFalse(allHolons.IsError,allHolons.Message);Assert.AreEqual(holonId,allHolons.Result.Single().Id);
            var otherType=holon.HolonType==NextGenSoftware.OASIS.API.Core.Enums.HolonType.Avatar
                ? NextGenSoftware.OASIS.API.Core.Enums.HolonType.AvatarDetail : NextGenSoftware.OASIS.API.Core.Enums.HolonType.Avatar;
            Assert.AreEqual(0,(await provider.LoadAllHolonsAsync(otherType)).Result.Count());
            var child=new Holon {Id=childId,IsActive=true,Name="owned child",ParentHolonId=holonId};
            Assert.IsFalse((await provider.SaveHolonAsync(child,saveChildren:false)).IsError);
            var parentRows=await provider.LoadHolonsForParentAsync(holonId,loadChildrenFromProvider:true);
            Assert.IsFalse(parentRows.IsError,parentRows.Message);Assert.AreEqual(childId,parentRows.Result.Single().Id);
            var parentKeyRows=provider.LoadHolonsForParent(holonId.ToString(),loadChildren:false);
            Assert.IsFalse(parentKeyRows.IsError,parentKeyRows.Message);Assert.AreEqual(childId,parentKeyRows.Result.Single().Id);
            var noParentRows=await provider.LoadHolonsForParentAsync(Guid.NewGuid());
            Assert.IsFalse(noParentRows.IsError,noParentRows.Message);Assert.AreEqual(0,noParentRows.Result.Count());
            Assert.AreEqual(0,(await provider.LoadHolonsForParentAsync(holonId,otherType)).Result.Count());
            var graph=await provider.LoadAllHolonsAsync(loadChildrenFromProvider:true,maxChildDepth:1);
            Assert.IsFalse(graph.IsError,graph.Message);
            Assert.AreEqual(childId,graph.Result.Single(row=>row.Id==holonId).Children.Single().Id);
            var singleGraph=await provider.LoadHolonAsync(holonId,loadChildrenFromProvider:true,maxChildDepth:1);
            Assert.IsFalse(singleGraph.IsError,singleGraph.Message);Assert.AreEqual(childId,singleGraph.Result.Children.Single().Id);
            var keyGraph=provider.LoadHolon(holonId.ToString(),loadChildrenFromProvider:true,maxChildDepth:1);
            Assert.IsFalse(keyGraph.IsError,keyGraph.Message);Assert.AreEqual(childId,keyGraph.Result.Children.Single().Id);
            Assert.AreEqual(0,(await provider.LoadHolonAsync(holonId,loadChildren:false,loadChildrenFromProvider:true)).Result.Children.Count());
            var flat=await provider.LoadAllHolonsAsync(loadChildren:false,loadChildrenFromProvider:true);
            Assert.IsTrue(flat.Result.All(row=>row.Children.Count()==0));
            var exported=await provider.ExportAllDataForAvatarByIdAsync(avatarId);
            Assert.IsFalse(exported.IsError,exported.Message);Assert.AreEqual(childId,exported.Result.Single().Children.Single().Id);
            var unowned=await provider.ExportAllDataForAvatarByIdAsync(Guid.NewGuid());
            Assert.IsFalse(unowned.IsError,unowned.Message);Assert.AreEqual(0,unowned.Result.Count());
            Assert.AreEqual(2,(await provider.ExportAllAsync()).Result.Count());
            Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,(await provider.ExportAllDataForAvatarByUsernameAsync(avatar.Username)).ErrorCode);
            Assert.IsFalse((await provider.DeleteAvatarAsync(duplicateAvatarId.ToString(),softDelete:false)).IsError);
            var byUsername=provider.ExportAllDataForAvatarByUsername("OWNED-AVATAR");
            Assert.IsFalse(byUsername.IsError,byUsername.Message);Assert.AreEqual(holonId,byUsername.Result.Single().Id);
            var byEmail=await provider.ExportAllDataForAvatarByEmailAsync("OWNED-AVATAR@EXAMPLE.INVALID");
            Assert.IsFalse(byEmail.IsError,byEmail.Message);Assert.AreEqual(childId,byEmail.Result.Single().Children.Single().Id);
            var pairedDetail=new AvatarDetail {Id=avatarId,IsActive=true,XP=444,Karma=555,
                MetaData=new Dictionary<string,object> {{"paired-field","paired-value"}}};
            pairedDetail.Inventory.Add(new InventoryItem {Id=Guid.NewGuid(),Name="paired seed"});ids.Add(avatarId);
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(pairedDetail)).IsError);
            var deleted=await provider.DeleteAvatarByUsernameAsync("OWNED-AVATAR");Assert.IsFalse(deleted.IsError,deleted.Message);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await provider.LoadAvatarAsync(avatarId)).ErrorCode);
            var tombstone=await db.Collection("avatars").Document(avatarId.ToString()).GetSnapshotAsync();
            var stored=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Deserialize<Avatar>(tombstone.GetValue<string>("data_json"));
            Assert.IsFalse(stored.IsActive);Assert.AreEqual(DateTimeKind.Utc,stored.DeletedDate.Kind);
            Assert.AreEqual("profile-value",stored.MetaData["profile-field"]);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await provider.LoadAvatarDetailAsync(avatarId)).ErrorCode);
            var detailTombstone=await db.Collection("avatarDetails").Document(avatarId.ToString()).GetSnapshotAsync();
            var storedDetail=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Deserialize<AvatarDetail>(detailTombstone.GetValue<string>("data_json"));
            Assert.IsFalse(storedDetail.IsActive);Assert.AreEqual(stored.DeletedDate,storedDetail.DeletedDate);
            Assert.AreEqual(444,storedDetail.XP);Assert.AreEqual(555L,storedDetail.Karma);
            Assert.AreEqual(pairedDetail.Inventory[0].Id,storedDetail.Inventory.Single().Id);
            var repeated=await provider.DeleteAvatarAsync(avatarId);Assert.IsFalse(repeated.IsError,repeated.Message);
            Assert.AreEqual(tombstone.UpdateTime,(await db.Collection("avatars").Document(avatarId.ToString()).GetSnapshotAsync()).UpdateTime);
            Assert.AreEqual(detailTombstone.UpdateTime,(await db.Collection("avatarDetails").Document(avatarId.ToString()).GetSnapshotAsync()).UpdateTime);
            Assert.IsFalse((await provider.DeleteAvatarAsync(avatarId.ToString(),softDelete:false)).IsError);
            Assert.IsFalse((await db.Collection("avatars").Document(avatarId.ToString()).GetSnapshotAsync()).Exists);
            var imported=await provider.ImportAsync(importedIds.Select(id=>new Holon {Id=id,IsActive=true,Name="owned imported",
                MetaData=new Dictionary<string,object> {{"import-field","import-value"}}}));
            Assert.IsFalse(imported.IsError,imported.Message);Assert.IsTrue(imported.Result);Assert.IsTrue(imported.IsSaved);
            foreach(var importedId in importedIds)
                Assert.AreEqual("import-value",(await provider.LoadHolonAsync(importedId,loadChildren:false)).Result.MetaData["import-field"]);
            Assert.IsFalse((await db.Collection("avatarDetails").Document(avatarId.ToString()).GetSnapshotAsync()).Exists);
            Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);
            Assert.IsFalse((await provider.DeleteAvatarByEmailAsync("OWNED-AVATAR@EXAMPLE.INVALID",softDelete:false)).IsError);
            Assert.IsFalse((await db.Collection("avatars").Document(avatarId.ToString()).GetSnapshotAsync()).Exists);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await provider.LoadHolonAsync(Guid.NewGuid(),loadChildren:false)).ErrorCode);
        }
        finally {foreach(var id in ids) await db.Collection("avatarDetails").Document(id.ToString()).DeleteAsync();foreach(var id in importedIds) await db.Collection("holons").Document(id.ToString()).DeleteAsync();await db.Collection("holons").Document(holonId.ToString()).DeleteAsync();await db.Collection("holons").Document(childId.ToString()).DeleteAsync();await db.Collection("avatars").Document(avatarId.ToString()).DeleteAsync();await db.Collection("avatars").Document(duplicateAvatarId.ToString()).DeleteAsync();await provider.DeActivateProviderAsync();}
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
                Settings=new Google.Cloud.Firestore.V1.FirestoreSettings {GetDocumentSettings=settings,CommitSettings=settings}}.Build();
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
                var failedId=Guid.NewGuid();
                var secondaryBefore=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.LocalFileOASIS).TotalRequests;
                var failed=await router.RouteRequestAsync<NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarDetail>(
                    new NextGenSoftware.OASIS.API.Core.StorageOperationRequest {Operation="SaveAvatarDetail",PreferredProvider=Core.Enums.ProviderType.GoogleCloudOASIS,
                        Payload=new AvatarDetail {Id=failedId,IsActive=true,Karma=888}});
                Assert.IsTrue(failed.IsError);
                Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(failed.Exception));
                Assert.AreEqual(secondaryBefore,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.LocalFileOASIS).TotalRequests);
                Assert.IsNull((await local.LoadAvatarDetailAsync(failedId)).Result);
            }
            finally {field.SetValue(google,healthy);}
        }
        finally {await local.DeleteAvatarDetailAsync(detail.Id,softDelete:false);await local.DeActivateProviderAsync();}
    }
}
