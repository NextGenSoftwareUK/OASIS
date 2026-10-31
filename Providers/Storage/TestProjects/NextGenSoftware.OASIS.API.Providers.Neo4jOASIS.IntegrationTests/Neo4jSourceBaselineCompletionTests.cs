using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.HyperDrive;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;
using Outbox=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.NeoHolonReplicationOutbox;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public Task NativeNeoSourceBaselineCompletionPreservesFencesAndOrdinaryDelivery(bool durableApplication)
        =>RunSourceCompletionScenario(durableApplication,false);

    [TestMethod]
    public Task NativeNeoReviewedRepairCommandUsesConfiguredSourceAndPreservesOriginalBinding()
        =>RunSourceCompletionScenario(true,true);

    private async Task RunSourceCompletionScenario(bool durableApplication,bool reviewedHost)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        var state=Path.GetFullPath(Path.Combine(work,"../../LocalState/COSMIC/Neo4jSourceCompletionCurrent"));Directory.CreateDirectory(state);
        var manifest=Path.Combine(state,"fixture.json");var applicationJournal=Path.Combine(state,"application.json");
        var targetPath=Path.Combine(work,"source-baseline-completion-target.db");
        foreach(var path in new[]{targetPath,targetPath+"-wal",targetPath+"-shm",applicationJournal,applicationJournal+".lock",applicationJournal+".tmp"})
            Assert.IsFalse(File.Exists(path),"Preserve unresolved original fixture: "+path);
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var inspect=driver.AsyncSession();
        const string ownedMatch="MATCH (n) WHERE (n:Holon AND n.Id=$id) OR (n:CosmicReplicaHead AND n.HolonId=$id) "
            +"OR (n:OasisRecordVersion AND n.Kind='Holon' AND n.RecordId=$id) "
            +"OR ((n:CosmicWriteReceipt OR n:CosmicHolonOutbox) AND n.OperationId IN $operations) ";
        if(File.Exists(manifest))
        {
            using var previous=JsonDocument.Parse(await File.ReadAllTextAsync(manifest));
            Assert.IsTrue(previous.RootElement.GetProperty("cleaned").GetBoolean(),"Preserve original unresolved native state.");
            var priorId=previous.RootElement.GetProperty("id").GetString();
            var priorOperations=previous.RootElement.GetProperty("operations").EnumerateArray().Select(row=>row.GetString()!).ToArray();
            var remains=await(await inspect.RunAsync(ownedMatch+"RETURN count(n) AS count",new{id=priorId,operations=priorOperations})).SingleAsync();
            Assert.AreEqual(0L,remains["count"].As<long>(),"Prior cleanup must remain true before journal reuse.");
        }
        var id=Guid.NewGuid();var actor=Guid.NewGuid();var initialization=Guid.NewGuid();var operations=new List<string>();
        var sourceId=reviewedHost ? HolonReplicationRoutes.SourceId(ProviderType.Neo4jOASIS):"neo-completion-source";
        var targetId=reviewedHost ? HolonReplicationRoutes.TargetId(ProviderType.SQLLiteDBOASIS):"sqlite-completion-target";
        var complete=false;
        void Journal(bool cleaned=false)
        {
            var bytes=JsonSerializer.SerializeToUtf8Bytes(new{owner="cosmic-neo-source-completion-v1",id,actor,initialization,operations,targetPath,cleaned});
            using(var file=new FileStream(manifest+".tmp",FileMode.CreateNew,FileAccess.Write,FileShare.None)){file.Write(bytes);file.Flush(true);}
            File.Move(manifest+".tmp",manifest,true);
        }
        Journal();
        var source=new Provider(uri,"neo4j","oasis-local-test-only");
        var target=new SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={targetPath};Pooling=False");
        try
        {
            Assert.IsFalse((await source.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await target.ActivateProviderAsync()).IsError);
            var existing=await(await inspect.RunAsync("MATCH (h:Holon {Id:$id}) RETURN count(h) AS count",new{id=id.ToString("D")})).SingleAsync();
            Assert.AreEqual(0L,existing["count"].As<long>());
            var outbox=(Outbox)source.GetHolonReplicationOutbox().Result;
            Assert.IsNull((await outbox.PeekNextAsync(targetId)).Result,"Never claim or alter an unrelated target obligation.");
            IHolon current=new Holon{Id=id,Name="owned Neo source completion"};
            async Task Save(int version)
            {
                var operation=Guid.NewGuid();operations.Add(operation.ToString("D"));Journal();
                var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation,actor,ProviderType.Neo4jOASIS,
                    new[]{new HolonWriteCondition(id,CosmicConcurrency.GetProviderRevisionId(current,ProviderType.Neo4jOASIS))}),
                    ConditionalHolonOperation.Save,new[]{current},saveChildren:false);
                var saved=version<4 ? await source.ExecuteConditionalHolonWriteAsync(request)
                    :await source.ExecuteReplicatingHolonWriteAsync(request,new HolonReplicationEnrollment(sourceId,new[]{targetId}));
                Assert.IsFalse(saved.IsError,saved.Message+saved.Exception);current=saved.Result.Single();Assert.AreEqual(version,current.Version);
            }
            async Task<string> Snapshot()
            {
                var rows=await(await inspect.RunAsync("MATCH (o:CosmicHolonOutbox {OperationId:$operation}) RETURN o AS row",
                    new{operation=operations[3]})).SingleAsync();
                return JsonSerializer.Serialize(rows["row"].As<Neo4j.Driver.INode>().Properties.OrderBy(pair=>pair.Key).ToDictionary(pair=>pair.Key,pair=>pair.Value));
            }
            for(var version=1;version<=4;version++)await Save(version);
            var dispatcher=new HolonOutboxDispatcher(outbox,new[]{new CommittedHolonReplicaTarget(targetId,target,
                ProviderType.SQLLiteDBOASIS,sourceId,ProviderType.Neo4jOASIS)},"owned-neo-completion-worker");
            Assert.AreEqual(1,(await dispatcher.DispatchOnceAsync(targetId)).Result.ReconciliationCount);
            var held=(await outbox.PeekNextAsync(targetId)).Result;
            var reviewed=new ReplicaBaselineRepairCommand.Request(actor,initialization,Guid.NewGuid(),held.Sequence,held.OperationId,
                actor,id,(await outbox.ReadObligationAsync(held.Sequence)).Result.PayloadDigest,
                ReplicaBaselineRepairCommand.NeoStoreIdentity(uri,"neo4j"),targetPath,"Neo4jOASIS");
            var preparation=reviewedHost ? ReplicaBaselineRepairCommand.Preparation(reviewed,ProviderType.Neo4jOASIS,outbox,targetPath)
                :new HolonReplicaInitializationPreparation("neo-source-completion",Guid.NewGuid(),sourceId,"owned-neo-service",
                    ProviderType.Neo4jOASIS,outbox,targetId,targetPath,ProviderType.SQLLiteDBOASIS,new SourceCompletionPolicy(actor,id,targetPath));
            var prepared=await preparation.PrepareAsync(actor,held.Sequence,initialization);Assert.IsFalse(prepared.IsError,prepared.Message+prepared.Exception);
            var intent=prepared.Result;var original=CommittedHolonReplicaPayload.Read((await outbox.ReadObligationAsync(held.Sequence)).Result.PayloadJson);
            var baseline=new CommittedHolonReplicaRequest(initialization,original.SourceId,original.SourceAuthority,original.ActorId,
                original.Operation,original.Roots,original.SaveChildren,original.Recursive,original.MaxChildDepth);
            Assert.IsTrue((await outbox.CompleteInitializedBaselineAsync(intent,(await target.ReadReplicaInitializationOutcomeAsync(baseline)).Result)).IsError);
            if(reviewedHost)
            {
                var dna=new OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                    HyperDriveMode=HyperDriveModes.V2,StorageProviders=new StorageProviderSettings {
                        Neo4jOASIS=new Neo4jOASISSettings {ConnectionString=uri,Username="neo4j",Password="oasis-local-test-only"},
                        SQLLiteDBOASIS=new SQLLiteDBOASISSettings {ConnectionString=$"Data Source={targetPath};Pooling=False"}}}};
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>ReplicaBaselineRepairCommand.ExecuteAsync(dna,
                    reviewed with{SourceStoreIdentity="neo4j-store:wrong"},applicationJournal));
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>ReplicaBaselineRepairCommand.ExecuteAsync(dna,
                    reviewed with{SourcePayloadDigest=new string('0',64)},applicationJournal));
                Assert.AreEqual(HolonDeliveryState.ReconciliationRequired,(await outbox.InspectAsync(held.Sequence)).Result.State);
                var applied=await ReplicaBaselineRepairCommand.ExecuteAsync(dna,reviewed,applicationJournal);
                Assert.IsFalse(applied.IsError,applied.Message+applied.Exception);
                var bytes=await File.ReadAllBytesAsync(applicationJournal);
                Assert.IsTrue((await ReplicaBaselineRepairCommand.ExecuteAsync(dna,reviewed,applicationJournal)).Result);
                CollectionAssert.AreEqual(bytes,await File.ReadAllBytesAsync(applicationJournal));
            }
            else if(durableApplication)
            {
                var application=new HolonReplicaInitializationApplication(preparation,target,outbox.CompleteInitializedBaselineAsync);
                var applied=await application.ExecuteAsync(actor,held.Sequence,initialization,applicationJournal);Assert.IsFalse(applied.IsError,applied.Message+applied.Exception);
                var bytes=await File.ReadAllBytesAsync(applicationJournal);
                Assert.IsTrue((await application.ExecuteAsync(actor,held.Sequence,initialization,applicationJournal)).Result);
                CollectionAssert.AreEqual(bytes,await File.ReadAllBytesAsync(applicationJournal));
            }
            else Assert.IsFalse((await target.InitializeCommittedHolonReplicaAsync(baseline)).IsError);
            var receipt=(await target.ReadReplicaInitializationOutcomeAsync(baseline)).Result;
            if(!durableApplication)
            {
                await(await inspect.RunAsync("MATCH (o:CosmicHolonOutbox {Sequence:$sequence}) SET o.AttemptCount=$attempt",
                    new{sequence=held.Sequence,attempt=intent.AttemptCount+1L})).ConsumeAsync();
                Assert.IsTrue((await outbox.CompleteInitializedBaselineAsync(intent,receipt)).IsError);
                Assert.AreEqual(HolonDeliveryState.ReconciliationRequired,(await outbox.InspectAsync(held.Sequence)).Result.State);
                await(await inspect.RunAsync("MATCH (o:CosmicHolonOutbox {Sequence:$sequence}) SET o.AttemptCount=$attempt",
                    new{sequence=held.Sequence,attempt=(long)intent.AttemptCount})).ConsumeAsync();
                var before=await Snapshot();using(var cancelled=new CancellationTokenSource())
                {
                    cancelled.Cancel();await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>outbox.CompleteInitializedBaselineAsync(intent,receipt,cancelled.Token));
                }
                Assert.AreEqual(before,await Snapshot());
                var raced=await Task.WhenAll(outbox.CompleteInitializedBaselineAsync(intent,receipt),new Outbox(driver).CompleteInitializedBaselineAsync(intent,receipt));
                foreach(var result in raced)Assert.IsTrue(result.Result,result.Message+result.Exception);
            }
            Assert.IsTrue((await outbox.CompleteInitializedBaselineAsync(intent,receipt)).Result);
            var acknowledged=await Snapshot();
            Assert.IsTrue((await new Outbox(driver).CompleteInitializedBaselineAsync(intent,receipt)).Result);
            Assert.AreEqual(acknowledged,await Snapshot());
            var completed=(await outbox.InspectAsync(held.Sequence)).Result;Assert.AreEqual(held.ErrorCode,completed.ErrorCode);Assert.AreEqual(held.Message,completed.Message);
            var changed=new HolonReplicaInitializationOutcome(initialization,ProviderType.SQLLiteDBOASIS,ConditionalHolonReceiptState.Committed,receipt.BaselineFingerprint,new string('0',64));
            Assert.IsTrue((await outbox.CompleteInitializedBaselineAsync(intent,changed)).IsError);Assert.AreEqual(acknowledged,await Snapshot());
            current.Name="ordinary post-baseline update";await Save(5);
            Assert.AreEqual(1,(await dispatcher.DispatchOnceAsync(targetId)).Result.CompletedCount);
            Assert.AreEqual(5,(await target.LoadHolonAsync(id,loadChildren:false)).Result.Version);
            Assert.IsNull((await outbox.PeekNextAsync(targetId)).Result);
            Assert.AreEqual(actor,(await source.LoadHolonAsync(id,loadChildren:false)).Result.CreatedByAvatarId);
            complete=true;
        }
        finally
        {
            target.Dispose();await source.DeActivateProviderAsync();
            if(complete)
            {
                await(await inspect.RunAsync(ownedMatch+"DETACH DELETE n",new{id=id.ToString("D"),operations})).ConsumeAsync();
                var remains=await(await inspect.RunAsync(ownedMatch+"RETURN count(n) AS count",new{id=id.ToString("D"),operations})).SingleAsync();
                Assert.AreEqual(0L,remains["count"].As<long>());
                foreach(var path in new[]{targetPath,targetPath+"-wal",targetPath+"-shm",applicationJournal,applicationJournal+".lock",applicationJournal+".tmp"})
                    if(File.Exists(path))File.Delete(path);
                Journal(true);
            }
        }
    }

    private sealed class SourceCompletionPolicy(Guid actor,Guid root,string target) : IHolonReplicaInitializationPolicy
    {
        public string BindingIdentity=>"owned-neo-source-completion-policy-v1";
        public Task<NextGenSoftware.OASIS.Common.OASISResult<bool>> AuthorizeAsync(HolonReplicaInitializationAuthorizationContext context,CancellationToken token)
            =>Task.FromResult(new NextGenSoftware.OASIS.Common.OASISResult<bool>(context.OperatorId==actor
                && context.SourceId=="neo-completion-source" && context.SourceAuthority==ProviderType.Neo4jOASIS
                && context.SourceStoreIdentity=="owned-neo-service" && context.TargetId=="sqlite-completion-target"
                && context.TargetAuthority==ProviderType.SQLLiteDBOASIS && context.TargetEndpointIdentity==target
                && (context.Mutation==null || context.Mutation.ActorId==actor && context.Mutation.Roots.Single().Id==root)));
    }
}
