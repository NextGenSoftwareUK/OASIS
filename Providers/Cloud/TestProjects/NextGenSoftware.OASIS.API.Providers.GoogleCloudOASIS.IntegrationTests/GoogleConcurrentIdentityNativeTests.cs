using System.Reflection;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

public partial class GoogleHistoryNativeTests
{
    private sealed class IdentityCommitGate
    {
        private int arrivals;
        private readonly TaskCompletionSource ready=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public async Task ArriveAsync(){if(Interlocked.Increment(ref arrivals)==2)ready.TrySetResult();await ready.Task.WaitAsync(TimeSpan.FromSeconds(15));}
    }
    private sealed class ConcurrentIdentityClient(Google.Cloud.Firestore.V1.FirestoreClient inner,IdentityCommitGate gate):Google.Cloud.Firestore.V1.FirestoreClient
    {
        public int Begins,Commits;
        public override Google.Cloud.Firestore.V1.FirestoreSettings Settings=>inner.Settings;
        public override Task<Google.Cloud.Firestore.V1.BeginTransactionResponse> BeginTransactionAsync(Google.Cloud.Firestore.V1.BeginTransactionRequest request,Google.Api.Gax.Grpc.CallSettings settings=null){Interlocked.Increment(ref Begins);return inner.BeginTransactionAsync(request,settings);}
        public override Task RollbackAsync(Google.Cloud.Firestore.V1.RollbackRequest request,Google.Api.Gax.Grpc.CallSettings settings=null)=>inner.RollbackAsync(request,settings);
        public override BatchGetDocumentsStream BatchGetDocuments(Google.Cloud.Firestore.V1.BatchGetDocumentsRequest request,Google.Api.Gax.Grpc.CallSettings settings=null)=>inner.BatchGetDocuments(request,settings);
        public override async Task<Google.Cloud.Firestore.V1.CommitResponse> CommitAsync(Google.Cloud.Firestore.V1.CommitRequest request,Google.Api.Gax.Grpc.CallSettings settings=null){
            Interlocked.Increment(ref Commits);Assert.IsFalse(request.Transaction.IsEmpty);
            // Both ORIGINAL transactions must finish their reads/staging before either commit is released.
            await gate.ArriveAsync();return await inner.CommitAsync(request,settings);
        }
    }

