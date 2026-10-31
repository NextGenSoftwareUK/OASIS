using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Common;
using MongoProvider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider=NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;
using NeoProvider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
using IoFile=System.IO.File;

public sealed partial class ReplicaBaselineRepairCommandTests
{
    [NativeFact]
    public Task ManagerContinuationMongoToSqlite()=>RemainingManagerContinuation(ProviderType.MongoDBOASIS,ProviderType.SQLLiteDBOASIS);
    [NativeFact]
    public Task ManagerContinuationSqliteToMongo()=>RemainingManagerContinuation(ProviderType.SQLLiteDBOASIS,ProviderType.MongoDBOASIS);
    [NativeFact]
    public Task ManagerContinuationNeoToSqlite()=>RemainingManagerContinuation(ProviderType.Neo4jOASIS,ProviderType.SQLLiteDBOASIS);
    [NativeFact]
    public Task ManagerContinuationNeoToMongo()=>RemainingManagerContinuation(ProviderType.Neo4jOASIS,ProviderType.MongoDBOASIS);

    [NativeFact]
    public Task ManagerInFlightMongoToSqlite()=>RemainingManagerContinuation(ProviderType.MongoDBOASIS,ProviderType.SQLLiteDBOASIS,true);
    [NativeFact]
    public Task ManagerInFlightSqliteToMongo()=>RemainingManagerContinuation(ProviderType.SQLLiteDBOASIS,ProviderType.MongoDBOASIS,true);
    [NativeFact]
    public Task ManagerInFlightNeoToSqlite()=>RemainingManagerContinuation(ProviderType.Neo4jOASIS,ProviderType.SQLLiteDBOASIS,true);
    [NativeFact]
    public Task ManagerInFlightNeoToMongo()=>RemainingManagerContinuation(ProviderType.Neo4jOASIS,ProviderType.MongoDBOASIS,true);
    [NativeFact]
    public Task ManagerInFlightMongoToNeo()=>RemainingManagerContinuation(ProviderType.MongoDBOASIS,ProviderType.Neo4jOASIS,true);
    [NativeFact]
    public Task ManagerInFlightSqliteToNeo()=>RemainingManagerContinuation(ProviderType.SQLLiteDBOASIS,ProviderType.Neo4jOASIS,true);

