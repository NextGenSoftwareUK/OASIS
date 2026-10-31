using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using MongoProvider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider=NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;
using IoFile=System.IO.File;

public sealed partial class ReplicaBaselineRepairCommandTests
{
    [NativeFact]
    public async Task ReviewedCommandRepairsActualSqliteToMongoAndPreservesNormalNextDelivery()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string database="cosmic_sqlite_source_baseline_target";
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../.."));
        var state=Path.Combine(root,"LocalState/COSMIC/SqliteSourceRepairNative");Directory.CreateDirectory(state);
        var results=Path.Combine(root,"TestResults/ReplicaRepairNativeCurrent");Directory.CreateDirectory(results);
        using var lease=new FileStream(Path.Combine(state,"fixture.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var sourcePath=Path.Combine(state,"source.db");var journal=Path.Combine(state,"application.json");var manifest=Path.Combine(state,"fixture.json");
        foreach(var path in new[]{sourcePath,sourcePath+"-wal",sourcePath+"-shm",journal,journal+".tmp",journal+".lock"})
            Assert.False(IoFile.Exists(path),"Preserve prior unresolved fixture.");
        if(IoFile.Exists(manifest))
        {
            using var previous=JsonDocument.Parse(await IoFile.ReadAllTextAsync(manifest));
            Assert.True(previous.RootElement.GetProperty("cleaned").GetBoolean(),"Preserve original outcomes.");
        }
        var client=new MongoClient(uri);
        using(var names=await client.ListDatabaseNamesAsync())Assert.DoesNotContain(database,await names.ToListAsync());
        var owner=Guid.NewGuid();var actor=Guid.NewGuid();var id=Guid.NewGuid();var initialization=Guid.NewGuid();var complete=false;
        void Record(bool cleaned)
        {
            using(var file=new FileStream(manifest+".tmp",FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {JsonSerializer.Serialize(file,new{owner,actor,id,initialization,sourcePath,database,journal,cleaned});file.Flush(true);}
            IoFile.Move(manifest+".tmp",manifest,true);
        }
        Record(false);
        await client.GetDatabase(database).GetCollection<BsonDocument>("CosmicOwnedFixture").InsertOneAsync(
            new BsonDocument{{"_id","sqlite-source-repair-v1"},{"owner",owner.ToString("D")}});
        var connection=$"Data Source={sourcePath};Pooling=False";
        var source=new SqliteProvider(connection);var target=new MongoProvider(uri,database);
        try
        {
            Assert.False((await source.ActivateProviderAsync()).IsError);Assert.False((await target.ActivateProviderAsync()).IsError);
            IHolon current=new Holon{Id=id,Name="owned SQLite source repair"};
            async Task Save(int version)
            {
                var write=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),actor,ProviderType.SQLLiteDBOASIS,
                    new[]{new HolonWriteCondition(id,CosmicConcurrency.GetProviderRevisionId(current,ProviderType.SQLLiteDBOASIS))}),
                    ConditionalHolonOperation.Save,new[]{current},saveChildren:false);
                var saved=version<4 ? await source.ExecuteConditionalHolonWriteAsync(write)
                    :await source.ExecuteReplicatingHolonWriteAsync(write,new HolonReplicationEnrollment(HolonReplicationRoutes.SourceId(ProviderType.SQLLiteDBOASIS),
                        new[]{HolonReplicationRoutes.TargetId(ProviderType.MongoDBOASIS)}));
                Assert.False(saved.IsError,saved.Message+saved.Exception);current=Assert.Single(saved.Result);Assert.Equal(version,current.Version);
            }
            for(var version=1;version<=4;version++)await Save(version);
            var outbox=(NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SqliteHolonReplicationOutbox)source.GetHolonReplicationOutbox().Result;
            var dispatcher=new HolonOutboxDispatcher(outbox,new[]{new CommittedHolonReplicaTarget(HolonReplicationRoutes.TargetId(ProviderType.MongoDBOASIS),target,
                ProviderType.MongoDBOASIS,HolonReplicationRoutes.SourceId(ProviderType.SQLLiteDBOASIS),ProviderType.SQLLiteDBOASIS)},"owned-sqlite-repair-worker");
            Assert.Equal(1,(await dispatcher.DispatchOnceAsync(HolonReplicationRoutes.TargetId(ProviderType.MongoDBOASIS))).Result.ReconciliationCount);
            var held=(await outbox.PeekNextAsync(HolonReplicationRoutes.TargetId(ProviderType.MongoDBOASIS))).Result;
            var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings {
                MongoDBOASIS=new() {ConnectionString=uri,DBName=database},SQLLiteDBOASIS=new SQLLiteDBOASISSettings {ConnectionString=connection}}}};
            var reviewed=new ReplicaBaselineRepairCommand.Request(actor,initialization,Guid.NewGuid(),held.Sequence,held.OperationId,actor,id,
                (await outbox.ReadObligationAsync(held.Sequence)).Result.PayloadDigest,ReplicaBaselineRepairCommand.SqliteStoreIdentity(connection),null,
                "SQLLiteDBOASIS","MongoDBOASIS",ReplicaBaselineRepairCommand.StoreIdentity(uri,database));
            await Assert.ThrowsAsync<InvalidOperationException>(()=>ReplicaBaselineRepairCommand.ExecuteAsync(dna,
                reviewed with{TargetStoreIdentity="mongo-store:wrong"},journal));
            dna.OASIS.StorageProviders.MongoDBOASIS.DBName=database+"_must_not_create";
            try
            {
                await Assert.ThrowsAsync<InvalidOperationException>(()=>ReplicaBaselineRepairCommand.ExecuteAsync(dna,
                    reviewed with{TargetStoreIdentity=ReplicaBaselineRepairCommand.StoreIdentity(uri,database+"_must_not_create")},journal));
                using var names=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(database+"_must_not_create",await names.ToListAsync());
            }
            finally { dna.OASIS.StorageProviders.MongoDBOASIS.DBName=database; }
            Assert.Equal(HolonDeliveryState.ReconciliationRequired,(await outbox.InspectAsync(held.Sequence)).Result.State);
            var applied=await ReplicaBaselineRepairCommand.ExecuteAsync(dna,reviewed,journal);Assert.False(applied.IsError,applied.Message+applied.Exception);Assert.True(applied.Result);
            var bytes=await IoFile.ReadAllBytesAsync(journal);
            Assert.True((await ReplicaBaselineRepairCommand.ExecuteAsync(dna,reviewed,journal)).Result);
            Assert.Equal(bytes,await IoFile.ReadAllBytesAsync(journal));
            Assert.Equal(HolonDeliveryState.Completed,(await outbox.InspectAsync(held.Sequence)).Result.State);
            Assert.Equal(held.ErrorCode,(await outbox.InspectAsync(held.Sequence)).Result.ErrorCode);
            current.Name="ordinary next version";await Save(5);
            Assert.Equal(1,(await dispatcher.DispatchOnceAsync(HolonReplicationRoutes.TargetId(ProviderType.MongoDBOASIS))).Result.CompletedCount);
            var loaded=(await target.LoadHolonAsync(id,loadChildren:false)).Result;Assert.Equal(5,loaded.Version);Assert.Equal(actor,loaded.CreatedByAvatarId);
            complete=true;
        }
        finally
        {
            source.Dispose();await target.DeActivateProviderAsync();
            if(complete)
            {
                var marker=await client.GetDatabase(database).GetCollection<BsonDocument>("CosmicOwnedFixture").Find(new BsonDocument("_id","sqlite-source-repair-v1")).SingleAsync();
                Assert.Equal(owner.ToString("D"),marker["owner"].AsString);await client.DropDatabaseAsync(database);
                using(var names=await client.ListDatabaseNamesAsync())Assert.DoesNotContain(database,await names.ToListAsync());
                foreach(var path in new[]{sourcePath,sourcePath+"-wal",sourcePath+"-shm",journal,journal+".lock",journal+".tmp"})if(IoFile.Exists(path))IoFile.Delete(path);
                Record(true);
            }
        }
    }
}