    [DataTestMethod][DataRow(false)][DataRow(true)][Timeout(60000)]
    public async Task NativeConcurrentIdentitySaveAndPairedDeleteNeverPublishInconsistentWinners(bool saveDetail)
    {
        var work=Admit();var id=Guid.NewGuid();var marker="owned-concurrent-identity-"+id.ToString("N");
        var healthy=Database();var verifier=Database();var field=typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!;
        GoogleCloudOASIS Provider(FirestoreDb db){var provider=new GoogleCloudOASIS("oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true,IsVersionControlEnabled=true};field.SetValue(provider,db);return provider;}
        var setup=Provider(healthy);var gate=new IdentityCommitGate();
        var saveClient=new ConcurrentIdentityClient(healthy.Client,gate);var deleteClient=new ConcurrentIdentityClient(Database().Client,gate);
        var saver=Provider(FirestoreDb.Create("oasis-google-native",saveClient));var deleter=Provider(FirestoreDb.Create("oasis-google-native",deleteClient));
        var references=new[]{verifier.Collection("avatars").Document(id.ToString()),verifier.Collection("avatarDetails").Document(id.ToString())};
        var journal=Path.Combine(work,"history-native-concurrent-identity.pending.json");
        var avatar=new Avatar{Id=id,IsActive=true,MetaData=new(){["owned_concurrent_identity"]=marker,["phase"]="initial"}};
        var detail=new AvatarDetail{Id=id,IsActive=true,MetaData=new(){["owned_concurrent_identity"]=marker,["phase"]="initial"}};
        try{
            foreach(var reference in references)Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);Journal(journal,id,"avatars+avatarDetails");
            var first=await setup.SaveAvatarAsync(avatar);Assert.IsFalse(first.IsError,first.Exception?.ToString());
            var second=await setup.SaveAvatarDetailAsync(detail);Assert.IsFalse(second.IsError,second.Exception?.ToString());
            var originals=new[]{await references[0].GetSnapshotAsync(),await references[1].GetSnapshotAsync()};
            if(saveDetail)detail.MetaData["phase"]="saved";else avatar.MetaData["phase"]="saved";
            async Task<(bool Error,bool Saved,int Count,Exception Cause,string Code)> Save(){
                if(saveDetail){var result=await saver.SaveAvatarDetailAsync(detail);return(result.IsError,result.IsSaved,result.SavedCount,result.Exception,result.ErrorCode);}
                else{var result=await saver.SaveAvatarAsync(avatar);return(result.IsError,result.IsSaved,result.SavedCount,result.Exception,result.ErrorCode);}
            }
            var saveTask=Save();var deleteTask=deleter.DeleteAvatarAsync(id,true);
            await Task.WhenAll(saveTask,deleteTask);var saved=await saveTask;var deleted=await deleteTask;
            Assert.IsTrue(saved.Error||deleted.IsError,"Competing original transactions cannot both publish success.");
            Assert.AreEqual(1,saveClient.Begins);Assert.AreEqual(1,deleteClient.Begins);Assert.AreEqual(1,saveClient.Commits);Assert.AreEqual(1,deleteClient.Commits);
            void Rejection(Exception failure,string code){
                Assert.IsInstanceOfType<Grpc.Core.RpcException>(failure,failure?.ToString());
                Assert.AreEqual(Grpc.Core.StatusCode.Aborted,((Grpc.Core.RpcException)failure).StatusCode,failure.ToString());
                Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,code);Assert.IsFalse(NativeMutationReconciliation.IsRequired(failure));
            }
            if(saved.Error)Rejection(saved.Cause,saved.Code);if(deleted.IsError)Rejection(deleted.Exception,deleted.ErrorCode);
            Assert.AreEqual(!saved.Error,saved.Saved);Assert.AreEqual(saved.Error?0:1,saved.Count);
            Assert.AreEqual(!deleted.IsError,deleted.IsDeleted);Assert.AreEqual(deleted.IsError?0:1,deleted.DeletedCount);
            for(var index=0;index<references.Length;index++){
                var actual=await references[index].GetSnapshotAsync();Assert.IsTrue(actual.Exists);
                var row=JObject.Parse(actual.GetValue<string>("data_json"));Assert.AreEqual(id.ToString(),row["Id"]!.Value<string>());
                Assert.AreEqual(marker,row["MetaData"]!["owned_concurrent_identity"]!.Value<string>());
                var target=index==(saveDetail?1:0);var deleteWon=!deleted.IsError;
                var saveWon=!saved.Error;var changed=deleteWon||saveWon&&target;
                Assert.AreEqual(deleteWon,row["IsDeleted"]!.Value<bool>());Assert.AreEqual(changed?2:1,row["Version"]!.Value<int>());
                Assert.AreEqual(saveWon&&target?"saved":"initial",row["MetaData"]!["phase"]!.Value<string>());
                if(!changed){Assert.AreEqual(originals[index].UpdateTime,actual.UpdateTime);Assert.AreEqual(originals[index].GetValue<string>("data_json"),actual.GetValue<string>("data_json"));}
                if(changed)Assert.AreEqual(1,((JArray)row["_oasis_record_history"]!["records"]!).Count);
                var removed=await references[index].DeleteAsync(Precondition.LastUpdated(actual.UpdateTime!.Value));Assert.AreNotEqual(default(Timestamp),removed.UpdateTime);
            }
            foreach(var reference in references)Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);File.Delete(journal);
        }finally{await saver.DeActivateProviderAsync();await deleter.DeActivateProviderAsync();await setup.DeActivateProviderAsync();}
    }
}
