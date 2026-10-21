using System.Reflection;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

public partial class GoogleHistoryNativeTests
{
    [DataTestMethod][DataRow(false,"username")][DataRow(false,"email")][DataRow(true,"username")][DataRow(true,"email")]
    public async Task NativeIdentityMismatchRejectsReadsAndSavesWithoutRepair(bool detail,string field)
    {
        var work=Admit();var id=Guid.NewGuid();var collection=detail?"avatarDetails":"avatars";
        var database=Database();var verifier=Database();var reference=verifier.Collection(collection).Document(id.ToString());
        var provider=new GoogleCloudOASIS("oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true,IsVersionControlEnabled=true};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,database);
        var marker="owned-google-identity-"+id.ToString("N");
        IHolonBase record=detail?new AvatarDetail{Id=id,Username=marker,Email=marker+"@example.invalid"}:new Avatar{Id=id,Username=marker,Email=marker+"@example.invalid"};
        record.MetaData=new(){["owned_history_native"]=marker};
        var journal=Path.Combine(work,$"history-native-identity-{detail}-{field}.pending.json");
        DocumentSnapshot? verified=null;string? originalJson=null;
        static void Rejected<T>(OASISResult<T> result){Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsInstanceOfType<InvalidDataException>(result.Exception);Assert.IsFalse(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception));}
        try{
            Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);Journal(journal,id,collection);
            if(detail){var saved=await provider.SaveAvatarDetailAsync((IAvatarDetail)record);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.IsTrue(saved.IsSaved);}
            else{var saved=await provider.SaveAvatarAsync((IAvatar)record);Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.IsTrue(saved.IsSaved);}
            var before=await reference.GetSnapshotAsync();Assert.IsTrue(before.Exists);Assert.IsNotNull(before.UpdateTime);originalJson=before.GetValue<string>("data_json");
            var wrong=marker+"-wrong";
            var acknowledged=await reference.UpdateAsync(new Dictionary<string,object>{{field,wrong}},Precondition.LastUpdated(before.UpdateTime.Value));Assert.AreNotEqual(default(Timestamp),acknowledged.UpdateTime);
            var current=await reference.GetSnapshotAsync();Assert.AreEqual(acknowledged.UpdateTime,current.UpdateTime);Assert.AreEqual(id.ToString(),current.GetValue<string>("id"));Assert.AreEqual(originalJson,current.GetValue<string>("data_json"));Assert.AreEqual(wrong,current.GetValue<string>(field));verified=current;
            var version=record.Version;
            foreach(var synchronous in new[]{false,true}){
                if(detail){Rejected(synchronous?provider.LoadAvatarDetail(id):await provider.LoadAvatarDetailAsync(id));Rejected(synchronous?provider.SaveAvatarDetail((IAvatarDetail)record):await provider.SaveAvatarDetailAsync((IAvatarDetail)record));}
                else{Rejected(synchronous?provider.LoadAvatar(id):await provider.LoadAvatarAsync(id));Rejected(synchronous?provider.SaveAvatar((IAvatar)record):await provider.SaveAvatarAsync((IAvatar)record));}
                Assert.AreEqual(version,record.Version);var unchanged=await reference.GetSnapshotAsync();Assert.AreEqual(verified.UpdateTime,unchanged.UpdateTime);Assert.AreEqual(originalJson,unchanged.GetValue<string>("data_json"));Assert.AreEqual(wrong,unchanged.GetValue<string>(field));
            }
        }finally{
            if(verified!=null){
                var current=await reference.GetSnapshotAsync();Assert.AreEqual(verified.UpdateTime,current.UpdateTime);Assert.AreEqual(originalJson,current.GetValue<string>("data_json"));
                var payload=Newtonsoft.Json.Linq.JObject.Parse(originalJson!);Assert.AreEqual(marker,payload["MetaData"]!["owned_history_native"]!.Value<string>());
                var cleanup=await reference.DeleteAsync(Precondition.LastUpdated(verified.UpdateTime!.Value));Assert.AreNotEqual(default(Timestamp),cleanup.UpdateTime);Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);File.Delete(journal);
            }
            await provider.DeActivateProviderAsync();
        }
    }
}
