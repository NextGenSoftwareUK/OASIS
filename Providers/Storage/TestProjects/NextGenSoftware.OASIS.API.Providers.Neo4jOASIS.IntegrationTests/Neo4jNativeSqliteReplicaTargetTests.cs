using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using Provider=NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public partial class Neo4jNativeAvatarTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeCosmicCommittedReplicaSqliteTargetPreservesRichNeo4jCommit(bool synchronous)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        var database=Path.Combine(work,"committed-replica-target.db");Assert.IsFalse(File.Exists(database),"Retained target evidence requires inspection.");
        var source=new Provider(uri,"neo4j","oasis-local-test-only");
        var target=new SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={database};Pooling=False");
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var inspection=driver.AsyncSession();
        var child=new Holon{Id=Guid.NewGuid(),Name="owned reverse child"};
        var root=new Holon{Id=Guid.NewGuid(),Name="owned reverse root",CustomKey="owned reverse key",ParentHolonId2=Guid.NewGuid(),Children=new List<IHolon>{child}};
        root.MetaData["nested"]=JObject.Parse("{\"exact\":9007199254740993,\"labels\":[\"one\",\"two\"],\"flag\":true}");
        root.ProviderMetaData[ProviderType.Neo4jOASIS]=new Dictionary<string,string>{{"origin-note","retained"}};
        root.ProviderMetaData[ProviderType.SQLLiteDBOASIS]=new Dictionary<string,string>{{"spoof-local","must not import"}};
        var ids=new[]{root.Id.ToString(),child.Id.ToString()};var operation=Guid.NewGuid();var actor=Guid.NewGuid();var complete=false;
        try
        {
            Assert.IsFalse((await source.ActivateProviderAsync()).IsError);Assert.IsFalse((await target.ActivateProviderAsync()).IsError);
            var create=new ConditionalHolonWriteRequest(new ConditionalHolonWrite(operation,actor,ProviderType.Neo4jOASIS,
                new[]{new HolonWriteCondition(root.Id,null),new HolonWriteCondition(child.Id,null)}),ConditionalHolonOperation.Save,new[]{root});
            var committed=synchronous ? source.ExecuteConditionalHolonWrite(create):await source.ExecuteConditionalHolonWriteAsync(create);
            Assert.IsFalse(committed.IsError,committed.Message+committed.Exception);var original=committed.Result.Single();
            Assert.IsNull(original.CreatedOASISType);var sourceJson=StoredRecordJson.Serialize(original);
            var request=new CommittedHolonReplicaRequest(operation,"owned-neo-origin-"+Guid.NewGuid().ToString("N"),ProviderType.Neo4jOASIS,actor,
                ConditionalHolonOperation.Save,committed.Result);
            var applied=synchronous ? target.ApplyCommittedHolonReplica(request):await target.ApplyCommittedHolonReplicaAsync(request);
            Assert.IsFalse(applied.IsError,applied.Message+applied.Exception);Assert.AreEqual(2,applied.SavedCount);
            var ack=applied.Result.Single();Assert.AreEqual(original.VersionId,ack.VersionId);Assert.AreEqual(original.CreatedDate,ack.CreatedDate);
            Assert.AreEqual(ProviderType.Neo4jOASIS,ack.CreatedProviderType.Value);Assert.IsNull(ack.CreatedOASISType);
            Assert.AreEqual(((Holon)original).CustomKey,((Holon)ack).CustomKey);Assert.AreEqual(original.ParentHolonId2,ack.ParentHolonId2);
            Assert.AreEqual(original.Children.Single().VersionId,ack.Children.Single().VersionId);
            Assert.IsTrue(JToken.DeepEquals(JToken.Parse(StoredRecordJson.Serialize(original.MetaData["nested"])),
                JToken.Parse(StoredRecordJson.Serialize(ack.MetaData["nested"]))));
            Assert.AreEqual("retained",ack.ProviderMetaData[ProviderType.Neo4jOASIS]["origin-note"]);
            Assert.IsFalse(ack.ProviderMetaData[ProviderType.SQLLiteDBOASIS].ContainsKey("spoof-local"));
            Assert.AreEqual(sourceJson,StoredRecordJson.Serialize(original));
            var replay=await target.ApplyCommittedHolonReplicaAsync(request);Assert.IsFalse(replay.IsError,replay.Message+replay.Exception);
            Assert.AreEqual(CosmicConcurrency.GetProviderRevisionId(ack,ProviderType.SQLLiteDBOASIS),CosmicConcurrency.GetProviderRevisionId(replay.Result.Single(),ProviderType.SQLLiteDBOASIS));
            complete=true;
        }
        finally
        {
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(new{ids,operation,database,complete}));
            if(complete)
            {
                await(await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h",new{ids})).ConsumeAsync();
                await(await inspection.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) DETACH DELETE r",new{operation=operation.ToString()})).ConsumeAsync();
            }
            await source.DeActivateProviderAsync();await target.DeActivateProviderAsync();
            if(complete)foreach(var path in new[]{database,database+"-wal",database+"-shm"})if(File.Exists(path))File.Delete(path);
        }
    }
}
