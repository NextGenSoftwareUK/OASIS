using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeCosmicOutboxDeliveryFencesOrdersAndRetainsEvidence(bool synchronous)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var inspection=driver.AsyncSession();
        var provider=new Provider(uri,"neo4j","oasis-local-test-only");
        IHolonReplicationReadinessOutbox store=null!;var other=new NeoHolonReplicationOutbox(driver);
        var suffix=Guid.NewGuid().ToString("N");var a="owned-delivery-a-"+suffix;var b="owned-delivery-b-"+suffix;
        var actor=Guid.NewGuid();var source="owned-delivery-source-"+suffix;var operations=new List<string>();
        var root=new Holon{Id=Guid.NewGuid(),Name="owned delivery initial"};var complete=false;
        async Task<DateTime> Now()=>new((await(await inspection.RunAsync("RETURN datetime.realtime().epochMillis * 10000 + 621355968000000000 AS ticks")).SingleAsync())["ticks"].As<long>(),DateTimeKind.Utc);
        async Task WaitUntil(DateTime until)
        {var watch=System.Diagnostics.Stopwatch.StartNew();while(await Now()<until){Assert.IsTrue(watch.Elapsed<TimeSpan.FromSeconds(10));await Task.Delay(25);}}
        async Task<IHolon> Commit(IHolon value,bool create)
        {
            var op=Guid.NewGuid();operations.Add(op.ToString("D"));
            var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(op,actor,ProviderType.Neo4jOASIS,
                new[]{new HolonWriteCondition(value.Id,create ? null:CosmicConcurrency.GetProviderRevisionId(value,ProviderType.Neo4jOASIS))}),
                ConditionalHolonOperation.Save,new[]{value},saveChildren:false);
            var enrollment=new HolonReplicationEnrollment(source,new[]{a,b});
            var result=synchronous ? provider.ExecuteReplicatingHolonWrite(request,enrollment):await provider.ExecuteReplicatingHolonWriteAsync(request,enrollment);
            Assert.IsFalse(result.IsError,result.Message+result.Exception);return result.Result.Single();
        }
        async Task<HolonDeliveryClaim> Claim(string target,TimeSpan? duration=null)
        {var result=await store.ClaimNextAsync(target,"$owned-worker",duration ?? TimeSpan.FromSeconds(15));Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.IsNotNull(result.Result);return result.Result;}
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var captured=provider.GetHolonReplicationOutbox();Assert.IsFalse(captured.IsError,captured.Message+captured.Exception);store=captured.Result;
            var first=await Commit(root,true);var update=OasisJson.Project<Holon>(first);update.Name="owned delivery second";var secondRecord=await Commit(update,false);
            var peek=await store.PeekNextAsync(a);Assert.IsFalse(peek.IsError,peek.Message+peek.Exception);Assert.AreEqual(HolonDeliveryState.Pending,peek.Result.State);Assert.AreEqual(0,peek.Result.AttemptCount);
            var old=await Claim(a,TimeSpan.FromSeconds(2));
            Assert.IsNull((await other.ClaimNextAsync(a,"other",TimeSpan.FromSeconds(1))).Result);
            var independent=await Claim(b);Assert.AreEqual(old.OperationId,independent.OperationId);
            Assert.IsTrue((await store.FinishWithRecoveryAsync(independent,HolonDeliveryOutcome.Completed,new(false,Array.Empty<string>()))).Result);
            await WaitUntil(old.LeaseUntilUtc.AddMilliseconds(1));
            Assert.AreEqual("COSMIC_OUTBOX_LEASE_LOST",(await store.RenewAsync(old,TimeSpan.FromSeconds(1))).ErrorCode);
            var current=await Claim(a);Assert.AreEqual(old.Sequence,current.Sequence);Assert.AreNotEqual(old.LeaseToken,current.LeaseToken);Assert.AreEqual(2,current.AttemptCount);
            Assert.AreEqual(old.PayloadJson,current.PayloadJson);
            Assert.AreEqual("COSMIC_OUTBOX_LEASE_LOST",(await store.FinishAsync(old,HolonDeliveryOutcome.Completed)).ErrorCode);
            Assert.IsTrue((await store.RenewAsync(current,TimeSpan.FromSeconds(20))).Result);
            var longer=(await(await inspection.RunAsync("MATCH (o:CosmicHolonOutbox {Sequence:$sequence}) RETURN o.LeaseUntilUtcTicks AS ticks",new{sequence=current.Sequence})).SingleAsync())["ticks"].As<long>();
            Assert.IsTrue((await store.RenewAsync(current,TimeSpan.FromMilliseconds(1))).Result);
            var shorter=(await(await inspection.RunAsync("MATCH (o:CosmicHolonOutbox {Sequence:$sequence}) RETURN o.LeaseUntilUtcTicks AS ticks",new{sequence=current.Sequence})).SingleAsync())["ticks"].As<long>();Assert.AreEqual(longer,shorter);
            var retry=(await Now()).AddSeconds(1);
            Assert.IsTrue((await store.FinishWithRecoveryAsync(current,HolonDeliveryOutcome.Retry,new(false,Array.Empty<string>()),"$Retry","$LeaseToken",retry)).Result);
            Assert.IsNull((await store.ClaimNextAsync(a,"other",TimeSpan.FromSeconds(1))).Result);
            var pending=await store.InspectAsync(current.Sequence);Assert.AreEqual(HolonDeliveryState.Pending,pending.Result.State);Assert.AreEqual("$Retry",pending.Result.ErrorCode);
            await WaitUntil(retry.AddMilliseconds(1));current=await Claim(a);Assert.AreEqual(3,current.AttemptCount);Assert.AreEqual(old.PayloadJson,current.PayloadJson);
            Assert.IsTrue((await store.FinishWithRecoveryAsync(current,HolonDeliveryOutcome.Completed,new(false,Array.Empty<string>()))).Result);
            var done=await store.InspectAsync(current.Sequence);Assert.AreEqual(HolonDeliveryState.Completed,done.Result.State);Assert.IsFalse(done.Result.RecoveryEvidence.RequiresNativeReconciliation);
            var second=await Claim(a);Assert.AreNotEqual(current.OperationId,second.OperationId);Assert.IsTrue(second.Sequence>current.Sequence);
            var nativeId="opaque native "+new string('x',5000);
            Assert.IsTrue((await store.FinishWithRecoveryAsync(second,HolonDeliveryOutcome.ReconciliationRequired,new(true,new[]{nativeId}),"NATIVE_UNKNOWN","Reconcile original" )).Result);
            var blockedHead=await new NeoHolonReplicationOutbox(driver).PeekNextAsync(a);Assert.IsFalse(blockedHead.IsError,blockedHead.Message+blockedHead.Exception);
            Assert.AreEqual(second.Sequence,blockedHead.Result.Sequence);Assert.AreEqual(HolonDeliveryState.ReconciliationRequired,blockedHead.Result.State);Assert.AreEqual(1,blockedHead.Result.AttemptCount);
            var blocked=await other.InspectAsync(second.Sequence);Assert.AreEqual(nativeId,blocked.Result.RecoveryEvidence.OriginalTransactionIds.Single());
            Assert.IsNull((await store.ClaimNextAsync(a,"other",TimeSpan.FromSeconds(1))).Result);
            var secondB=await Claim(b);
            await(await inspection.RunAsync("MATCH (o:CosmicHolonOutbox {Sequence:$sequence}) SET o.PayloadDigest='owned corrupt checksum'",new{sequence=secondB.Sequence})).ConsumeAsync();
            // Release the owner without consuming or rewriting the immutable payload.
            Assert.IsTrue((await store.FinishAsync(secondB,HolonDeliveryOutcome.Retry,"TEST_RETRY",nextAttemptUtc:(await Now()).AddMilliseconds(200))).Result);
            await WaitUntil((await Now()).AddMilliseconds(250));
            Assert.AreEqual("COSMIC_OUTBOX_CORRUPT",(await store.PeekNextAsync(b)).ErrorCode);
            var corrupt=await store.ClaimNextAsync(b,"other",TimeSpan.FromSeconds(1));Assert.AreEqual("COSMIC_OUTBOX_CORRUPT",corrupt.ErrorCode);
            Assert.AreEqual(1,(await store.InspectAsync(secondB.Sequence)).Result.AttemptCount);
            var digest=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(secondB.PayloadJson)));
            await(await inspection.RunAsync("MATCH (o:CosmicHolonOutbox {Sequence:$sequence}) SET o.PayloadDigest=$digest",new{sequence=secondB.Sequence,digest})).ConsumeAsync();
            var racing=await Task.WhenAll(store.ClaimNextAsync(b,"racer-a",TimeSpan.FromSeconds(15)),other.ClaimNextAsync(b,"racer-b",TimeSpan.FromSeconds(15)));
            Assert.AreEqual(1,racing.Count(value=>!value.IsError && value.Result!=null));
            var winner=racing.Single(value=>!value.IsError && value.Result!=null).Result;
            Assert.IsTrue((await store.FinishWithRecoveryAsync(winner,HolonDeliveryOutcome.Completed,new(false,Array.Empty<string>()))).Result);
            var third=OasisJson.Project<Holon>(secondRecord);third.Description="owned uncertain claim";await Commit(third,false);
            var commits=0;var cause=new IOException("owned delivery claim acknowledgement lost");
            var cleanup=new Neo4jOrdinaryWriteFaultProxy.Faults("cleanup");
            var faultyDriver=Neo4jOrdinaryWriteFaultProxy.Wrap(Neo4jCommitFaultProxy.Wrap(driver,cause,()=>commits++),cleanup);
            var uncertain=await new NeoHolonReplicationOutbox(faultyDriver).ClaimNextAsync(b,"uncertain",TimeSpan.FromSeconds(2));
            Assert.AreEqual("COSMIC_OUTBOX_RECONCILIATION_REQUIRED",uncertain.ErrorCode);Assert.AreEqual(1,commits);Assert.AreEqual(0,cleanup.Rollbacks);
            Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(uncertain.Exception));
            StringAssert.Contains(uncertain.Exception.ToString(),cause.Message);
            StringAssert.Contains(uncertain.Exception.ToString(),cleanup.TransactionCleanup.Message);
            StringAssert.Contains(uncertain.Exception.ToString(),cleanup.SessionCleanup.Message);
            var uncertainRows=await(await inspection.RunAsync("MATCH (o:CosmicHolonOutbox {OperationId:$operation,TargetId:$target}) RETURN o.Sequence AS sequence,o.AttemptCount AS attempts,o.LeaseUntilUtcTicks AS until",new{operation=operations.Last(),target=b})).SingleAsync();
            Assert.AreEqual(1L,uncertainRows["attempts"].As<long>());
            await WaitUntil(new DateTime(uncertainRows["until"].As<long>(),DateTimeKind.Utc).AddMilliseconds(1));
            var reclaimed=await Claim(b);Assert.AreEqual(2,reclaimed.AttemptCount);
            Assert.IsTrue((await store.FinishWithRecoveryAsync(reclaimed,HolonDeliveryOutcome.Completed,new(false,Array.Empty<string>()))).Result);
            complete=true;
        }
        finally
        {
            if(complete)
            {
                await(await inspection.RunAsync("MATCH (h:Holon {Id:$id}) DETACH DELETE h",new{id=root.Id.ToString()})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (r) WHERE (r:CosmicWriteReceipt OR r:CosmicHolonOutbox) AND r.OperationId IN $operations DETACH DELETE r",new{operations})).ConsumeAsync();
            }
            await provider.DeActivateProviderAsync();
        }
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeCosmicOutboxDeliveryDispatchesNeo4jToSqlite(bool synchronous)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        var database=Path.Combine(work,"outbox-delivery-target.db");Assert.IsFalse(File.Exists(database),"Preserve retained fixtures for inspection.");
        var target=new SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={database};Pooling=False");
        var provider=new Provider(uri,"neo4j","oasis-local-test-only");
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var inspection=driver.AsyncSession();
        var store=new NeoHolonReplicationOutbox(driver);var origin="owned-dispatch-"+Guid.NewGuid().ToString("N");var targetId=origin+"-sqlite";
        var adapter=new CommittedHolonReplicaTarget(targetId,target,ProviderType.SQLLiteDBOASIS,origin,ProviderType.Neo4jOASIS);
        var dispatcher=new HolonOutboxDispatcher(store,new[]{adapter},"owned-neo-dispatcher");
        var actor=Guid.NewGuid();var ids=new[]{Guid.NewGuid(),Guid.NewGuid()};var operations=new List<string>();var complete=false;
        IEnumerable<IHolon> Flatten(IHolon value){yield return value;foreach(var child in value.Children)foreach(var node in Flatten(child))yield return node;}
        async Task<IHolon> Commit(IHolon value,bool create,ConditionalHolonOperation kind=ConditionalHolonOperation.Save)
        {
            var op=Guid.NewGuid();operations.Add(op.ToString("D"));
            var conditions=Flatten(value).Select(h=>new HolonWriteCondition(h.Id,create ? null:CosmicConcurrency.GetProviderRevisionId(h,ProviderType.Neo4jOASIS)));
            var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(op,actor,ProviderType.Neo4jOASIS,conditions),kind,new[]{value});
            var enrollment=new HolonReplicationEnrollment(origin,new[]{targetId});
            var result=synchronous ? provider.ExecuteReplicatingHolonWrite(request,enrollment):await provider.ExecuteReplicatingHolonWriteAsync(request,enrollment);
            Assert.IsFalse(result.IsError,result.Message+result.Exception);return result.Result.Single();
        }
        async Task Deliver()
        {
            var result=await dispatcher.DispatchOnceAsync(targetId,1);Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.AreEqual(1,result.Result.CompletedCount);
            var sequence=(await(await inspection.RunAsync("MATCH (o:CosmicHolonOutbox {OperationId:$op}) RETURN o.Sequence AS sequence",new{op=operations.Last()})).SingleAsync())["sequence"].As<long>();
            var state=await store.InspectAsync(sequence);Assert.AreEqual(HolonDeliveryState.Completed,state.Result.State);Assert.IsFalse(state.Result.RecoveryEvidence.RequiresNativeReconciliation);
        }
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);Assert.IsFalse((await target.ActivateProviderAsync()).IsError);
            var input=new Holon{Id=ids[0],Name="owned dispatch initial",Children=new List<IHolon>{new Holon{Id=ids[1],Name="owned dispatch child"}}};
            var first=await Commit(input,true);await Deliver();
            var loaded=await target.LoadHolonAsync(ids[0]);Assert.IsFalse(loaded.IsError,loaded.Message);Assert.AreEqual(first.VersionId,loaded.Result.VersionId);Assert.AreEqual(first.Children.Single().VersionId,loaded.Result.Children.Single().VersionId);
            var edit=OasisJson.Project<Holon>(first);edit.Children=first.Children.Select(h=>(IHolon)OasisJson.Project<Holon>(h)).ToList();edit.Name="owned dispatch updated";
            var second=await Commit(edit,false);await Deliver();
            Assert.AreEqual(second.VersionId,(await target.LoadHolonAsync(ids[0])).Result.VersionId);
            var soft=await Commit(second,false,ConditionalHolonOperation.SoftDelete);await Deliver();
            var softLoaded=await target.LoadHolonAsync(ids[0]);Assert.AreEqual(soft.DeletedDate,softLoaded.Result.DeletedDate);
            var hard=await Commit(soft,false,ConditionalHolonOperation.HardDelete);await Deliver();
            var absent=await target.LoadHolonAsync(ids[0]);Assert.IsTrue(absent.IsError || absent.Result==null);
            var queue=await(await inspection.RunAsync("MATCH (o:CosmicHolonOutbox {OperationId:$op}) RETURN o.PayloadJson AS payload",new{op=operations.Last()})).SingleAsync();
            var replay=await adapter.ApplyReplicatedMutationAsync(CommittedHolonReplicaTarget.ReadItem(queue["payload"].As<string>()),CancellationToken.None);
            Assert.IsFalse(replay.IsError,replay.Message+replay.Exception);Assert.IsTrue(replay.Result);
            complete=true;
        }
        finally
        {
            if(complete)
            {
                await(await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h",new{ids=ids.Select(id=>id.ToString()).ToArray()})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (r) WHERE (r:CosmicWriteReceipt OR r:CosmicHolonOutbox) AND r.OperationId IN $operations DETACH DELETE r",new{operations})).ConsumeAsync();
            }
            await provider.DeActivateProviderAsync();await target.DeActivateProviderAsync();
            if(complete)foreach(var path in new[]{database,database+"-wal",database+"-shm"})if(File.Exists(path))File.Delete(path);
        }
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeCosmicOutboxDeliveryDispatchesSqliteToNeo4j(bool synchronous)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        var database=Path.Combine(work,"outbox-delivery-source.db");Assert.IsFalse(File.Exists(database),"Preserve retained fixtures for inspection.");
        var provider=new SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={database};Pooling=False");
        var target=new Provider(uri,"neo4j","oasis-local-test-only");
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var inspection=driver.AsyncSession();
        var store=new SQLLiteDBOASIS.SqliteHolonReplicationOutbox($"Data Source={database};Pooling=False");var origin="owned-dispatch-"+Guid.NewGuid().ToString("N");var targetId=origin+"-neo4j";
        var adapter=new CommittedHolonReplicaTarget(targetId,target,ProviderType.Neo4jOASIS,origin,ProviderType.SQLLiteDBOASIS);
        var dispatcher=new HolonOutboxDispatcher(store,new[]{adapter},"owned-sqlite-dispatcher");
        var actor=Guid.NewGuid();var ids=new[]{Guid.NewGuid(),Guid.NewGuid()};var operations=new List<string>();var complete=false;
        IEnumerable<IHolon> Flatten(IHolon value){yield return value;foreach(var child in value.Children)foreach(var node in Flatten(child))yield return node;}
        async Task<IHolon> Commit(IHolon value,bool create,ConditionalHolonOperation kind=ConditionalHolonOperation.Save)
        {
            var op=Guid.NewGuid();operations.Add(op.ToString("D"));
            var conditions=Flatten(value).Select(h=>new HolonWriteCondition(h.Id,create ? null:CosmicConcurrency.GetProviderRevisionId(h,ProviderType.SQLLiteDBOASIS)));
            var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(op,actor,ProviderType.SQLLiteDBOASIS,conditions),kind,new[]{value});
            var enrollment=new HolonReplicationEnrollment(origin,new[]{targetId});
            var result=synchronous ? provider.ExecuteReplicatingHolonWrite(request,enrollment):await provider.ExecuteReplicatingHolonWriteAsync(request,enrollment);
            Assert.IsFalse(result.IsError,result.Message+result.Exception);return result.Result.Single();
        }
        async Task<(long Sequence,string Payload)> ReadQueue(string operation)
        {
            using var connection=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={database};Mode=ReadWrite;Pooling=False");
            await connection.OpenAsync();using var command=connection.CreateCommand();
            command.CommandText="SELECT Sequence,PayloadJson FROM CosmicHolonOutbox WHERE OperationId=$operation";
            command.Parameters.AddWithValue("$operation",operation);using var reader=await command.ExecuteReaderAsync();
            Assert.IsTrue(await reader.ReadAsync());var value=(reader.GetInt64(0),reader.GetString(1));Assert.IsFalse(await reader.ReadAsync());return value;
        }
        async Task Deliver()
        {
            var result=await dispatcher.DispatchOnceAsync(targetId,1);Assert.IsFalse(result.IsError,result.Message+result.Exception);Assert.AreEqual(1,result.Result.CompletedCount);
            var sequence=(await ReadQueue(operations.Last())).Sequence;
            var state=await store.InspectAsync(sequence);Assert.AreEqual(HolonDeliveryState.Completed,state.Result.State);Assert.IsFalse(state.Result.RecoveryEvidence.RequiresNativeReconciliation);
        }
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);Assert.IsFalse((await target.ActivateProviderAsync()).IsError);
            var input=new Holon{Id=ids[0],Name="owned dispatch initial",Children=new List<IHolon>{new Holon{Id=ids[1],Name="owned dispatch child"}}};
            var first=await Commit(input,true);await Deliver();
            var loaded=await target.LoadHolonAsync(ids[0]);Assert.IsFalse(loaded.IsError,loaded.Message);Assert.AreEqual(first.VersionId,loaded.Result.VersionId);Assert.AreEqual(first.Children.Single().VersionId,loaded.Result.Children.Single().VersionId);
            var edit=OasisJson.Project<Holon>(first);edit.Children=first.Children.Select(h=>(IHolon)OasisJson.Project<Holon>(h)).ToList();edit.Name="owned dispatch updated";
            var second=await Commit(edit,false);await Deliver();
            Assert.AreEqual(second.VersionId,(await target.LoadHolonAsync(ids[0])).Result.VersionId);
            var soft=await Commit(second,false,ConditionalHolonOperation.SoftDelete);await Deliver();
            var softLoaded=await target.LoadHolonAsync(ids[0]);Assert.AreEqual(soft.DeletedDate,softLoaded.Result.DeletedDate);
            var hard=await Commit(soft,false,ConditionalHolonOperation.HardDelete);await Deliver();
            var absent=await target.LoadHolonAsync(ids[0]);Assert.IsTrue(absent.IsError || absent.Result==null);
            var queue=await ReadQueue(operations.Last());
            var replay=await adapter.ApplyReplicatedMutationAsync(CommittedHolonReplicaTarget.ReadItem(queue.Payload),CancellationToken.None);
            Assert.IsFalse(replay.IsError,replay.Message+replay.Exception);Assert.IsTrue(replay.Result);
            complete=true;
        }
        finally
        {
            if(complete)
            {
                await(await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h",new{ids=ids.Select(id=>id.ToString()).ToArray()})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (h:CosmicReplicaHead) WHERE h.HolonId IN $ids DETACH DELETE h",new{ids=ids.Select(id=>id.ToString()).ToArray()})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (r) WHERE (r:CosmicWriteReceipt OR r:CosmicHolonOutbox) AND r.OperationId IN $operations DETACH DELETE r",new{operations})).ConsumeAsync();
            }
            await provider.DeActivateProviderAsync();await target.DeActivateProviderAsync();
            if(complete)foreach(var path in new[]{database,database+"-wal",database+"-shm"})if(File.Exists(path))File.Delete(path);
        }
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeCosmicOutboxHostingAutomaticallyDeliversSqliteToNeo4j(bool synchronous)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        var database=Path.Combine(work,"outbox-host-source.db");Assert.IsFalse(File.Exists(database),"Preserve retained fixtures for inspection.");
        var provider=new SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={database};Pooling=False");
        var target=new Provider(uri,"neo4j","oasis-local-test-only");
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var inspection=driver.AsyncSession();
        var store=new SQLLiteDBOASIS.SqliteHolonReplicationOutbox($"Data Source={database};Pooling=False");var origin="owned-host-"+Guid.NewGuid().ToString("N");
        var actor=Guid.NewGuid();var ids=new[]{Guid.NewGuid(),Guid.NewGuid()};var operations=new List<string>();var complete=false;
        var grantClock=new NativeCosmicHostGrantClock {Value=DateTime.UtcNow};
        var grantExpiry=grantClock.Value.AddMinutes(1);
        var catalog=new HolonReplicationCatalog("owned-native-catalog",Guid.NewGuid(),origin,"owned-sqlite-host-source",ProviderType.SQLLiteDBOASIS,store,
            new[]{new HolonReplicationDestination("neo4j","owned-loopback-neo4j-default",ProviderType.Neo4jOASIS,target,
                new HolonReplicationActorGrant(new[]{actor},grantExpiry,grantClock))});
        var targetId=catalog.TargetIds.Single();var adapter=catalog.Targets.Single();
        Microsoft.Extensions.Hosting.IHost? host=null;
        IEnumerable<IHolon> Flatten(IHolon value){yield return value;foreach(var child in value.Children)foreach(var node in Flatten(child))yield return node;}
        async Task<IHolon> Commit(IHolon value,bool create,ConditionalHolonOperation kind=ConditionalHolonOperation.Save)
        {
            var op=Guid.NewGuid();operations.Add(op.ToString("D"));
            var conditions=Flatten(value).Select(h=>new HolonWriteCondition(h.Id,create ? null:CosmicConcurrency.GetProviderRevisionId(h,ProviderType.SQLLiteDBOASIS)));
            var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(op,actor,ProviderType.SQLLiteDBOASIS,conditions),kind,new[]{value});
            var enrollment=catalog.Enrollment;
            var result=synchronous ? provider.ExecuteReplicatingHolonWrite(request,enrollment):await provider.ExecuteReplicatingHolonWriteAsync(request,enrollment);
            Assert.IsFalse(result.IsError,result.Message+result.Exception);return result.Result.Single();
        }
        async Task<(long Sequence,string Payload)> ReadQueue(string operation)
        {
            using var connection=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={database};Mode=ReadWrite;Pooling=False");
            await connection.OpenAsync();using var command=connection.CreateCommand();
            command.CommandText="SELECT Sequence,PayloadJson FROM CosmicHolonOutbox WHERE OperationId=$operation";
            command.Parameters.AddWithValue("$operation",operation);using var reader=await command.ExecuteReaderAsync();
            Assert.IsTrue(await reader.ReadAsync());var value=(reader.GetInt64(0),reader.GetString(1));Assert.IsFalse(await reader.ReadAsync());return value;
        }
        async Task Deliver()
        {
            var sequence=(await ReadQueue(operations.Last())).Sequence;var deadline=DateTime.UtcNow.AddSeconds(15);
            while(true)
            {
                var state=await store.InspectAsync(sequence);Assert.IsFalse(state.IsError,state.Message+state.Exception);
                Assert.AreNotEqual(HolonDeliveryState.ReconciliationRequired,state.Result.State,state.Result.ErrorCode+" "+state.Result.Message);
                if(state.Result.State==HolonDeliveryState.Completed){Assert.IsFalse(state.Result.RecoveryEvidence.RequiresNativeReconciliation);break;}
                Assert.IsTrue(DateTime.UtcNow<deadline,"Automatic host failed to complete the retained obligation.");await Task.Delay(25);
            }
        }
        try
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);Assert.IsFalse((await target.ActivateProviderAsync()).IsError);
            host=new Microsoft.Extensions.Hosting.HostBuilder().ConfigureServices((_,services)=>
                services.AddCosmicHolonReplication(new HolonReplicationHostOptions("owned-native-host",TimeSpan.FromMilliseconds(25),TimeSpan.FromMilliseconds(50)),catalog)).Build();
            await host.StartAsync();
            var input=new Holon{Id=ids[0],Name="owned dispatch initial",Children=new List<IHolon>{new Holon{Id=ids[1],Name="owned dispatch child"}}};
            var first=await Commit(input,true);await Deliver();
            var loaded=await target.LoadHolonAsync(ids[0]);Assert.IsFalse(loaded.IsError,loaded.Message);Assert.AreEqual(first.VersionId,loaded.Result.VersionId);Assert.AreEqual(first.Children.Single().VersionId,loaded.Result.Children.Single().VersionId);
            var edit=OasisJson.Project<Holon>(first);edit.Children=first.Children.Select(h=>(IHolon)OasisJson.Project<Holon>(h)).ToList();edit.Name="owned dispatch updated";
            var second=await Commit(edit,false);await Deliver();
            Assert.AreEqual(second.VersionId,(await target.LoadHolonAsync(ids[0])).Result.VersionId);
            var soft=await Commit(second,false,ConditionalHolonOperation.SoftDelete);await Deliver();
            var softLoaded=await target.LoadHolonAsync(ids[0]);Assert.AreEqual(soft.DeletedDate,softLoaded.Result.DeletedDate);
            var hard=await Commit(soft,false,ConditionalHolonOperation.HardDelete);await Deliver();
            var absent=await target.LoadHolonAsync(ids[0]);Assert.IsTrue(absent.IsError || absent.Result==null);
            var queue=await ReadQueue(operations.Last());
            var replay=await adapter.ApplyReplicatedMutationAsync(CommittedHolonReplicaTarget.ReadItem(queue.Payload),CancellationToken.None);
            Assert.IsFalse(replay.IsError,replay.Message+replay.Exception);Assert.IsTrue(replay.Result);
            // Expired authorization must persist an ordered block without any native target write.
            grantClock.Value=grantExpiry;
            var deniedId=Guid.NewGuid();var deniedOperation=Guid.NewGuid();operations.Add(deniedOperation.ToString("D"));
            var deniedRequest=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(deniedOperation,actor,ProviderType.SQLLiteDBOASIS,
                new[]{new HolonWriteCondition(deniedId,null)}),ConditionalHolonOperation.Save,new[]{new Holon{Id=deniedId,Name="owned expired-grant record"}},saveChildren:false);
            var committed=synchronous ? provider.ExecuteReplicatingHolonWrite(deniedRequest,catalog.Enrollment)
                :await provider.ExecuteReplicatingHolonWriteAsync(deniedRequest,catalog.Enrollment);
            Assert.IsFalse(committed.IsError,committed.Message+committed.Exception);
            var deniedQueue=await ReadQueue(operations.Last());var deniedDeadline=DateTime.UtcNow.AddSeconds(15);
            while(true)
            {
                var state=await store.InspectAsync(deniedQueue.Sequence);Assert.IsFalse(state.IsError,state.Message+state.Exception);
                if(state.Result.State==HolonDeliveryState.ReconciliationRequired)
                {
                    Assert.AreEqual("COSMIC_REPLICA_AUTHORIZATION_DENIED",state.Result.ErrorCode);Assert.AreEqual(1,state.Result.AttemptCount);
                    Assert.IsFalse(state.Result.RecoveryEvidence.RequiresNativeReconciliation);break;
                }
                Assert.AreNotEqual(HolonDeliveryState.Completed,state.Result.State);Assert.IsTrue(DateTime.UtcNow<deniedDeadline);await Task.Delay(25);
            }
            var deniedRows=await(await inspection.RunAsync("MATCH (h:Holon {Id:$id}) RETURN count(h) AS count",new{id=deniedId.ToString()})).SingleAsync();
            Assert.AreEqual(0L,deniedRows["count"].As<long>());
            var deniedReceipts=await(await inspection.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) RETURN count(r) AS count",new{operation=deniedOperation.ToString("D")})).SingleAsync();
            Assert.AreEqual(0L,deniedReceipts["count"].As<long>());
            using(var stop=new CancellationTokenSource(TimeSpan.FromSeconds(10)))await host.StopAsync(stop.Token);host.Dispose();
            host=new Microsoft.Extensions.Hosting.HostBuilder().ConfigureServices((_,services)=>
                services.AddCosmicHolonReplication(new HolonReplicationHostOptions("owned-restarted-host",TimeSpan.FromMilliseconds(25),TimeSpan.FromMilliseconds(50)),catalog)).Build();
            await host.StartAsync();
            var readiness=Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<HolonReplicationHostState>(host.Services);
            var readyDeadline=DateTime.UtcNow.AddSeconds(10);
            while(readiness.Snapshot().Single().Availability!=HolonReplicationRouteAvailability.Degraded)
            {Assert.IsTrue(DateTime.UtcNow<readyDeadline);await Task.Delay(25);}
            Assert.IsFalse(readiness.IsReady);Assert.AreEqual("COSMIC_OUTBOX_RECONCILIATION_REQUIRED",readiness.Snapshot().Single().Code);
            var stillBlocked=await new SQLLiteDBOASIS.SqliteHolonReplicationOutbox($"Data Source={database};Pooling=False").PeekNextAsync(targetId);
            Assert.AreEqual(deniedQueue.Sequence,stillBlocked.Result.Sequence);Assert.AreEqual(1,stillBlocked.Result.AttemptCount);
            complete=true;
        }
        finally
        {
            if(host!=null){using var stop=new CancellationTokenSource(TimeSpan.FromSeconds(10));await host.StopAsync(stop.Token);host.Dispose();}
            if(complete)
            {
                await(await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h",new{ids=ids.Select(id=>id.ToString()).ToArray()})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (h:CosmicReplicaHead) WHERE h.HolonId IN $ids DETACH DELETE h",new{ids=ids.Select(id=>id.ToString()).ToArray()})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (r) WHERE (r:CosmicWriteReceipt OR r:CosmicHolonOutbox) AND r.OperationId IN $operations DETACH DELETE r",new{operations})).ConsumeAsync();
            }
            await provider.DeActivateProviderAsync();await target.DeActivateProviderAsync();
            if(complete)foreach(var path in new[]{database,database+"-wal",database+"-shm"})if(File.Exists(path))File.Delete(path);
        }
    }

    private sealed class NativeCosmicHostGrantClock : NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization.IHyperDriveClock
    {
        private long _ticks;
        public DateTime Value {get=>new(Interlocked.Read(ref _ticks),DateTimeKind.Utc);set=>Interlocked.Exchange(ref _ticks,value.Ticks);}
        public DateTime UtcNow=>Value;
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeCosmicConfiguredHostDeliversNeo4jToSqlite(bool synchronous)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        var database=Path.Combine(work,"configured-host-target.db");Assert.IsFalse(File.Exists(database),"Preserve retained fixtures for inspection.");
        var target=new SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={database};Pooling=False");
        var provider=new Provider(uri,"neo4j","oasis-local-test-only");
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var inspection=driver.AsyncSession();
        var store=new NeoHolonReplicationOutbox(driver);var origin="owned-configured-"+Guid.NewGuid().ToString("N");
        var actor=Guid.NewGuid();var ids=new[]{Guid.NewGuid(),Guid.NewGuid()};var operations=new List<string>();var complete=false;
        HolonReplicationCatalog catalog=null!;string targetId=null!;NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization.IHyperDriveIdempotentReplicationTarget adapter=null!;
        Microsoft.Extensions.Hosting.IHost? host=null;
        var document=new Newtonsoft.Json.Linq.JObject{["enabled"]=true,["workerId"]="owned-configured-host",["catalogs"]=new Newtonsoft.Json.Linq.JArray(new Newtonsoft.Json.Linq.JObject{
            ["id"]="owned-catalog",["version"]=Guid.NewGuid().ToString("D"),["sourceId"]=origin,["storeIdentity"]="owned-neo-default",["sourceProvider"]="Neo4jOASIS",
            ["targets"]=new Newtonsoft.Json.Linq.JArray(new Newtonsoft.Json.Linq.JObject{["name"]="sqlite",["endpointIdentity"]="owned-sqlite-target",["provider"]="SQLLiteDBOASIS",
                ["actors"]=new Newtonsoft.Json.Linq.JArray(actor.ToString("D")),["expiresAtUtc"]=DateTime.UtcNow.AddMinutes(10).ToString("O")})})};
        IEnumerable<IHolon> Flatten(IHolon value){yield return value;foreach(var child in value.Children)foreach(var node in Flatten(child))yield return node;}
        async Task<IHolon> Commit(IHolon value,bool create,ConditionalHolonOperation kind=ConditionalHolonOperation.Save)
        {
            var op=Guid.NewGuid();operations.Add(op.ToString("D"));
            var conditions=Flatten(value).Select(h=>new HolonWriteCondition(h.Id,create ? null:CosmicConcurrency.GetProviderRevisionId(h,ProviderType.Neo4jOASIS)));
            var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(op,actor,ProviderType.Neo4jOASIS,conditions),kind,new[]{value});
            var enrollment=catalog.Enrollment;
            var result=synchronous ? provider.ExecuteReplicatingHolonWrite(request,enrollment):await provider.ExecuteReplicatingHolonWriteAsync(request,enrollment);
            Assert.IsFalse(result.IsError,result.Message+result.Exception);return result.Result.Single();
        }
        async Task Deliver()
        {
            var sequence=(await(await inspection.RunAsync("MATCH (o:CosmicHolonOutbox {OperationId:$op}) RETURN o.Sequence AS sequence",new{op=operations.Last()})).SingleAsync())["sequence"].As<long>();
            var deadline=DateTime.UtcNow.AddSeconds(15);
            while(true)
            {
                var state=await store.InspectAsync(sequence);Assert.IsFalse(state.IsError,state.Message+state.Exception);
                Assert.AreNotEqual(HolonDeliveryState.ReconciliationRequired,state.Result.State,state.Result.ErrorCode+" "+state.Result.Message);
                if(state.Result.State==HolonDeliveryState.Completed){Assert.IsFalse(state.Result.RecoveryEvidence.RequiresNativeReconciliation);break;}
                Assert.IsTrue(DateTime.UtcNow<deadline);await Task.Delay(25);
            }
        }
        try
        {
            Assert.IsFalse(provider.IsProviderActivated);Assert.IsFalse(target.IsProviderActivated);
            host=new Microsoft.Extensions.Hosting.HostBuilder().ConfigureServices((_,services)=>
                services.AddConfiguredCosmicHolonReplication(document.ToString(),type=>type==ProviderType.Neo4jOASIS ? provider:target)).Build();
            await host.StartAsync();Assert.IsTrue(provider.IsProviderActivated);Assert.IsTrue(target.IsProviderActivated);
            var configured=Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<ConfiguredHolonReplicationHostedService>(host.Services);
            catalog=configured.Catalogs.Single();targetId=catalog.TargetIds.Single();adapter=catalog.Targets.Single();
            var input=new Holon{Id=ids[0],Name="owned dispatch initial",Children=new List<IHolon>{new Holon{Id=ids[1],Name="owned dispatch child"}}};
            var first=await Commit(input,true);await Deliver();
            var loaded=await target.LoadHolonAsync(ids[0]);Assert.IsFalse(loaded.IsError,loaded.Message);Assert.AreEqual(first.VersionId,loaded.Result.VersionId);Assert.AreEqual(first.Children.Single().VersionId,loaded.Result.Children.Single().VersionId);
            var edit=OasisJson.Project<Holon>(first);edit.Children=first.Children.Select(h=>(IHolon)OasisJson.Project<Holon>(h)).ToList();edit.Name="owned dispatch updated";
            var second=await Commit(edit,false);await Deliver();
            Assert.AreEqual(second.VersionId,(await target.LoadHolonAsync(ids[0])).Result.VersionId);
            var soft=await Commit(second,false,ConditionalHolonOperation.SoftDelete);await Deliver();
            var softLoaded=await target.LoadHolonAsync(ids[0]);Assert.AreEqual(soft.DeletedDate,softLoaded.Result.DeletedDate);
            var hard=await Commit(soft,false,ConditionalHolonOperation.HardDelete);await Deliver();
            var absent=await target.LoadHolonAsync(ids[0]);Assert.IsTrue(absent.IsError || absent.Result==null);
            var queue=await(await inspection.RunAsync("MATCH (o:CosmicHolonOutbox {OperationId:$op}) RETURN o.PayloadJson AS payload",new{op=operations.Last()})).SingleAsync();
            var replay=await adapter.ApplyReplicatedMutationAsync(CommittedHolonReplicaTarget.ReadItem(queue["payload"].As<string>()),CancellationToken.None);
            Assert.IsFalse(replay.IsError,replay.Message+replay.Exception);Assert.IsTrue(replay.Result);
            complete=true;
        }
        finally
        {
            if(host!=null){using var stop=new CancellationTokenSource(TimeSpan.FromSeconds(10));await host.StopAsync(stop.Token);host.Dispose();}
            if(complete)
            {
                await(await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h",new{ids=ids.Select(id=>id.ToString()).ToArray()})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (r) WHERE (r:CosmicWriteReceipt OR r:CosmicHolonOutbox) AND r.OperationId IN $operations DETACH DELETE r",new{operations})).ConsumeAsync();
            }
            await provider.DeActivateProviderAsync();await target.DeActivateProviderAsync();
            if(complete)foreach(var path in new[]{database,database+"-wal",database+"-shm"})if(File.Exists(path))File.Delete(path);
        }
    }

}
