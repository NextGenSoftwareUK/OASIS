using System.Reflection;
using Google.Api.Gax.Grpc;
using Google.Cloud.Firestore;
using Google.Cloud.Firestore.V1;
using Google.Cloud.Storage.V1;
using Grpc.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.GoogleCloudOASIS.UnitTests;

[TestClass]
public class GoogleActivationProbeTests
{
    private sealed class Reader:IAsyncStreamReader<RunQueryResponse>
    {
        private bool read;
        public RunQueryResponse Current {get;}=new(){ReadTime=Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(DateTime.UtcNow)};
        public Task<bool> MoveNext(CancellationToken cancellationToken){var available=!read;read=true;return Task.FromResult(available);}
    }
    private sealed class Stream:FirestoreClient.RunQueryStream
    {
        public override AsyncServerStreamingCall<RunQueryResponse> GrpcCall {get;}=new(new Reader(),Task.FromResult(new Metadata()),()=>Status.DefaultSuccess,()=>new Metadata(),()=>{});
    }
    private sealed class QueryClient(bool fail):FirestoreClient
    {
        public int Reads,Writes;public RunQueryRequest? Query;public readonly IOException Cause=new("owned activation read failure");
        public override FirestoreSettings Settings=>FirestoreSettings.GetDefault();
        public override RunQueryStream RunQuery(RunQueryRequest request,CallSettings callSettings=null){Reads++;Query=request.Clone();if(fail)throw Cause;return new Stream();}
        public override Task<CommitResponse> CommitAsync(CommitRequest request,CallSettings callSettings=null){Writes++;throw new InvalidOperationException("Activation must never commit.");}
    }
    private sealed class BucketClient(bool missing):StorageClient
    {
        public int Reads,Creates;public readonly Google.GoogleApiException Cause=new("storage","owned missing configured bucket"){HttpStatusCode=System.Net.HttpStatusCode.NotFound};
        public override Task<Google.Apis.Storage.v1.Data.Bucket> GetBucketAsync(string bucket,GetBucketOptions options=null,CancellationToken cancellationToken=default)
        {Reads++;Assert.AreEqual("owned-existing-bucket",bucket);if(missing)throw Cause;return Task.FromResult(new Google.Apis.Storage.v1.Data.Bucket{Name=bucket});}
        public override Task<Google.Apis.Storage.v1.Data.Bucket> CreateBucketAsync(string projectId,string bucket,CreateBucketOptions options=null,CancellationToken cancellationToken=default)
        {Creates++;throw new InvalidOperationException("Activation must not provision buckets.");}
    }
    private static MethodInfo Probe(string name)
    {
        var method=typeof(GoogleCloudOASIS).GetMethod(name,BindingFlags.Static|BindingFlags.NonPublic);
        Assert.IsNotNull(method,"Production activation must own the non-mutating service probe.");return method;
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task FirestoreActivationReadsOneBoundedQueryWithoutCommitting(bool fail)
    {
        var client=new QueryClient(fail);var db=FirestoreDb.Create("owned-probe-project",client);
        var task=(Task)Probe("VerifyFirestoreAvailabilityAsync").Invoke(null,new object[]{db})!;
        if(fail){var cause=await Assert.ThrowsExceptionAsync<IOException>(()=>task);Assert.AreSame(client.Cause,cause);}else await task;
        Assert.AreEqual(1,client.Reads);Assert.AreEqual(0,client.Writes);Assert.AreEqual(1,client.Query!.StructuredQuery.Limit);Assert.AreEqual("_oasis_health",client.Query.StructuredQuery.From.Single().CollectionId);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task BucketActivationRequiresConfiguredBucketWithoutProvisioning(bool missing)
    {
        var client=new BucketClient(missing);var task=(Task)Probe("VerifyGoogleBucketAsync").Invoke(null,new object[]{client,"owned-existing-bucket"})!;
        if(missing){var cause=await Assert.ThrowsExceptionAsync<Google.GoogleApiException>(()=>task);Assert.AreSame(client.Cause,cause);}else await task;
        Assert.AreEqual(1,client.Reads);Assert.AreEqual(0,client.Creates);
    }
}
