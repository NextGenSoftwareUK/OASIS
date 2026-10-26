using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Amazon.DynamoDBv2.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.DynamoDBOASIS.IntegrationTests;

// Real official SDK + retained DynamoDB Local + shipping host + real Mongo outbox.
// Reservation authority is explicitly synthetic; no hosted AWS or tariff verification.
[TestClass][DoNotParallelize]
public sealed class DynamoResourceHostNativeTests
{
    private const string Database="oasis_sdk_resource_join_test";
    private const string Service="DynamoResourceHostNativeTests";
    private sealed class Authority(string user,Action<string> journal):IUsageReservationAuthority
    {
        public int Starts,Settlements;
        public Task<SubscriptionAuthorizationResult> AuthorizeUsageAsync(string bearer,UsageAuthorizationRequest request,CancellationToken token=default)
        {
            journal(request.OperationId);
            return Task.FromResult(new SubscriptionAuthorizationResult{Allowed=true,UserId=user,OperationId=request.OperationId,
                Status="authorized",ExpiresAtUtc=DateTime.UtcNow.AddMinutes(5)});
        }
        public Task<UsageStartResult> StartUsageAsync(UsageStartRequest request,CancellationToken token=default)
        {Starts++;return Task.FromResult(new UsageStartResult{Started=true,OperationId=request.OperationId,Status="executing"});}
        public Task<UsageSettlementResult> SettleUsageAsync(UsageSettlementRequest request,CancellationToken token=default)
        {Settlements++;throw new InvalidOperationException("This unpriced native fixture must remain unresolved, never settle at zero.");}
    }

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public Task OfficialSdkCapacityFlowsThroughPreparedHostIntoDurableMongoWithoutInventedPrice(bool aws,bool synchronous)
        => CaptureNativeOperation(aws,synchronous,false);

    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public Task OfficialSdkWriteAndItsReadKeepEveryNativeObservationWithoutReplayOrInventedPrice(bool aws,bool synchronous)
        => CaptureNativeOperation(aws,synchronous,true);

