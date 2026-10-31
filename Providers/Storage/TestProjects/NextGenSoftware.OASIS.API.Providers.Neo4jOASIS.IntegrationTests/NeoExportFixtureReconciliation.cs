using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class NeoExportFixtureReconciliation
{
    [TestMethod]
    public async Task ArchiveExactReviewedIndependentExportHistoryFixture()
    {
        const string uri="bolt://127.0.0.1:17687";
        const string marker="405b6e2764234df29116992f084a4760";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("087E53F7B755B2619EA4AC8FA8B79BDBFB2489F1820E83D5CE689969ACDF0C56",
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(work,"neo4j-native-current.trx")))));
        var expected=new Dictionary<string,(string kind,int version,string field,string value)>{
            ["18521af4-d78c-4c28-b11c-6dd31b7c78ac"]=("Avatar",2,"Username","export-new-"+marker),
            ["201b328e-beba-4259-8a2f-e93c974f4a88"]=("Holon",2,"Name","grandchild-v2-"+marker),
            ["3edcee59-6975-4747-b2ae-6b267da0794d"]=("Holon",2,"Name","child-v2-"+marker),
            ["df5a3ad0-7a13-44e4-9de0-a600e027dba6"]=("Holon",3,"Name","root-v3-"+marker)};
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var session=driver.AsyncSession();
        var rows=await(await session.RunAsync("MATCH (n) WHERE n:Avatar OR n:AvatarDetail OR n:Holon OR n:OasisRecordVersion RETURN n")).ToListAsync();
        Assert.AreEqual(9,rows.Count);
        var nodes=rows.Select(row=>row["n"].As<INode>()).ToArray();
        foreach(var pair in expected){
            var current=nodes.Single(n=>n.Labels.SequenceEqual(new[]{pair.Value.kind})&&n.Properties["Id"].As<string>()==pair.Key);
            var live=JObject.Parse(current.Properties["SnapshotJson"].As<string>());
            Assert.AreEqual(pair.Key,live.Value<string>("Id"));Assert.AreEqual(pair.Value.version,live.Value<int>("Version"));
            Assert.AreEqual(pair.Value.value,live.Value<string>(pair.Value.field));Assert.IsFalse(live.Value<bool>("IsDeleted"));
            var history=nodes.Where(n=>n.Labels.SequenceEqual(new[]{"OasisRecordVersion"})&&n.Properties["RecordId"].As<string>()==pair.Key).OrderBy(n=>n.Properties["Version"].As<long>()).ToArray();
            Assert.AreEqual(pair.Value.version-1,history.Length);
            for(var index=0;index<history.Length;index++){
                var node=history[index];Assert.AreEqual(pair.Value.kind,node.Properties["Kind"]);Assert.AreEqual((long)index+1,node.Properties["Version"]);
                var old=JObject.Parse(node.Properties["SnapshotJson"].As<string>());
                Assert.AreEqual(pair.Key,old.Value<string>("Id"));Assert.AreEqual(index+1,old.Value<int>("Version"));
                Assert.IsTrue(old.Value<string>(pair.Value.field)!.EndsWith(marker,StringComparison.Ordinal));Assert.IsFalse(old.Value<bool>("IsDeleted"));
            }
        }
        var links=await(await session.RunAsync("MATCH (a)-[r]->(b) WHERE a.Id IN $ids OR b.Id IN $ids RETURN a.Id AS source,b.Id AS target,type(r) AS kind",new{ids=expected.Keys.ToArray()})).ToListAsync();
        CollectionAssert.AreEquivalent(new[]{
            "df5a3ad0-7a13-44e4-9de0-a600e027dba6|HAS_CHILD|3edcee59-6975-4747-b2ae-6b267da0794d",
            "3edcee59-6975-4747-b2ae-6b267da0794d|HAS_CHILD|201b328e-beba-4259-8a2f-e93c974f4a88",
            "df5a3ad0-7a13-44e4-9de0-a600e027dba6|HAS_CQL_PROJECTION|df5a3ad0-7a13-44e4-9de0-a600e027dba6",
            "3edcee59-6975-4747-b2ae-6b267da0794d|HAS_CQL_PROJECTION|3edcee59-6975-4747-b2ae-6b267da0794d",
            "201b328e-beba-4259-8a2f-e93c974f4a88|HAS_CQL_PROJECTION|201b328e-beba-4259-8a2f-e93c974f4a88"
        },links.Select(link=>string.Join("|",link["source"].As<string>(),link["kind"].As<string>(),link["target"].As<string>())).ToArray());
        var path=Path.Combine(work,"neo-export-reviewed-history-fixture.json");Assert.IsFalse(File.Exists(path));
        using(var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){
            await System.Text.Json.JsonSerializer.SerializeAsync(output,new{marker,nodes=nodes.Select(n=>new{labels=n.Labels,properties=n.Properties}),links=links.Select(r=>r.Values)});output.Flush(true);
        }
        using var tx=await session.BeginTransactionAsync();
        foreach(var node in nodes){
            var removed=await(await tx.RunAsync("MATCH (n) WHERE labels(n)=$labels AND properties(n)=$properties DETACH DELETE n RETURN count(n) AS count",new{labels=node.Labels.ToArray(),properties=node.Properties.ToDictionary(p=>p.Key,p=>p.Value)})).SingleAsync();
            Assert.AreEqual(1L,removed["count"].As<long>());
        }
        await tx.CommitAsync();
        Assert.AreEqual(0L,(await(await session.RunAsync("MATCH (n) WHERE n:Avatar OR n:AvatarDetail OR n:Holon OR n:OasisRecordVersion RETURN count(n) AS count")).SingleAsync())["count"].As<long>());
    }
}
