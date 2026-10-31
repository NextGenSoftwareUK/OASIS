using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using MongoProvider=NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider=NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;
using NeoProvider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
using IoFile=System.IO.File;

public sealed partial class ReplicaBaselineRepairCommandTests
{
    [NativeFact] public Task LostCommitAckMongoAuthority()=>LostCommittedAck(ProviderType.MongoDBOASIS);
    [NativeFact] public Task LostCommitAckSqliteAuthority()=>LostCommittedAck(ProviderType.SQLLiteDBOASIS);
    [NativeFact] public Task LostCommitAckNeoAuthority()=>LostCommittedAck(ProviderType.Neo4jOASIS);

    public class NativeAckLossProxy : DispatchProxy,IConditionalHolonWriteRecoveryStorageProvider,IAtomicHolonReplicationStorageProvider
    {
        public IOASISStorageProvider Native=null!;
        public string IntentPath=null!;
        public bool LoseApplicationAck = true;
        public int NativeWrites;
        public int Attempts;
        public int LegacyAttempts;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            if(method!.Name.StartsWith("SaveHolon",StringComparison.Ordinal)||method.Name.StartsWith("DeleteHolon",StringComparison.Ordinal))
            {LegacyAttempts++;throw new InvalidOperationException("Legacy mutation is forbidden in this conditional outcome test.");}
            try{return method.Invoke(Native,args);}
            catch(TargetInvocationException error)when(error.InnerException!=null){ExceptionDispatchInfo.Capture(error.InnerException).Throw();throw;}
        }
        public OASISResult<IEnumerable<IHolon>> ExecuteConditionalHolonWrite(ConditionalHolonWriteRequest request)=>Lose(request,request.Write.ReplicationEnrollment).GetAwaiter().GetResult();
        public Task<OASISResult<IEnumerable<IHolon>>> ExecuteConditionalHolonWriteAsync(ConditionalHolonWriteRequest request,CancellationToken token=default)=>Lose(request,request.Write.ReplicationEnrollment,token);
        public OASISResult<IEnumerable<IHolon>> ExecuteReplicatingHolonWrite(ConditionalHolonWriteRequest request,HolonReplicationEnrollment enrollment)=>Lose(request,enrollment).GetAwaiter().GetResult();
        public Task<OASISResult<IEnumerable<IHolon>>> ExecuteReplicatingHolonWriteAsync(ConditionalHolonWriteRequest request,HolonReplicationEnrollment enrollment,CancellationToken token=default)=>Lose(request,enrollment,token);
        public Task<OASISResult<ConditionalHolonWriteOutcome>> ReadConditionalHolonWriteOutcomeAsync(ConditionalHolonWriteRequest original,CancellationToken token=default)
            =>((IConditionalHolonWriteRecoveryStorageProvider)Native).ReadConditionalHolonWriteOutcomeAsync(original,token);
        private async Task<OASISResult<IEnumerable<IHolon>>> Lose(ConditionalHolonWriteRequest request,HolonReplicationEnrollment enrollment,CancellationToken token=default)
        {
            Attempts++;Assert.Equal(1,Attempts);Assert.Equal(0,NativeWrites);Assert.NotNull(enrollment);
            using(var file=new FileStream(IntentPath,FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {
                JsonSerializer.Serialize(file,new {request.Write.OperationId,request.Write.ActorId,request.Write.Authority,
                    request.Write.Conditions,request.Operation,request.SaveChildren,request.Recursive,request.MaxChildDepth,
                    enrollment.SourceId,enrollment.TargetIds,
                    roots=request.Roots.Select(row=>StoredRecordJson.SerializeInput(row)).ToArray(),
                    relationships=request.SubmittedRelationships.ToDictionary(pair=>pair.Key.ToString("D"),pair=>HolonChildRoleManifestJson.Serialize(pair.Value))});file.Flush(true);
            }
            var committed=await ((IAtomicHolonReplicationStorageProvider)Native).ExecuteReplicatingHolonWriteAsync(request,enrollment,token);
            Assert.False(committed.IsError,committed.Message+committed.Exception);Assert.Single(committed.Result);NativeWrites++;
            if (!LoseApplicationAck) return committed;
            // The native transaction really completed; only the application ACK is lost.
            return new(){IsError=true,IsSaved=false,ErrorCode="COSMIC_WRITE_RECONCILIATION_REQUIRED",
                Message="Controlled acknowledgement loss after native commit",Exception=new LostAck()};
        }
        private sealed class LostAck:IOException,INativeMutationReconciliationRequired
        {public string OriginalTransactionId=>string.Empty;}
    }

    private static ConditionalHolonWriteRequest ReadAckLossIntent(string path)
    {
        using var json=JsonDocument.Parse(IoFile.ReadAllBytes(path));var r=json.RootElement;
        var enrollment=new HolonReplicationEnrollment(r.GetProperty("SourceId").GetString()!,r.GetProperty("TargetIds").EnumerateArray().Select(row=>row.GetString()!));
        var conditions=r.GetProperty("Conditions").EnumerateArray().Select(row=>new HolonWriteCondition(row.GetProperty("HolonId").GetGuid(),
            row.GetProperty("ExpectedRevisionId").ValueKind==JsonValueKind.Null?null:row.GetProperty("ExpectedRevisionId").GetGuid())).ToArray();
        return new(new ConditionalHolonWrite(r.GetProperty("OperationId").GetGuid(),r.GetProperty("ActorId").GetGuid(),(ProviderType)r.GetProperty("Authority").GetInt32(),conditions,enrollment),
            (ConditionalHolonOperation)r.GetProperty("Operation").GetInt32(),r.GetProperty("roots").EnumerateArray().Select(row=>(IHolon)StoredRecordJson.DeserializeStoredHolon(row.GetString()!)),
            r.GetProperty("SaveChildren").GetBoolean(),r.GetProperty("Recursive").GetBoolean(),r.GetProperty("MaxChildDepth").GetInt32(),
            r.GetProperty("relationships").EnumerateObject().ToDictionary(pair=>Guid.Parse(pair.Name),pair=>HolonChildRoleManifestJson.Deserialize(pair.Value.GetString()!)));
    }

    private async Task LostCommittedAck(ProviderType authority, bool transport = false)
    {
        const string mongoUri="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string neoUri="bolt://127.0.0.1:17687";
        var root=Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../.."));
        var state=Path.Combine(root,(transport ? "LocalState/COSMIC/LostTransportAck" : "LocalState/COSMIC/LostCommittedAck"),authority.ToString());Directory.CreateDirectory(state);
        using var lease=new FileStream(Path.Combine(state,"fixture.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var manifest=Path.Combine(state,"fixture.json");var sqliteFile=Path.Combine(state,"owned.db");var originalPath=Path.Combine(state,"original-request.json");
        foreach(var file in new[]{sqliteFile,sqliteFile+"-wal",sqliteFile+"-shm",originalPath})Assert.False(IoFile.Exists(file),"Preserve existing outcome; never replace an intent.");
        if(IoFile.Exists(manifest)){using var previous=JsonDocument.Parse(await IoFile.ReadAllTextAsync(manifest));Assert.True(previous.RootElement.GetProperty("cleaned").GetBoolean());}
        var database=(transport ? "cosmic_lost_transport_ack_" : "cosmic_lost_ack_")+authority;var client=new MongoClient(mongoUri);
        using(var names=await client.ListDatabaseNamesAsync())Assert.DoesNotContain(database,await names.ToListAsync());
        var owner=Guid.NewGuid();var actor=Guid.NewGuid();var id=Guid.NewGuid();var operation=Guid.NewGuid();var completed=false;
        void Record(bool cleaned)
        {
            using(var file=new FileStream(manifest+".tmp",FileMode.CreateNew,FileAccess.Write,FileShare.None))
            {JsonSerializer.Serialize(file,new{owner,actor,id,operation,authority,database,sqliteFile,cleaned});file.Flush(true);}
            IoFile.Move(manifest+".tmp",manifest,true);
        }
        Record(false);
        await client.GetDatabase(database).GetCollection<BsonDocument>("CosmicOwnedFixture").InsertOneAsync(new BsonDocument{{"_id","lost-ack-v1"},{"owner",owner.ToString("D")}});
        using var driver=GraphDatabase.Driver(neoUri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var inspect=driver.AsyncSession();
        var providers=new Dictionary<ProviderType,IOASISStorageProvider>{
            [ProviderType.MongoDBOASIS]=new MongoProvider(mongoUri,database),[ProviderType.SQLLiteDBOASIS]=new SqliteProvider($"Data Source={sqliteFile};Pooling=False"),
            [ProviderType.Neo4jOASIS]=new NeoProvider(neoUri,"neo4j","oasis-local-test-only")};
        var native=providers[authority];var targets=providers.Where(pair=>pair.Key!=authority).ToArray();
        try
        {
            foreach(var provider in providers.Values)Assert.False((await provider.ActivateProviderAsync()).IsError);
            foreach(var provider in providers.Values)Assert.Null((await provider.LoadHolonAsync(id,loadChildren:false)).Result);
            var wrapper=DispatchProxy.Create<IOASISStorageProvider,NativeAckLossProxy>();var fault=(NativeAckLossProxy)(object)wrapper;
            fault.Native=native;fault.IntentPath=originalPath;fault.LoseApplicationAck=!transport;
            var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {HyperDriveMode=HyperDriveModes.V2,StorageProviders=new(){LogSwitchingProviders=false}}};
            dna.OASIS.SubscriptionConfig.PlanType="Gold";
            ProviderManager Registry(bool wrapped)
            {
                var registry=new ProviderManager(null,dna);
                foreach(var pair in providers)registry.RegisterProvider(wrapped&&pair.Key==authority?wrapper:pair.Value);
                Assert.False(registry.PublishRoutingOverrides(new ProviderRoutingOverrides(true,true,true,providers.Keys,providers.Keys,providers.Keys)).IsError);
                return registry;
            }
            var caller=new Holon{Id=id,Name="owned committed ACK loss"};
            var manager=new HolonManager(null,dna,Registry(true));
            if (transport)
                await LoseCommittedHttpAcknowledgement(manager, caller, actor, operation, authority);
            else
            {
                var failed=await manager.SaveOwnedHolonAsync(caller,actor,new CosmicWriteOptions(operation),saveChildren:false,providerType:authority);
                Assert.True(failed.IsError);Assert.False(failed.IsSaved);
            }
            Assert.Equal(1,fault.NativeWrites);Assert.Equal(1,fault.Attempts);Assert.Equal(0,fault.LegacyAttempts);
            Assert.Equal(0,caller.Version);Assert.Equal(Guid.Empty,caller.VersionId);Assert.True(HolonChangeTracking.HasChanges(caller));
            foreach(var pair in targets)Assert.Null((await pair.Value.LoadHolonAsync(id,loadChildren:false)).Result);
            var stored=(await native.LoadHolonAsync(id,loadChildren:false)).Result;Assert.Equal(1,stored.Version);Assert.Equal(actor,stored.CreatedByAvatarId);
            var outbox=((IHolonReplicationOutboxSource)native).GetHolonReplicationOutbox().Result;
            var pending=new List<HolonDeliveryInspection>();
            foreach(var pair in targets)
            {
                var head=(await outbox.PeekNextAsync(HolonReplicationRoutes.TargetId(pair.Key))).Result;
                Assert.Equal(operation,head.OperationId);Assert.Equal(HolonDeliveryState.Pending,head.State);pending.Add(head);
            }
            // A fresh manager reads the durable original intent; this is status-only,
            // not a retry, caller write-back or an observation of the current row.
            var restored=ReadAckLossIntent(originalPath);var reader=new HolonManager(null,dna,Registry(false));
            var outcome=await reader.ReadConditionalHolonWriteOutcomeAsync(restored,actor);
            Assert.False(outcome.IsError,outcome.Message+outcome.Exception);Assert.Equal(ConditionalHolonReceiptState.Committed,outcome.Result.ReceiptState);
            Assert.Equal(operation,outcome.Result.OperationId);Assert.Equal(authority,outcome.Result.Authority);Assert.Equal(actor,outcome.Result.ActorId);
            Assert.False(string.IsNullOrWhiteSpace(outcome.Result.CommittedDeltaSummaryJson));
            Assert.Equal(0,caller.Version);Assert.Equal(Guid.Empty,caller.VersionId);Assert.Equal(1,fault.NativeWrites);Assert.Equal(1,fault.Attempts);Assert.Equal(0,fault.LegacyAttempts);
            foreach(var head in pending)
            {
                var after=(await outbox.InspectAsync(head.Sequence)).Result;
                Assert.Equal(head.State,after.State);Assert.Equal(head.AttemptCount,after.AttemptCount);
            }
            foreach(var pair in targets)Assert.Null((await pair.Value.LoadHolonAsync(id,loadChildren:false)).Result);
            var dispatcher=new HolonOutboxDispatcher(outbox,targets.Select(pair=>new CommittedHolonReplicaTarget(HolonReplicationRoutes.TargetId(pair.Key),
                (ICommittedHolonReplicaStorageProvider)pair.Value,pair.Key,HolonReplicationRoutes.SourceId(authority),authority)),"owned-lost-ack-delivery");
            foreach(var pair in targets)
            {
                Assert.Equal(1,(await dispatcher.DispatchOnceAsync(HolonReplicationRoutes.TargetId(pair.Key))).Result.CompletedCount);
                var copy=(await pair.Value.LoadHolonAsync(id,loadChildren:false)).Result;
                Assert.Equal(1,copy.Version);Assert.Equal(stored.VersionId,copy.VersionId);Assert.Equal(caller.Name,copy.Name);Assert.Equal(actor,copy.CreatedByAvatarId);
                Assert.Null((await outbox.PeekNextAsync(HolonReplicationRoutes.TargetId(pair.Key))).Result);
            }
            Assert.Equal(stored.VersionId,(await native.LoadHolonAsync(id,loadChildren:false)).Result.VersionId);Assert.Equal(1,fault.NativeWrites);Assert.Equal(1,fault.Attempts);Assert.Equal(0,fault.LegacyAttempts);completed=true;
        }
        finally
        {
            foreach(var provider in providers.Values){try{await provider.DeActivateProviderAsync();}finally{(provider as IDisposable)?.Dispose();}}
            if(completed)
            {
                var marker=await client.GetDatabase(database).GetCollection<BsonDocument>("CosmicOwnedFixture").Find(new BsonDocument("_id","lost-ack-v1")).SingleAsync();
                Assert.Equal(owner.ToString("D"),marker["owner"].AsString);await client.DropDatabaseAsync(database);
                using(var names=await client.ListDatabaseNamesAsync())Assert.DoesNotContain(database,await names.ToListAsync());
                const string owned="MATCH (n) WHERE (n:Holon AND n.Id=$id) OR (n:CosmicReplicaHead AND n.HolonId=$id) OR (n:OasisRecordVersion AND n.Kind='Holon' AND n.RecordId=$id) OR ((n:CosmicWriteReceipt OR n:CosmicHolonOutbox) AND n.OperationId=$operation) ";
                await(await inspect.RunAsync(owned+"DETACH DELETE n",new{id=id.ToString("D"),operation=operation.ToString("D")})).ConsumeAsync();
                var remaining=await(await inspect.RunAsync(owned+"RETURN count(n) AS count",new{id=id.ToString("D"),operation=operation.ToString("D")})).SingleAsync();Assert.Equal(0L,remaining["count"].As<long>());
                foreach(var file in new[]{sqliteFile,sqliteFile+"-wal",sqliteFile+"-shm"})if(IoFile.Exists(file))IoFile.Delete(file);
                Record(true);
            }
        }
    }
}
