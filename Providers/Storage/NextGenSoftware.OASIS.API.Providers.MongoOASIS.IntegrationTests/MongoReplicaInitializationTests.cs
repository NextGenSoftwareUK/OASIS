using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using Xunit;
using Provider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class MongoReplicaInitializationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NativeInitialBaselinePreservesLineageReceiptsAndTargetAbsence(bool synchronous)
    {
        const string uri="mongodb://127.0.0.1:27039/?replicaSet=cosmicLinuxDemoRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri,Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        Assert.Equal("/var/tmp/oasis-cosmic-mongo",Environment.GetEnvironmentVariable("OASIS_COSMIC_LINUX_MONGO_STATE"));
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../.."));
        using var lease=File.Open(Path.Combine(root,"TestResults/Web4SandboxCqlCurrent/mongo-baseline-fixture.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        const string sourceName="cosmic_replica_initialization_source",targetName="cosmic_replica_initialization_target";
        var client=new MongoClient(uri);
        using(var names=await client.ListDatabaseNamesAsync())
        {
            var existing=await names.ToListAsync();Assert.DoesNotContain(sourceName,existing);Assert.DoesNotContain(targetName,existing);
        }
        var sourceDb=client.GetDatabase(sourceName);var targetDb=client.GetDatabase(targetName);
        var owner=Guid.NewGuid().ToString("D");
        foreach(var db in new[]{sourceDb,targetDb})await db.GetCollection<BsonDocument>("CosmicOwnedFixture").InsertOneAsync(new BsonDocument{{"_id","initial-baseline-v1"},{"owner",owner}});
        var source=new Provider(uri,sourceName);var target=new Provider(uri,targetName);var other=new Provider(uri,targetName);
        var complete=false;var actor=Guid.NewGuid();
        var rows=targetDb.GetCollection<BsonDocument>("Holon");var receipts=targetDb.GetCollection<BsonDocument>("CosmicWriteReceipt");
        long Count(Guid id)=>rows.CountDocuments(new BsonDocument("HolonId",id.ToString("D")));
        CommittedHolonReplicaRequest Transfer(IHolon value)=>new(Guid.NewGuid(),"owned-mongo-baseline-source",ProviderType.MongoDBOASIS,actor,ConditionalHolonOperation.Save,new[]{value});
        async Task<IHolon> Save(Provider provider,IHolon value)
        {
            var nodes=new[]{value}.Concat(value.Children);
            var result=await provider.ExecuteConditionalHolonWriteAsync(new ConditionalHolonWriteRequest(
                new ConditionalHolonWrite(Guid.NewGuid(),actor,ProviderType.MongoDBOASIS,nodes.Select(node=>new HolonWriteCondition(node.Id,
                    node.Version==0 ? null:CosmicConcurrency.GetProviderRevisionId(node,ProviderType.MongoDBOASIS)))),ConditionalHolonOperation.Save,new[]{value}));
            Assert.False(result.IsError,result.Message+result.Exception);return Assert.Single(result.Result);
        }
        async Task<IHolon> VersionFour()
        {
            IHolon value=new Holon{Id=Guid.NewGuid(),Name="source baseline",Children=new List<IHolon>{new Holon{Id=Guid.NewGuid(),Name="source child"}}};
            for(var version=1;version<=4;version++)value=await Save(source,value);
            return value;
        }
        try
        {
            foreach(var provider in new[]{source,target,other})Assert.False((await provider.ActivateProviderAsync()).IsError);
            var committed=await VersionFour();var baseline=Transfer(committed);
            Assert.Equal("COSMIC_REPLICA_LINEAGE_CONFLICT",(await target.ApplyCommittedHolonReplicaAsync(baseline)).ErrorCode);
            var absent=await target.ReadReplicaInitializationOutcomeAsync(baseline);
            Assert.False(absent.IsError,absent.Message+absent.Exception);Assert.Equal(ConditionalHolonReceiptState.Absent,absent.Result.ReceiptState);
            Assert.Equal(0,await receipts.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));Assert.Equal(0,Count(committed.Id));
            var installed=synchronous ? target.InitializeCommittedHolonReplica(baseline):await target.InitializeCommittedHolonReplicaAsync(baseline);
            Assert.False(installed.IsError,installed.Message+installed.Exception);
            var ack=Assert.Single(installed.Result);Assert.Equal(4,ack.Version);Assert.Equal(committed.VersionId,ack.VersionId);
            Assert.Equal(committed.PreviousVersionId,ack.PreviousVersionId);Assert.Equal(committed.CreatedDate,ack.CreatedDate);
            Assert.Single(ack.Children);
            Assert.NotEqual(CosmicConcurrency.GetProviderRevisionId(committed,ProviderType.MongoDBOASIS),CosmicConcurrency.GetProviderRevisionId(ack,ProviderType.MongoDBOASIS));
            var revision=CosmicConcurrency.GetProviderRevisionId(ack,ProviderType.MongoDBOASIS);
            var beforeInspection=await receipts.Find(new BsonDocument("_id",baseline.OperationId.ToString("D"))).SingleAsync();
            var status=await target.ReadReplicaInitializationOutcomeAsync(baseline);
            Assert.False(status.IsError,status.Message+status.Exception);Assert.Equal(ConditionalHolonReceiptState.Committed,status.Result.ReceiptState);
            Assert.Equal(64,status.Result.ReceiptDigest.Length);
            Assert.Equal(beforeInspection.ToBson(),(await receipts.Find(new BsonDocument("_id",baseline.OperationId.ToString("D"))).SingleAsync()).ToBson());
            await other.DeActivateProviderAsync();other=new Provider(uri,targetName);Assert.False((await other.ActivateProviderAsync()).IsError);
            var replay=await other.InitializeCommittedHolonReplicaAsync(baseline);Assert.False(replay.IsError,replay.Message+replay.Exception);
            Assert.Equal(revision,CosmicConcurrency.GetProviderRevisionId(Assert.Single(replay.Result),ProviderType.MongoDBOASIS));
            var next=StoredRecordJson.Project<Holon>(committed);next.Children=committed.Children.Select(child=>(IHolon)StoredRecordJson.Project<Holon>(child)).ToList();
            next.Name="version five";var fifth=await Save(source,next);
            var ordinary=Transfer(fifth);Assert.False((await target.ApplyCommittedHolonReplicaAsync(ordinary)).IsError);
            Assert.Equal("COSMIC_INITIALIZATION_STATUS_UNVERIFIED",(await target.ReadReplicaInitializationOutcomeAsync(ordinary)).ErrorCode);
            Assert.Equal(status.Result.ReceiptDigest,(await other.ReadReplicaInitializationOutcomeAsync(baseline)).Result.ReceiptDigest);
            var damaged=beforeInspection["ResultJson"].AsString.Replace("source baseline","corrupt baseline");Assert.NotEqual(beforeInspection["ResultJson"].AsString,damaged);
            await receipts.UpdateOneAsync(new BsonDocument("_id",baseline.OperationId.ToString("D")),new BsonDocument("$set",new BsonDocument("ResultJson",damaged)));
            Assert.Equal("COSMIC_INITIALIZATION_STATUS_UNVERIFIED",(await target.ReadReplicaInitializationOutcomeAsync(baseline)).ErrorCode);
            await receipts.UpdateOneAsync(new BsonDocument("_id",baseline.OperationId.ToString("D")),new BsonDocument("$set",new BsonDocument("ResultJson",beforeInspection["ResultJson"])));
            Assert.Equal(4,Assert.Single((await target.InitializeCommittedHolonReplicaAsync(baseline)).Result).Version);
            Assert.Equal(5,(await target.LoadHolonAsync(committed.Id,false)).Result.Version);
            Assert.Equal("COSMIC_OPERATION_ID_REUSED",(await target.ApplyCommittedHolonReplicaAsync(baseline)).ErrorCode);

            var contested=await VersionFour();var a=Transfer(contested);var b=Transfer(contested);
            var race=await Task.WhenAll(target.InitializeCommittedHolonReplicaAsync(a),other.InitializeCommittedHolonReplicaAsync(b));
            Assert.Single(race.Where(result=>!result.IsError));
            Assert.Contains(Assert.Single(race.Where(result=>result.IsError)).ErrorCode,new[]{"COSMIC_REPLICA_LINEAGE_CONFLICT","COSMIC_CONCURRENT_WRITE"});
            Assert.Equal(1,await receipts.CountDocumentsAsync(new BsonDocument("_id",new BsonDocument("$in",new BsonArray{a.OperationId.ToString("D"),b.OperationId.ToString("D")}))));

            var blocked=await VersionFour();var childId=Assert.Single(blocked.Children).Id;
            var independent=await Save(target,new Holon{Id=childId,Name="independent child"});
            var rejected=Transfer(blocked);
            Assert.Equal("COSMIC_REPLICA_LINEAGE_CONFLICT",(await target.InitializeCommittedHolonReplicaAsync(rejected)).ErrorCode);
            Assert.Equal(0,Count(blocked.Id));Assert.Equal(independent.VersionId,(await target.LoadHolonAsync(childId,false)).Result.VersionId);
            Assert.Equal(0,await receipts.CountDocumentsAsync(new BsonDocument("_id",rejected.OperationId.ToString("D"))));
            var deleted=await target.ExecuteConditionalHolonWriteAsync(new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),actor,ProviderType.MongoDBOASIS,
                new[]{new HolonWriteCondition(childId,CosmicConcurrency.GetProviderRevisionId(independent,ProviderType.MongoDBOASIS))}),ConditionalHolonOperation.HardDelete,new[]{independent},saveChildren:false));
            Assert.False(deleted.IsError,deleted.Message+deleted.Exception);Assert.Equal(0,Count(childId));
            Assert.Equal("COSMIC_REPLICA_LINEAGE_CONFLICT",(await target.InitializeCommittedHolonReplicaAsync(rejected)).ErrorCode);
            Assert.Equal(0,Count(blocked.Id));Assert.Equal(0,Count(childId));

            var fresh=await VersionFour();
            var unknownId=Guid.NewGuid().ToString("D");
            await receipts.InsertOneAsync(new BsonDocument{{"_id",unknownId},{"Fingerprint","unrecognized"},{"ResultJson","{}"}});
            Assert.Equal("COSMIC_REPLICA_BASELINE_RECONCILIATION_REQUIRED",(await target.InitializeCommittedHolonReplicaAsync(Transfer(fresh))).ErrorCode);
            foreach(var malformed in new BsonValue[]{BsonNull.Value,new BsonString("wrong"),new BsonArray(),new BsonArray{1}})
            {
                await receipts.UpdateOneAsync(new BsonDocument("_id",unknownId),new BsonDocument("$set",new BsonDocument("HolonIds",malformed)));
                Assert.Equal("COSMIC_REPLICA_BASELINE_RECONCILIATION_REQUIRED",(await target.InitializeCommittedHolonReplicaAsync(Transfer(fresh))).ErrorCode);
            }
            Assert.Equal(0,await targetDb.GetCollection<BsonDocument>("CosmicHolonLineageFence").CountDocumentsAsync(new BsonDocument("_id",fresh.Id.ToString("D"))));
            Assert.Equal(0,Count(fresh.Id));complete=true;
        }
        finally
        {
            foreach(var provider in new[]{source,target,other})await provider.DeActivateProviderAsync();
            if(complete)
            {
                foreach(var db in new[]{sourceDb,targetDb})
                {
                    var marker=await db.GetCollection<BsonDocument>("CosmicOwnedFixture").Find(new BsonDocument("_id","initial-baseline-v1")).SingleAsync();
                    Assert.Equal(owner,marker["owner"].AsString);await client.DropDatabaseAsync(db.DatabaseNamespace.DatabaseName);
                }
                using var names=await client.ListDatabaseNamesAsync();var remaining=await names.ToListAsync();
                Assert.DoesNotContain(sourceName,remaining);Assert.DoesNotContain(targetName,remaining);
            }
        }
    }
}
