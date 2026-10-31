using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class Neo4jReadFrontierFixtureReconciliation
{
    [TestMethod]
    public async Task ArchiveOnlyTheReviewedExhaustedFrontierFixture()
    {
        const string uri="bolt://127.0.0.1:17687";
        const string root="1fbeae3b-852f-4efc-9185-4575a452637b",child="ab1acc6e-d855-4b9e-9a29-4eeb2c6596f1",leaf="9af6e34b-795d-4896-8342-c3eb81f335a2";
        var ids=new[]{root,child,leaf};
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("EB6F27E9DF11A94C5D9B12246624A32289C39227D3461F29391C20C87729A38A",
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(work,"read-depth-full-before-frontier-assertion.trx")))));
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var session=driver.AsyncSession();
        var rows=await(await session.RunAsync("MATCH (n) WHERE n:Holon OR n:Avatar OR n:AvatarDetail OR n:OasisRecordVersion RETURN n")).ToListAsync();
        Assert.AreEqual(3,rows.Count);
        var nodes=rows.Select(row=>row["n"].As<INode>()).ToArray();
        CollectionAssert.AreEquivalent(ids,nodes.Select(node=>node.Properties["Id"].As<string>()).ToArray());
        foreach(var node in nodes)
        {
            CollectionAssert.AreEqual(new[]{"Holon"},node.Labels.ToArray());
            var id=node.Properties["Id"].As<string>();var snapshot=JObject.Parse(node.Properties["SnapshotJson"].As<string>());
            Assert.AreEqual(id,snapshot.Value<string>("Id"));Assert.IsFalse(snapshot.Value<bool>("IsDeleted"));
            Assert.AreEqual(id==root ? Guid.Empty.ToString():id==child ? root:child,snapshot.Value<string>("ParentHolonId"));
            Assert.AreEqual(id==child ? "Avatar":"Holon",node.Properties["HolonType"].As<string>());
            Assert.IsTrue(string.IsNullOrEmpty(snapshot.Value<string>("Name")));
            if(id==root)Assert.AreEqual("root",snapshot["MetaData"]?["selected"]?.Value<string>());
        }
        const string edgeQuery="MATCH (p)-[r]->(c) WHERE p.Id IN $ids OR c.Id IN $ids RETURN p.Id AS parent,c.Id AS child,type(r) AS kind,properties(r) AS properties";
        var edges=await(await session.RunAsync(edgeQuery,new{ids})).ToListAsync();
        Assert.AreEqual(2,edges.Count);
        CollectionAssert.AreEquivalent(new[]{root+":"+child,child+":"+leaf},edges.Select(edge=>edge["parent"].As<string>()+":"+edge["child"].As<string>()).ToArray());
        Assert.IsTrue(edges.All(edge=>edge["kind"].As<string>()=="HAS_CHILD"));
        var path=Path.GetFullPath(Path.Combine(work,"read-depth-reviewed-frontier-fixture.json"));
        Assert.AreEqual(work,Path.GetDirectoryName(path));Assert.IsFalse(File.Exists(path));
        using(var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None))
        {
            await System.Text.Json.JsonSerializer.SerializeAsync(output,new{reason="Reviewed local read-frontier fixture retained by obsolete one-extra-level assertion",
                nodes=nodes.Select(node=>new{labels=node.Labels,properties=node.Properties}).ToArray(),
                edges=edges.Select(edge=>new{parent=edge["parent"].As<string>(),child=edge["child"].As<string>(),kind=edge["kind"].As<string>(),properties=edge["properties"].As<Dictionary<string,object>>()} ).ToArray()});output.Flush(true);
        }
        using(var proof=JsonDocument.Parse(File.ReadAllBytes(path)))
        {Assert.AreEqual(3,proof.RootElement.GetProperty("nodes").GetArrayLength());Assert.AreEqual(2,proof.RootElement.GetProperty("edges").GetArrayLength());}
        var tx=await session.BeginTransactionAsync();
        try
        {
            var current=await(await tx.RunAsync(edgeQuery,new{ids})).ToListAsync();
            Assert.AreEqual(2,current.Count);
            foreach(var edge in edges)
            {
                var match=current.Single(row=>row["parent"].As<string>()==edge["parent"].As<string>() && row["child"].As<string>()==edge["child"].As<string>());
                Assert.AreEqual(edge["kind"].As<string>(),match["kind"].As<string>());
                Assert.AreEqual(System.Text.Json.JsonSerializer.Serialize(edge["properties"].As<Dictionary<string,object>>()),System.Text.Json.JsonSerializer.Serialize(match["properties"].As<Dictionary<string,object>>()));
            }
            foreach(var node in nodes)
            {
                var deleted=await(await tx.RunAsync("MATCH (n:Holon {Id:$id}) WHERE labels(n)=$labels AND properties(n)=$properties DETACH DELETE n RETURN count(n) AS count",
                    new{id=node.Properties["Id"].As<string>(),labels=node.Labels.ToArray(),properties=node.Properties.ToDictionary(pair=>pair.Key,pair=>pair.Value)})).SingleAsync();
                Assert.AreEqual(1L,deleted["count"].As<long>());
            }
            await tx.CommitAsync();
        }
        finally{await tx.DisposeAsync();}
    }
}
