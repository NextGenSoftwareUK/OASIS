using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Xunit;
using Provider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed partial class CosmicConditionalWriteTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeOutboxDeliveryFencesOrdersAndRetainsRecoveryAcrossStoreInstances(bool synchronous)
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(workspace,"TestResults/SubscriptionUsageCurrent/cosmic-conditional-environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string name="cosmic_outbox_delivery_fixture";
        var client=new MongoClient(uri);using var names=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(name,await names.ToListAsync());
        var database=client.GetDatabase(name);await database.CreateCollectionAsync("CosmicOwnedFixture");
        var provider=new Provider(uri,name);var complete=false;var actor=Guid.NewGuid();var root=new Holon{Id=Guid.NewGuid(),Name="first"};
        var enrollment=new HolonReplicationEnrollment("source",new[]{"a","b"});
        var firstOperation=Guid.NewGuid();
        async Task<IHolon> Write(IHolon value,Guid? expected,Guid operation,HolonReplicationEnrollment overrideEnrollment=null)
        {
            var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation,actor,ProviderType.MongoDBOASIS,
                new[]{new HolonWriteCondition(value.Id,expected)}),ConditionalHolonOperation.Save,new[]{value},saveChildren:false);
            var result=synchronous ? provider.ExecuteReplicatingHolonWrite(request,overrideEnrollment ?? enrollment):await provider.ExecuteReplicatingHolonWriteAsync(request,overrideEnrollment ?? enrollment);
            Assert.False(result.IsError,result.Message+result.Exception);return Assert.Single(result.Result);
        }
        async Task<DateTime> ServerTime()=>(await database.RunCommandAsync<BsonDocument>(new BsonDocument("hello",1)))["localTime"].ToUniversalTime();
        async Task WaitUntil(DateTime until)
        {
            var deadline=DateTime.UtcNow.AddSeconds(10);
            while(await ServerTime()<until){Assert.True(DateTime.UtcNow<deadline,"Native lease/retry clock did not reach the bounded deadline.");await Task.Delay(20);}
        }
        try
        {
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            var one=await Write(root,null,firstOperation);one.Name="second";
            await Write(one,CosmicConcurrency.GetProviderRevisionId(one,ProviderType.MongoDBOASIS),Guid.NewGuid());
            var captured=provider.GetHolonReplicationOutbox();Assert.False(captured.IsError,captured.Message+captured.Exception);
            var store=captured.Result;var other=new MongoHolonReplicationOutbox(provider.Database);
            var peek=await store.PeekNextAsync("a");Assert.False(peek.IsError,peek.Message+peek.Exception);Assert.NotNull(peek.Result);
            Assert.Equal(HolonDeliveryState.Pending,peek.Result.State);Assert.Equal(0,peek.Result.AttemptCount);
            var secondaryContext=new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories.MongoDbContext(uri,name);secondaryContext.MongoDB=secondaryContext.MongoDB.WithReadPreference(ReadPreference.Secondary);
            var authoritativePeek=await new MongoHolonReplicationOutbox(secondaryContext).PeekNextAsync("a");
            Assert.False(authoritativePeek.IsError,authoritativePeek.Message+authoritativePeek.Exception);Assert.Equal(peek.Result.Sequence,authoritativePeek.Result.Sequence);
            Assert.Equal(HolonDeliveryState.Pending,authoritativePeek.Result.State);
            var a=await store.ClaimNextAsync("a","$worker",TimeSpan.FromSeconds(3));Assert.False(a.IsError,a.Message+a.Exception);Assert.NotNull(a.Result);
            Assert.Equal(firstOperation,a.Result.OperationId);Assert.Equal("first",CommittedHolonReplicaPayload.Read(a.Result.PayloadJson).Roots[0].Name);
            Assert.Null((await other.ClaimNextAsync("a","other",TimeSpan.FromMinutes(1))).Result);
            var b=await other.ClaimNextAsync("b","worker",TimeSpan.FromMinutes(2));Assert.NotNull(b.Result);
            Assert.True((await store.RenewAsync(a.Result,TimeSpan.FromSeconds(10))).Result);
            await WaitUntil(a.Result.LeaseUntilUtc.AddTicks(1));
            Assert.True((await store.FinishWithRecoveryAsync(a.Result,HolonDeliveryOutcome.Completed,new(false,Array.Empty<string>()))).Result);
            var second=(await other.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1))).Result;
            Assert.NotNull(second);Assert.NotEqual(firstOperation,second.OperationId);
            var retryAt=(await ServerTime()).AddSeconds(1);
            Assert.True((await other.FinishWithRecoveryAsync(second,HolonDeliveryOutcome.Retry,new(false,Array.Empty<string>()),"OFFLINE","$LeaseToken",retryAt)).Result);
            Assert.Null((await store.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1))).Result);
            Assert.Equal("$LeaseToken",(await store.InspectAsync(second.Sequence)).Result.Message);
            await WaitUntil(retryAt);
            var expired=(await store.ClaimNextAsync("a","worker",TimeSpan.FromMilliseconds(200))).Result;Assert.NotNull(expired);
            Assert.Equal(second.PayloadJson,expired.PayloadJson);Assert.Equal(2,expired.AttemptCount);
            await WaitUntil(expired.LeaseUntilUtc.AddTicks(1));
            Assert.Equal("COSMIC_OUTBOX_LEASE_LOST",(await store.RenewAsync(expired,TimeSpan.FromMinutes(1))).ErrorCode);
            Assert.Equal("COSMIC_OUTBOX_LEASE_LOST",(await store.FinishAsync(expired,HolonDeliveryOutcome.Completed)).ErrorCode);
            var fresh=(await other.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1))).Result;Assert.NotNull(fresh);
            Assert.NotEqual(expired.LeaseToken,fresh.LeaseToken);Assert.Equal(expired.OperationId,fresh.OperationId);
            Assert.Equal("COSMIC_OUTBOX_LEASE_LOST",(await store.FinishAsync(expired,HolonDeliveryOutcome.Completed)).ErrorCode);
            Assert.True((await other.FinishWithRecoveryAsync(fresh,HolonDeliveryOutcome.Completed,new(false,Array.Empty<string>()))).Result);
            Assert.Null((await store.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1))).Result);
            var rows=database.GetCollection<BsonDocument>("CosmicHolonOutbox");
            var evidence=new HolonDeliveryRecoveryEvidence(true,new[]{new string('x',5000)+"/original-native"});
            await database.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","CosmicHolonOutbox"},{"validator",new BsonDocument("DeliveryState",new BsonDocument("$ne","ReconciliationRequired"))}});
            try
            {
                var rejected=await store.FinishWithRecoveryAsync(b.Result,HolonDeliveryOutcome.ReconciliationRequired,evidence,"UNKNOWN");Assert.True(rejected.IsError);
                var unchanged=(await other.InspectAsync(b.Result.Sequence)).Result;Assert.Equal(HolonDeliveryState.InFlight,unchanged.State);Assert.Null(unchanged.RecoveryEvidence);
            }
            finally{await database.RunCommandAsync<BsonDocument>(new BsonDocument{{"collMod","CosmicHolonOutbox"},{"validator",new BsonDocument()}});}
            Assert.True((await store.FinishWithRecoveryAsync(b.Result,HolonDeliveryOutcome.ReconciliationRequired,evidence,"UNKNOWN")).Result);
            var inspected=await new MongoHolonReplicationOutbox(provider.Database).InspectAsync(b.Result.Sequence);
            Assert.False(inspected.IsError,inspected.Message+inspected.Exception);Assert.Equal(HolonDeliveryState.ReconciliationRequired,inspected.Result.State);
            Assert.Equal(evidence.OriginalTransactionIds,inspected.Result.RecoveryEvidence.OriginalTransactionIds);
            var blockedHead=await new MongoHolonReplicationOutbox(provider.Database).PeekNextAsync("b");Assert.False(blockedHead.IsError,blockedHead.Message+blockedHead.Exception);
            Assert.Equal(HolonDeliveryState.ReconciliationRequired,blockedHead.Result.State);Assert.NotNull(blockedHead.Result.RecoveryEvidence);
            var unchangedHead=await other.InspectAsync(blockedHead.Result.Sequence);Assert.Equal(blockedHead.Result.AttemptCount,unchangedHead.Result.AttemptCount);
            var blocked=await other.ClaimNextAsync("b","worker",TimeSpan.FromMinutes(1));Assert.Null(blocked.Result);Assert.Contains("reconciliation",blocked.Message);
            var cleanupContext=new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories.MongoDbContext(uri,name){
                MongoClient=MongoReceiptCleanupFault.Wrap(provider.Database.MongoClient,failDispose:true),MongoDB=provider.Database.MongoDB};
            var cleanup=await new MongoHolonReplicationOutbox(cleanupContext).ClaimNextAsync("b","worker",TimeSpan.FromMinutes(1));
            Assert.False(cleanup.IsError,cleanup.Message+cleanup.Exception);Assert.Null(cleanup.Result);Assert.True(cleanup.IsWarning);
            Assert.Equal(2,Assert.IsType<AggregateException>(cleanup.Exception).Flatten().InnerExceptions.Count);
            await rows.UpdateOneAsync(new BsonDocument("Sequence",fresh.Sequence),new BsonDocument("$set",new BsonDocument{{"DeliveryState","Pending"},{"CompletedAtUtcTicks",0L},{"PayloadDigest","corrupt"}}));
            Assert.Equal("COSMIC_OUTBOX_CORRUPT",(await store.PeekNextAsync("a")).ErrorCode);
            var corrupted=await store.ClaimNextAsync("a","worker",TimeSpan.FromMinutes(1));Assert.True(corrupted.IsError);Assert.Equal("COSMIC_OUTBOX_CORRUPT",corrupted.ErrorCode);
            Assert.Equal(fresh.AttemptCount,(await rows.Find(new BsonDocument("Sequence",fresh.Sequence)).SingleAsync())["AttemptCount"].AsInt32);
            await Write(new Holon{Id=Guid.NewGuid(),Name="owned competing claim"},null,Guid.NewGuid(),new HolonReplicationEnrollment("source",new[]{"race"}));
            var competing=await Task.WhenAll(store.ClaimNextAsync("race","one",TimeSpan.FromMinutes(1)),other.ClaimNextAsync("race","two",TimeSpan.FromMinutes(1)));
            var winner=Assert.Single(competing.Where(result=>result.Result!=null));
            Assert.All(competing.Where(result=>result.IsError),result=>Assert.Equal("COSMIC_OUTBOX_CONCURRENT_WRITE",result.ErrorCode));
            Assert.True((await store.FinishWithRecoveryAsync(winner.Result,HolonDeliveryOutcome.Completed,new(false,Array.Empty<string>()))).Result);
            await Write(new Holon{Id=Guid.NewGuid(),Name="owned uncertain delivery"},null,Guid.NewGuid(),new HolonReplicationEnrollment("source",new[]{"uncertain"}));
            await VerifyDeliveryCommitUncertainty(uri,name,provider.Database);
            complete=true;
        }
        finally{await provider.DeActivateProviderAsync();if(complete)await client.DropDatabaseAsync(name);}
    }

    private async Task VerifyDeliveryCommitUncertainty(string uri,string databaseName,NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories.MongoDbContext healthy)
    {
        const string app="cosmic-delivery-original-commit";
        var settings=MongoClientSettings.FromConnectionString(uri+"&appName="+app);var aborts=0;var updates=0;
        settings.ClusterConfigurator=cluster=>cluster.Subscribe<MongoDB.Driver.Core.Events.CommandStartedEvent>(command=>{
            if(command.CommandName=="abortTransaction")System.Threading.Interlocked.Increment(ref aborts);
            if(command.CommandName=="update")System.Threading.Interlocked.Increment(ref updates);
        });
        var monitored=new MongoClient(settings);
        var context=new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories.MongoDbContext(uri+"&appName="+app,databaseName){MongoClient=monitored,MongoDB=monitored.GetDatabase(databaseName)};
        var store=new MongoHolonReplicationOutbox(context);var claimed=await store.ClaimNextAsync("uncertain","worker",TimeSpan.FromMinutes(1));
        Assert.False(claimed.IsError,claimed.Message+claimed.Exception);Assert.NotNull(claimed.Result);var priorAborts=aborts;
        var admin=new MongoClient(uri).GetDatabase("admin");
        try
        {
            await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode","alwaysOn"},
                {"data",new BsonDocument{{"failCommands",new BsonArray{"commitTransaction"}},{"appName",app},
                    {"writeConcernError",new BsonDocument{{"code",64},{"errmsg","owned delivery commit acknowledgement fault"}}}}}});
            var cleanupContext=new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories.MongoDbContext(uri+"&appName="+app,databaseName){
                MongoClient=MongoReceiptCleanupFault.Wrap(monitored,failDispose:true),MongoDB=monitored.GetDatabase(databaseName)};
            var result=await new MongoHolonReplicationOutbox(cleanupContext).FinishWithRecoveryAsync(claimed.Result,HolonDeliveryOutcome.ReconciliationRequired,
                new HolonDeliveryRecoveryEvidence(true,new[]{"owned-target-native-identity"}),"TARGET_UNKNOWN");
            Assert.True(result.IsError);Assert.Equal("COSMIC_OUTBOX_RECONCILIATION_REQUIRED",result.ErrorCode);
            var failures=Assert.IsType<AggregateException>(result.Exception).Flatten().InnerExceptions;
            var original=Assert.Single(failures.OfType<MongoTransactionReconciliationException>());
            Assert.Single(failures.OfType<IOException>());
            Assert.Equal(original.OriginalTransactionId,Assert.Single(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.OriginalTransactionIds(result.Exception)));
            var native=Assert.IsType<MongoWriteConcernException>(original.InnerException);
            Assert.Equal(priorAborts,aborts);Assert.Equal(1,updates);
            var reply=native.WriteConcernResult.Response;Assert.True(reply.Contains("operationTime"));
            var read=await healthy.MongoDB.RunCommandAsync<BsonDocument>(new BsonDocument{{"find","CosmicHolonOutbox"},
                {"filter",new BsonDocument("Sequence",claimed.Result.Sequence)},
                {"readConcern",new BsonDocument{{"level","majority"},{"afterClusterTime",reply["operationTime"]}}},
                {"singleBatch",true},{"batchSize",2},{"maxTimeMS",5000}});
            var persisted=Assert.Single(read["cursor"]["firstBatch"].AsBsonArray).AsBsonDocument;
            Assert.Equal("ReconciliationRequired",persisted["DeliveryState"].AsString);
            var evidence=HolonRecoveryEvidenceJson.ReadStored(persisted["RecoveryEvidenceJson"].AsString,persisted["RecoveryEvidenceDigest"].AsString,HolonDeliveryState.ReconciliationRequired);
            Assert.Equal("owned-target-native-identity",Assert.Single(evidence.OriginalTransactionIds));
            output.WriteLine("Original delivery state transaction="+original.OriginalTransactionId+"; independently reconciled without another update.");
            Assert.Equal(1,updates);
        }
        finally{await admin.RunCommandAsync<BsonDocument>(new BsonDocument{{"configureFailPoint","failCommand"},{"mode","off"}});}
    }
}
