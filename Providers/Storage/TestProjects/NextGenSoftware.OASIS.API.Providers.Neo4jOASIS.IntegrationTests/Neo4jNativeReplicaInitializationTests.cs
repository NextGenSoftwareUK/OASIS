using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task NativeExplicitReplicaInitializationPreservesGraphReceiptsAndIndependentData(bool synchronous,bool history)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        var path=Path.Combine(work,"initial-baseline-source.db");Assert.IsFalse(File.Exists(path),"Inspect a retained source before reuse.");
        var source=new SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False");
        var target=new Provider(uri,"neo4j","oasis-local-test-only"){IsVersionControlEnabled=history};
        var other=new Provider(uri,"neo4j","oasis-local-test-only"){IsVersionControlEnabled=history};
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var inspect=driver.AsyncSession();
        var ids=new HashSet<string>();var operations=new HashSet<string>();var actor=Guid.NewGuid();var complete=false;
        var state=Path.GetFullPath(Path.Combine(work,"../../LocalState/COSMIC/Neo4jReplicaInitialization"));
        Directory.CreateDirectory(state);var journal=Path.Combine(state,"fixture.json");
        void Journal(bool cleaned=false)
        {
            var bytes=System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new{format=1,owner="cosmic-neo-baseline-v1",ids,operations,path,cleaned,synchronous,history});
            using(var output=new FileStream(journal+".tmp",FileMode.Create,FileAccess.Write,FileShare.None)){output.Write(bytes);output.Flush(true);}
            File.Move(journal+".tmp",journal,true);
        }
        if(File.Exists(journal))
        {
            using var prior=System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(journal));
            Assert.AreEqual("cosmic-neo-baseline-v1",prior.RootElement.GetProperty("owner").GetString());
            Assert.IsTrue(prior.RootElement.GetProperty("cleaned").GetBoolean(),"Reconcile retained fixture ownership before another test.");
            var oldIds=prior.RootElement.GetProperty("ids").EnumerateArray().Select(value=>value.GetString()!).ToArray();
            var oldOperations=prior.RootElement.GetProperty("operations").EnumerateArray().Select(value=>value.GetString()!).ToArray();
            var remaining=await(await inspect.RunAsync("MATCH (n) WHERE (n:Holon AND n.Id IN $ids) OR (n:CosmicReplicaHead AND n.HolonId IN $ids) "
                +"OR (n:OasisRecordVersion AND n.Kind='Holon' AND n.RecordId IN $ids) OR (n:CosmicWriteReceipt AND n.OperationId IN $operations) RETURN count(n) AS count",
                new{ids=oldIds,operations=oldOperations})).SingleAsync();
            Assert.AreEqual(0L,remaining["count"].As<long>(),"Previous fixture cleanup must still be true before its journal is reused.");
        }
        Journal();
        Guid Operation(){var id=Guid.NewGuid();operations.Add(id.ToString("D"));Journal();return id;}
        CommittedHolonReplicaRequest Transfer(IHolon value)=>new(Operation(),"owned-sqlite-baseline",ProviderType.SQLLiteDBOASIS,actor,ConditionalHolonOperation.Save,new[]{value});
        async Task<IHolon> Save(SQLLiteDBOASIS.SQLLiteDBOASIS provider,IHolon value)
        {
            var result=await provider.ExecuteConditionalHolonWriteAsync(new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Operation(),actor,ProviderType.SQLLiteDBOASIS,
                new[]{value}.Concat(value.Children).Select(row=>new HolonWriteCondition(row.Id,row.Version==0 ? null:CosmicConcurrency.GetProviderRevisionId(row,ProviderType.SQLLiteDBOASIS)))),ConditionalHolonOperation.Save,new[]{value}));
            Assert.IsFalse(result.IsError,result.Message+result.Exception);return result.Result.Single();
        }
        async Task<IHolon> VersionFour()
        {
            IHolon value=new Holon{Id=Guid.NewGuid(),Name="owned baseline root",Children=new List<IHolon>{new Holon{Id=Guid.NewGuid(),Name="owned baseline child"}}};
            foreach(var row in new[]{value}.Concat(value.Children))ids.Add(row.Id.ToString("D"));
            Journal();
            for(var version=1;version<=4;version++)value=await Save(source,value);return value;
        }
        async Task<long> Count(string query,object parameters)=>(await(await inspect.RunAsync(query,parameters)).SingleAsync())["count"].As<long>();
        try
        {
            Assert.IsFalse((await source.ActivateProviderAsync()).IsError);
            Assert.IsFalse((await target.ActivateProviderAsync()).IsError);Assert.IsFalse((await other.ActivateProviderAsync()).IsError);
            var committed=await VersionFour();var baseline=Transfer(committed);
            Assert.AreEqual("COSMIC_REPLICA_LINEAGE_CONFLICT",(await target.ApplyCommittedHolonReplicaAsync(baseline)).ErrorCode);
            var result=synchronous ? target.InitializeCommittedHolonReplica(baseline):await target.InitializeCommittedHolonReplicaAsync(baseline);
            Assert.IsFalse(result.IsError,result.Message+result.Exception);var ack=result.Result.Single();
            Assert.AreEqual(4,ack.Version);Assert.AreEqual(committed.VersionId,ack.VersionId);Assert.AreEqual(committed.PreviousVersionId,ack.PreviousVersionId);
            Assert.AreEqual(committed.CreatedDate,ack.CreatedDate);Assert.AreEqual(actor,ack.CreatedByAvatarId);Assert.AreEqual(1,ack.Children.Count);
            var token=CosmicConcurrency.GetProviderRevisionId(ack,ProviderType.Neo4jOASIS);
            Assert.AreNotEqual(CosmicConcurrency.GetProviderRevisionId(committed,ProviderType.SQLLiteDBOASIS),token);
            await other.DeActivateProviderAsync();other=new Provider(uri,"neo4j","oasis-local-test-only"){IsVersionControlEnabled=history};
            Assert.IsFalse((await other.ActivateProviderAsync()).IsError);
            var replay=await other.InitializeCommittedHolonReplicaAsync(baseline);Assert.IsFalse(replay.IsError,replay.Message+replay.Exception);
            Assert.AreEqual(token,CosmicConcurrency.GetProviderRevisionId(replay.Result.Single(),ProviderType.Neo4jOASIS));
            var next=OasisJson.Project<Holon>(committed);next.Children=committed.Children.Select(child=>(IHolon)OasisJson.Project<Holon>(child)).ToList();
            next.Name="owned version five";var fifth=await Save(source,next);
            Assert.IsFalse((await target.ApplyCommittedHolonReplicaAsync(Transfer(fifth))).IsError);
            Assert.AreEqual(4,(await target.InitializeCommittedHolonReplicaAsync(baseline)).Result.Single().Version);
            Assert.AreEqual(5,(await target.LoadHolonAsync(committed.Id,false)).Result.Version);
            Assert.AreEqual("COSMIC_OPERATION_ID_REUSED",(await target.ApplyCommittedHolonReplicaAsync(baseline)).ErrorCode);

            var contested=await VersionFour();var a=Transfer(contested);var b=Transfer(contested);
            var race=await Task.WhenAll(target.InitializeCommittedHolonReplicaAsync(a),other.InitializeCommittedHolonReplicaAsync(b));
            Assert.AreEqual(1,race.Count(value=>!value.IsError));
            CollectionAssert.Contains(new[]{"COSMIC_REPLICA_LINEAGE_CONFLICT","COSMIC_CONCURRENT_WRITE"},race.Single(value=>value.IsError).ErrorCode);
            Assert.AreEqual(1L,await Count("MATCH (r:CosmicWriteReceipt) WHERE r.OperationId IN $operations RETURN count(r) AS count",new{operations=new[]{a.OperationId.ToString("D"),b.OperationId.ToString("D")}}));

            var blocked=await VersionFour();var childId=blocked.Children.Single().Id;
            var local=await target.ExecuteConditionalHolonWriteAsync(new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Operation(),actor,ProviderType.Neo4jOASIS,
                new[]{new HolonWriteCondition(childId,null)}),ConditionalHolonOperation.Save,new[]{new Holon{Id=childId,Name="owned independent target"}},saveChildren:false));
            Assert.IsFalse(local.IsError,local.Message+local.Exception);var independent=local.Result.Single();var rejected=Transfer(blocked);
            Assert.AreEqual("COSMIC_REPLICA_LINEAGE_CONFLICT",(await target.InitializeCommittedHolonReplicaAsync(rejected)).ErrorCode);
            Assert.AreEqual(0L,await Count("MATCH (h:Holon {Id:$id}) RETURN count(h) AS count",new{id=blocked.Id.ToString("D")}));
            Assert.AreEqual(independent.VersionId,(await target.LoadHolonAsync(childId,false)).Result.VersionId);
            var deleted=await target.ExecuteConditionalHolonWriteAsync(new ConditionalHolonWriteRequest(new ConditionalHolonWrite(Operation(),actor,ProviderType.Neo4jOASIS,
                new[]{new HolonWriteCondition(childId,CosmicConcurrency.GetProviderRevisionId(independent,ProviderType.Neo4jOASIS))}),ConditionalHolonOperation.HardDelete,new[]{independent},saveChildren:false));
            Assert.IsFalse(deleted.IsError,deleted.Message+deleted.Exception);
            Assert.AreEqual("COSMIC_REPLICA_LINEAGE_CONFLICT",(await target.InitializeCommittedHolonReplicaAsync(rejected)).ErrorCode);
            Assert.AreEqual(0L,await Count("MATCH (h:Holon) WHERE h.Id IN $ids RETURN count(h) AS count",new{ids=new[]{blocked.Id.ToString("D"),childId.ToString("D")}}));

            var fresh=await VersionFour();var unknown=Operation().ToString("D");
            await(await inspect.RunAsync("CREATE (r:CosmicWriteReceipt {OperationId:$id,ResultJson:'{}'})",new{id=unknown})).ConsumeAsync();
            var refused=Transfer(fresh);
            Assert.AreEqual("COSMIC_REPLICA_BASELINE_RECONCILIATION_REQUIRED",(await target.InitializeCommittedHolonReplicaAsync(refused)).ErrorCode);
            Assert.AreEqual(0L,await Count("MATCH (h:Holon {Id:$id}) RETURN count(h) AS count",new{id=fresh.Id.ToString("D")}));
            Assert.AreEqual(0L,await Count("MATCH (r:CosmicWriteReceipt {OperationId:$id}) RETURN count(r) AS count",new{id=refused.OperationId.ToString("D")}));
            complete=true;
        }
        finally
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{ids,operations,complete,path}));
            if(complete)
            {
                foreach(var query in new[]{"MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h","MATCH (h:CosmicReplicaHead) WHERE h.HolonId IN $ids DETACH DELETE h",
                    "MATCH (h:OasisRecordVersion {Kind:'Holon'}) WHERE h.RecordId IN $ids DETACH DELETE h"})
                    await(await inspect.RunAsync(query,new{ids=ids.ToArray()})).ConsumeAsync();
                await(await inspect.RunAsync("MATCH (r:CosmicWriteReceipt) WHERE r.OperationId IN $operations DETACH DELETE r",new{operations=operations.ToArray()})).ConsumeAsync();
                Assert.AreEqual(0L,await Count("MATCH (h:Holon) WHERE h.Id IN $ids RETURN count(h) AS count",new{ids=ids.ToArray()}));
                Assert.AreEqual(0L,await Count("MATCH (h:CosmicReplicaHead) WHERE h.HolonId IN $ids RETURN count(h) AS count",new{ids=ids.ToArray()}));
                Assert.AreEqual(0L,await Count("MATCH (h:OasisRecordVersion {Kind:'Holon'}) WHERE h.RecordId IN $ids RETURN count(h) AS count",new{ids=ids.ToArray()}));
                Assert.AreEqual(0L,await Count("MATCH (r:CosmicWriteReceipt) WHERE r.OperationId IN $operations RETURN count(r) AS count",new{operations=operations.ToArray()}));
            }
            await target.DeActivateProviderAsync();await other.DeActivateProviderAsync();await source.DeActivateProviderAsync();source.Dispose();
            if(complete)foreach(var owned in new[]{path,path+"-wal",path+"-shm"})if(File.Exists(owned))File.Delete(owned);
            if(complete)Journal(true);
        }
    }
}
