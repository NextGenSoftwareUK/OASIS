using System.Reflection;
using Google.Api.Gax.Grpc;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Grpc.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Document=Google.Cloud.Firestore.V1.Document;
using Value=Google.Cloud.Firestore.V1.Value;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public class GoogleConditionalWriterTests
{
    private sealed class Reader(BatchGetDocumentsResponse response):IAsyncStreamReader<BatchGetDocumentsResponse>
    {
        private bool _read;
        public BatchGetDocumentsResponse Current=>response;
        public Task<bool> MoveNext(CancellationToken cancellationToken){var available=!_read;_read=true;return Task.FromResult(available);}
    }
    private sealed class SnapshotStream(BatchGetDocumentsResponse response):FirestoreClient.BatchGetDocumentsStream
    {
        public override AsyncServerStreamingCall<BatchGetDocumentsResponse> GrpcCall {get;}=new(new Reader(response),Task.FromResult(new Metadata()),()=>Status.DefaultSuccess,()=>new Metadata(),()=>{});
    }
    private sealed class Client:FirestoreClient
    {
        public Document? Stored;public CommitRequest? Submitted;public int Writes,Reads;public string Mode="normal";public bool RetryCapable;
        public readonly IOException Lost=new("owned controlled response loss after simulated commit");
        public RpcException? Rejection;
        public override FirestoreSettings Settings=>RetryCapable?FirestoreSettings.GetDefault():(FirestoreSettings)typeof(GoogleCloudOASIS).GetMethod("CreateFirestoreSettings",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,null)!;
        public override BatchGetDocumentsStream BatchGetDocuments(BatchGetDocumentsRequest request,CallSettings callSettings=null)
        {Reads++;return new SnapshotStream(Stored==null?new(){Missing=request.Documents.Single(),ReadTime=Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow)}:new(){Found=Stored.Clone(),ReadTime=Stored.UpdateTime});}
        public override Task<CommitResponse> CommitAsync(CommitRequest request,CallSettings callSettings=null)
        {
            Writes++;Submitted=request.Clone();
            if(Rejection!=null)throw Rejection;
            if(Mode.StartsWith("conflict"))throw new RpcException(new Status(Mode=="conflict-exists"?StatusCode.AlreadyExists:Mode=="conflict-missing"?StatusCode.NotFound:StatusCode.FailedPrecondition,"owned controlled conflict"));
            var write=request.Writes.Single();var now=Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow);
            Stored=write.Update.Clone();Stored.UpdateTime=now;Stored.CreateTime=now;
            if(Mode=="lost")throw Lost;
            var response=new CommitResponse{CommitTime=Mode=="invalid-ack"?new Google.Protobuf.WellKnownTypes.Timestamp():now};response.WriteResults.Add(new Google.Cloud.Firestore.V1.WriteResult{UpdateTime=Mode=="invalid-ack"?new Google.Protobuf.WellKnownTypes.Timestamp():now});return Task.FromResult(response);
        }
    }
    private static Task<Holon> Write(Client client,Holon row,bool history)
    {
        var db=FirestoreDb.Create("owned-controlled-history",client);var reference=db.Collection("holons").Document(row.Id.ToString());
        var fields=new Dictionary<string,object>{["id"]=row.Id.ToString(),["data_json"]=OasisJson.Serialize(row)};
        return (Task<Holon>)typeof(GoogleCloudOASIS).GetMethod("WriteFirestoreRecordAsync",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(Holon)).Invoke(null,new object[]{reference,fields,history})!;
    }
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public async Task PublicInvalidArgumentPreservesDefiniteRejectionWithoutFalseConflict(int kind)
    {
        var original=new RpcException(new Status(StatusCode.InvalidArgument,"owned invalid request"));var client=new Client{Rejection=original};
        var provider=new GoogleCloudOASIS("owned-controlled-history",enableStorage:false,enableFirestore:true,enableBigQuery:false){IsProviderActivated=true,IsVersionControlEnabled=true};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,FirestoreDb.Create("owned-controlled-history",client));
        NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase row=kind==0?new Avatar{Id=Guid.NewGuid()}:kind==1?new AvatarDetail{Id=Guid.NewGuid()}:new Holon{Id=Guid.NewGuid()};
        try{
            if(kind==0)Check(await provider.SaveAvatarAsync((Avatar)row));else if(kind==1)Check(await provider.SaveAvatarDetailAsync((AvatarDetail)row));else Check(await provider.SaveHolonAsync((Holon)row,saveChildren:false));
            Assert.AreEqual(1,client.Writes);Assert.AreEqual(0,row.Version);Assert.IsNull(client.Stored);
        }finally{await provider.DeActivateProviderAsync();}
        void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result){
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);Assert.AreSame(original,result.Exception);
            Assert.IsFalse(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception));
            Assert.AreNotEqual(NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException.ErrorCode,result.ErrorCode);
        }
    }
    [DataTestMethod][DataRow(StatusCode.DeadlineExceeded)][DataRow(StatusCode.Unavailable)]
    public async Task AmbiguousRpcFailureStillRequiresOriginalOutcomeReconciliation(StatusCode code)
    {
        var original=new RpcException(new Status(code,"owned ambiguous transport"));var client=new Client{Rejection=original};var row=new Holon{Id=Guid.NewGuid()};
        var failure=await Assert.ThrowsExceptionAsync<GoogleFirestoreMutationReconciliationException>(()=>Write(client,row,true));
        Assert.AreSame(original,failure.InnerException);Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(failure));Assert.AreEqual(1,client.Writes);Assert.AreEqual(0,row.Version);
    }
    [DataTestMethod]
    [DataRow(0,"conflict")][DataRow(1,"conflict")][DataRow(2,"conflict")]
    [DataRow(0,"conflict-exists")][DataRow(1,"conflict-exists")][DataRow(2,"conflict-exists")]
    [DataRow(0,"conflict-missing")][DataRow(1,"conflict-missing")][DataRow(2,"conflict-missing")]
    public async Task PublicSaveKnownConflictIsTerminalToActualV2Policy(int kind,string mode)
    {
        var client=new Client{Mode=mode};
        var provider=new GoogleCloudOASIS("owned-controlled-history",enableFirestore:true){IsProviderActivated=true};
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!
            .SetValue(provider,FirestoreDb.Create("owned-controlled-history",client));
        NextGenSoftware.OASIS.API.Core.Interfaces.IHolonBase row=kind==0?new Avatar{Id=Guid.NewGuid()}:kind==1?new AvatarDetail{Id=Guid.NewGuid()}:new Holon{Id=Guid.NewGuid()};
        if(kind==0)Check(await provider.SaveAvatarAsync((Avatar)row));
        else if(kind==1)Check(await provider.SaveAvatarDetailAsync((AvatarDetail)row));
        else Check(await provider.SaveHolonAsync((Holon)row,saveChildren:false));
        Assert.AreEqual(1,client.Writes);Assert.AreEqual(0,row.Version);Assert.IsNull(client.Stored);
        void Check<T>(NextGenSoftware.OASIS.Common.OASISResult<T> result)
        {
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.IsNull(result.Result);
            Assert.IsInstanceOfType<RpcException>(result.Exception);
            Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException.ErrorCode,result.ErrorCode);
            var method=typeof(NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.OASISHyperDrive)
                .GetMethod("ShouldFailOver",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(T));
            Assert.AreEqual(false,method.Invoke(null,new object?[]{null,result}));
        }
    }
    [TestMethod]
    public async Task RetryCapableSdkClientIsRejectedBeforeNativeReadOrWrite()
    {
        var client=new Client{RetryCapable=true};
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>Write(client,new Holon{Id=Guid.NewGuid()},true));
        Assert.AreEqual(0,client.Reads);Assert.AreEqual(0,client.Writes);Assert.IsNull(client.Stored);
    }
    [DataTestMethod][DataRow(true)][DataRow(false)]
    public async Task LifecycleRejectsRetryCapableCommitClientBeforeTransaction(bool softDelete)
    {
        var client=new Client{RetryCapable=true};
        var provider=new GoogleCloudOASIS("owned-controlled-history",enableFirestore:true);
        provider.IsProviderActivated=true;
        typeof(GoogleCloudOASIS).GetField("_firestoreDb",BindingFlags.Instance|BindingFlags.NonPublic)!
            .SetValue(provider,FirestoreDb.Create("owned-controlled-history",client));
        var result=await provider.DeleteAvatarAsync(Guid.NewGuid(),softDelete);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);
        StringAssert.Contains(result.Message,"single-attempt");
        Assert.AreEqual(0,client.Reads);Assert.AreEqual(0,client.Writes);Assert.IsNull(client.Stored);
    }
    [TestMethod]
    public async Task RealSdkBuildsCreateAndRevisionConditionalReplacementWithLiteralFieldRemoval()
    {
        var client=new Client();var row=new Holon{Id=Guid.NewGuid(),Name="first"};
        var first=await Write(client,row,true);Assert.AreEqual(1,first.Version);Assert.IsFalse(client.Submitted!.Writes.Single().CurrentDocument.Exists);
        var revision=client.Stored!.UpdateTime.Clone();client.Stored.Fields["old.dot"]=new Value{StringValue="removed"};row.Name="second";
        var second=await Write(client,row,false);var request=client.Submitted!.Writes.Single();
        Assert.AreEqual(revision,request.CurrentDocument.UpdateTime);CollectionAssert.Contains(request.UpdateMask.FieldPaths.ToArray(),"`old.dot`");
        Assert.IsFalse(request.Update.Fields.ContainsKey("old.dot"));Assert.AreEqual(2,second.Version);Assert.AreEqual(first.VersionId,second.PreviousVersionId);Assert.AreEqual(0,row.Version);Assert.AreEqual(2,client.Writes);
    }
    [DataTestMethod][DataRow("conflict")][DataRow("lost")][DataRow("invalid-ack")]
    public async Task OneAttemptDistinguishesKnownConflictFromUnknownAcknowledgement(string mode)
    {
        var client=new Client{Mode=mode};var row=new Holon{Id=Guid.NewGuid()};
        if(mode=="conflict"){await Assert.ThrowsExceptionAsync<RpcException>(()=>Write(client,row,true));Assert.IsNull(client.Stored);}
        else{var error=await Assert.ThrowsExceptionAsync<GoogleFirestoreMutationReconciliationException>(()=>Write(client,row,true));Assert.IsNotNull(client.Stored);if(mode=="lost")Assert.AreSame(client.Lost,error.InnerException);}
        Assert.AreEqual(1,client.Writes);Assert.AreEqual(0,row.Version);
    }
}
