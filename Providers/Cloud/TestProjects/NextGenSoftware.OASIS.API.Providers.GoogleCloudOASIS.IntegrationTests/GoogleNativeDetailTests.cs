using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
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
        var graphIds=new[] {Guid.NewGuid(),Guid.NewGuid(),Guid.NewGuid()};
        try {
            var active=await provider.ActivateProviderAsync();Assert.IsFalse(active.IsError,active.Message);
            var dbField=typeof(GoogleCloudOASIS).GetField("_firestoreDb",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
            var retainedDb=dbField.GetValue(provider);
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);Assert.AreSame(retainedDb,dbField.GetValue(provider));
            Assert.AreEqual(0,(await provider.LoadAllAvatarsAsync()).Result.Count());
            var avatar=new Avatar {Id=avatarId,IsActive=true,Username="owned-avatar",Email="owned-avatar@example.invalid",FirstName="Owned",
                MetaData=new Dictionary<string,object> {{"profile-field","profile-value"},{"Latitude",-33.0},{"Longitude",151.0}}};
            var avatarSaved=await provider.SaveAvatarAsync(avatar);Assert.IsFalse(avatarSaved.IsError,avatarSaved.Message);Assert.IsTrue(avatarSaved.IsSaved);
            var avatarRead=await provider.LoadAvatarAsync(avatarId);Assert.IsFalse(avatarRead.IsError,avatarRead.Message);
            var keyReadAvatar=await provider.LoadAvatarByProviderKeyAsync(avatarId.ToString());
            Assert.IsFalse(keyReadAvatar.IsError,keyReadAvatar.Message);Assert.IsTrue(keyReadAvatar.IsLoaded);
            Assert.AreEqual(avatarId,keyReadAvatar.Result.Id);Assert.AreEqual(avatarRead.Result.Username,keyReadAvatar.Result.Username);
            Assert.AreEqual(avatarRead.Result.Email,keyReadAvatar.Result.Email);
            var keyReadSync=provider.LoadAvatarByProviderKey(avatarId.ToString());
            Assert.IsFalse(keyReadSync.IsError,keyReadSync.Message);Assert.AreEqual(avatarId,keyReadSync.Result.Id);
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
            for(var index=0;index<2;index++)detail.KarmaAkashicRecords.Add(new KarmaAkashicRecord{
                AvatarId=detail.Id,Date=new DateTime(2026,10,20,0,0,0,DateTimeKind.Utc).AddSeconds(index),
                Karma=index==0?39:10,TotalKarma=index==0?39:29,KarmaSourceTitle="Firestore native",KarmaSourceDesc="owned history",
                WebLink="https://example.invalid/owned-history",
                Provider=new(NextGenSoftware.OASIS.API.Core.Enums.ProviderType.GoogleCloudOASIS),
                KarmaSource=new(NextGenSoftware.OASIS.API.Core.Enums.KarmaSourceType.Game),
                KarmaEarntOrLost=new(index==0?NextGenSoftware.OASIS.API.Core.Enums.KarmaEarntOrLost.Earnt:NextGenSoftware.OASIS.API.Core.Enums.KarmaEarntOrLost.Lost),
                KarmaTypePositive=new(index==0?NextGenSoftware.OASIS.API.Core.Enums.KarmaTypePositive.Other:NextGenSoftware.OASIS.API.Core.Enums.KarmaTypePositive.None),
                KarmaTypeNegative=new(index==0?NextGenSoftware.OASIS.API.Core.Enums.KarmaTypeNegative.None:NextGenSoftware.OASIS.API.Core.Enums.KarmaTypeNegative.Other)});
            void CheckHistory(NextGenSoftware.OASIS.API.Core.Interfaces.IAvatarDetail actual){
                Assert.AreEqual(2,actual.KarmaAkashicRecords.Count);
                for(var index=0;index<2;index++){
                    var expected=detail.KarmaAkashicRecords[index];var entry=actual.KarmaAkashicRecords[index];
                    Assert.AreEqual(detail.Id,entry.AvatarId);Assert.AreEqual(expected.Date,entry.Date);Assert.AreEqual(expected.Karma,entry.Karma);Assert.AreEqual(expected.TotalKarma,entry.TotalKarma);
                    Assert.AreEqual(expected.Provider.Value,entry.Provider.Value);Assert.AreEqual(expected.KarmaSource.Value,entry.KarmaSource.Value);
                    Assert.AreEqual(expected.KarmaEarntOrLost.Value,entry.KarmaEarntOrLost.Value);Assert.AreEqual(expected.KarmaTypePositive.Value,entry.KarmaTypePositive.Value);Assert.AreEqual(expected.KarmaTypeNegative.Value,entry.KarmaTypeNegative.Value);
                    Assert.AreEqual(expected.KarmaSourceTitle,entry.KarmaSourceTitle);Assert.AreEqual(expected.KarmaSourceDesc,entry.KarmaSourceDesc);Assert.AreEqual(expected.WebLink,entry.WebLink);
                }
            }
            var saved=await provider.SaveAvatarDetailAsync(detail);Assert.IsFalse(saved.IsError,saved.Message);Assert.IsTrue(saved.IsSaved);
            var loaded=await provider.LoadAvatarDetailAsync(detail.Id);Assert.IsFalse(loaded.IsError,loaded.Message);
            CheckHistory(loaded.Result);
            detail.KarmaAkashicRecords[1].KarmaSourceDesc="owned updated history";
            var updatedHistory=provider.SaveAvatarDetail(detail);Assert.IsFalse(updatedHistory.IsError,updatedHistory.Message);
            foreach(var selectedDetail in new[]{await provider.LoadAvatarDetailAsync(detail.Id),await provider.LoadAvatarDetailByUsernameAsync(detail.Username),provider.LoadAvatarDetailByEmail(detail.Email)}){
                Assert.IsFalse(selectedDetail.IsError,selectedDetail.Message);CheckHistory(selectedDetail.Result);
            }
            var storedHistory=await db.Collection("avatarDetails").Document(detail.Id.ToString()).GetSnapshotAsync();
            CheckHistory(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Deserialize<AvatarDetail>(storedHistory.GetValue<string>("data_json")));
            Assert.AreEqual(321,loaded.Result.XP);Assert.AreEqual(29L,loaded.Result.Karma);
            Assert.AreEqual(detail.Inventory[0].Id,loaded.Result.Inventory.Single().Id);Assert.AreEqual("owned-value",loaded.Result.MetaData["owned-field"]);
            Assert.AreEqual(detail.Id,(await provider.LoadAvatarDetailByUsernameAsync(detail.Username)).Result.Id);
            Assert.AreEqual(detail.Id,provider.LoadAvatarDetailByEmail(detail.Email).Result.Id);
            Assert.AreEqual(detail.Id,(await provider.LoadAvatarDetailByUsernameAsync(detail.Username.ToUpperInvariant())).Result.Id);
            Assert.AreEqual(detail.Id,provider.LoadAvatarDetailByEmail(detail.Email.ToUpperInvariant()).Result.Id);
            var duplicate=new AvatarDetail {Id=Guid.NewGuid(),IsActive=true,Username=detail.Username.ToUpperInvariant(),Email=detail.Email.ToUpperInvariant()};ids.Add(duplicate.Id);
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(duplicate)).IsError);
            Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,(await provider.LoadAvatarDetailByEmailAsync(detail.Email)).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.AmbiguousIdentity,(await provider.LoadAvatarDetailByUsernameAsync(detail.Username)).ErrorCode);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await provider.LoadAvatarDetailAsync(Guid.NewGuid())).ErrorCode);
            Assert.AreEqual(2,(await provider.LoadAllAvatarDetailsAsync()).Result.Count());
            await VerifyV2Async(provider,detail);
            await VerifySqliteDeleteReplicationAsync(provider,db);
            await VerifyNativePartialBatchAsync(db);
            var holon=new Holon {Id=holonId,IsActive=true,Name="owned holon",ParentHolonId=detail.Id,CreatedByAvatarId=avatarId,
                MetaData=new Dictionary<string,object> {{"owned-field","owned-value"},{"Latitude",-33.0},{"Longitude",151.0}}};
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
            var network=(Core.Interfaces.IOASISNETProvider)provider;
            var nearAvatars=network.GetAvatarsNearMe(-33000000,151000000,100);
            Assert.IsFalse(nearAvatars.IsError,nearAvatars.Message);Assert.AreEqual(avatarId,nearAvatars.Result.Single().Id);
            var nearHolons=network.GetHolonsNearMe(-33000000,151000000,100,Core.Enums.HolonType.All);
            Assert.IsFalse(nearHolons.IsError,nearHolons.Message);Assert.AreEqual(holonId,nearHolons.Result.Single().Id);
            Assert.IsFalse(nearHolons.Result.Single().MetaData.ContainsKey("Distance"));
            Assert.AreEqual(0,network.GetHolonsNearMe(0,0,100,Core.Enums.HolonType.All).Result.Count());
            Assert.AreEqual(0,network.GetHolonsNearMe(-33001000,151000000,50,Core.Enums.HolonType.All).Result.Count());
            Assert.AreEqual(holonId,network.GetHolonsNearMe(-33001000,151000000,200,Core.Enums.HolonType.All).Result.Single().Id);
            Assert.AreEqual(0,network.GetHolonsNearMe(-33000000,151000000,100,otherType).Result.Count());
            var priorCulture=System.Globalization.CultureInfo.CurrentCulture;
            try
            {
                System.Globalization.CultureInfo.CurrentCulture=new System.Globalization.CultureInfo("fr-FR");
                var nearbyProfile=network.GetAvatarsNearMe(-33000000,151000000,100);
                Assert.IsFalse(nearbyProfile.IsError,nearbyProfile.Message);Assert.IsTrue(nearbyProfile.IsLoaded);
                Assert.AreEqual("profile-value",nearbyProfile.Result.Single().MetaData["profile-field"]);
                foreach(var record in new NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase[] {avatar,holon})
                {
                    var original=record.MetaData["Longitude"];
                    record.MetaData.Remove("Longitude");
                    if(record is NextGenSoftware.OASIS.API.Core.Interfaces.IAvatar profile)
                        Assert.IsFalse((await provider.SaveAvatarAsync(profile)).IsError);
                    else Assert.IsFalse((await provider.SaveHolonAsync((NextGenSoftware.OASIS.API.Core.Interfaces.IHolon)record,saveChildren:false)).IsError);
                    if(record is NextGenSoftware.OASIS.API.Core.Interfaces.IAvatar)
                    {
                        var corrupt=network.GetAvatarsNearMe(-33000000,151000000,100);
                        Assert.IsTrue(corrupt.IsError);Assert.IsNull(corrupt.Result);Assert.IsFalse(corrupt.IsLoaded);Assert.IsInstanceOfType(corrupt.Exception,typeof(System.IO.InvalidDataException));
                    }
                    else
                    {
                        var corrupt=network.GetHolonsNearMe(-33000000,151000000,100,Core.Enums.HolonType.All);
                        Assert.IsTrue(corrupt.IsError);Assert.IsNull(corrupt.Result);Assert.IsFalse(corrupt.IsLoaded);Assert.IsInstanceOfType(corrupt.Exception,typeof(System.IO.InvalidDataException));
                    }
                    record.MetaData["Longitude"]=original;
                    if(record is NextGenSoftware.OASIS.API.Core.Interfaces.IAvatar restored)
                        Assert.IsFalse((await provider.SaveAvatarAsync(restored)).IsError);
                    else Assert.IsFalse((await provider.SaveHolonAsync((NextGenSoftware.OASIS.API.Core.Interfaces.IHolon)record,saveChildren:false)).IsError);
                }
                Assert.IsFalse(network.GetAvatarsNearMe(-33000000,151000000,100).IsError);
                Assert.IsFalse(network.GetHolonsNearMe(-33000000,151000000,100,Core.Enums.HolonType.All).IsError);
            }
            finally {System.Globalization.CultureInfo.CurrentCulture=priorCulture;}
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
            Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.AvatarTombstoneEvidence.TryGet(deleted,out var committedTombstone));
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
            Assert.AreEqual(stored.DeletedDate,committedTombstone.DeletedAtUtc);
            Assert.IsFalse((await provider.ApplyAvatarTombstoneAsync(committedTombstone)).IsError);
            var conflict=await provider.ApplyAvatarTombstoneAsync(new AvatarTombstone(avatarId,committedTombstone.DeletedAtUtc.AddSeconds(1)));
            Assert.IsTrue(conflict.IsError);Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException.ErrorCode,conflict.ErrorCode);
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
            var graphGrandchild=new Holon {Id=graphIds[2],IsActive=true,Name="graph grandchild"};
            var graphChild=new Holon {Id=graphIds[1],IsActive=true,Name="graph child",Children=new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> {graphGrandchild}};
            var graphRoot=new Holon {Id=graphIds[0],IsActive=true,Name="graph root",Children=new List<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon> {graphChild}};
            Assert.IsFalse((await provider.SaveHolonAsync(graphRoot,saveChildren:false)).IsError);
            Assert.IsFalse((await db.Collection("holons").Document(graphIds[1].ToString()).GetSnapshotAsync()).Exists);
            Assert.IsFalse((await provider.SaveHolonsAsync(new[] {graphRoot},maxChildDepth:1,curentChildDepth:1)).IsError);
            Assert.IsFalse((await db.Collection("holons").Document(graphIds[1].ToString()).GetSnapshotAsync()).Exists);
            Assert.IsFalse((await provider.SaveHolonAsync(graphRoot,maxChildDepth:1)).IsError);
            Assert.IsFalse((await db.Collection("holons").Document(graphIds[2].ToString()).GetSnapshotAsync()).Exists);
            Assert.IsFalse((await provider.SaveHolonAsync(graphRoot,recursive:false)).IsError);
            Assert.AreEqual(graphIds[0],(await provider.LoadHolonAsync(graphIds[1],loadChildren:false)).Result.ParentHolonId);
            Assert.IsFalse((await db.Collection("holons").Document(graphIds[2].ToString()).GetSnapshotAsync()).Exists);
            Assert.IsFalse((await provider.SaveHolonAsync(graphRoot)).IsError);
            var graphLoaded=await provider.LoadHolonAsync(graphIds[0],loadChildrenFromProvider:true);
            Assert.IsFalse(graphLoaded.IsError,graphLoaded.Message);
            Assert.AreEqual(graphIds[2],graphLoaded.Result.Children.Single().Children.Single().Id);
            var deleteHolon=await provider.DeleteHolonAsync(graphIds[2]);
            Assert.IsFalse(deleteHolon.IsError,deleteHolon.Message);Assert.AreEqual(graphIds[1],deleteHolon.Result.ParentHolonId);
            Assert.IsFalse((await db.Collection("holons").Document(graphIds[2].ToString()).GetSnapshotAsync()).Exists);
            var deleteKey=await provider.DeleteHolonAsync(importedIds[0].ToString());
            Assert.IsFalse(deleteKey.IsError,deleteKey.Message);Assert.AreEqual("import-value",deleteKey.Result.MetaData["import-field"]);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await provider.DeleteHolonAsync(importedIds[0].ToString())).ErrorCode);
            Assert.IsFalse((await db.Collection("avatarDetails").Document(avatarId.ToString()).GetSnapshotAsync()).Exists);
            Assert.IsFalse((await provider.SaveAvatarAsync(avatar)).IsError);
            Assert.IsFalse((await provider.DeleteAvatarByEmailAsync("OWNED-AVATAR@EXAMPLE.INVALID",softDelete:false)).IsError);
            Assert.IsFalse((await db.Collection("avatars").Document(avatarId.ToString()).GetSnapshotAsync()).Exists);
            Assert.IsFalse((await provider.SaveAvatarAsync(new Avatar {Id=duplicateAvatarId,IsActive=true,Username="replica-owned"})).IsError);
            ids.Add(duplicateAvatarId);
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail {Id=duplicateAvatarId,IsActive=true,XP=777,Karma=888})).IsError);
            var exact=new AvatarTombstone(duplicateAvatarId,new DateTime(2026,10,15,1,2,3,DateTimeKind.Utc),Guid.NewGuid());
            var applied=provider.ApplyAvatarTombstone(exact);Assert.IsFalse(applied.IsError,applied.Message);
            Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.AvatarTombstoneEvidence.TryGet(applied,out var appliedEvidence));
            Assert.AreEqual(exact.DeletedAtUtc,appliedEvidence.DeletedAtUtc);Assert.AreEqual(exact.DeletedByAvatarId,appliedEvidence.DeletedByAvatarId);
            var replicaSnapshot=await db.Collection("avatarDetails").Document(duplicateAvatarId.ToString()).GetSnapshotAsync();
            var replica=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Deserialize<AvatarDetail>(replicaSnapshot.GetValue<string>("data_json"));
            Assert.AreEqual(exact.DeletedAtUtc,replica.DeletedDate);Assert.AreEqual(exact.DeletedByAvatarId,replica.DeletedByAvatarId);
            Assert.AreEqual(777,replica.XP);Assert.AreEqual(888L,replica.Karma);
            Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,(await provider.LoadHolonAsync(Guid.NewGuid(),loadChildren:false)).ErrorCode);
        }
        finally {foreach(var id in ids) await db.Collection("avatarDetails").Document(id.ToString()).DeleteAsync();foreach(var id in importedIds.Concat(graphIds)) await db.Collection("holons").Document(id.ToString()).DeleteAsync();await db.Collection("holons").Document(holonId.ToString()).DeleteAsync();await db.Collection("holons").Document(childId.ToString()).DeleteAsync();await db.Collection("avatars").Document(avatarId.ToString()).DeleteAsync();await db.Collection("avatars").Document(duplicateAvatarId.ToString()).DeleteAsync();await provider.DeActivateProviderAsync();}
    }

    private static async Task VerifySqliteDeleteReplicationAsync(GoogleCloudOASIS google,FirestoreDb db)
    {
        var path=Path.GetFullPath("TestResults/GoogleRoutingFiles/tombstone-routing.db");
        if(File.Exists(path)) Assert.Fail("Retained SQLite evidence exists; refusing reset.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var sqlite=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False");
        var ids=new List<Guid>();var verified=false;
        try
        {
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            foreach(var primary in new[] {Core.Enums.ProviderType.GoogleCloudOASIS,Core.Enums.ProviderType.SQLLiteDBOASIS})
            foreach(var asynchronous in new[] {false,true})
            {
                var id=Guid.NewGuid();ids.Add(id);
                foreach(var store in new Core.Interfaces.IOASISStorageProvider[] {google,sqlite})
                {
                    var avatar=new Avatar {Id=id,Username="owned-"+id,Email=id+"@example.invalid",IsActive=true,
                        AvatarType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)};
                    Assert.IsFalse((await store.SaveAvatarAsync(avatar)).IsError);
                    var detail=new AvatarDetail {Id=id,Username=avatar.Username,IsActive=true,XP=ReferenceEquals(store,google) ? 321:999,Karma=29};
                    detail.Inventory.Add(new InventoryItem {Id=id,Name="retained owned seed"});
                    Assert.IsFalse((await store.SaveAvatarDetailAsync(detail)).IsError);
                }
                var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                    HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,StorageProviders=new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings(),
                    SubscriptionConfig=new NextGenSoftware.OASIS.API.DNA.SubscriptionConfig {PlanType="Gold"}}};
                var manager=new Core.Managers.ProviderManager(null,dna) {IsAutoReplicationEnabled=true,IsAutoFailOverEnabled=false,IsAutoLoadBalanceEnabled=false};
                manager.RegisterProvider(google);manager.RegisterProvider(sqlite);
                Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(new[] {
                    new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.GoogleCloudOASIS),
                    new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS)}).IsError);
                var router=new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
                var actor=Guid.NewGuid();
                var profile=new Avatar {Id=id,IsActive=true,Username="owned-"+id,Email=id+"@example.invalid",FirstName="replicated profile",DeletedByAvatarId=actor,
                    AvatarType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User),
                    MetaData=new Dictionary<string,object> {{"replicated-field","replicated-value"}}};
                var profileRequest=new Core.StorageOperationRequest {Operation="SaveAvatar",Payload=profile,PreferredProvider=primary};
                var profileSaved=asynchronous ? await router.RouteRequestAsync<Core.Interfaces.IAvatar>(profileRequest):router.RouteRequest<Core.Interfaces.IAvatar>(profileRequest);
                Assert.IsFalse(profileSaved.IsError,profileSaved.Message);Assert.IsFalse(profileSaved.IsWarning,profileSaved.Message);
                foreach(var store in new Core.Interfaces.IOASISStorageProvider[] {google,sqlite})
                {
                    var read=await store.LoadAvatarAsync(id);Assert.IsFalse(read.IsError,read.Message);
                    Assert.AreEqual("replicated profile",read.Result.FirstName);Assert.AreEqual("replicated-value",read.Result.MetaData["replicated-field"].ToString());
                }
                var holon=new Holon {Id=id,IsActive=true,Name="replicated holon",ParentHolonId=Guid.NewGuid(),CreatedByAvatarId=id,
                    MetaData=new Dictionary<string,object> {{"replicated-holon","retained-value"}}};
                var holonRequest=new Core.StorageOperationRequest {Operation="SaveHolon",Payload=holon,SaveChildren=false,PreferredProvider=primary};
                var holonSaved=asynchronous ? await router.RouteRequestAsync<Core.Interfaces.IHolon>(holonRequest):router.RouteRequest<Core.Interfaces.IHolon>(holonRequest);
                Assert.IsFalse(holonSaved.IsError,holonSaved.Message);Assert.IsFalse(holonSaved.IsWarning,holonSaved.Message);
                foreach(var store in new Core.Interfaces.IOASISStorageProvider[] {google,sqlite})
                {
                    var read=await store.LoadHolonAsync(id,loadChildren:false);Assert.IsFalse(read.IsError,read.Message);
                    Assert.AreEqual(holon.ParentHolonId,read.Result.ParentHolonId);Assert.AreEqual(id,read.Result.CreatedByAvatarId);
                    Assert.AreEqual("retained-value",read.Result.MetaData["replicated-holon"].ToString());
                }
                if(primary==Core.Enums.ProviderType.GoogleCloudOASIS && !asynchronous)
                    await VerifyAvatarHolonFailoverAsync(google,sqlite,manager,router,id);
                var request=new Core.StorageOperationRequest {Operation="DeleteAvatar",AvatarId=id,SoftDelete=true,PreferredProvider=primary};
                var deleted=asynchronous ? await router.RouteRequestAsync<bool>(request):router.RouteRequest<bool>(request);
                Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsFalse(deleted.IsWarning,deleted.Message);Assert.IsTrue(deleted.Result);
                Assert.IsTrue(Core.Helpers.AvatarTombstoneEvidence.TryGet(deleted,out var committed));
                Assert.AreEqual(actor,committed.DeletedByAvatarId);
                var googleAvatar=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Deserialize<Avatar>(
                    (await db.Collection("avatars").Document(id.ToString()).GetSnapshotAsync()).GetValue<string>("data_json"));
                var googleDetail=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.Deserialize<AvatarDetail>(
                    (await db.Collection("avatarDetails").Document(id.ToString()).GetSnapshotAsync()).GetValue<string>("data_json"));
                var sqlAvatar=(await sqlite.LoadAvatarAsync(id)).Result;var sqlDetail=(await sqlite.LoadAvatarDetailAsync(id)).Result;
                Assert.IsFalse(googleAvatar.IsActive);Assert.IsFalse(googleDetail.IsActive);Assert.IsFalse(sqlAvatar.IsActive);Assert.IsFalse(sqlDetail.IsActive);
                foreach(var row in new Core.Interfaces.IHolonBase[] {googleAvatar,googleDetail,sqlAvatar,sqlDetail})
                {
                    Assert.AreEqual(committed.DeletedAtUtc,row.DeletedDate);Assert.AreEqual(DateTimeKind.Utc,row.DeletedDate.Kind);
                    Assert.AreEqual(committed.DeletedByAvatarId,row.DeletedByAvatarId);
                }
                Assert.AreEqual(321,googleDetail.XP);Assert.AreEqual(999,sqlDetail.XP);
                Assert.AreEqual(id,googleDetail.Inventory.Single().Id);Assert.AreEqual(id,sqlDetail.Inventory.Single().Id);
                var hardRequest=new Core.StorageOperationRequest {Operation="DeleteAvatar",AvatarId=id,SoftDelete=false,PreferredProvider=primary};
                var hard=asynchronous ? await router.RouteRequestAsync<bool>(hardRequest):router.RouteRequest<bool>(hardRequest);
                Assert.IsFalse(hard.IsError,hard.Message);Assert.IsFalse(hard.IsWarning,hard.Message);Assert.IsTrue(hard.Result);
                Assert.IsFalse((await db.Collection("avatars").Document(id.ToString()).GetSnapshotAsync()).Exists);
                Assert.IsFalse((await db.Collection("avatarDetails").Document(id.ToString()).GetSnapshotAsync()).Exists);
                Assert.IsNull((await sqlite.LoadAvatarAsync(id)).Result);Assert.IsNull((await sqlite.LoadAvatarDetailAsync(id)).Result);
            }
            verified=true;
        }
        finally
        {
            foreach(var id in ids) {await db.Collection("avatars").Document(id.ToString()).DeleteAsync();await db.Collection("avatarDetails").Document(id.ToString()).DeleteAsync();await db.Collection("holons").Document(id.ToString()).DeleteAsync();}
            await sqlite.DeActivateProviderAsync();sqlite.Dispose();
            if(verified && File.Exists(path)) File.Delete(path);
        }
    }

    private static async Task VerifyAvatarHolonFailoverAsync(GoogleCloudOASIS google,Core.Interfaces.IOASISStorageProvider sqlite,
        Core.Managers.ProviderManager manager,Core.Managers.OASISHyperDrive.OASISHyperDrive router,Guid id)
    {
        manager.IsAutoReplicationEnabled=false;manager.IsAutoFailOverEnabled=true;
        Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(new[] {
            new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.GoogleCloudOASIS),
            new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS)}).IsError);
        var field=typeof(GoogleCloudOASIS).GetField("_firestoreDb",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
        var healthy=field.GetValue(google);
        try
        {
            var staleId=Guid.NewGuid();
            Assert.IsFalse((await sqlite.SaveAvatarAsync(new Avatar {Id=staleId,IsActive=true,Username="stale-"+staleId,
                AvatarType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)})).IsError);
            Assert.IsFalse((await sqlite.SaveHolonAsync(new Holon {Id=staleId,IsActive=true},saveChildren:false)).IsError);
            foreach(var asynchronous in new[] {false,true})
            {
                var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests;
                var missingAvatar=new Core.StorageOperationRequest {Operation="LoadAvatar",AvatarId=staleId,PreferredProvider=Core.Enums.ProviderType.GoogleCloudOASIS};
                var avatar=asynchronous ? await router.RouteRequestAsync<Core.Interfaces.IAvatar>(missingAvatar):router.RouteRequest<Core.Interfaces.IAvatar>(missingAvatar);
                Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,avatar.ErrorCode);Assert.IsNull(avatar.Result);
                var missingHolon=new Core.StorageOperationRequest {Operation="LoadHolon",HolonId=staleId,LoadChildren=false,PreferredProvider=Core.Enums.ProviderType.GoogleCloudOASIS};
                var holon=asynchronous ? await router.RouteRequestAsync<Core.Interfaces.IHolon>(missingHolon):router.RouteRequest<Core.Interfaces.IHolon>(missingHolon);
                Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,holon.ErrorCode);Assert.IsNull(holon.Result);
                Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests);
            }
            var settings=Google.Api.Gax.Grpc.CallSettingsExtensions.WithRetry(
                Google.Api.Gax.Grpc.CallSettings.FromExpiration(Google.Api.Gax.Expiration.FromTimeout(TimeSpan.FromSeconds(2))),
                Google.Api.Gax.Grpc.RetrySettings.FromConstantBackoff(1,TimeSpan.Zero,_=>false));
            var client=new Google.Cloud.Firestore.V1.FirestoreClientBuilder {Endpoint="127.0.0.1:18083",ChannelCredentials=Grpc.Core.ChannelCredentials.Insecure,
                Settings=new Google.Cloud.Firestore.V1.FirestoreSettings {GetDocumentSettings=settings}}.Build();
            field.SetValue(google,FirestoreDb.Create("oasis-google-native",client));
            foreach(var asynchronous in new[] {false,true})
            {
                var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests;
                var avatarRequest=new Core.StorageOperationRequest {Operation="LoadAvatar",AvatarId=id,PreferredProvider=Core.Enums.ProviderType.GoogleCloudOASIS};
                var avatar=asynchronous ? await router.RouteRequestAsync<Core.Interfaces.IAvatar>(avatarRequest):router.RouteRequest<Core.Interfaces.IAvatar>(avatarRequest);
                Assert.IsFalse(avatar.IsError,avatar.Message);Assert.AreEqual("replicated profile",avatar.Result.FirstName);
                Assert.AreEqual(Core.Enums.ProviderType.SQLLiteDBOASIS,manager.LastFailoverDiagnostic.SelectedProvider);
                var holonRequest=new Core.StorageOperationRequest {Operation="LoadHolon",HolonId=id,LoadChildren=false,PreferredProvider=Core.Enums.ProviderType.GoogleCloudOASIS};
                var holon=asynchronous ? await router.RouteRequestAsync<Core.Interfaces.IHolon>(holonRequest):router.RouteRequest<Core.Interfaces.IHolon>(holonRequest);
                Assert.IsFalse(holon.IsError,holon.Message);Assert.AreEqual(id,holon.Result.Id);
                Assert.AreEqual("retained-value",holon.Result.MetaData["replicated-holon"].ToString());
                Assert.AreEqual(Core.Enums.ProviderType.SQLLiteDBOASIS,manager.LastFailoverDiagnostic.SelectedProvider);
                Assert.AreEqual(before+2,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS).TotalRequests);
            }
        }
        finally {field.SetValue(google,healthy);manager.IsAutoFailOverEnabled=false;manager.IsAutoReplicationEnabled=true;}
    }

    private sealed class InterruptAfterFirstWriteProvider : GoogleCloudOASIS
    {
        private readonly FirestoreDb _loseReply;
        public int Writes;
        public InterruptAfterFirstWriteProvider(FirestoreDb loseReply):base(projectId:"oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false)
            =>_loseReply=loseReply;
        protected override async Task<OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>> SavePlannedHolonAsync(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.PlannedHolonWrite write)
        {
            Writes++;
            var result=await base.SavePlannedHolonAsync(write);
            if(Writes==1 && !result.IsError)
                typeof(GoogleCloudOASIS).GetField("_firestoreDb",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(this,_loseReply);
            return result;
        }
    }

    private static async Task VerifyNativePartialBatchAsync(FirestoreDb healthy)
    {
        var client=new GoogleNativeFirestore.CommittedReplyLossClient(GoogleNativeFirestore.Database().Client);
        var provider=new InterruptAfterFirstWriteProvider(FirestoreDb.Create("oasis-google-native",client));
        var rows=Enumerable.Range(0,3).Select(_=>new Holon {Id=Guid.NewGuid(),IsActive=true,Name="owned partial"}).ToArray();
        var work=Environment.GetEnvironmentVariable("OASIS_FIRESTORE_HISTORY_WORK")??throw new InvalidOperationException("Durable native runner required.");
        var journal=Path.Combine(work,"history-native-partial.pending.json");
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            foreach(var row in rows)Assert.IsFalse((await healthy.Collection("holons").Document(row.Id.ToString()).GetSnapshotAsync()).Exists);
            using(var intent=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
                intent.Write(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new{collection="holons",ids=rows.Select(row=>row.Id).ToArray(),state="before-partial-batch"}));intent.Flush(true);
            }
            var result=await provider.SaveHolonsAsync(rows,saveChildren:false,continueOnError:true);
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(2,provider.Writes);
            var partial=result.Exception as GoogleFirestorePartialMutationException;
            Assert.IsNotNull(partial);CollectionAssert.AreEqual(new[] {rows[0].Id},partial.AcknowledgedRecordIds.ToArray());
            Assert.IsInstanceOfType(partial.InnerException,typeof(GoogleFirestoreMutationReconciliationException));
            Assert.AreSame(client.Lost,partial.InnerException.InnerException);Assert.AreEqual(1,client.Commits);
            Assert.IsTrue((await healthy.Collection("holons").Document(rows[0].Id.ToString()).GetSnapshotAsync()).Exists);
            Assert.IsTrue((await healthy.Collection("holons").Document(rows[1].Id.ToString()).GetSnapshotAsync()).Exists,"Inspect actual committed original; never replay it.");
            Assert.IsFalse((await healthy.Collection("holons").Document(rows[2].Id.ToString()).GetSnapshotAsync()).Exists);
            foreach(var row in rows.Take(2)){
                var reference=healthy.Collection("holons").Document(row.Id.ToString());var observed=await reference.GetSnapshotAsync();
                Assert.AreEqual(row.Id.ToString(),observed.GetValue<string>("id"));
                await reference.DeleteAsync(Precondition.LastUpdated(observed.UpdateTime!.Value));Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);
            }
            File.Delete(journal);
        }
        finally
        {
            await provider.DeActivateProviderAsync();
        }
    }

    private static async Task VerifyV2Async(GoogleCloudOASIS google,AvatarDetail detail)
    {
        var credentialId=Guid.NewGuid();var walletId=Guid.NewGuid();var credential="owned-policy-"+credentialId;
        var operations=new[] {"LoadAvatarByVerificationToken","LoadAvatarByResetToken","LoadAvatarByRefreshToken","LoadAvatarByPublicKey","LoadAvatarByPrivateKey"};
        Core.StorageOperationRequest CredentialRequest(string operation)=>new() {Operation=operation,VerificationToken=credential,ResetToken=credential,
            RefreshToken=credential,PublicKey=credential,PrivateKey=credential};
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
            foreach(var primary in providers)
            foreach(var asynchronous in new[] {false,true})
            {
                var profile=new Avatar {Id=credentialId,Username="owned-policy-"+credentialId,IsActive=true,
                    AvatarType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User),
                    VerificationToken=credential,ResetToken=credential,RefreshTokens=new() {new RefreshToken {Token=credential}},
                    ProviderWallets=new() {{Core.Enums.ProviderType.SolanaOASIS,new() {new ProviderWallet {Id=walletId,PublicKey=credential,PrivateKey=credential}}}}};
                var request=new Core.StorageOperationRequest {Operation="SaveAvatar",Payload=profile,PreferredProvider=primary.Value};
                var saved=asynchronous ? await router.RouteRequestAsync<Core.Interfaces.IAvatar>(request):router.RouteRequest<Core.Interfaces.IAvatar>(request);
                Assert.IsFalse(saved.IsError,saved.Message);Assert.IsFalse(saved.IsWarning,saved.Message);
                var detailRequest=new Core.StorageOperationRequest {Operation="SaveAvatarDetail",Payload=new AvatarDetail {Id=credentialId,IsActive=true},PreferredProvider=primary.Value};
                var paired=asynchronous ? await router.RouteRequestAsync<Core.Interfaces.IAvatarDetail>(detailRequest):router.RouteRequest<Core.Interfaces.IAvatarDetail>(detailRequest);
                Assert.IsFalse(paired.IsError,paired.Message);Assert.IsFalse(paired.IsWarning,paired.Message);
                manager.IsAutoReplicationEnabled=false;
                foreach(var store in providers)
                foreach(var operation in operations)
                {
                    var readRequest=CredentialRequest(operation);readRequest.PreferredProvider=store.Value;
                    var read=asynchronous ? await router.RouteRequestAsync<Core.Interfaces.IAvatar>(readRequest):router.RouteRequest<Core.Interfaces.IAvatar>(readRequest);
                    Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(credentialId,read.Result.Id);
                    Assert.AreEqual(walletId,read.Result.ProviderWallets[Core.Enums.ProviderType.SolanaOASIS].Single().Id);
                }
                manager.IsAutoReplicationEnabled=true;
            }
            manager.IsAutoReplicationEnabled=false;manager.IsAutoLoadBalanceEnabled=true;
            foreach(var operation in operations)
            {
                var before=providers.Select(type=>manager.PerformanceMonitor.GetMetrics(type.Value).TotalRequests).ToArray();
                for(var index=0;index<2;index++)
                {
                    var request=CredentialRequest(operation);
                    var read=index==0 ? router.RouteRequest<Core.Interfaces.IAvatar>(request,Core.Enums.LoadBalancingStrategy.RoundRobin)
                        :await router.RouteRequestAsync<Core.Interfaces.IAvatar>(request,Core.Enums.LoadBalancingStrategy.RoundRobin);
                    Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(credentialId,read.Result.Id);
                }
                for(var index=0;index<2;index++) Assert.AreEqual(before[index]+1,manager.PerformanceMonitor.GetMetrics(providers[index].Value).TotalRequests);
            }
            manager.IsAutoLoadBalanceEnabled=false;
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
                Settings=new Google.Cloud.Firestore.V1.FirestoreSettings {GetDocumentSettings=settings,CommitSettings=settings,RunQuerySettings=settings}}.Build();
            var unreachable=FirestoreDb.Create("oasis-google-native",client);
            try {
                field.SetValue(google,unreachable);
                foreach(var operation in operations)
                foreach(var asynchronous in new[] {false,true})
                {
                    // Fresh contexts require a real SDK failure for each selector, not an already-open circuit.
                    var failover=new Core.Managers.ProviderManager(null,dna) {IsAutoFailOverEnabled=true};failover.RegisterProvider(google);failover.RegisterProvider(local);
                    Assert.IsFalse(failover.SetAndReplaceAutoFailOverListForProviders(providers).IsError);
                    var fallback=new Core.Managers.OASISHyperDrive.OASISHyperDrive(failover);var request=CredentialRequest(operation);
                    request.PreferredProvider=Core.Enums.ProviderType.GoogleCloudOASIS;
                    var read=asynchronous ? await fallback.RouteRequestAsync<Core.Interfaces.IAvatar>(request):fallback.RouteRequest<Core.Interfaces.IAvatar>(request);
                    Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(credentialId,read.Result.Id);
                    foreach(var provider in providers) Assert.AreEqual(1L,failover.PerformanceMonitor.GetMetrics(provider.Value).TotalRequests);
                }
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
                Assert.IsFalse(failed.IsError,failed.Exception?.ToString());
                Assert.IsFalse(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(failed.Exception));
                Assert.AreEqual(secondaryBefore+1,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.LocalFileOASIS).TotalRequests);
                var recovered=await local.LoadAvatarDetailAsync(failedId);Assert.IsFalse(recovered.IsError,recovered.Message);
                Assert.AreEqual(888L,recovered.Result.Karma);
                Assert.IsFalse((await ((FirestoreDb)healthy!).Collection("avatarDetails").Document(failedId.ToString()).GetSnapshotAsync()).Exists,
                    "Failed authoritative pre-read must not have dispatched a primary mutation.");
                Assert.IsFalse((await local.DeleteAvatarDetailAsync(failedId,softDelete:false)).IsError);
                Assert.IsNull((await local.LoadAvatarDetailAsync(failedId)).Result);
            }
            finally {field.SetValue(google,healthy);}
        }
        finally {await google.DeleteAvatarAsync(credentialId,false);await local.DeleteAvatarAsync(credentialId,false);
            await local.DeleteAvatarDetailAsync(detail.Id,softDelete:false);await local.DeActivateProviderAsync();}
    }
}
