using System.Reflection;
using Google.Api.Gax.Grpc;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Grpc.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Document=Google.Cloud.Firestore.V1.Document;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public class GooglePairedLifecycleTransactionTests
{
    private sealed class Reader(BatchGetDocumentsResponse response):IAsyncStreamReader<BatchGetDocumentsResponse>
    {
        private bool read;public BatchGetDocumentsResponse Current=>response;
        public Task<bool> MoveNext(CancellationToken token){var next=!read;read=true;return Task.FromResult(next);}
    }
    private sealed class Stream(BatchGetDocumentsResponse response):FirestoreClient.BatchGetDocumentsStream
    {
        public override AsyncServerStreamingCall<BatchGetDocumentsResponse> GrpcCall{get;}=new(new Reader(response),Task.FromResult(new Metadata()),()=>Status.DefaultSuccess,()=>new Metadata(),()=>{});
    }
    private sealed class Client(string mode):FirestoreClient
    {
        public Dictionary<string,Document> Documents=new();public int Begins,Commits,Rollbacks;
        public override FirestoreSettings Settings=>(FirestoreSettings)typeof(GoogleCloudOASIS).GetMethod("CreateFirestoreSettings",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,null)!;
        public override Task<BeginTransactionResponse> BeginTransactionAsync(BeginTransactionRequest request,CallSettings settings=null){Begins++;return Task.FromResult(new BeginTransactionResponse{Transaction=Google.Protobuf.ByteString.CopyFromUtf8("owned-paired-lifecycle")});}
        public override Task RollbackAsync(RollbackRequest request,CallSettings settings=null){Rollbacks++;return Task.CompletedTask;}
        public override BatchGetDocumentsStream BatchGetDocuments(BatchGetDocumentsRequest request,CallSettings settings=null){
            var path=request.Documents.Single();return new Stream(Documents.TryGetValue(path,out var record)?new(){Found=record.Clone()}:new(){Missing=path});
        }
        public override Task<CommitResponse> CommitAsync(CommitRequest request,CallSettings settings=null){
            Commits++;Assert.AreEqual(2,request.Writes.Count);
            var now=Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow);
            foreach(var write in request.Writes){
                if(write.OperationCase==Google.Cloud.Firestore.V1.Write.OperationOneofCase.Delete)Documents.Remove(write.Delete);
                else{
                    var original=Documents[write.Update.Name].Clone();
                    foreach(var field in write.Update.Fields)original.Fields[field.Key]=field.Value.Clone();
                    original.UpdateTime=now;Documents[original.Name]=original;
                }
            }
            var result=new CommitResponse{CommitTime=now};
            if(mode!="missing-result")foreach(var _ in request.Writes)result.WriteResults.Add(new Google.Cloud.Firestore.V1.WriteResult{UpdateTime=mode=="invalid-time"?new():now});
            return Task.FromResult(result);
        }
    }
    [DataTestMethod][DataRow(false,"missing-result")][DataRow(true,"missing-result")][DataRow(false,"invalid-time")][DataRow(true,"invalid-time")]
    public async Task PairedDeletionCannotPublishWithoutEveryNativeAcknowledgement(bool soft,string mode)
    {
        var id=Guid.NewGuid();var client=new Client(mode);var db=FirestoreDb.Create("owned-paired",client);
        foreach(var collection in new[]{"avatars","avatarDetails"}){
            IHolonBase row=collection=="avatars"?new Avatar{Id=id,IsActive=true}:new AvatarDetail{Id=id,IsActive=true};
            var now=Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow);
            var native=new Document{Name=db.Collection(collection).Document(id.ToString()).Path,CreateTime=now,UpdateTime=now};
            native.Fields.Add("id",new Value{StringValue=id.ToString()});native.Fields.Add("data_json",new Value{StringValue=OasisJson.Serialize(row)});
            client.Documents.Add(native.Name,native);
        }
        var provider=new GoogleCloudOASIS("owned-paired",enableFirestore:true){IsProviderActivated=true};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,db);
        var result=await provider.DeleteAvatarAsync(id,soft);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsFalse(result.IsDeleted);
        Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));
        Assert.AreEqual(1,client.Begins);Assert.AreEqual(1,client.Commits);Assert.AreEqual(0,client.Rollbacks);
    }
}
