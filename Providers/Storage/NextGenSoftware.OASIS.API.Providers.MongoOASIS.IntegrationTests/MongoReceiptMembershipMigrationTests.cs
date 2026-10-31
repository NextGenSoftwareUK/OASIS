using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using Xunit;
using Provider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class MongoReceiptMembershipMigrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task NativeAuditedMembershipMigrationPreservesAcknowledgementsAndRejectsStaleBatches(bool stale)
    {
        const string uri="mongodb://127.0.0.1:27039/?replicaSet=cosmicLinuxDemoRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        Assert.Equal("/var/tmp/oasis-cosmic-mongo",Environment.GetEnvironmentVariable("OASIS_COSMIC_LINUX_MONGO_STATE"));
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(root,"TestResults/Web4SandboxCqlCurrent/mongo-membership-fixture.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string sourceName="cosmic_replica_membership_source",targetName="cosmic_replica_membership_target";
        var client=new MongoClient(uri);
        using(var names=await client.ListDatabaseNamesAsync())
        {var existing=await names.ToListAsync();Assert.DoesNotContain(sourceName,existing);Assert.DoesNotContain(targetName,existing);}
        var state=Path.Combine(root,"LocalState/COSMIC/MongoReplicaMembershipMigration");Directory.CreateDirectory(state);
        var journal=Path.Combine(state,"fixture.json");
        if(File.Exists(journal))
        {using var prior=System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(journal));Assert.True(prior.RootElement.GetProperty("cleaned").GetBoolean(),"Reconcile prior migration ownership before submission.");}
        var owner=Guid.NewGuid().ToString("D");var operations=new List<Guid>();var jobs=new List<Guid>();
        var intents=new Dictionary<string,MongoReplicaReceiptMembershipAudit>();
        MongoReplicaReceiptMembershipAudit audited=null;
        void Journal(bool cleaned=false)
        {
            var bytes=System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new{owner,sourceName,targetName,operations,jobs,audited,intents,cleaned});
            using(var output=new FileStream(journal+".tmp",FileMode.Create,FileAccess.Write,FileShare.None)){output.Write(bytes);output.Flush(true);}
            File.Move(journal+".tmp",journal,true);
        }
        Guid Operation(){var id=Guid.NewGuid();operations.Add(id);Journal();return id;}
        Guid Job(MongoReplicaReceiptMembershipAudit intent){var id=Guid.NewGuid();jobs.Add(id);intents.Add(id.ToString("D"),intent);Journal();return id;}
        Journal();
        var sourceDb=client.GetDatabase(sourceName);var targetDb=client.GetDatabase(targetName);
        foreach(var db in new[]{sourceDb,targetDb})await db.GetCollection<BsonDocument>("CosmicOwnedFixture").InsertOneAsync(new BsonDocument{{"_id","receipt-membership-v1"},{"owner",owner}});
        var source=new Provider(uri,sourceName);var target=new Provider(uri,targetName);Provider reopened=null;
        var actor=Guid.NewGuid();var complete=false;
        var receipts=targetDb.GetCollection<BsonDocument>("CosmicWriteReceipt");var migrationJobs=targetDb.GetCollection<BsonDocument>("CosmicReceiptMembershipMigration");
        async Task<(IHolon Value,Guid Operation)> Save(Provider provider,IHolon value,ConditionalHolonOperation kind=ConditionalHolonOperation.Save)
        {
            var operation=Operation();
            var result=await provider.ExecuteConditionalHolonWriteAsync(new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation,actor,ProviderType.MongoDBOASIS,
                new[]{new HolonWriteCondition(value.Id,value.Version==0 ? null:CosmicConcurrency.GetProviderRevisionId(value,ProviderType.MongoDBOASIS))}),kind,new[]{value},saveChildren:false));
            Assert.False(result.IsError,result.Message+result.Exception);return(result.Result.Single(),operation);
        }
        async Task<IHolon> VersionFour(Guid id)
        {IHolon value=new Holon{Id=id,Name="owned committed migration source"};for(var version=1;version<=4;version++)value=(await Save(source,value)).Value;return value;}
        CommittedHolonReplicaRequest Transfer(IHolon value)=>new(Operation(),"owned-membership-source",ProviderType.MongoDBOASIS,actor,ConditionalHolonOperation.Save,new[]{value},saveChildren:false);
        try
        {
            Assert.False((await source.ActivateProviderAsync()).IsError);Assert.False((await target.ActivateProviderAsync()).IsError);
            var oldId=Guid.NewGuid();var sourceOld=await VersionFour(oldId);
            var independent=await Save(target,new Holon{Id=oldId,Name="owned prior independent target"});
            var deleted=await Save(target,independent.Value,ConditionalHolonOperation.HardDelete);
            var selected=new[]{independent.Operation,deleted.Operation};
            // Fixture models pre-upgrade receipts; only these owned metadata/fence records are changed.
            await receipts.UpdateManyAsync(FilterDefinition<BsonDocument>.Empty,new BsonDocument("$unset",new BsonDocument("HolonIds","")));
            await targetDb.GetCollection<BsonDocument>("CosmicHolonLineageFence").DeleteOneAsync(new BsonDocument("_id",oldId.ToString("D")));
            var original=await receipts.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            var fresh=await VersionFour(Guid.NewGuid());
            Assert.Equal("COSMIC_REPLICA_BASELINE_RECONCILIATION_REQUIRED",(await target.InitializeCommittedHolonReplicaAsync(Transfer(fresh))).ErrorCode);
            var auditResult=await target.AuditReplicaReceiptMembershipAsync(actor,selected);
            Assert.False(auditResult.IsError,auditResult.Message+auditResult.Exception);audited=auditResult.Result;Journal();
            Assert.Equal(2,audited.Entries.Count);Assert.All(audited.Entries,row=>Assert.Equal(oldId,Assert.Single(row.HolonIds)));
            Assert.Equal("COSMIC_RECEIPT_MEMBERSHIP_STORE_MISMATCH",(await source.MigrateReplicaReceiptMembershipAsync(Job(audited),audited)).ErrorCode);

            var forged=new MongoReplicaReceiptMembershipAudit(audited.StoreHash,audited.OperatorId,audited.Entries.Select(row=>new MongoReplicaReceiptMembershipAudit.Entry(row.OperationId,row.ReceiptHash,new[]{Guid.NewGuid()})));
            var forgedJob=Job(forged);Assert.True((await target.MigrateReplicaReceiptMembershipAsync(forgedJob,forged)).IsError);
            Assert.Equal(0,await migrationJobs.CountDocumentsAsync(new BsonDocument("_id",forgedJob.ToString("D"))));
            var job=Job(audited);
            if(stale)
            {
                var last=audited.Entries.Last().OperationId.ToString("D");
                await receipts.UpdateOneAsync(new BsonDocument("_id",last),new BsonDocument("$set",new BsonDocument("OwnedAfterAuditMarker",true)));
                Assert.Equal("COSMIC_RECEIPT_MEMBERSHIP_AUDIT_STALE",(await target.MigrateReplicaReceiptMembershipAsync(job,audited)).ErrorCode);
                Assert.Equal(0,await receipts.CountDocumentsAsync(new BsonDocument("HolonIds",new BsonDocument("$exists",true))));
                Assert.Equal(0,await migrationJobs.CountDocumentsAsync(new BsonDocument("_id",job.ToString("D"))));
                await receipts.UpdateOneAsync(new BsonDocument("_id",last),new BsonDocument("$unset",new BsonDocument("OwnedAfterAuditMarker","")));
            }
            var migrated=await target.MigrateReplicaReceiptMembershipAsync(job,audited);
            Assert.False(migrated.IsError,migrated.Message+migrated.Exception);Assert.Equal(2,migrated.Result);Assert.True(migrated.IsSaved);
            Assert.Equal(actor.ToString("D"),(await migrationJobs.Find(new BsonDocument("_id",job.ToString("D"))).SingleAsync())["OperatorId"].AsString);
            foreach(var before in original)
            {
                var after=await receipts.Find(new BsonDocument("_id",before["_id"])).SingleAsync();
                Assert.Equal(oldId.ToString("D"),Assert.Single(after["HolonIds"].AsBsonArray).AsString);
                after.Remove("HolonIds");Assert.Equal(before.ToBson(),after.ToBson()); // Complete old receipt bytes preserved.
            }
            reopened=new Provider(uri,targetName);Assert.False((await reopened.ActivateProviderAsync()).IsError);
            using(var retained=System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(journal)))
            {
                var plan=retained.RootElement.GetProperty("audited");
                audited=new MongoReplicaReceiptMembershipAudit(plan.GetProperty("StoreHash").GetString(),plan.GetProperty("OperatorId").GetGuid(),plan.GetProperty("Entries").EnumerateArray().Select(row=>
                    new MongoReplicaReceiptMembershipAudit.Entry(row.GetProperty("OperationId").GetGuid(),row.GetProperty("ReceiptHash").GetString(),row.GetProperty("HolonIds").EnumerateArray().Select(value=>value.GetGuid()))));
            }
            var replay=await reopened.MigrateReplicaReceiptMembershipAsync(job,audited);Assert.False(replay.IsError,replay.Message+replay.Exception);Assert.Equal(2,replay.Result);
            var changedAudit=(await reopened.AuditReplicaReceiptMembershipAsync(actor,selected)).Result;
            Assert.Equal("COSMIC_OPERATION_ID_REUSED",(await reopened.MigrateReplicaReceiptMembershipAsync(job,changedAudit)).ErrorCode);
            var concurrentJob=Job(changedAudit);
            var race=await Task.WhenAll(target.MigrateReplicaReceiptMembershipAsync(concurrentJob,changedAudit),reopened.MigrateReplicaReceiptMembershipAsync(concurrentJob,changedAudit));
            Assert.Contains(race,result=>!result.IsError && result.Result==0);
            Assert.All(race,result=>Assert.True(!result.IsError || result.ErrorCode=="COSMIC_CONCURRENT_WRITE",result.Message+result.Exception));
            Assert.Equal(1,await migrationJobs.CountDocumentsAsync(new BsonDocument("_id",concurrentJob.ToString("D"))));
            using(var cancel=new CancellationTokenSource())
            {
                var cancelledJob=Job(changedAudit);cancel.Cancel();
                Assert.Equal("COSMIC_RECEIPT_MEMBERSHIP_CANCELLED",(await target.MigrateReplicaReceiptMembershipAsync(cancelledJob,changedAudit,cancel.Token)).ErrorCode);
                Assert.Equal(0,await migrationJobs.CountDocumentsAsync(new BsonDocument("_id",cancelledJob.ToString("D"))));
            }
            Assert.Equal("COSMIC_REPLICA_LINEAGE_CONFLICT",(await target.InitializeCommittedHolonReplicaAsync(Transfer(sourceOld))).ErrorCode);
            Assert.False((await target.InitializeCommittedHolonReplicaAsync(Transfer(fresh))).IsError);
            await migrationJobs.UpdateOneAsync(new BsonDocument("_id",job.ToString("D")),new BsonDocument("$set",new BsonDocument("UpdatedCount",0)));
            Assert.Equal("COSMIC_RECEIPT_MEMBERSHIP_RECONCILIATION_REQUIRED",(await target.MigrateReplicaReceiptMembershipAsync(job,audited)).ErrorCode);

            var invalid=Operation();await receipts.InsertOneAsync(new BsonDocument{{"_id",invalid.ToString("D")},{"Fingerprint",new string('A',64)},{"ResultJson","{}"}});
            Assert.Equal("COSMIC_RECEIPT_MEMBERSHIP_INVALID",(await target.AuditReplicaReceiptMembershipAsync(actor,new[]{invalid})).ErrorCode);
            complete=true;
        }
        finally
        {
            await source.DeActivateProviderAsync();await target.DeActivateProviderAsync();if(reopened!=null)await reopened.DeActivateProviderAsync();
            if(complete)
            {
                foreach(var db in new[]{sourceDb,targetDb})
                {var marker=await db.GetCollection<BsonDocument>("CosmicOwnedFixture").Find(new BsonDocument("_id","receipt-membership-v1")).SingleAsync();Assert.Equal(owner,marker["owner"].AsString);await client.DropDatabaseAsync(db.DatabaseNamespace.DatabaseName);}
                using var names=await client.ListDatabaseNamesAsync();var remaining=await names.ToListAsync();Assert.DoesNotContain(sourceName,remaining);Assert.DoesNotContain(targetName,remaining);Journal(true);
            }
        }
    }
}
