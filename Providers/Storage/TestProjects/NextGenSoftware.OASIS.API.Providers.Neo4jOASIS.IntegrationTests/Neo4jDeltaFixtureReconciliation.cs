using System.Security.Cryptography;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class Neo4jDeltaFixtureReconciliation
{
    [TestMethod]
    public async Task InspectAndArchiveExactCreationFixturesBeforeRemoval()
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        var report=Path.Combine(work,"neo-delta-creation-path-red.trx");
        Assert.AreEqual("ADC7C4F0447B492857185EA784F699E67CEA68C9244B0AA6A5DEFE643695268E",Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(report))));
        var xml=XDocument.Load(report);XNamespace ns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        var failed=xml.Descendants(ns+"UnitTestResult").Where(row=>(string?)row.Attribute("outcome")=="Failed").ToArray();
        Assert.AreEqual(4,failed.Length);
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var session=driver.AsyncSession();
        using var tx=await session.BeginTransactionAsync();var archive=new List<object>();
        var allNodes=new List<INode>();var allReceipts=new List<INode>();
        foreach(var failure in failed)
        {
            StringAssert.StartsWith((string)failure.Attribute("testName")!,"NativeCosmicConditionalGraphReceiptsAndRaces (");
            var output=failure.Descendants(ns+"StdOut").Single().Value;
            var fixture=JObject.Parse(output.Split("Owned Neo conditional fixture: ",StringSplitOptions.None)[1].Trim());
            Assert.IsFalse(fixture.Value<bool>("complete"));
            var ids=fixture["ids"]!.Values<string>().ToArray();var operations=fixture["operations"]!.Values<string>().ToArray();
            Assert.AreEqual(2,ids.Length);Assert.AreEqual(1,operations.Length);var actor=fixture.Value<string>("actor");
            var rows=await(await tx.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids SET h.__reviewLock=true REMOVE h.__reviewLock RETURN h",new{ids})).ToListAsync();
            Assert.AreEqual(2,rows.Count);var nodes=rows.Select(row=>row["h"].As<INode>()).ToArray();
            var receipt=(await(await tx.RunAsync("MATCH (r:CosmicWriteReceipt {OperationId:$operation}) SET r.__reviewLock=true REMOVE r.__reviewLock RETURN r",new{operation=operations[0]})).SingleAsync())["r"].As<INode>();
            var packet=JObject.Parse(receipt.Properties["ResultJson"].As<string>());Assert.AreEqual(4,packet.Value<int>("format"));
            var delta=HolonCommittedDelta.ReadStored(packet.Value<string>("delta"));Assert.AreEqual(actor,delta.ActorId.ToString());Assert.AreEqual(operations[0],delta.OperationId.ToString());
            var root=ConditionalHolonReceipt.Read(receipt.Properties["ResultJson"].As<string>()).Single();var child=root.Children.Single();
            CollectionAssert.AreEquivalent(ids,new[]{root.Id.ToString(),child.Id.ToString()});
            Assert.AreEqual("owned neo conditional root",root.Name);Assert.AreEqual("owned neo conditional child",child.Name);
            var records=HolonGraphWritePlan.Create(new[]{root},true,true,0,0).Nodes.ToDictionary(node=>node.Source.Id,node=>node.Source);
            foreach(var node in nodes)
            {
                CollectionAssert.AreEqual(new[]{"Holon"},node.Labels.ToArray());var id=Guid.Parse(node.Properties["Id"].As<string>());
                var current=OasisJson.DeserializeStored<Holon>(node.Properties["SnapshotJson"].As<string>());
                Assert.AreEqual(id,current.Id);Assert.AreEqual(1,current.Version);Assert.AreEqual(Guid.Parse(actor!),current.CreatedByAvatarId);
                Assert.IsFalse(current.IsDeleted);Assert.AreEqual(records[id].VersionId,current.VersionId);
                Assert.AreEqual(records[id].Name,current.Name);
            }
            var edges=await(await tx.RunAsync("MATCH (p)-[r]->(c) WHERE p.Id IN $ids OR c.Id IN $ids RETURN p.Id AS parent,c.Id AS child,type(r) AS kind",new{ids})).ToListAsync();
            Assert.AreEqual(1,edges.Count);Assert.AreEqual(root.Id.ToString(),edges[0]["parent"].As<string>());Assert.AreEqual(child.Id.ToString(),edges[0]["child"].As<string>());Assert.AreEqual("HAS_CHILD",edges[0]["kind"].As<string>());
            Assert.AreEqual(0L,(await(await tx.RunAsync("MATCH (v:OasisRecordVersion) WHERE v.RecordId IN $ids RETURN count(v) AS count",new{ids})).SingleAsync())["count"].As<long>());
            archive.Add(new{fixture,nodes=nodes.Select(node=>node.Properties).ToArray(),receipt=receipt.Properties});allNodes.AddRange(nodes);allReceipts.Add(receipt);
        }
        var path=Path.Combine(work,"neo-delta-reviewed-creation-fixtures.json");Assert.IsFalse(File.Exists(path));
        using(var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None))
        {await System.Text.Json.JsonSerializer.SerializeAsync(output,archive);output.Flush(true);}
        foreach(var node in allNodes.Concat(allReceipts))
        {
            var removed=await(await tx.RunAsync("MATCH (n) WHERE id(n)=$id AND properties(n)=$properties DETACH DELETE n RETURN count(n) AS count",new{id=node.Id,properties=node.Properties.ToDictionary(pair=>pair.Key,pair=>pair.Value)})).SingleAsync();
            Assert.AreEqual(1L,removed["count"].As<long>());
        }
        await tx.CommitAsync();
    }
}