    private async Task RemainingManagerContinuation(ProviderType sourceType,ProviderType targetType,bool duringInitialization=false)
    {
        const string mongoUri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string neoUri="bolt://127.0.0.1:17687";
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../.."));
        var state=Path.Combine(root,"LocalState/COSMIC/ManagerContinuation",sourceType+"-"+targetType+(duringInitialization?"-InFlight":""));Directory.CreateDirectory(state);
        using var lease=new FileStream(Path.Combine(state,"fixture.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var manifest=Path.Combine(state,"fixture.json");var journal=Path.Combine(state,"application.json");var sqliteFile=Path.Combine(state,"owned.db");
        foreach(var file in new[]{sqliteFile,sqliteFile+"-wal",sqliteFile+"-shm",journal,journal+".tmp",journal+".lock"})
            Assert.False(IoFile.Exists(file),"Preserve original unresolved fixture.");
        using var driver=GraphDatabase.Driver(neoUri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var inspect=driver.AsyncSession();
        const string ownedMatch="MATCH (n) WHERE (n:Holon AND n.Id=$id) OR (n:CosmicReplicaHead AND n.HolonId=$id) "
            +"OR (n:OasisRecordVersion AND n.Kind='Holon' AND n.RecordId=$id) "
            +"OR ((n:CosmicWriteReceipt OR n:CosmicHolonOutbox) AND n.OperationId IN $operations) ";
        if(IoFile.Exists(manifest))
        {
            using var prior=JsonDocument.Parse(await IoFile.ReadAllTextAsync(manifest));
            Assert.True(prior.RootElement.GetProperty("cleaned").GetBoolean(),"Preserve failed native outcomes.");
            var priorId=prior.RootElement.GetProperty("id").GetString();
            var priorOps=prior.RootElement.GetProperty("operations").EnumerateArray().Select(row=>row.GetString()!).ToArray();
            var remaining=await(await inspect.RunAsync(ownedMatch+"RETURN count(n) AS count",new{id=priorId,operations=priorOps})).SingleAsync();
            Assert.Equal(0L,remaining["count"].As<long>());
        }
        var usesMongo=sourceType==ProviderType.MongoDBOASIS || targetType==ProviderType.MongoDBOASIS;
        var usesNeo=sourceType==ProviderType.Neo4jOASIS || targetType==ProviderType.Neo4jOASIS;
        var database=(duringInitialization?"cosmic_flight_":"cosmic_manager_")+sourceType+"_"+targetType;
        var client=new MongoClient(mongoUri);
        if(usesMongo){using var names=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(database,await names.ToListAsync());}
        var owner=Guid.NewGuid();var actor=Guid.NewGuid();var id=Guid.NewGuid();var initialization=Guid.NewGuid();var operations=new List<string>{initialization.ToString("D")};var complete=false;
        void Record(bool cleaned)
        {
            using(var file=new FileStream(manifest+".tmp",FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {JsonSerializer.Serialize(file,new{owner,actor,id,initialization,operations,sourceType,targetType,database,sqliteFile,cleaned});file.Flush(true);}
            IoFile.Move(manifest+".tmp",manifest,true);
        }
        Record(false);
        if(usesMongo)await client.GetDatabase(database).GetCollection<BsonDocument>("CosmicOwnedFixture").InsertOneAsync(
            new BsonDocument{{"_id","manager-continuation-v1"},{"owner",owner.ToString("D")}});
        var connection=$"Data Source={sqliteFile};Pooling=False";
        IOASISStorageProvider Provider(ProviderType type)=>type switch {
            ProviderType.MongoDBOASIS=>new MongoProvider(mongoUri,database),
            ProviderType.SQLLiteDBOASIS=>new SqliteProvider(connection),
            ProviderType.Neo4jOASIS=>new NeoProvider(neoUri,"neo4j","oasis-local-test-only"),
            _=>throw new InvalidOperationException("Only named native test providers.")};
        var source=Provider(sourceType);var target=Provider(targetType);
        var sourceId=HolonReplicationRoutes.SourceId(sourceType);var targetId=HolonReplicationRoutes.TargetId(targetType);
        var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings {
            LogSwitchingProviders=false,MongoDBOASIS=new(){ConnectionString=mongoUri,DBName=database},SQLLiteDBOASIS=new(){ConnectionString=connection},
            Neo4jOASIS=new(){ConnectionString=neoUri,Username="neo4j",Password="oasis-local-test-only"}}}};
        dna.OASIS.SubscriptionConfig.PlanType="Gold";
        var registry=new ProviderManager(null,dna);registry.RegisterProvider(source);registry.RegisterProvider(target);
        // Node list contains only the authority. The avatar must supply the actual
        // target; the request controls when enrollment starts, without node mutation.
        Assert.False(registry.PublishRoutingOverrides(new ProviderRoutingOverrides(false,false,false,replicationProviders:new[]{sourceType})).IsError);
        var manager=new HolonManager(null,dna,registry);
        try
        {
            Assert.False((await source.ActivateProviderAsync()).IsError);Assert.False((await target.ActivateProviderAsync()).IsError);
            var outbox=((IHolonReplicationOutboxSource)source).GetHolonReplicationOutbox().Result;
            Assert.Null((await outbox.PeekNextAsync(targetId)).Result);
            IHolon current=new Holon{Id=id};
            async Task Save(int version)
            {
                var operation=Guid.NewGuid();operations.Add(operation.ToString("D"));Record(false);
                current.Name="owned manager continuation version "+version;
                var oldAvatar=OASISRequestContext.AvatarRoutingOverrides;var oldRequest=OASISRequestContext.RequestRoutingOverrides;
                try
                {
                    OASISRequestContext.AvatarRoutingOverrides=new ProviderRoutingOverrides(autoReplicationEnabled:true,replicationProviders:new[]{targetType});
                    OASISRequestContext.RequestRoutingOverrides=new ProviderRoutingOverrides(autoReplicationEnabled:version>=4,autoFailoverEnabled:false,autoLoadBalancingEnabled:false);
                    var saved=await manager.SaveOwnedHolonAsync(current,actor,new CosmicWriteOptions(operation),saveChildren:false,providerType:sourceType);
                    Assert.False(saved.IsError,saved.Message+saved.Exception);Assert.True(saved.IsSaved);Assert.Same(current,saved.Result);Assert.Equal(version,current.Version);
                    if(version<4)Assert.Null((await outbox.PeekNextAsync(targetId)).Result);
                }
                finally {OASISRequestContext.AvatarRoutingOverrides=oldAvatar;OASISRequestContext.RequestRoutingOverrides=oldRequest;}
            }
            for(var version=1;version<=4;version++)await Save(version);
            var dispatcher=new HolonOutboxDispatcher(outbox,new[]{new CommittedHolonReplicaTarget(targetId,(ICommittedHolonReplicaStorageProvider)target,targetType,sourceId,sourceType)},"owned-manager-continuation");
            Assert.Equal(1,(await dispatcher.DispatchOnceAsync(targetId)).Result.ReconciliationCount);
            var held=(await outbox.PeekNextAsync(targetId)).Result;
            var reader=(IHolonReplicationObligationReader)outbox;
            string Identity(ProviderType type)=>type switch {
                ProviderType.MongoDBOASIS=>ReplicaBaselineRepairCommand.StoreIdentity(mongoUri,database),
                ProviderType.SQLLiteDBOASIS=>ReplicaBaselineRepairCommand.SqliteStoreIdentity(connection),
                ProviderType.Neo4jOASIS=>ReplicaBaselineRepairCommand.NeoStoreIdentity(neoUri,"neo4j"),_=>throw new InvalidOperationException()};
            var reviewed=new ReplicaBaselineRepairCommand.Request(actor,initialization,Guid.NewGuid(),held.Sequence,held.OperationId,actor,id,
                (await reader.ReadObligationAsync(held.Sequence)).Result.PayloadDigest,Identity(sourceType),targetType==ProviderType.SQLLiteDBOASIS?sqliteFile:null,
                sourceType.ToString(),targetType.ToString(),targetType==ProviderType.SQLLiteDBOASIS?null:Identity(targetType));
            if(duringInitialization)
            {
                // Retain the reviewed catalogue/operation binding before application.
                using(var requestFile=new FileStream(Path.Combine(state,"reviewed-request.json"),FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {JsonSerializer.Serialize(requestFile,reviewed);requestFile.Flush(true);}
                var preparation=ReplicaBaselineRepairCommand.Preparation(reviewed,sourceType,reader,
                    targetType==ProviderType.SQLLiteDBOASIS?Path.GetFullPath(sqliteFile):Identity(targetType));
                Func<HolonReplicaInitializationIntent,HolonReplicaInitializationOutcome,CancellationToken,Task<OASISResult<bool>>> completeSource=outbox switch {
                    NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoHolonReplicationOutbox mongo=>mongo.CompleteInitializedBaselineAsync,
                    NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SqliteHolonReplicationOutbox sqlite=>sqlite.CompleteInitializedBaselineAsync,
                    NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.NeoHolonReplicationOutbox neo=>neo.CompleteInitializedBaselineAsync,
                    _=>throw new InvalidOperationException("Native source completion required.")};
                var paused=new PausedNativeInitialization((ICommittedHolonReplicaInitializationRecoveryStorageProvider)target);
                var application=new HolonReplicaInitializationApplication(preparation,paused,completeSource);
                using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(60));
                var pending=application.ExecuteAsync(actor,held.Sequence,initialization,journal,deadline.Token);
                try
                {
                    await paused.Entered.Task.WaitAsync(deadline.Token);
                    // Baseline intent and Submitted journal already exist. Commit a
                    // real newer source version before native target installation.
                    await Save(5);
                    Assert.Equal(held.Sequence,(await outbox.PeekNextAsync(targetId)).Result.Sequence);
                    Assert.Null((await target.LoadHolonAsync(id,loadChildren:false)).Result);
                }
                finally
                {
                    paused.Release.TrySetResult();
                    await pending; // Join owned work before any provider teardown.
                }
                var applied=pending.Result;Assert.False(applied.IsError,applied.Message+applied.Exception);Assert.True(applied.Result);
                Assert.Equal(1,paused.Initializations);
            }
            else
            {
                var applied=await ReplicaBaselineRepairCommand.ExecuteAsync(dna,reviewed,journal);Assert.False(applied.IsError,applied.Message+applied.Exception);Assert.True(applied.Result);
            }
            var journalBytes=await IoFile.ReadAllBytesAsync(journal);
            var replay=await ReplicaBaselineRepairCommand.ExecuteAsync(dna,reviewed,journal);
            Assert.True(replay.Result,replay.ErrorCode+": "+replay.Message);Assert.Equal(journalBytes,await IoFile.ReadAllBytesAsync(journal));
            Assert.Equal(HolonDeliveryState.Completed,(await outbox.InspectAsync(held.Sequence)).Result.State);
            var baseline=(await target.LoadHolonAsync(id,loadChildren:false)).Result;Assert.Equal(4,baseline.Version);Assert.Equal(actor,baseline.CreatedByAvatarId);
            if(!duringInitialization)await Save(5);
            Assert.Equal(1,(await dispatcher.DispatchOnceAsync(targetId)).Result.CompletedCount);
            var final=(await target.LoadHolonAsync(id,loadChildren:false)).Result;Assert.Equal(5,final.Version);Assert.Equal(current.Name,final.Name);Assert.Equal(actor,final.CreatedByAvatarId);
            Assert.Null((await outbox.PeekNextAsync(targetId)).Result);
            if(duringInitialization)
            {
                await Save(6);
                Assert.Equal(1,(await dispatcher.DispatchOnceAsync(targetId)).Result.CompletedCount);
                var latest=(await target.LoadHolonAsync(id,loadChildren:false)).Result;
                Assert.Equal(6,latest.Version);Assert.Equal(current.Name,latest.Name);Assert.Equal(actor,latest.CreatedByAvatarId);
                Assert.Null((await outbox.PeekNextAsync(targetId)).Result);
            }
            complete=true;
        }
        finally
        {
            try {try{await source.DeActivateProviderAsync();}finally{(source as IDisposable)?.Dispose();}}
            finally {try{await target.DeActivateProviderAsync();}finally{(target as IDisposable)?.Dispose();}}
            if(complete)
            {
                if(usesMongo)
                {
                    var marker=await client.GetDatabase(database).GetCollection<BsonDocument>("CosmicOwnedFixture").Find(new BsonDocument("_id","manager-continuation-v1")).SingleAsync();
                    Assert.Equal(owner.ToString("D"),marker["owner"].AsString);await client.DropDatabaseAsync(database);
                    using var names=await client.ListDatabaseNamesAsync();Assert.DoesNotContain(database,await names.ToListAsync());
                }
                if(usesNeo)
                {
                    await(await inspect.RunAsync(ownedMatch+"DETACH DELETE n",new{id=id.ToString("D"),operations})).ConsumeAsync();
                    var remaining=await(await inspect.RunAsync(ownedMatch+"RETURN count(n) AS count",new{id=id.ToString("D"),operations})).SingleAsync();Assert.Equal(0L,remaining["count"].As<long>());
                }
                foreach(var file in new[]{sqliteFile,sqliteFile+"-wal",sqliteFile+"-shm",journal,journal+".tmp",journal+".lock"})if(IoFile.Exists(file))IoFile.Delete(file);
                Record(true);
            }
        }
    }

    /// <summary>Only schedules the interleaving; the real native provider owns
    /// initialization and receipts. No synthetic success or alternate data path.</summary>
    private sealed class PausedNativeInitialization(ICommittedHolonReplicaInitializationRecoveryStorageProvider target)
        : ICommittedHolonReplicaInitializationRecoveryStorageProvider
    {
        public TaskCompletionSource Entered {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release {get;}=new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int Initializations {get;private set;}
        public OASISResult<IEnumerable<IHolon>> InitializeCommittedHolonReplica(CommittedHolonReplicaRequest request)
            =>throw new NotSupportedException("This controlled application gate requires its async native owner.");
        public async Task<OASISResult<IEnumerable<IHolon>>> InitializeCommittedHolonReplicaAsync(CommittedHolonReplicaRequest request,CancellationToken token=default)
        {
            Initializations++;Entered.TrySetResult();
            await Release.Task.WaitAsync(token);
            return await target.InitializeCommittedHolonReplicaAsync(request,token);
        }
        public Task<OASISResult<HolonReplicaInitializationOutcome>> ReadReplicaInitializationOutcomeAsync(CommittedHolonReplicaRequest request,CancellationToken token=default)
            =>target.ReadReplicaInitializationOutcomeAsync(request,token);
    }

}
