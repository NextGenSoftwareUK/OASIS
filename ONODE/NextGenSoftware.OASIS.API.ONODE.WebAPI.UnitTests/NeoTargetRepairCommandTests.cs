using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using MongoProvider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider=NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;
using NeoProvider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
using IoFile=System.IO.File;

public sealed partial class ReplicaBaselineRepairCommandTests
{
    [NativeFact]
    public Task ReviewedCommandRepairsActualSqliteToNeoAndPreservesNormalNextDelivery()=>RunReviewedNeoTarget(false);

    [NativeFact]
    public Task ReviewedCommandRepairsActualMongoToNeoAndPreservesNormalNextDelivery()=>RunReviewedNeoTarget(true);

    [NativeFact]
    public Task ManagerSaveMaintainsRecoveredMongoToNeoRoute()=>RunReviewedNeoTarget(true,true);

    [NativeFact]
    public Task ManagerSaveMaintainsRecoveredSqliteToNeoRoute()=>RunReviewedNeoTarget(false,true);

    private async Task RunReviewedNeoTarget(bool mongoSource,bool managerWrites=false)
    {
        const string mongoUri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string neoUri="bolt://127.0.0.1:17687";
        var database=managerWrites?"cosmic_managed_neo_cutover_source":"cosmic_reviewed_neo_target_source";
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../.."));
        var state=Path.Combine(root,"LocalState/COSMIC",(mongoSource?"MongoToNeoRepairNative":"SqliteToNeoRepairNative")+(managerWrites?"-ManagerCutover":""));Directory.CreateDirectory(state);
        using var lease=new FileStream(Path.Combine(state,"fixture.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var manifest=Path.Combine(state,"fixture.json");var journal=Path.Combine(state,"application.json");var sourceFile=Path.Combine(state,"source.db");
        foreach(var file in new[]{sourceFile,sourceFile+"-wal",sourceFile+"-shm",journal,journal+".tmp",journal+".lock"})
            Assert.False(IoFile.Exists(file),"Preserve unresolved original fixture.");
        using var driver=GraphDatabase.Driver(neoUri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var inspect=driver.AsyncSession();
        const string ownedMatch="MATCH (n) WHERE (n:Holon AND n.Id=$id) OR (n:CosmicReplicaHead AND n.HolonId=$id) "
            +"OR (n:OasisRecordVersion AND n.Kind='Holon' AND n.RecordId=$id) "
            +"OR (n:CosmicWriteReceipt AND n.OperationId IN $operations) ";
        if(IoFile.Exists(manifest))
        {
            using var prior=JsonDocument.Parse(await IoFile.ReadAllTextAsync(manifest));
            Assert.True(prior.RootElement.GetProperty("cleaned").GetBoolean(),"Preserve original native outcomes.");
            var priorId=prior.RootElement.GetProperty("id").GetString();
            var priorOps=prior.RootElement.GetProperty("operations").EnumerateArray().Select(row=>row.GetString()!).ToArray();
            var remaining=await(await inspect.RunAsync(ownedMatch+"RETURN count(n) AS count",new{id=priorId,operations=priorOps})).SingleAsync();
            Assert.Equal(0L,remaining["count"].As<long>());
        }
        var client=new MongoClient(mongoUri);
        if(mongoSource){using var names=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(database,await names.ToListAsync());}
        var owner=Guid.NewGuid();var actor=Guid.NewGuid();var id=Guid.NewGuid();var initialization=Guid.NewGuid();var operations=new List<string>();var complete=false;
        void Record(bool cleaned)
        {
            using(var file=new FileStream(manifest+".tmp",FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {JsonSerializer.Serialize(file,new{owner,actor,id,initialization,operations,database,mongoSource,sourceFile,cleaned});file.Flush(true);}
            IoFile.Move(manifest+".tmp",manifest,true);
        }
        Record(false);
        if(mongoSource)await client.GetDatabase(database).GetCollection<BsonDocument>("CosmicOwnedFixture").InsertOneAsync(
            new BsonDocument{{"_id","reviewed-neo-target-v1"},{"owner",owner.ToString("D")}});
        var sqlite=$"Data Source={sourceFile};Pooling=False";
        IOASISStorageProvider source=mongoSource?new MongoProvider(mongoUri,database):new SqliteProvider(sqlite);
        var target=new NeoProvider(neoUri,"neo4j","oasis-local-test-only");
        var sourceType=mongoSource?ProviderType.MongoDBOASIS:ProviderType.SQLLiteDBOASIS;
        var sourceId=HolonReplicationRoutes.SourceId(sourceType);var targetId=HolonReplicationRoutes.TargetId(ProviderType.Neo4jOASIS);
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings {
                MongoDBOASIS=new(){ConnectionString=mongoUri,DBName=database},SQLLiteDBOASIS=new(){ConnectionString=sqlite},
                Neo4jOASIS=new(){ConnectionString=neoUri,Username="neo4j",Password="oasis-local-test-only"}}}};
        dna.OASIS.StorageProviders.LogSwitchingProviders=false;
        dna.OASIS.SubscriptionConfig.PlanType="Gold";
        var registry=new ProviderManager(null,dna);
        registry.RegisterProvider(source);registry.RegisterProvider(target);
        Assert.False(registry.PublishRoutingOverrides(new ProviderRoutingOverrides(false,false,false,
            replicationProviders:new[]{ProviderType.Neo4jOASIS})).IsError);
        var manager=new HolonManager(null,dna,registry);
        try
        {
            Assert.False((await source.ActivateProviderAsync()).IsError);Assert.False((await target.ActivateProviderAsync()).IsError);
            var outbox=((IHolonReplicationOutboxSource)source).GetHolonReplicationOutbox().Result;
            Assert.Null((await outbox.PeekNextAsync(targetId)).Result);
            IHolon current=new Holon{Id=id,Name="owned reviewed Neo target"};
            async Task Save(int version)
            {
                var operation=Guid.NewGuid();operations.Add(operation.ToString("D"));Record(false);
                if(managerWrites)
                {
                    current.Name="owned managed late enrollment version "+version;
                    var oldAvatar=OASISRequestContext.AvatarRoutingOverrides;
                    var oldRequest=OASISRequestContext.RequestRoutingOverrides;
                    try
                    {
                        OASISRequestContext.AvatarRoutingOverrides=new ProviderRoutingOverrides(autoReplicationEnabled:true,
                            replicationProviders:new[]{ProviderType.Neo4jOASIS});
                        OASISRequestContext.RequestRoutingOverrides=new ProviderRoutingOverrides(autoReplicationEnabled:version>=4,
                            autoFailoverEnabled:false,autoLoadBalancingEnabled:false);
                        // No explicit ReplicationEnrollment: HolonManager must derive it
                        // from request > avatar > node and atomically enrol the write.
                        var managed=await manager.SaveOwnedHolonAsync(current,actor,new CosmicWriteOptions(operation),
                            saveChildren:false,providerType:sourceType);
                        Assert.False(managed.IsError,managed.Message+managed.Exception);Assert.True(managed.IsSaved);
                        Assert.Same(current,managed.Result);Assert.Equal(version,current.Version);
                        if(version<4)Assert.Null((await outbox.PeekNextAsync(targetId)).Result);
                    }
                    finally
                    {
                        OASISRequestContext.AvatarRoutingOverrides=oldAvatar;
                        OASISRequestContext.RequestRoutingOverrides=oldRequest;
                    }
                    return;
                }
                var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation,actor,sourceType,
                    new[]{new HolonWriteCondition(id,CosmicConcurrency.GetProviderRevisionId(current,sourceType))}),
                    ConditionalHolonOperation.Save,new[]{current},saveChildren:false);
                var saved=version<4?await ((IConditionalHolonStorageProvider)source).ExecuteConditionalHolonWriteAsync(request)
                    :await ((IAtomicHolonReplicationStorageProvider)source).ExecuteReplicatingHolonWriteAsync(request,new HolonReplicationEnrollment(sourceId,new[]{targetId}));
                Assert.False(saved.IsError,saved.Message+saved.Exception);current=Assert.Single(saved.Result);Assert.Equal(version,current.Version);
            }
            for(var version=1;version<=4;version++)await Save(version);
            var dispatcher=new HolonOutboxDispatcher(outbox,new[]{new CommittedHolonReplicaTarget(targetId,target,ProviderType.Neo4jOASIS,sourceId,sourceType)},"owned-reviewed-neo-target-worker");
            Assert.Equal(1,(await dispatcher.DispatchOnceAsync(targetId)).Result.ReconciliationCount);
            var held=(await outbox.PeekNextAsync(targetId)).Result;
            var reader=(IHolonReplicationObligationReader)outbox;

            var reviewed=new ReplicaBaselineRepairCommand.Request(actor,initialization,Guid.NewGuid(),held.Sequence,held.OperationId,actor,id,
                (await reader.ReadObligationAsync(held.Sequence)).Result.PayloadDigest,
                mongoSource?ReplicaBaselineRepairCommand.StoreIdentity(mongoUri,database):ReplicaBaselineRepairCommand.SqliteStoreIdentity(sqlite),null,
                sourceType.ToString(),"Neo4jOASIS",ReplicaBaselineRepairCommand.NeoStoreIdentity(neoUri,"neo4j"));
            await Assert.ThrowsAsync<InvalidOperationException>(()=>ReplicaBaselineRepairCommand.ExecuteAsync(dna,reviewed with{TargetStoreIdentity="neo4j-store:wrong"},journal));
            await Assert.ThrowsAsync<InvalidOperationException>(()=>ReplicaBaselineRepairCommand.ExecuteAsync(dna,reviewed with{SourcePayloadDigest=new string('0',64)},journal));
            Assert.False(IoFile.Exists(journal));
            Assert.Equal(HolonDeliveryState.ReconciliationRequired,(await outbox.InspectAsync(held.Sequence)).Result.State);
            var result=await ReplicaBaselineRepairCommand.ExecuteAsync(dna,reviewed,journal);Assert.False(result.IsError,result.Message+result.Exception);Assert.True(result.Result);
            var bytes=await IoFile.ReadAllBytesAsync(journal);
            Assert.True((await ReplicaBaselineRepairCommand.ExecuteAsync(dna,reviewed,journal)).Result);Assert.Equal(bytes,await IoFile.ReadAllBytesAsync(journal));
            var completed=(await outbox.InspectAsync(held.Sequence)).Result;
            Assert.Equal(HolonDeliveryState.Completed,completed.State);Assert.Equal(held.ErrorCode,completed.ErrorCode);Assert.Equal(held.Message,completed.Message);
            var baseline=(await target.LoadHolonAsync(id,loadChildren:false)).Result;Assert.Equal(4,baseline.Version);Assert.Equal(actor,baseline.CreatedByAvatarId);
            current.Name="ordinary post-baseline Neo update";await Save(5);
            Assert.Equal(1,(await dispatcher.DispatchOnceAsync(targetId)).Result.CompletedCount);
            var final=(await target.LoadHolonAsync(id,loadChildren:false)).Result;Assert.Equal(5,final.Version);Assert.Equal(actor,final.CreatedByAvatarId);Assert.Equal(current.Name,final.Name);
            complete=true;
        }
        finally
        {
            try {try {await source.DeActivateProviderAsync();}finally{(source as IDisposable)?.Dispose();}}
            finally {await target.DeActivateProviderAsync();}
            if(complete)
            {
                if(mongoSource)
                {
                    var marker=await client.GetDatabase(database).GetCollection<BsonDocument>("CosmicOwnedFixture").Find(new BsonDocument("_id","reviewed-neo-target-v1")).SingleAsync();
                    Assert.Equal(owner.ToString("D"),marker["owner"].AsString);await client.DropDatabaseAsync(database);
                    using var names=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(database,await names.ToListAsync());
                }
                await(await inspect.RunAsync(ownedMatch+"DETACH DELETE n",new{id=id.ToString("D"),operations=operations.Append(initialization.ToString("D")).ToArray()})).ConsumeAsync();
                var remaining=await(await inspect.RunAsync(ownedMatch+"RETURN count(n) AS count",new{id=id.ToString("D"),operations=operations.Append(initialization.ToString("D")).ToArray()})).SingleAsync();
                Assert.Equal(0L,remaining["count"].As<long>());
                foreach(var file in new[]{sourceFile,sourceFile+"-wal",sourceFile+"-shm",journal,journal+".tmp",journal+".lock"})if(IoFile.Exists(file))IoFile.Delete(file);
                Record(true);
            }
        }
    }
}
