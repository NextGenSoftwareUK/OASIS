using System.Reflection;
using Google.Api.Gax.Grpc;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Grpc.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Document=Google.Cloud.Firestore.V1.Document;
using NativePrecondition=Google.Cloud.Firestore.V1.Precondition;
using NativeWrite=Google.Cloud.Firestore.V1.Write;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public class GoogleAtomicGraphTests
{
    private sealed class Reader(BatchGetDocumentsResponse response):IAsyncStreamReader<BatchGetDocumentsResponse>
    {
        private bool read;public BatchGetDocumentsResponse Current=>response;
        public Task<bool> MoveNext(CancellationToken token){var next=!read;read=true;return Task.FromResult(next);}
    }
    private sealed class Stream(BatchGetDocumentsResponse response):FirestoreClient.BatchGetDocumentsStream
    {
        public override AsyncServerStreamingCall<BatchGetDocumentsResponse> GrpcCall{get;}=
            new(new Reader(response),Task.FromResult(new Metadata()),()=>Status.DefaultSuccess,()=>new Metadata(),()=>{});
    }
    private sealed class Client:FirestoreClient
    {
        public Dictionary<string,Document> Documents=new();public int Reads,Commits;public bool LoseAck,Reject;
        public CommitRequest? Submitted;
        public override FirestoreSettings Settings=>(FirestoreSettings)typeof(GoogleCloudOASIS)
            .GetMethod("CreateFirestoreSettings",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,null)!;
        public override BatchGetDocumentsStream BatchGetDocuments(BatchGetDocumentsRequest request,CallSettings settings=null) {
            Reads++;var path=request.Documents.Single();var now=Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow);
            return new Stream(Documents.TryGetValue(path,out var document)?
                new(){Found=document.Clone(),ReadTime=now}:new(){Missing=path,ReadTime=now});
        }
        public override Task<CommitResponse> CommitAsync(CommitRequest request,CallSettings settings=null) {
            Commits++;Submitted=request.Clone();
            if(Reject)throw new RpcException(new Status(StatusCode.FailedPrecondition,"owned rejected commit"));
            foreach(var write in request.Writes) {
                var path=write.OperationCase==NativeWrite.OperationOneofCase.Delete?write.Delete:write.Update.Name;
                var condition=write.CurrentDocument;
                if(condition?.ConditionTypeCase==NativePrecondition.ConditionTypeOneofCase.Exists&&!condition.Exists&&Documents.ContainsKey(path))
                    throw new RpcException(new Status(StatusCode.AlreadyExists,"owned create conflict"));
                if(condition?.ConditionTypeCase==NativePrecondition.ConditionTypeOneofCase.UpdateTime&&
                    (!Documents.TryGetValue(path,out var old)||!old.UpdateTime.Equals(condition.UpdateTime)))
                    throw new RpcException(new Status(StatusCode.FailedPrecondition,"owned revision conflict"));
            }
            var timestamp=Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow);
            var response=new CommitResponse{CommitTime=timestamp};
            foreach(var write in request.Writes) {
                if(write.OperationCase==NativeWrite.OperationOneofCase.Delete)Documents.Remove(write.Delete);
                else {var document=write.Update.Clone();document.CreateTime=timestamp;document.UpdateTime=timestamp;Documents[document.Name]=document;}
                response.WriteResults.Add(new Google.Cloud.Firestore.V1.WriteResult{UpdateTime=timestamp});
            }
            if(LoseAck){LoseAck=false;throw new IOException("owned ACK dropped after simulated atomic commit");}
            return Task.FromResult(response);
        }
    }
    private static GoogleCloudOASIS Provider(Client client) {
        var provider=new GoogleCloudOASIS("owned-atomic",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true,IsVersionControlEnabled=true};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!
            .SetValue(provider,FirestoreDb.Create("owned-atomic",client));return provider;
    }
    private static ConditionalHolonWriteRequest Request(Holon root,Guid operation,Guid? revision=null,ConditionalHolonOperation kind=ConditionalHolonOperation.Save)
        =>new(new ConditionalHolonWrite(operation,Guid.Empty,ProviderType.GoogleCloudOASIS,new[]{new HolonWriteCondition(root.Id,revision)}),kind,new[]{root},false);
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task DisabledStoreAndCancellationRejectBeforeServiceActivation(bool cancelled) {
        var client=new Client();var provider=cancelled?Provider(client):
            new GoogleCloudOASIS("owned-no-activation",enableStorage:false,enableFirestore:false,enableBigQuery:false);
        var result=await provider.ExecuteConditionalHolonWriteAsync(Request(new Holon{Id=Guid.NewGuid()},Guid.NewGuid()),
            cancelled?new CancellationToken(true):default);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
        Assert.IsTrue(cancelled?result.Exception is OperationCanceledException:result.Exception is NotSupportedException);
        Assert.AreEqual(0,client.Reads);Assert.AreEqual(0,client.Commits);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task AtomicReceiptReplayIsStableAndChangedIntentNeverCommits(bool synchronous) {
        var client=new Client();var provider=Provider(client);var root=new Holon{Id=Guid.NewGuid(),Name="owned"};
        var request=Request(root,Guid.NewGuid());
        var first=synchronous?provider.ExecuteConditionalHolonWrite(request):await provider.ExecuteConditionalHolonWriteAsync(request);
        Assert.IsFalse(first.IsError,first.Exception?.ToString());Assert.IsTrue(first.IsSaved);
        Assert.AreEqual(3,client.Submitted!.Writes.Count,"Holon, receipt and shared operation fence.");
        Assert.IsTrue(client.Submitted.Writes.All(write=>write.CurrentDocument is { Exists:false }));
        var version=first.Result.Single().VersionId;Assert.AreEqual(0,root.Version);
        var replay=await provider.ExecuteConditionalHolonWriteAsync(request);
        Assert.IsFalse(replay.IsError,replay.Exception?.ToString());Assert.AreEqual(version,replay.Result.Single().VersionId);
        root.Name="different";
        Assert.AreEqual("COSMIC_OPERATION_ID_REUSED",(await provider.ExecuteConditionalHolonWriteAsync(Request(root,request.Write.OperationId))).ErrorCode);
        Assert.AreEqual(1,client.Commits);
    }
    [TestMethod]
    public async Task UnknownCommitReturnRetainsMarkerAndRecoversOnlyOriginalReceipt() {
        var client=new Client{LoseAck=true};var provider=Provider(client);
        var request=Request(new Holon{Id=Guid.NewGuid()},Guid.NewGuid());
        var unknown=await provider.ExecuteConditionalHolonWriteAsync(request);
        Assert.AreEqual("COSMIC_CONDITIONAL_RECONCILIATION_REQUIRED",unknown.ErrorCode);
        Assert.IsInstanceOfType<INativeMutationReconciliationRequired>(unknown.Exception);Assert.AreEqual(1,client.Commits);
        var recovered=await provider.ExecuteConditionalHolonWriteAsync(request);
        Assert.IsFalse(recovered.IsError,recovered.Exception?.ToString());Assert.AreEqual(1,client.Commits);
    }
    [DataTestMethod][DataRow(ConditionalHolonOperation.SoftDelete)][DataRow(ConditionalHolonOperation.HardDelete)]
    public async Task ReplacementAndDeletionCompareObservedRevisionsInOneCommit(ConditionalHolonOperation deletion) {
        var client=new Client();var provider=Provider(client);
        var first=await provider.ExecuteConditionalHolonWriteAsync(Request(new Holon{Id=Guid.NewGuid()},Guid.NewGuid()));
        Assert.IsFalse(first.IsError,first.Exception?.ToString());var row=(Holon)first.Result.Single();
        row.Name="replacement";
        var update=await provider.ExecuteConditionalHolonWriteAsync(Request(row,Guid.NewGuid(),CosmicConcurrency.GetProviderRevisionId(row,ProviderType.GoogleCloudOASIS)));
        Assert.IsFalse(update.IsError,update.Exception?.ToString());Assert.IsNotNull(client.Submitted!.Writes[0].CurrentDocument.UpdateTime);
        row=(Holon)update.Result.Single();
        var request=Request(row,Guid.NewGuid(),CosmicConcurrency.GetProviderRevisionId(row,ProviderType.GoogleCloudOASIS),deletion);
        var deleted=await provider.ExecuteConditionalHolonWriteAsync(request);
        Assert.IsFalse(deleted.IsError,deleted.Exception?.ToString());Assert.IsTrue(deleted.IsDeleted);
        Assert.AreEqual(3,deleted.Result.Single().Version);Assert.IsTrue(deleted.Result.Single().IsDeleted);
        Assert.IsFalse((await provider.ExecuteConditionalHolonWriteAsync(request)).IsError);Assert.AreEqual(3,client.Commits);
    }
    [TestMethod]
    public async Task OrdinaryReplacementInvalidatesOldAtomicRevision() {
        var client=new Client();var provider=Provider(client);
        var first=await provider.ExecuteConditionalHolonWriteAsync(Request(new Holon{Id=Guid.NewGuid()},Guid.NewGuid()));
        var row=(Holon)first.Result.Single();var old=CosmicConcurrency.GetProviderRevisionId(row,ProviderType.GoogleCloudOASIS);
        row.Name="ordinary";var saved=await provider.SaveHolonAsync(row,saveChildren:false);
        Assert.IsFalse(saved.IsError,saved.Exception?.ToString());Assert.AreNotEqual(old,CosmicConcurrency.GetProviderRevisionId(row,ProviderType.GoogleCloudOASIS));
        var stale=await provider.ExecuteConditionalHolonWriteAsync(Request(row,Guid.NewGuid(),old));
        Assert.AreEqual("COSMIC_CONCURRENCY_CONFLICT",stale.ErrorCode);Assert.AreEqual(2,client.Commits);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task RejectionAndCapacityDoNotBecomeUnknownExecution(bool capacity) {
        var client=new Client{Reject=!capacity};var provider=Provider(client);
        var row=new Holon{Id=Guid.NewGuid(),Description=capacity?new string('x',1024*1024):"owned"};
        var result=await provider.ExecuteConditionalHolonWriteAsync(Request(row,Guid.NewGuid()));
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
        Assert.IsFalse(result.Exception is INativeMutationReconciliationRequired);
        Assert.AreEqual(capacity?0:1,client.Commits);Assert.AreEqual(0,client.Documents.Count);
    }
}
