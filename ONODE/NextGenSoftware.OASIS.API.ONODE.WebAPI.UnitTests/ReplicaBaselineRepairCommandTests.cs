using System.Security.Cryptography;
using System.Text;
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

public sealed partial class ReplicaBaselineRepairCommandTests
{
    private sealed class NativeFact : FactAttribute
    {
        public NativeFact(){if(Environment.GetEnvironmentVariable("COSMIC_REPLICA_REPAIR_NATIVE")!="owned-local-27029")Skip="Explicit reusable native repair profile required.";}
    }

    [NativeFact]
    public async Task ReviewedCommandRepairsActualMongoToSqliteAndPreservesNormalNextDelivery()
    {
        const string uri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string database="cosmic_demo_baseline_repair";
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../.."));
        var state=Path.Combine(root,"LocalState/COSMIC/ReplicaRepairNative");Directory.CreateDirectory(state);
        var results=Path.Combine(root,"TestResults/ReplicaRepairNativeCurrent");Directory.CreateDirectory(results);
        using var lease=File.Open(Path.Combine(state,"fixture.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var targetPath=Path.Combine(results,"target.db");var journal=Path.Combine(state,"initialization.json");
        foreach(var path in new[]{targetPath,targetPath+"-wal",targetPath+"-shm",journal,journal+".tmp",journal+".lock"})
            Assert.False(File.Exists(path),"Preserve original fixture: "+path);
        var ownership=Path.Combine(state,"fixture.json");
        if(File.Exists(ownership))Assert.True(JsonDocument.Parse(await File.ReadAllTextAsync(ownership)).RootElement.GetProperty("cleaned").GetBoolean());
        var client=new MongoClient(uri);using(var names=await client.ListDatabaseNamesAsync())Assert.DoesNotContain(database,await names.ToListAsync());
        var actor=Guid.NewGuid();var id=Guid.NewGuid();var initialization=Guid.NewGuid();var owner=Guid.NewGuid().ToString("D");
        void Record(bool cleaned)
        {
            using(var file=new FileStream(ownership+".tmp",FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {JsonSerializer.Serialize(file,new{owner,database,targetPath,journal,actor,id,initialization,cleaned});file.Flush(true);}
            File.Move(ownership+".tmp",ownership,true);
        }
        Record(false);
        await client.GetDatabase(database).GetCollection<BsonDocument>("CosmicOwnedFixture")
            .InsertOneAsync(new BsonDocument{{"_id","demo-repair-v1"},{"owner",owner}});
        var source=new MongoProvider(uri,database);
        var connection="Data Source="+targetPath+";Pooling=False";
        var complete=false;
        try
        {
            Assert.False((await source.ActivateProviderAsync()).IsError);
            // Establish the owned test target schema; the operator command must
            // subsequently open this file and may never replace a missing store.
            using(var target=new SqliteProvider(connection))Assert.False((await target.ActivateProviderAsync()).IsError);
            IHolon current=new Holon{Id=id,Name="late demo baseline"};
            async Task Save(int version)
            {
                var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Guid.NewGuid(),actor,ProviderType.MongoDBOASIS,
                    new[]{new HolonWriteCondition(id,CosmicConcurrency.GetProviderRevisionId(current,ProviderType.MongoDBOASIS))}),
                    ConditionalHolonOperation.Save,new[]{current},saveChildren:false);
                var result=version<4 ? await source.ExecuteConditionalHolonWriteAsync(request)
                    :await source.ExecuteReplicatingHolonWriteAsync(request,new HolonReplicationEnrollment(HolonReplicationRoutes.SourceId(ProviderType.MongoDBOASIS),
                        new[]{HolonReplicationRoutes.TargetId(ProviderType.SQLLiteDBOASIS)}));
                Assert.False(result.IsError,result.Message+result.Exception);current=Assert.Single(result.Result);Assert.Equal(version,current.Version);
            }
            for(var version=1;version<=4;version++)await Save(version);
            var outbox=(IHolonReplicationObligationReader)source.GetHolonReplicationOutbox().Result;
            using(var target=new SqliteProvider(connection))
            {
                Assert.False((await target.ActivateProviderAsync()).IsError);
                var delivery=new HolonOutboxDispatcher(outbox,new[]{new CommittedHolonReplicaTarget(HolonReplicationRoutes.TargetId(ProviderType.SQLLiteDBOASIS),target,
                    ProviderType.SQLLiteDBOASIS,HolonReplicationRoutes.SourceId(ProviderType.MongoDBOASIS),ProviderType.MongoDBOASIS)},"owned-command-test");
                var failed=await delivery.DispatchOnceAsync(HolonReplicationRoutes.TargetId(ProviderType.SQLLiteDBOASIS));
                Assert.Equal(1,failed.Result.ReconciliationCount);
            }
            var held=(await outbox.PeekNextAsync(HolonReplicationRoutes.TargetId(ProviderType.SQLLiteDBOASIS))).Result;
            var original=(await outbox.ReadObligationAsync(held.Sequence)).Result;
            var reviewed=new ReplicaBaselineRepairCommand.Request(Guid.NewGuid(),initialization,Guid.NewGuid(),held.Sequence,
                held.OperationId,actor,id,original.PayloadDigest,ReplicaBaselineRepairCommand.StoreIdentity(uri,database),targetPath);
            var dna=new OASISDNA{OASIS=new OASIS{HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings
            {MongoDBOASIS=new MongoDBOASISProviderSettings{ConnectionString=uri,DBName=database},SQLLiteDBOASIS=new SQLLiteDBOASISSettings{ConnectionString=connection}}}};
            await Assert.ThrowsAsync<InvalidOperationException>(()=>ReplicaBaselineRepairCommand.ExecuteAsync(dna,reviewed with{SourcePayloadDigest=new string('0',64)},journal));
            Assert.Equal(HolonDeliveryState.ReconciliationRequired,(await outbox.InspectAsync(held.Sequence)).Result.State);
            var repaired=await ReplicaBaselineRepairCommand.ExecuteAsync(dna,reviewed,journal);
            Assert.False(repaired.IsError,repaired.Message+repaired.Exception);Assert.True(repaired.Result);
            var acknowledged=await File.ReadAllBytesAsync(journal);
            Assert.True((await ReplicaBaselineRepairCommand.ExecuteAsync(dna,reviewed,journal)).Result);
            Assert.Equal(acknowledged,await File.ReadAllBytesAsync(journal));
            current.Name="ordinary next update";await Save(5);
            using(var target=new SqliteProvider(connection))
            {
                Assert.False((await target.ActivateProviderAsync()).IsError);
                var delivery=new HolonOutboxDispatcher(outbox,new[]{new CommittedHolonReplicaTarget(HolonReplicationRoutes.TargetId(ProviderType.SQLLiteDBOASIS),target,
                    ProviderType.SQLLiteDBOASIS,HolonReplicationRoutes.SourceId(ProviderType.MongoDBOASIS),ProviderType.MongoDBOASIS)},"owned-command-test");
                var next=await delivery.DispatchOnceAsync(HolonReplicationRoutes.TargetId(ProviderType.SQLLiteDBOASIS));
                Assert.Equal(1,next.Result.CompletedCount);
                Assert.Equal(5,(await target.LoadHolonAsync(id,loadChildren:false)).Result.Version);
            }
            complete=true;
        }
        finally
        {
            await source.DeActivateProviderAsync();
            if(complete)
            {
                var marker=await client.GetDatabase(database).GetCollection<BsonDocument>("CosmicOwnedFixture").Find(new BsonDocument("_id","demo-repair-v1")).SingleAsync();
                Assert.Equal(owner,marker["owner"].AsString);await client.DropDatabaseAsync(database);
                using(var names=await client.ListDatabaseNamesAsync())Assert.DoesNotContain(database,await names.ToListAsync());
                foreach(var path in new[]{targetPath,targetPath+"-wal",targetPath+"-shm",journal,journal+".tmp",journal+".lock"})if(File.Exists(path))File.Delete(path);
                Record(true);
            }
        }
    }
}
