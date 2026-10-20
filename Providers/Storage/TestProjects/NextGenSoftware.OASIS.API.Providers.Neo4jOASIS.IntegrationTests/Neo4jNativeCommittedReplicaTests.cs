using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task NativeCosmicCommittedReplicaPreservesSourceLineageAndReceipts(bool synchronous,bool history)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        var database=Path.Combine(work,"committed-replica-source.db");
        Assert.IsFalse(File.Exists(database),"A retained source fixture requires inspection, not reset.");
        var source=new SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={database};Pooling=False");
        var target=new Provider(uri,"neo4j","oasis-local-test-only"){IsVersionControlEnabled=history};
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var inspection=driver.AsyncSession();
        var ids=new HashSet<string>();var operations=new HashSet<string>();var actor=Guid.NewGuid();
        var sourceId="owned-sqlite-origin-"+Guid.NewGuid().ToString("N");var complete=false;
        Holon New(string name){var row=new Holon{Id=Guid.NewGuid(),Name="owned replica "+name};ids.Add(row.Id.ToString());return row;}
        static Holon Clone(IHolon value){var row=OasisJson.Project<Holon>(value);row.Children=value.Children.Select(Clone).Cast<IHolon>().ToList();return row;}
        static IEnumerable<IHolon> Flatten(IHolon value){yield return value;foreach(var child in value.Children)foreach(var node in Flatten(child))yield return node;}
        async Task<(IHolon Record,Guid Operation)> Commit(IHolon value,bool create=false,ConditionalHolonOperation kind=ConditionalHolonOperation.Save,bool children=true)
        {
            var operation=Guid.NewGuid();operations.Add(operation.ToString());
            var nodes=children ? Flatten(value):new[]{value};
            var conditions=nodes.Select(row=>new HolonWriteCondition(row.Id,create ? null:CosmicConcurrency.GetProviderRevisionId(row,ProviderType.SQLLiteDBOASIS)));
            var request=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation,actor,ProviderType.SQLLiteDBOASIS,conditions),kind,new[]{value},saveChildren:children);
            var result=synchronous ? source.ExecuteConditionalHolonWrite(request):await source.ExecuteConditionalHolonWriteAsync(request);
            Assert.IsFalse(result.IsError,result.Message+result.Exception);return(result.Result.Single(),operation);
        }
        CommittedHolonReplicaRequest Delivery((IHolon Record,Guid Operation) committed,ConditionalHolonOperation kind=ConditionalHolonOperation.Save,bool children=true,string? origin=null)
            =>new(committed.Operation,origin ?? sourceId,ProviderType.SQLLiteDBOASIS,actor,kind,new[]{committed.Record},saveChildren:children);
        Task<OASISResult<IEnumerable<IHolon>>> ApplyOn(Provider receiver,CommittedHolonReplicaRequest request)
            =>synchronous ? Task.Run(()=>receiver.ApplyCommittedHolonReplica(request)):receiver.ApplyCommittedHolonReplicaAsync(request);
        Task<OASISResult<IEnumerable<IHolon>>> Apply(CommittedHolonReplicaRequest request)=>ApplyOn(target,request);
        async Task<string> Snapshot(Guid id)=>(await(await inspection.RunAsync("MATCH (h:Holon {Id:$id}) RETURN h.SnapshotJson AS json",new{id=id.ToString()})).SingleAsync())["json"].As<string>();
        async Task<long> Count(string query,object values)=>(await(await inspection.RunAsync(query,values)).SingleAsync())["count"].As<long>();
        Guid TargetToken(IHolon row)=>CosmicConcurrency.GetProviderRevisionId(row,ProviderType.Neo4jOASIS)!.Value;
        try
        {
            Assert.IsFalse((await source.ActivateProviderAsync()).IsError);Assert.IsFalse((await target.ActivateProviderAsync()).IsError);
            var input=New("root");input.Children=new List<IHolon>{New("child")};
            input.CustomKey="owned committed custom key";input.ParentHolonId2=Guid.NewGuid();
            input.ProviderMetaData[ProviderType.SQLLiteDBOASIS]=new Dictionary<string,string>{{"origin-note","retained"}};
            input.ProviderMetaData[ProviderType.Neo4jOASIS]=new Dictionary<string,string>{{"spoof-local","must not import"}};
            var initial=await Commit(input,create:true);var initialSource=OasisJson.Serialize(initial.Record);
            var first=await Apply(Delivery(initial));Assert.IsFalse(first.IsError,first.Message+first.Exception);
            var ack=first.Result.Single();Assert.AreEqual(initial.Record.VersionId,ack.VersionId);Assert.AreEqual(1,ack.Version);
            Assert.AreEqual(initial.Record.CreatedDate,ack.CreatedDate);Assert.AreEqual(actor,ack.CreatedByAvatarId);
            Assert.AreEqual(((Holon)initial.Record).CustomKey,((Holon)ack).CustomKey);
            Assert.AreEqual(initial.Record.ParentHolonId2,ack.ParentHolonId2);
            Assert.AreEqual("retained",ack.ProviderMetaData[ProviderType.SQLLiteDBOASIS]["origin-note"]);
            Assert.IsFalse(ack.ProviderMetaData[ProviderType.Neo4jOASIS].ContainsKey("spoof-local"));
            Assert.AreEqual(initial.Record.Children.Single().VersionId,ack.Children.Single().VersionId);
            Assert.AreNotEqual(CosmicConcurrency.GetProviderRevisionId(initial.Record,ProviderType.SQLLiteDBOASIS),TargetToken(ack));
            Assert.AreEqual(initialSource,OasisJson.Serialize(initial.Record));
            var secondInput=Clone(initial.Record);secondInput.Name="owned replica second";secondInput.Children.Single().Name="owned replica child second";
            var second=await Commit(secondInput);
            var thirdInput=Clone(second.Record);thirdInput.Description="owned replica third";var third=await Commit(thirdInput);
            var firstSnapshot=await Snapshot(input.Id);
            var gap=await Apply(Delivery(third));Assert.IsTrue(gap.IsError);Assert.AreEqual("COSMIC_REPLICA_LINEAGE_CONFLICT",gap.ErrorCode);
            Assert.AreEqual(firstSnapshot,await Snapshot(input.Id));
            Assert.AreEqual(0L,await Count("MATCH (r:CosmicWriteReceipt {OperationId:$id}) RETURN count(r) AS count",new{id=third.Operation.ToString()}));
            var appliedSecond=await Apply(Delivery(second));Assert.IsFalse(appliedSecond.IsError,appliedSecond.Message+appliedSecond.Exception);
            Assert.AreEqual(second.Record.VersionId,appliedSecond.Result.Single().VersionId);Assert.AreEqual(2,appliedSecond.Result.Single().Version);
            var secondSnapshot=await Snapshot(input.Id);
            await target.DeActivateProviderAsync();target=new Provider(uri,"neo4j","oasis-local-test-only"){IsVersionControlEnabled=history};
            Assert.IsFalse((await target.ActivateProviderAsync()).IsError);
            var replay=await Apply(Delivery(initial));Assert.IsFalse(replay.IsError,replay.Message+replay.Exception);
            Assert.AreEqual(TargetToken(ack),TargetToken(replay.Result.Single()));Assert.AreEqual(1,replay.Result.Single().Version);
            Assert.AreEqual(secondSnapshot,await Snapshot(input.Id));
            var name=initial.Record.Name;initial.Record.Name="changed original intent";
            Assert.AreEqual("COSMIC_OPERATION_ID_REUSED",(await Apply(Delivery(initial))).ErrorCode);initial.Record.Name=name;
            Assert.AreEqual("COSMIC_REPLICA_LINEAGE_CONFLICT",(await Apply(Delivery(third,origin:sourceId+"-different"))).ErrorCode);
            Assert.IsFalse((await Apply(Delivery(third))).IsError);
            var childId=third.Record.Children.Single().Id;var childBefore=await Snapshot(childId);
            var soft=await Commit(Clone(third.Record),kind:ConditionalHolonOperation.SoftDelete,children:false);
            var softResult=await Apply(Delivery(soft,ConditionalHolonOperation.SoftDelete,children:false));
            Assert.IsFalse(softResult.IsError,softResult.Message+softResult.Exception);Assert.IsTrue(softResult.IsDeleted);
            Assert.AreEqual(soft.Record.DeletedDate,softResult.Result.Single().DeletedDate);Assert.AreEqual(soft.Record.VersionId,softResult.Result.Single().VersionId);
            Assert.AreEqual(childBefore,await Snapshot(childId));
            var hard=await Commit(Clone(soft.Record),kind:ConditionalHolonOperation.HardDelete,children:false);
            var hardResult=await Apply(Delivery(hard,ConditionalHolonOperation.HardDelete,children:false));
            Assert.IsFalse(hardResult.IsError,hardResult.Message+hardResult.Exception);Assert.AreEqual(hard.Record.VersionId,hardResult.Result.Single().VersionId);
            Assert.AreEqual(0L,await Count("MATCH (h:Holon {Id:$id}) RETURN count(h) AS count",new{id=input.Id.ToString()}));
            Assert.AreEqual(1L,await Count("MATCH (h:CosmicReplicaHead {HolonId:$id,HardDeleted:true}) RETURN count(h) AS count",new{id=input.Id.ToString()}));
            var hardReplay=await Apply(Delivery(hard,ConditionalHolonOperation.HardDelete,children:false));
            Assert.IsFalse(hardReplay.IsError,hardReplay.Message);Assert.AreEqual(TargetToken(hardResult.Result.Single()),TargetToken(hardReplay.Result.Single()));
            Assert.AreEqual(childBefore,await Snapshot(childId));
            var oldReplay=await Apply(Delivery(initial));Assert.IsFalse(oldReplay.IsError,oldReplay.Message);
            Assert.AreEqual(initial.Record.VersionId,oldReplay.Result.Single().VersionId);
            Assert.AreEqual(0L,await Count("MATCH (h:Holon {Id:$id}) RETURN count(h) AS count",new{id=input.Id.ToString()}));
            var recreated=New("new logical branch after hard deletion");ids.Remove(recreated.Id.ToString());recreated.Id=input.Id;
            var recreation=await Commit(recreated,create:true,children:false);
            Assert.AreEqual("COSMIC_REPLICA_LINEAGE_CONFLICT",(await Apply(Delivery(recreation,children:false))).ErrorCode);

            // Local raw writes can keep the source logical ID with history off; the physical anchor must still reject them.
            var fork=await Commit(New("local conflict"),create:true,children:false);Assert.IsFalse((await Apply(Delivery(fork,children:false))).IsError);
            var loaded=await target.LoadHolonAsync(fork.Record.Id,loadChildren:false);loaded.Result.Description="owned local write";
            var local=await target.SaveHolonAsync(loaded.Result,saveChildren:false);Assert.IsFalse(local.IsError,local.Message);
            if(!history)Assert.AreEqual(fork.Record.VersionId,local.Result.VersionId);
            var localSnapshot=await Snapshot(fork.Record.Id);var forkNextInput=Clone(fork.Record);forkNextInput.Name="owned incoming conflict";
            var forkNext=await Commit(forkNextInput,children:false);
            Assert.AreEqual("COSMIC_REPLICA_LINEAGE_CONFLICT",(await Apply(Delivery(forkNext,children:false))).ErrorCode);
            Assert.AreEqual(localSnapshot,await Snapshot(fork.Record.Id));

            // A real target commit followed by a lost application acknowledgement must replay the exact receipt.
            var uncertainSource=await Commit(New("lost acknowledgement"),create:true,children:false);
            var field=typeof(Provider).GetField("_driver",BindingFlags.NonPublic|BindingFlags.Instance)!;
            var healthy=(IDriver)field.GetValue(target)!;var commits=0;var cause=new IOException("owned replica response lost after real commit");
            try
            {
                field.SetValue(target,Neo4jCommitFaultProxy.Wrap(healthy,cause,()=>commits++));
                var uncertain=await Apply(Delivery(uncertainSource,children:false));Assert.IsTrue(uncertain.IsError);
                var evidence=(NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jReplicaReconciliationException)uncertain.Exception;
                Assert.AreEqual(uncertainSource.Operation,evidence.OperationId);Assert.AreEqual(sourceId,evidence.SourceId);Assert.AreSame(cause,evidence.InnerException);
                Assert.AreEqual(string.Empty,evidence.OriginalTransactionId);Assert.AreEqual(1,commits);
                Assert.AreEqual(1L,await Count("MATCH (r:CosmicWriteReceipt {OperationId:$id}) RETURN count(r) AS count",new{id=uncertainSource.Operation.ToString()}));
                var native=await target.LoadHolonAsync(uncertainSource.Record.Id,loadChildren:false);Assert.IsFalse(native.IsError,native.Message);
                Assert.AreEqual(uncertainSource.Record.VersionId,native.Result.VersionId);
                var exact=await Apply(Delivery(uncertainSource,children:false));Assert.IsFalse(exact.IsError,exact.Message+exact.Exception);
                Assert.AreEqual(native.Result.VersionId,exact.Result.Single().VersionId);Assert.AreEqual(1,commits);
            }
            finally{field.SetValue(target,healthy);}

            // Competing workers applying the same accepted operation must share its original receipt.
            var other=new Provider(uri,"neo4j","oasis-local-test-only"){IsVersionControlEnabled=history};
            try
            {
                Assert.IsFalse((await other.ActivateProviderAsync()).IsError);
                var raced=await Commit(New("duplicate worker"),create:true,children:false);var racedRequest=Delivery(raced,children:false);
                var racedResults=await Task.WhenAll(Apply(racedRequest),ApplyOn(other,racedRequest));
                Assert.IsTrue(racedResults.All(result=>!result.IsError),string.Join(" | ",racedResults.Select(result=>result.Message+result.Exception)));
                Assert.AreEqual(TargetToken(racedResults[0].Result.Single()),TargetToken(racedResults[1].Result.Single()));
                var nextInput=Clone(raced.Record);nextInput.Name="owned ordered replica second";var next=await Commit(nextInput,children:false);
                var afterInput=Clone(next.Record);afterInput.Name="owned ordered replica third";var after=await Commit(afterInput,children:false);
                var ordered=await Task.WhenAll(Apply(Delivery(next,children:false)),ApplyOn(other,Delivery(after,children:false)));
                Assert.IsFalse(ordered[0].IsError,ordered[0].Message+ordered[0].Exception);
                if(ordered[1].IsError)
                {
                    Assert.AreEqual("COSMIC_REPLICA_LINEAGE_CONFLICT",ordered[1].ErrorCode);
                    var retried=await Apply(Delivery(after,children:false));Assert.IsFalse(retried.IsError,retried.Message+retried.Exception);
                }
                var orderedHead=await target.LoadHolonAsync(raced.Record.Id,loadChildren:false);
                Assert.IsFalse(orderedHead.IsError,orderedHead.Message);Assert.AreEqual(after.Record.VersionId,orderedHead.Result.VersionId);
            }
            finally{await other.DeActivateProviderAsync();}

            // An invalid returned acknowledgement rolls back the graph, initial head and receipt together.
            var invalidAckSource=await Commit(New("invalid acknowledgement"),create:true,children:false);
            try
            {
                field.SetValue(target,Neo4jPayloadFaultProxy.Wrap(healthy));
                var invalidAck=await Apply(Delivery(invalidAckSource,children:false));Assert.IsTrue(invalidAck.IsError);
                Assert.IsFalse(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(invalidAck.Exception));
            }
            finally{field.SetValue(target,healthy);}
            Assert.AreEqual(0L,await Count("MATCH (h:Holon {Id:$id}) RETURN count(h) AS count",new{id=invalidAckSource.Record.Id.ToString()}));
            Assert.AreEqual(0L,await Count("MATCH (h:CosmicReplicaHead {HolonId:$id}) RETURN count(h) AS count",new{id=invalidAckSource.Record.Id.ToString()}));
            Assert.AreEqual(0L,await Count("MATCH (r:CosmicWriteReceipt {OperationId:$id}) RETURN count(r) AS count",new{id=invalidAckSource.Operation.ToString()}));
            var admittedAfterRollback=await Apply(Delivery(invalidAckSource,children:false));
            Assert.IsFalse(admittedAfterRollback.IsError,admittedAfterRollback.Message+admittedAfterRollback.Exception);

            var corruptSource=await Commit(New("corrupt head"),create:true,children:false);
            Assert.IsFalse((await Apply(Delivery(corruptSource,children:false))).IsError);
            var corruptBefore=await Snapshot(corruptSource.Record.Id);
            var headJson=(await(await inspection.RunAsync("MATCH (h:CosmicReplicaHead {HolonId:$id}) RETURN h.SnapshotJson AS json",
                new{id=corruptSource.Record.Id.ToString()})).SingleAsync())["json"].As<string>();
            var changedHead=Newtonsoft.Json.Linq.JObject.Parse(headJson);changedHead["Name"]="corrupt stored head content";
            await(await inspection.RunAsync("MATCH (h:CosmicReplicaHead {HolonId:$id}) SET h.SnapshotJson=$json",
                new{id=corruptSource.Record.Id.ToString(),json=changedHead.ToString(Newtonsoft.Json.Formatting.None)})).ConsumeAsync();
            var corruptNextInput=Clone(corruptSource.Record);corruptNextInput.Name="must not replace corrupt head";
            var corruptNext=await Commit(corruptNextInput,children:false);
            var corruptResult=await Apply(Delivery(corruptNext,children:false));Assert.IsTrue(corruptResult.IsError);
            StringAssert.Contains(corruptResult.Exception.Message,"checksum");
            Assert.AreEqual("COSMIC_REPLICA_WRITE_FAILED",corruptResult.ErrorCode);Assert.AreEqual(corruptBefore,await Snapshot(corruptSource.Record.Id));
            Assert.AreEqual(0L,await Count("MATCH (r:CosmicWriteReceipt {OperationId:$id}) RETURN count(r) AS count",new{id=corruptNext.Operation.ToString()}));
            complete=true;
        }
        finally
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{ids,operations,sourceId,complete,database}));
            if(complete)
            {
                await(await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h",new{ids=ids.ToArray()})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (h:CosmicReplicaHead) WHERE h.HolonId IN $ids DETACH DELETE h",new{ids=ids.ToArray()})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (h:OasisRecordVersion {Kind:'Holon'}) WHERE h.RecordId IN $ids DETACH DELETE h",new{ids=ids.ToArray()})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (r:CosmicWriteReceipt) WHERE r.OperationId IN $operations DETACH DELETE r",new{operations=operations.ToArray()})).ConsumeAsync();
            }
            await target.DeActivateProviderAsync();await source.DeActivateProviderAsync();
            if(complete)foreach(var path in new[]{database,database+"-wal",database+"-shm"})if(File.Exists(path))File.Delete(path);
        }
    }
}