    private async Task CaptureNativeOperation(bool aws,bool synchronous,bool write)
    {
        Assert.AreEqual("http://127.0.0.1:18000",Environment.GetEnvironmentVariable("DYNAMODB_SERVICE_URL"));
        var uri=Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI");Assert.IsFalse(string.IsNullOrWhiteSpace(uri));
        var url=new MongoUrl(uri);Assert.AreEqual("127.0.0.1",url.Server.Host);Assert.AreEqual(27029,url.Server.Port);
        Assert.AreEqual("oasisRecoveryRs",url.ReplicaSetName);Assert.IsNull(url.Username);
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_DYNAMO_HISTORY_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","DynamoHistoryCurrent"),StringComparison.OrdinalIgnoreCase));
        var journal=Path.Combine(work,$"dynamo-resource-host-{(write?"write-":"")}{aws}-{synchronous}.pending.json");
        var cleanupJournal=journal+".verified-cleanup.json";
        Assert.IsFalse(File.Exists(journal),"Preserve and reconcile the original fixture; never replay a pending admission.");
        Assert.IsFalse(File.Exists(cleanupJournal),"Preserve the original exact native cleanup evidence.");
        var settings=MongoClientSettings.FromConnectionString(uri);settings.WriteConcern=WriteConcern.WMajority.With(journal:true);
        settings.ReadConcern=ReadConcern.Majority;settings.ReadPreference=ReadPreference.Primary;
        var collection=new MongoClient(settings).GetDatabase(Database).GetCollection<BsonDocument>("subscription_usage_outbox_v1");
        Assert.AreEqual(0L,await collection.CountDocumentsAsync(new BsonDocument("Service",Service)),"An unresolved earlier join fixture must be reviewed, not hidden by a fresh fixture.");
        var store=new MongoUsageOutboxStore(uri!,Database);await store.InitializeAsync(default);
        var user=Guid.NewGuid().ToString("D");var key="resource-join-"+Guid.NewGuid().ToString("N");
        var id=Guid.NewGuid();var marker="owned-resource-host-"+id.ToString("N");
        var endpoint=write?"POST /synthetic-owned-holon":"GET /synthetic-missing-holon";
        byte[]? journalBytes=null;
        var authority=new Authority(user,operationId=>{
            var bytes=JsonSerializer.SerializeToUtf8Bytes(new{OperationId=operationId,UserId=user,Database,Service,aws,synchronous,write,NativeRecordId=id,Marker=marker,
                TestAssemblySha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(DynamoResourceHostNativeTests).Assembly.Location)))});
            if(journalBytes==null){using var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None);file.Write(bytes);file.Flush(true);journalBytes=bytes;}
            else CollectionAssert.AreEqual(journalBytes,File.ReadAllBytes(journal));
        });
        var executor=new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service=Service,PersistenceTimeout=TimeSpan.FromSeconds(10)});
        var meter=new UsageEndpointPolicy{MeterCategory="native-resource-join",ReservedUnits=1,PricingCatalogueVersion="unpriced-test"};
        var execution=await executor.BeginAsync("synthetic-fixture-bearer",user,key,new string('a',64),endpoint,meter);
        var type=aws?ProviderType.AWSOASIS:ProviderType.DynamoDBOASIS;
        var provider=aws?(DynamoDBOASIS)new NextGenSoftware.OASIS.API.Providers.AWSOASIS.AWSOASIS("us-east-1","synthetic","synthetic","http://127.0.0.1:18000")
            :new DynamoDBOASIS("synthetic","synthetic","us-east-1","http://127.0.0.1:18000");
        provider.IsVersionControlEnabled=true;
        using var verifier=DynamoHistoryNativeTests.Client();
        var recordKey=new Dictionary<string,AttributeValue>{{"id",new(){S=id.ToString("D")}}};
        var verified=false;string? nativeOriginal=null;
        try{
            var activation=await provider.ActivateProviderAsync();Assert.IsFalse(activation.IsError,activation.Exception?.ToString());
            var policy=new PreparedStorageExecution(execution,"native-sdk-resource-join",new Dictionary<ProviderType,string>(),new[]{type});
            Assert.AreEqual(0,(await verifier.GetItemAsync(new(){TableName="OasisHolons",Key=recordKey,ConsistentRead=true})).Item.Count);
            var input=new Holon{Id=id,Name=marker,CreatedByAvatarId=Guid.Parse(user)};
            Func<Task<OASISResult<IHolon>>> operation=write
                ?()=>synchronous?Task.FromResult(provider.SaveHolon(input,saveChildren:false)):provider.SaveHolonAsync(input,saveChildren:false)
                :()=>synchronous?Task.FromResult(provider.LoadHolon(id,loadChildren:false)):provider.LoadHolonAsync(id,loadChildren:false);
            // OUR Core boundary; never reflection into SDK internals or replacement of its client.
            var execute=typeof(PreparedStorageExecution).GetMethod("ExecuteAsync",BindingFlags.Instance|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(IHolon));
            var result=await (Task<OASISResult<IHolon>>)execute.Invoke(policy,new object[]{provider,operation})!;
            if(write){
                Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.IsSaved);Assert.AreEqual(1,result.SavedCount);Assert.AreSame(input,result.Result);
                var native=await verifier.GetItemAsync(new(){TableName="OasisHolons",Key=recordKey,ConsistentRead=true});
                nativeOriginal=native.Item["data_json"].S;
                var canonical=NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.DeserializeStored<Holon>(nativeOriginal);
                Assert.AreEqual(id,canonical.Id);Assert.AreEqual(marker,canonical.Name);Assert.AreEqual(Guid.Parse(user),canonical.CreatedByAvatarId);
            }else{
                Assert.IsTrue(result.IsError,"Synthetic absent GUID should retain its provider absence result.");Assert.IsNull(result.Result);
                Assert.AreEqual(OASISResultErrorCodes.RecordNotFound,result.ErrorCode,result.Exception?.ToString());
            }
            var captured=await new MongoUsageOutboxStore(uri!,Database).GetAsync(execution.OperationId,default);
            var expectedBatches=write?2:1;
            var attempt=captured.ProviderAttempts.Single();Assert.IsTrue(attempt.ResourceCaptureExpected);Assert.AreEqual(expectedBatches,attempt.ResourceCaptureCount);
            var batches=captured.ResourceObservations.OrderBy(batch=>batch.Sequence).ToArray();
            CollectionAssert.AreEqual(write?new[]{"GetItem","PutItem"}:new[]{"GetItem"},batches.Select(batch=>batch.NativeOperation).ToArray());
            foreach(var batch in batches){
            Assert.AreEqual(Array.IndexOf(batches,batch)+1,batch.Sequence);
            Assert.AreEqual(type.ToString(),batch.Provider);Assert.AreEqual(attempt.ClientRequestId,batch.ClientRequestId);Assert.AreEqual(attempt.ProviderRequestId,batch.ProviderRequestId);
            Assert.IsTrue(batch.Observations.Count>0);
            foreach(var row in batch.Observations){row.Validate(type.ToString(),attempt.ProviderRequestId,requireComplete:true);
                Assert.IsTrue(row.Quantity>=0);
                const string tableScope="us-east-1/table/OasisHolons";
                Assert.IsTrue(row.ResourceScope==tableScope||write&&row.View=="Breakdown"&&
                    (row.ResourceScope.StartsWith(tableScope+"/gsi/",StringComparison.Ordinal)||row.ResourceScope.StartsWith(tableScope+"/lsi/",StringComparison.Ordinal)));}
            }
            Assert.AreEqual(expectedBatches,batches.Select(batch=>batch.Observations[0].NativeRequestId).Distinct(StringComparer.Ordinal).Count());
            if(write){
                var put=batches.Single(batch=>batch.NativeOperation=="PutItem").Observations;
                Assert.IsTrue(put.Any(row=>row.View=="Total"&&row.Unit=="capacity-units"&&row.Quantity>0));
                // Alternative views conserve evidence; adding Total to Breakdown would double bill.
                Assert.AreEqual(put.Where(row=>row.View=="Total"&&row.Unit=="capacity-units").Sum(row=>row.Quantity),
                    put.Where(row=>row.View=="Breakdown"&&row.Unit=="capacity-units").Sum(row=>row.Quantity));
            }
            Assert.IsTrue(captured.ResourceObservationBytes>0);Assert.IsNull(captured.Settlement);Assert.AreEqual(0,captured.Receipts.Count);
            await execution.CompleteOrReconcileAsync(write?"succeeded":"failed");
            var unresolved=await store.GetAsync(execution.OperationId,default);
            Assert.AreEqual("recovery-required",unresolved.State);Assert.AreEqual(write?"succeeded":"failed",unresolved.ObservedOutcome);
            Assert.IsNull(unresolved.Settlement);Assert.AreEqual(0,authority.Settlements);Assert.AreEqual(1,authority.Starts);
            await Assert.ThrowsExceptionAsync<UsageProtocolException>(()=>executor.BeginAsync("synthetic-fixture-bearer",user,key,new string('a',64),endpoint,meter));
            Assert.AreEqual(1,authority.Starts);Assert.AreEqual(expectedBatches,(await store.GetAsync(execution.OperationId,default)).ResourceObservations.Count);
            verified=true;
        }finally{
            var stopped=await provider.DeActivateProviderAsync();Assert.IsFalse(stopped.IsError,stopped.Exception?.ToString());
        }
        if(verified){
            byte[]? cleanupBytes=null;
            if(write){
                Assert.IsNotNull(nativeOriginal);
                cleanupBytes=JsonSerializer.SerializeToUtf8Bytes(new{Id=id,Marker=marker,Table="OasisHolons",ExpectedNativeJson=nativeOriginal,
                    NativeJsonSha256=Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(nativeOriginal)))});
                using(var file=new FileStream(cleanupJournal,FileMode.CreateNew,FileAccess.Write,FileShare.None)){file.Write(cleanupBytes);file.Flush(true);}
                await DynamoHistoryNativeTests.RemoveOwned(verifier,"OasisHolons",id,nativeOriginal);
            }
            var original=await collection.Find(new BsonDocument("_id",execution.OperationId)).SingleAsync();
            Assert.AreEqual(execution.UserId,original["UserId"].AsString);Assert.AreEqual(Service,original["Service"].AsString);
            CollectionAssert.AreEqual(journalBytes!,File.ReadAllBytes(journal));
            // Exact original BSON condition, no broad database/drop or unknown-outcome replay.
            Assert.AreEqual(1L,(await collection.DeleteOneAsync(original)).DeletedCount);
            Assert.AreEqual(0L,await collection.CountDocumentsAsync(new BsonDocument("_id",execution.OperationId)));
            CollectionAssert.AreEqual(journalBytes!,File.ReadAllBytes(journal));
            if(cleanupBytes!=null){CollectionAssert.AreEqual(cleanupBytes,File.ReadAllBytes(cleanupJournal));File.Delete(cleanupJournal);}
            File.Delete(journal);
        }
    }
}
