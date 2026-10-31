using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using MongoProvider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using NeoProvider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
using SqliteProvider=NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;
using IoFile=System.IO.File;

public sealed partial class ReplicaBaselineRepairCommandTests
{
    [NativeFact]
    public Task ReviewedInFlightSqliteFixtureNeo()=>ReconcileOriginalSqliteInFlight(ProviderType.Neo4jOASIS);
    [NativeFact]
    public Task ReviewedInFlightSqliteFixtureMongo()=>ReconcileOriginalSqliteInFlight(ProviderType.MongoDBOASIS);

    // An explicit continuation of the two retained test outcomes. No new avatar,
    // record, Save or initialization operation is created, and journals are not repaired.
    private async Task ReconcileOriginalSqliteInFlight(ProviderType sourceType)
    {
        const string mongoUri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string neoUri="bolt://127.0.0.1:17687";
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../.."));
        var state=Path.Combine(root,"LocalState/COSMIC/ManagerContinuation",sourceType+"-SQLLiteDBOASIS-InFlight");
        using var lease=new FileStream(Path.Combine(state,"fixture.lock"),FileMode.Open,FileAccess.ReadWrite,FileShare.None);
        var manifest=Path.Combine(state,"fixture.json");var journal=Path.Combine(state,"application.json");
        using var fixture=JsonDocument.Parse(await IoFile.ReadAllTextAsync(manifest));var f=fixture.RootElement;
        Assert.False(f.GetProperty("cleaned").GetBoolean());
        var id=f.GetProperty("id").GetGuid();var actor=f.GetProperty("actor").GetGuid();var owner=f.GetProperty("owner").GetGuid();
        var initialization=f.GetProperty("initialization").GetGuid();var database=f.GetProperty("database").GetString()!;
        var operations=f.GetProperty("operations").EnumerateArray().Select(row=>row.GetString()!).ToArray();
        Assert.Equal(6,operations.Length);Assert.Equal(initialization.ToString("D"),operations[0]);
        Assert.Equal("cosmic_flight_"+sourceType+"_SQLLiteDBOASIS",database);
        var sqliteFile=f.GetProperty("sqliteFile").GetString()!;
        Assert.Equal(Path.Combine(state,"owned.db"),sqliteFile);
        Assert.NotEqual(sqliteFile,Path.GetFullPath(sqliteFile)); // Reproduced test composition mismatch.
        var originalJournal=await IoFile.ReadAllBytesAsync(journal);
        using var application=JsonDocument.Parse(originalJournal);var a=application.RootElement;
        Assert.Equal("SourceCompleted",a.GetProperty("phase").GetString());
        Assert.Equal(initialization,a.GetProperty("intent").GetProperty("InitializationOperationId").GetGuid());
        Assert.Equal(actor,a.GetProperty("intent").GetProperty("OperatorId").GetGuid());
        var sequence=a.GetProperty("intent").GetProperty("Sequence").GetInt64();
        var evidence=Path.Combine(state,"OriginalFailure");Directory.CreateDirectory(evidence);
        Assert.False(IoFile.Exists(Path.Combine(evidence,"application.json")),"Already reviewed: do not repeat this continuation.");
        IoFile.Copy(journal,Path.Combine(evidence,"application.json"));IoFile.Copy(manifest,Path.Combine(evidence,"fixture.json"));
        IOASISStorageProvider source=sourceType==ProviderType.MongoDBOASIS?new MongoProvider(mongoUri,database):new NeoProvider(neoUri,"neo4j","oasis-local-test-only");
        var target=new SqliteProvider($"Data Source={sqliteFile};Pooling=False;Mode=ReadWrite");
        var completed=false;
        using var driver=GraphDatabase.Driver(neoUri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var inspect=driver.AsyncSession();
        var client=new MongoClient(mongoUri);
        try
        {
            Assert.False((await source.ActivateProviderAsync()).IsError);Assert.False((await target.ActivateProviderAsync()).IsError);
            var outbox=((IHolonReplicationOutboxSource)source).GetHolonReplicationOutbox().Result;
            Assert.Equal(HolonDeliveryState.Completed,(await outbox.InspectAsync(sequence)).Result.State);
            var frozen=(await ((IHolonReplicationObligationReader)outbox).ReadObligationAsync(sequence)).Result;
            var original=CommittedHolonReplicaPayload.Read(frozen.PayloadJson);
            Assert.Equal(Guid.Parse(operations[4]),original.OperationId);Assert.Equal(id,Assert.Single(original.Roots).Id);Assert.Equal(4,original.Roots[0].Version);
            var baseline=new CommittedHolonReplicaRequest(initialization,original.SourceId,original.SourceAuthority,original.ActorId,
                original.Operation,original.Roots,original.SaveChildren,original.Recursive,original.MaxChildDepth);
            var receipt=(await target.ReadReplicaInitializationOutcomeAsync(baseline)).Result;
            Assert.Equal(ConditionalHolonReceiptState.Committed,receipt.ReceiptState);Assert.Equal(a.GetProperty("receiptDigest").GetString(),receipt.ReceiptDigest);
            Assert.Equal(a.GetProperty("intent").GetProperty("BaselineFingerprint").GetString(),receipt.BaselineFingerprint);
            var before=(await target.LoadHolonAsync(id,loadChildren:false)).Result;Assert.Equal(4,before.Version);Assert.Equal(actor,before.CreatedByAvatarId);
            var current=(await source.LoadHolonAsync(id,loadChildren:false)).Result;Assert.Equal(5,current.Version);Assert.Equal(actor,current.CreatedByAvatarId);
            var targetId="hyperdrive-target:SQLLiteDBOASIS";var pending=(await outbox.PeekNextAsync(targetId)).Result;
            Assert.Equal(Guid.Parse(operations[5]),pending.OperationId);Assert.Equal(HolonDeliveryState.Pending,pending.State);
            var intentPath=Path.Combine(evidence,"delivery.intent.json");
            using(var file=new FileStream(intentPath,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {JsonSerializer.Serialize(file,new{id,actor,sequence=pending.Sequence,operation=pending.OperationId});file.Flush(true);}
            var dispatcher=new HolonOutboxDispatcher(outbox,new[]{new CommittedHolonReplicaTarget(targetId,target,ProviderType.SQLLiteDBOASIS,
                "hyperdrive-source:"+sourceType,sourceType)},"reviewed-original-fixture-continuation");
            Assert.Equal(1,(await dispatcher.DispatchOnceAsync(targetId)).Result.CompletedCount);
            var after=(await target.LoadHolonAsync(id,loadChildren:false)).Result;Assert.Equal(5,after.Version);Assert.Equal(current.Name,after.Name);Assert.Equal(actor,after.CreatedByAvatarId);
            Assert.Null((await outbox.PeekNextAsync(targetId)).Result);Assert.Equal(HolonDeliveryState.Completed,(await outbox.InspectAsync(pending.Sequence)).Result.State);
            Assert.Equal(originalJournal,await IoFile.ReadAllBytesAsync(journal));
            using(var file=new FileStream(Path.Combine(evidence,"delivery.acknowledged.json"),FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {JsonSerializer.Serialize(file,new{id,actor,sequence=pending.Sequence,version=after.Version,baselineReceipt=receipt.ReceiptDigest,newSaves=0,newInitializations=0});file.Flush(true);}
            completed=true;
        }
        finally
        {
            try{await source.DeActivateProviderAsync();}finally{target.Dispose();}
            if(completed)
            {
                if(sourceType==ProviderType.MongoDBOASIS)
                {
                    var marker=await client.GetDatabase(database).GetCollection<BsonDocument>("CosmicOwnedFixture").Find(new BsonDocument("_id","manager-continuation-v1")).SingleAsync();
                    Assert.Equal(owner.ToString("D"),marker["owner"].AsString);await client.DropDatabaseAsync(database);
                    using var names=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(database,await names.ToListAsync());
                }
                else
                {
                    const string owned="MATCH (n) WHERE (n:Holon AND n.Id=$id) OR (n:CosmicReplicaHead AND n.HolonId=$id) OR (n:OasisRecordVersion AND n.Kind='Holon' AND n.RecordId=$id) OR ((n:CosmicWriteReceipt OR n:CosmicHolonOutbox) AND n.OperationId IN $operations) ";
                    await(await inspect.RunAsync(owned+"DETACH DELETE n",new{id=id.ToString("D"),operations})).ConsumeAsync();
                    var count=await(await inspect.RunAsync(owned+"RETURN count(n) AS count",new{id=id.ToString("D"),operations})).SingleAsync();Assert.Equal(0L,count["count"].As<long>());
                }
                foreach(var file in new[]{sqliteFile,sqliteFile+"-wal",sqliteFile+"-shm",journal,journal+".tmp",journal+".lock"})if(IoFile.Exists(file))IoFile.Delete(file);
                var metadata=System.Text.Json.Nodes.JsonNode.Parse(await IoFile.ReadAllTextAsync(manifest))!;metadata["cleaned"]=true;
                using(var file=new FileStream(manifest+".tmp",FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {JsonSerializer.Serialize(file,metadata);file.Flush(true);}IoFile.Move(manifest+".tmp",manifest,true);
            }
        }
    }
}
