using System.Reflection;
using Google.Cloud.Firestore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.IntegrationTests;

public partial class GoogleHistoryNativeTests
{
    private sealed class PairedLifecycleBoundary(Google.Cloud.Firestore.V1.FirestoreClient inner,string mode):Google.Cloud.Firestore.V1.FirestoreClient
    {
        public int Begins,Commits;
        public override Google.Cloud.Firestore.V1.FirestoreSettings Settings=>inner.Settings;
        public override Task<Google.Cloud.Firestore.V1.BeginTransactionResponse> BeginTransactionAsync(Google.Cloud.Firestore.V1.BeginTransactionRequest request,Google.Api.Gax.Grpc.CallSettings settings=null){Begins++;return inner.BeginTransactionAsync(request,settings);}
        public override Task RollbackAsync(Google.Cloud.Firestore.V1.RollbackRequest request,Google.Api.Gax.Grpc.CallSettings settings=null)=>inner.RollbackAsync(request,settings);
        public override BatchGetDocumentsStream BatchGetDocuments(Google.Cloud.Firestore.V1.BatchGetDocumentsRequest request,Google.Api.Gax.Grpc.CallSettings settings=null)=>inner.BatchGetDocuments(request,settings);
        public override async Task<Google.Cloud.Firestore.V1.CommitResponse> CommitAsync(Google.Cloud.Firestore.V1.CommitRequest request,Google.Api.Gax.Grpc.CallSettings settings=null){
            Commits++;Assert.AreEqual(2,request.Writes.Count);Assert.IsFalse(request.Transaction.IsEmpty);
            if(mode=="expired")await inner.RollbackAsync(new Google.Cloud.Firestore.V1.RollbackRequest{Database=request.Database,Transaction=request.Transaction},settings);
            var original=await inner.CommitAsync(request,settings);
            Assert.AreEqual(2,original.WriteResults.Count);
            if(mode=="lost")throw new IOException("Owned paired lifecycle response withheld after real native commit.");
            var changed=original.Clone();
            if(mode=="missing-result")changed.WriteResults.RemoveAt(1);
            if(mode=="invalid-time")changed.WriteResults[1].UpdateTime=new();
            return changed;
        }
    }

    [DataTestMethod]
    [DataRow(false,"lost")][DataRow(true,"lost")][DataRow(false,"missing-result")][DataRow(true,"missing-result")]
    [DataRow(false,"invalid-time")][DataRow(true,"invalid-time")][DataRow(false,"expired")][DataRow(true,"expired")]
    public async Task NativePairedLifecycleRequiresCompleteAcknowledgementWithoutReplay(bool soft,string mode)
    {
        var work=Admit();var id=Guid.NewGuid();var healthy=Database();var verifier=Database();
        var references=new[]{verifier.Collection("avatars").Document(id.ToString()),verifier.Collection("avatarDetails").Document(id.ToString())};
        var provider=new GoogleCloudOASIS("oasis-google-native",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true,IsVersionControlEnabled=true};
        var field=typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!;field.SetValue(provider,healthy);
        var journal=Path.Combine(work,"history-native-paired-fault.pending.json");
        try{
            foreach(var reference in references)Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);Journal(journal,id,"avatars+avatarDetails");
            var first=await provider.SaveAvatarAsync(new Avatar{Id=id,IsActive=true});Assert.IsFalse(first.IsError,first.Exception?.ToString());
            var second=await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=id,IsActive=true,XP=321});Assert.IsFalse(second.IsError,second.Exception?.ToString());
            var originals=new[]{await references[0].GetSnapshotAsync(),await references[1].GetSnapshotAsync()};
            var boundary=new PairedLifecycleBoundary(healthy.Client,mode);field.SetValue(provider,FirestoreDb.Create("oasis-google-native",boundary));
            var result=await provider.DeleteAvatarAsync(id,soft);
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsFalse(result.IsDeleted);Assert.AreEqual(0,result.DeletedCount);
            Assert.AreEqual(1,boundary.Begins);Assert.AreEqual(1,boundary.Commits);
            if(mode=="expired"){
                Assert.IsInstanceOfType<Grpc.Core.RpcException>(result.Exception);
                Assert.AreEqual(Grpc.Core.StatusCode.Aborted,((Grpc.Core.RpcException)result.Exception).StatusCode);
                Assert.AreEqual(StorageLifecycleConflictException.ErrorCode,result.ErrorCode);
            }else{
                var error=result.Exception as GoogleFirestoreMutationReconciliationException;Assert.IsNotNull(error);
                Assert.IsTrue(NativeMutationReconciliation.IsRequired(error));Assert.IsFalse(string.IsNullOrEmpty(error.OriginalTransactionId));
            }
            // Inspect only the original attempt. Never repeat deletion to infer its outcome.
            for(var index=0;index<references.Length;index++){
                var actual=await references[index].GetSnapshotAsync();
                if(!soft&&mode!="expired"){Assert.IsFalse(actual.Exists);continue;}
                Assert.IsTrue(actual.Exists);Assert.AreEqual(id.ToString(),actual.GetValue<string>("id"));
                if(mode=="expired"){
                    Assert.AreEqual(originals[index].UpdateTime,actual.UpdateTime);
                    Assert.AreEqual(originals[index].GetValue<string>("data_json"),actual.GetValue<string>("data_json"));
                }else{
                    var canonical=JObject.Parse(actual.GetValue<string>("data_json"));Assert.IsTrue(canonical["IsDeleted"]!.Value<bool>());
                    Assert.AreEqual(2,canonical["Version"]!.Value<int>());
                    Assert.AreEqual(1,((JArray)canonical["_oasis_record_history"]!["records"]!).Count);
                }
                var removed=await references[index].DeleteAsync(Precondition.LastUpdated(actual.UpdateTime!.Value));Assert.AreNotEqual(default(Timestamp),removed.UpdateTime);
            }
            foreach(var reference in references)Assert.IsFalse((await reference.GetSnapshotAsync()).Exists);File.Delete(journal);
        }finally{await provider.DeActivateProviderAsync();}
    }
}
