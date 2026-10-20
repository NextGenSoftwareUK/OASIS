using System.Reflection;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

public partial class GoogleHistoryNativeTests
{
    [DataTestMethod]
    [DataRow(false,0)][DataRow(true,0)][DataRow(false,1)][DataRow(true,1)][DataRow(false,2)][DataRow(true,2)]
    [DataRow(false,3)][DataRow(true,3)][DataRow(false,4)][DataRow(true,4)]
    public async Task NativeStandaloneDetailLifecycleUsesOriginalRevisionAndPreservesAvatar(bool soft,int mode)
    {
        var work=Admit();var id=Guid.NewGuid();var name="owned-detail-"+id.ToString("N");
        var healthy=Database();var verifier=Database();var detail=verifier.Collection("avatarDetails").Document(id.ToString());var avatar=verifier.Collection("avatars").Document(id.ToString());
        var provider=new GoogleCloudOASIS("oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true,IsVersionControlEnabled=true};
        var field=typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!;field.SetValue(provider,healthy);
        var journal=Path.Combine(work,$"history-native-detail-{soft}-{mode}.pending.json");
        try {
            Assert.IsFalse((await detail.GetSnapshotAsync()).Exists);Assert.IsFalse((await avatar.GetSnapshotAsync()).Exists);Journal(journal,id,"avatarDetails+avatars");
            Assert.IsFalse((await provider.SaveAvatarAsync(new Avatar{Id=id,Username=name,Email=name+"@example.invalid",IsActive=true})).IsError);
            Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,Username=name,Email=name+"@example.invalid",IsActive=true,XP=321,Karma=29})).IsError);
            var originalAvatar=(await avatar.GetSnapshotAsync()).GetValue<string>("data_json");
            NativeCommitBoundary? boundary=null;
            if(mode>=3){boundary=new NativeCommitBoundary(healthy.Client,mode==3?"lost":"conflict");field.SetValue(provider,FirestoreDb.Create("oasis-google-native",boundary));}
            IOASISStorageProvider storage=provider;
            var result=mode==1?storage.DeleteAvatarDetailByUsername(name,soft):mode==2?await storage.DeleteAvatarDetailByEmailAsync(name+"@example.invalid",soft):await storage.DeleteAvatarDetailAsync(id,soft);
            Assert.AreEqual(mode>=3,result.IsError,result.Exception?.ToString());Assert.AreEqual(mode<3,result.Result);
            Assert.AreEqual(originalAvatar,(await avatar.GetSnapshotAsync()).GetValue<string>("data_json"));
            if(mode>=3){Assert.AreEqual(1,boundary!.Attempts);if(mode==3)Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));else Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);}
            var actual=await detail.GetSnapshotAsync();
            if(soft){Assert.IsTrue(actual.Exists);var json=JObject.Parse(actual.GetValue<string>("data_json"));Assert.AreEqual(2,json["Version"]!.Value<int>());Assert.IsTrue(actual.GetValue<bool>("isDeleted"));
                Assert.AreEqual(name,json[EmbeddedRecordHistory.PropertyName]!["records"]![0]!["Username"]!.Value<string>());
            }else Assert.IsFalse(actual.Exists);
            if(actual.Exists)await detail.DeleteAsync(Precondition.LastUpdated(actual.UpdateTime!.Value));
            var original=await avatar.GetSnapshotAsync();await avatar.DeleteAsync(Precondition.LastUpdated(original.UpdateTime!.Value));
            Assert.IsFalse((await detail.GetSnapshotAsync()).Exists);Assert.IsFalse((await avatar.GetSnapshotAsync()).Exists);File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
}
