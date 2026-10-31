using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class Neo4jCredentialFixtureReconciliation
{
    [TestMethod]
    public async Task ArchiveOnlyTheReviewedObsoleteCredentialHistoryFixture()
    {
        const string uri="bolt://127.0.0.1:17687";
        const string id="af1bc282-2a9e-47bb-b6ca-69f07fab4c25";
        const string marker="a094a17af9d14277b7099f8051a4abda";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        var report=Path.Combine(work,"neo4j-graph-stack-baseline-failed.trx");
        Assert.AreEqual("BF743621A2C88663DB77120F628F1E3F6D605DBDFBAA268B4F8A70BFC77F8213",
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(report))));
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var session=driver.AsyncSession();
        var rows=await(await session.RunAsync("MATCH (n) WHERE n:Avatar OR n:AvatarDetail OR n:Holon OR n:OasisRecordVersion RETURN n")).ToListAsync();
        Assert.AreEqual(2,rows.Count,"Only the exact reviewed current/avatar history pair is admitted.");
        var nodes=rows.Select(row=>row["n"].As<INode>()).ToArray();
        var current=nodes.Single(node=>node.Labels.SequenceEqual(new[]{"Avatar"}));
        var archive=nodes.Single(node=>node.Labels.SequenceEqual(new[]{"OasisRecordVersion"}));
        Assert.AreEqual(id,current.Properties["Id"]);Assert.AreEqual(id,archive.Properties["RecordId"]);
        Assert.AreEqual("Avatar",archive.Properties["Kind"]);Assert.AreEqual(1L,archive.Properties["Version"].As<long>());
        var live=JObject.Parse(current.Properties["SnapshotJson"].As<string>());
        var old=JObject.Parse(archive.Properties["SnapshotJson"].As<string>());
        foreach(var snapshot in new[]{live,old})
        {Assert.AreEqual(id,snapshot.Value<string>("Id"));Assert.AreEqual("history-"+marker,snapshot.Value<string>("Username"));Assert.IsFalse(snapshot.Value<bool>("IsDeleted"));}
        Assert.AreEqual(2,live.Value<int>("Version"));Assert.AreEqual(1,old.Value<int>("Version"));
        Assert.AreEqual(old.Value<string>("VersionId"),live.Value<string>("PreviousVersionId"));
        foreach(var name in new[]{"VerificationToken","ResetToken"})
        {Assert.AreEqual("current-"+marker,live.Value<string>(name));Assert.AreEqual("old-"+marker,old.Value<string>(name));}
        Assert.AreEqual(0L,(await(await session.RunAsync("MATCH (n)-[r]-() WHERE (n:Avatar AND n.Id=$id) OR (n:OasisRecordVersion AND n.RecordId=$id) RETURN count(r) AS count",new{id})).SingleAsync())["count"].As<long>(),
            "Do not discard unreviewed relationships.");
        var path=Path.GetFullPath(Path.Combine(work,"graph-stack-reviewed-credential-fixture.json"));
        Assert.AreEqual(work,Path.GetDirectoryName(path));Assert.IsFalse(File.Exists(path));
        using(var output=new FileStream(path,FileMode.CreateNew,FileAccess.Write,FileShare.None))
        {
            await System.Text.Json.JsonSerializer.SerializeAsync(output,new{reason="Exact local credential fixture retained by obsolete historical-selector expectations",id,marker,
                nodes=nodes.Select(node=>new{labels=node.Labels,properties=node.Properties}).ToArray()});
            output.Flush(true);
        }
        using(var proof=JsonDocument.Parse(File.ReadAllBytes(path)))Assert.AreEqual(2,proof.RootElement.GetProperty("nodes").GetArrayLength());
        var tx=await session.BeginTransactionAsync();
        try
        {
            foreach(var node in nodes)
            {
                var row=await(await tx.RunAsync("MATCH (n) WHERE labels(n)=$labels AND properties(n)=$properties DETACH DELETE n RETURN count(n) AS count",
                    new{labels=node.Labels.ToArray(),properties=node.Properties.ToDictionary(pair=>pair.Key,pair=>pair.Value)})).SingleAsync();
                Assert.AreEqual(1L,row["count"].As<long>());
            }
            await tx.CommitAsync();
        }
        finally{await tx.DisposeAsync();}
    }
}
