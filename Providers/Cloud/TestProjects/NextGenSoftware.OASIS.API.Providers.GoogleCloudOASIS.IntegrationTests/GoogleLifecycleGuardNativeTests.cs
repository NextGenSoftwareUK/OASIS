using System.Reflection;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

public partial class GoogleHistoryNativeTests
{
    [DataTestMethod]
    [DataRow(false,false,false)][DataRow(true,false,false)][DataRow(false,true,false)][DataRow(true,true,false)]
    [DataRow(false,false,true)][DataRow(true,false,true)][DataRow(false,true,true)][DataRow(true,true,true)]
    public async Task NativeRelatedIdentityTombstonePreventsNewActiveRecord(bool saveDetail,bool history,bool synchronous)
    {
        var work=Admit();var id=Guid.NewGuid();var relatedCollection=saveDetail?"avatars":"avatarDetails";
        IHolonBase tombstone=saveDetail?new Avatar{Id=id}:new AvatarDetail{Id=id};
        tombstone.IsActive=false;tombstone.DeletedDate=DateTime.UtcNow;
        var json=OasisJson.Serialize(tombstone);var verifier=Database();
        var related=verifier.Collection(relatedCollection).Document(id.ToString());
        var target=verifier.Collection(saveDetail?"avatarDetails":"avatars").Document(id.ToString());
        var provider=new GoogleCloudOASIS("oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true,IsVersionControlEnabled=history};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,Database());
        var journal=Path.Combine(work,"history-native-related-tombstone.pending.json");
        try{
            Assert.IsFalse((await target.GetSnapshotAsync()).Exists);Assert.IsFalse((await related.GetSnapshotAsync()).Exists);Journal(journal,id,"avatars+avatarDetails");
            var created=await related.CreateAsync(new Dictionary<string,object>{["id"]=id.ToString(),["data_json"]=json,["isActive"]=false,["isDeleted"]=true});
            Assert.AreNotEqual(default(Timestamp),created.UpdateTime);
            void Check<T>(OASISResult<T> result){Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(0,result.SavedCount);Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);Assert.IsInstanceOfType<StorageLifecycleConflictException>(result.Exception);}
            if(saveDetail){var active=new AvatarDetail{Id=id,IsActive=true};Check(synchronous?provider.SaveAvatarDetail(active):await provider.SaveAvatarDetailAsync(active));Assert.IsFalse(active.IsDeleted);}
            else{var active=new Avatar{Id=id,IsActive=true};Check(synchronous?provider.SaveAvatar(active):await provider.SaveAvatarAsync(active));Assert.IsFalse(active.IsDeleted);}
            Assert.IsFalse((await target.GetSnapshotAsync()).Exists);var original=await related.GetSnapshotAsync();
            Assert.AreEqual(created.UpdateTime,original.UpdateTime);Assert.AreEqual(json,original.GetValue<string>("data_json"));
            var removed=await related.DeleteAsync(Precondition.LastUpdated(created.UpdateTime));Assert.AreNotEqual(default(Timestamp),removed.UpdateTime);
            Assert.IsFalse((await related.GetSnapshotAsync()).Exists);Assert.IsFalse((await target.GetSnapshotAsync()).Exists);File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }

    [DataTestMethod]
    [DataRow(0,false,false)][DataRow(1,false,false)][DataRow(2,false,false)]
    [DataRow(0,true,false)][DataRow(1,true,false)][DataRow(2,true,false)]
    [DataRow(0,false,true)][DataRow(1,false,true)][DataRow(2,false,true)]
    [DataRow(0,true,true)][DataRow(1,true,true)][DataRow(2,true,true)]
    public async Task NativeActiveSaveCannotReviveCommittedTombstone(int kind,bool history,bool synchronous)
    {
        var work=Admit();var id=Guid.NewGuid();var collection=kind==0?"avatars":kind==1?"avatarDetails":"holons";
        IHolonBase tombstone=kind==0?new Avatar{Id=id}:kind==1?new AvatarDetail{Id=id}:new Holon{Id=id};
        tombstone.IsActive=false;tombstone.DeletedDate=DateTime.UtcNow;
        var originalJson=OasisJson.Serialize(tombstone);
        var verifier=Database();var reference=verifier.Collection(collection).Document(id.ToString());
        var provider=new GoogleCloudOASIS("oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true,IsVersionControlEnabled=history};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,Database());
        var journal=Path.Combine(work,"history-native-tombstone-guard.pending.json");
        try{
            Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);Journal(journal,id,collection);
            var created=await reference.CreateAsync(new Dictionary<string,object>{["id"]=id.ToString(),["data_json"]=originalJson,["isActive"]=false,["isDeleted"]=true});
            Assert.AreNotEqual(default(Timestamp),created.UpdateTime);
            var before=await reference.GetSnapshotAsync();Assert.AreEqual(created.UpdateTime,before.UpdateTime);Assert.AreEqual(originalJson,before.GetValue<string>("data_json"));
            void Check<T>(OASISResult<T> result){
                Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(0,result.SavedCount);
                Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);
                Assert.IsInstanceOfType<StorageLifecycleConflictException>(result.Exception);
            }
            if(kind==0){var active=new Avatar{Id=id,IsActive=true};Check(synchronous?provider.SaveAvatar(active):await provider.SaveAvatarAsync(active));Assert.IsFalse(active.IsDeleted);}
            else if(kind==1){var active=new AvatarDetail{Id=id,IsActive=true};Check(synchronous?provider.SaveAvatarDetail(active):await provider.SaveAvatarDetailAsync(active));Assert.IsFalse(active.IsDeleted);}
            else{var active=new Holon{Id=id,IsActive=true};Check(synchronous?provider.SaveHolon(active,saveChildren:false):await provider.SaveHolonAsync(active,saveChildren:false));Assert.IsFalse(active.IsDeleted);}
            var after=await reference.GetSnapshotAsync();Assert.AreEqual(before.UpdateTime,after.UpdateTime);Assert.AreEqual(originalJson,after.GetValue<string>("data_json"));
            Assert.IsFalse(after.GetValue<bool>("isActive"));Assert.IsTrue(after.GetValue<bool>("isDeleted"));
            var removed=await reference.DeleteAsync(Precondition.LastUpdated(before.UpdateTime!.Value));Assert.AreNotEqual(default(Timestamp),removed.UpdateTime);
            Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
}
