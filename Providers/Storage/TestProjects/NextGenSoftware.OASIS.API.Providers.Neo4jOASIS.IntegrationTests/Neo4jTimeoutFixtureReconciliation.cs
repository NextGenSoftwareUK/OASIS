using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class Neo4jTimeoutFixtureReconciliation
{
    [TestMethod]
    public async Task ArchiveExactReviewedTimeoutFixturesBeforeRemoval()
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        var expected=new Dictionary<string,string>{
            ["credential-policies.db"]="C477951BDB1DCDD9EAEC1F1F525E43AC8821F0C4B267B644FE551E9323C2B252",
            ["routing.db"]="699C7083EC8EE12DDF0DAB825AAD0173ABBE077E7DBFEFF1420834C20CD528E8",
            ["neo4j-native-current.trx"]="198996D9B4D28DC3A7B6EE780C03AAF00510A37A301A063DA80B58BC3F4B2E3C"};
        string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        foreach(var file in expected)Assert.AreEqual(file.Value,Hash(Path.Combine(work,file.Key)),"Reviewed evidence changed; refuse cleanup.");
        const string avatar="b2f93650-0e22-408b-b347-a7b5a0c6d4b0",holon="4517b83b-e66a-4511-a2cc-f109a1a540b9";
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var session=driver.AsyncSession();
        var rows=await (await session.RunAsync("MATCH (n) RETURN labels(n) AS labels,n.Id AS id,n.SnapshotJson AS snapshot")).ToListAsync();
        Assert.AreEqual(3,rows.Count);
        var snapshots=new List<object>();var identities=new HashSet<string>();
        foreach(var row in rows){
            var labels=row["labels"].As<List<string>>();Assert.AreEqual(1,labels.Count);var label=labels.Single();var id=row["id"].As<string>();
            Assert.IsTrue(identities.Add(label+":"+id));
            Assert.IsTrue(label is "Avatar" or "AvatarDetail"?id==avatar:label=="Holon"&&id==holon);
            var snapshot=row["snapshot"].As<string>();
            snapshots.Add(new {label,id,snapshot});
        }
        CollectionAssert.AreEquivalent(new[]{"Avatar:"+avatar,"AvatarDetail:"+avatar,"Holon:"+holon},identities.ToArray());
        var archive=Path.Combine(work,"reviewed-timeout-fixtures-archive.json");Assert.IsFalse(File.Exists(archive));
        using(var output=new FileStream(archive,FileMode.CreateNew,FileAccess.Write,FileShare.None)){
            await JsonSerializer.SerializeAsync(output,new {reason="Reviewed local SDK timeout fixture; not native chain recovery",expected,snapshots});output.Flush(true);
        }
        using(var verified=JsonDocument.Parse(File.ReadAllBytes(archive)))Assert.AreEqual(3,verified.RootElement.GetProperty("snapshots").GetArrayLength());
        // Preserve the exact snapshot predicate during deletion; no broad database reset.
        foreach(var row in rows){
            var deleted=await (await session.RunAsync("MATCH (n {Id:$id}) WHERE labels(n)=$labels AND n.SnapshotJson=$snapshot DETACH DELETE n RETURN count(n) AS count",
                new {id=row["id"].As<string>(),labels=row["labels"].As<List<string>>(),snapshot=row["snapshot"].As<string>()})).SingleAsync();
            Assert.AreEqual(1L,deleted["count"].As<long>());
        }
        Assert.AreEqual(0L,(await (await session.RunAsync("MATCH (n) RETURN count(n) AS count")).SingleAsync())["count"].As<long>());
        foreach(var name in new[]{"credential-policies.db","routing.db"}){
            var path=Path.GetFullPath(Path.Combine(work,name));Assert.AreEqual(work,Path.GetDirectoryName(path));
            Assert.AreEqual(expected[name],Hash(path));File.Delete(path);
        }
    }
}
