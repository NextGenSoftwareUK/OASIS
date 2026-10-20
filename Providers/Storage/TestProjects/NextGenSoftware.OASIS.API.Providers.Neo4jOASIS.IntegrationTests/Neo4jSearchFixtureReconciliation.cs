using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class Neo4jSearchFixtureReconciliation
{
    [TestMethod]
    public async Task ArchiveOnlyExactNavigationSearchFixtures()
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        var report=Path.GetFullPath(Path.Combine(work,"../CosmicMongoCurrent/neo-search-selection-focused-before.trx"));
        const string reportHash="5F711C9384011BF23A87E86A11B8DA9C870946EED01BA5F1C0E648D84DDC9697";
        Assert.AreEqual(reportHash,Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(report))));
        var cohorts=new[]{new[]{"22f954dd-e74e-455e-9017-1e3f0144d2a2","9153a60a-99a4-496e-888d-704bbc25a110","8c0c1f56-ee0d-4770-a70a-04ea05f6926b"},
            new[]{"0c1fe862-5f17-44d0-954a-535eadcd4017","87f783c6-3762-49f6-8137-1c866309a8c3","b0535a97-bb92-43cc-a5c1-982669c55d08"}};
        var ids=cohorts.SelectMany(value=>value).ToArray();
        var operations=new[]{"e76e7e29-7d2f-45dd-bff6-d1f4287ae530","4f1d43b6-493b-4f5b-ab44-2560798e6dc0","9e4e68c9-859b-4bf5-af75-f4c7726d5850",
            "7010e53a-33cd-43ba-a8a1-0fae42dfd5c5","70ce65fa-201d-423c-9e8e-5d038daba6ee","f4f0b7d7-cf13-4edc-85a8-0872d8da1604"};
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var session=driver.AsyncSession();
        var nodes=(await(await session.RunAsync("MATCH (n:Holon) WHERE n.Id IN $ids RETURN n",new{ids})).ToListAsync()).Select(row=>row["n"].As<Neo4j.Driver.INode>()).ToArray();
        var receipts=(await(await session.RunAsync("MATCH (n:CosmicWriteReceipt) WHERE n.OperationId IN $operations RETURN n",new{operations})).ToListAsync()).Select(row=>row["n"].As<Neo4j.Driver.INode>()).ToArray();
        Assert.AreEqual(6,nodes.Length);Assert.AreEqual(6,receipts.Length);
        CollectionAssert.AreEquivalent(ids,nodes.Select(node=>node["Id"].As<string>()).ToArray());
        CollectionAssert.AreEquivalent(operations,receipts.Select(node=>node["OperationId"].As<string>()).ToArray());
        var acknowledged=new Dictionary<(Guid,int),IHolon>();
        foreach(var receipt in receipts)
            foreach(var node in HolonGraphWritePlan.Create(ConditionalHolonReceipt.Read(receipt["ResultJson"].As<string>()),true,true,0,0).Nodes)
                Assert.IsTrue(acknowledged.TryAdd((node.Snapshot.Id,node.Snapshot.Version),node.Snapshot));
        var expected=new HashSet<(Guid,int)>();
        foreach(var cohort in cohorts)
        {
            var parent=Guid.Parse(cohort[0]);var root=Guid.Parse(cohort[1]);var child=Guid.Parse(cohort[2]);
            foreach(var item in new[]{(parent,1),(root,1),(root,2),(root,3),(child,1),(child,2)})expected.Add(item);
            var parentOwner=acknowledged[(parent,1)].ParentHolonId;
            Assert.AreEqual(acknowledged[(parent,1)].CreatedByAvatarId,parentOwner);
            foreach(var (id,name,version,parentId) in new[]{(parent,"query parent",1,parentOwner),(root,"scalar-only owner",3,parent),(child,"query member edited",2,root)})
            {
                var raw=nodes.Single(node=>node["Id"].As<string>()==id.ToString());
                CollectionAssert.AreEqual(new[]{"Holon"},raw.Labels.ToArray());
                var value=StoredRecordJson.DeserializeStored<Holon>(raw["SnapshotJson"].As<string>());
                Assert.AreEqual(id,value.Id);Assert.AreEqual(name,value.Name);Assert.AreEqual(version,value.Version);Assert.AreEqual(parentId,value.ParentHolonId);
                Assert.AreEqual(value.VersionId,acknowledged[(id,version)].VersionId);
                for(var v=2;v<=version;v++)Assert.AreEqual(acknowledged[(id,v-1)].VersionId,acknowledged[(id,v)].PreviousVersionId);
            }
        }
        Assert.IsTrue(expected.SetEquals(acknowledged.Keys));
        const string edgeQuery="MATCH (p)-[r]->(c) WHERE p.Id IN $ids OR c.Id IN $ids RETURN p.Id AS parent,c.Id AS child,type(r) AS kind,properties(r) AS properties";
        var edges=await(await session.RunAsync(edgeQuery,new{ids})).ToListAsync();Assert.AreEqual(4,edges.Count);
        CollectionAssert.AreEquivalent(cohorts.SelectMany(c=>new[]{c[0]+":"+c[1],c[1]+":"+c[2]}).ToArray(),edges.Select(edge=>edge["parent"].As<string>()+":"+edge["child"].As<string>()).ToArray());
        Assert.IsTrue(edges.All(edge=>edge["kind"].As<string>()=="HAS_CHILD"));
        Assert.AreEqual(0L,(await(await session.RunAsync("MATCH (n:OasisRecordVersion) WHERE n.RecordId IN $ids RETURN count(n) AS count",new{ids})).SingleAsync())["count"].As<long>());
        var archive=Path.Combine(work,"typed-search-navigation-fixtures.json");Assert.IsFalse(File.Exists(archive));
        await using(var file=new FileStream(archive,FileMode.CreateNew,FileAccess.Write,FileShare.None))
        {
            await JsonSerializer.SerializeAsync(file,new{reportHash,nodes=nodes.Select(node=>new{labels=node.Labels,properties=node.Properties}),
                receipts=receipts.Select(node=>new{labels=node.Labels,properties=node.Properties}),
                edges=edges.Select(edge=>new{parent=edge["parent"].As<string>(),child=edge["child"].As<string>(),kind=edge["kind"].As<string>(),properties=edge["properties"].As<Dictionary<string,object>>()})});
            file.Flush(true);
        }
        using(var proof=JsonDocument.Parse(File.ReadAllBytes(archive)))
        {Assert.AreEqual(6,proof.RootElement.GetProperty("nodes").GetArrayLength());Assert.AreEqual(6,proof.RootElement.GetProperty("receipts").GetArrayLength());Assert.AreEqual(4,proof.RootElement.GetProperty("edges").GetArrayLength());}
        var tx=await session.BeginTransactionAsync();
        try
        {
            var current=await(await tx.RunAsync(edgeQuery,new{ids})).ToListAsync();Assert.AreEqual(4,current.Count);
            foreach(var edge in edges)
            {
                var match=current.Single(row=>row["parent"].As<string>()==edge["parent"].As<string>() && row["child"].As<string>()==edge["child"].As<string>());
                Assert.AreEqual(edge["kind"].As<string>(),match["kind"].As<string>());
                Assert.AreEqual(JsonSerializer.Serialize(edge["properties"].As<Dictionary<string,object>>()),JsonSerializer.Serialize(match["properties"].As<Dictionary<string,object>>()));
            }
            foreach(var node in nodes.Concat(receipts))
            {
                var label=node.Labels.Single();Assert.IsTrue(label is "Holon" or "CosmicWriteReceipt");
                var key=label=="Holon" ? "Id":"OperationId";
                var removed=await(await tx.RunAsync($"MATCH (n:{label} {{{key}:$id}}) WHERE labels(n)=$labels AND properties(n)=$properties DETACH DELETE n RETURN count(n) AS count",
                    new{id=node[key].As<string>(),labels=node.Labels.ToArray(),properties=node.Properties.ToDictionary(pair=>pair.Key,pair=>pair.Value)})).SingleAsync();
                Assert.AreEqual(1L,removed["count"].As<long>());
            }
            await tx.CommitAsync();
        }
        finally{await tx.DisposeAsync();}
        Console.WriteLine("Archived exact typed search fixtures SHA256="+Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(archive)))+"; no writes replayed.");
    }
}
