using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class Neo4jIdentityRollbackFixtureReconciliation
{
    [TestMethod]
    public async Task ArchiveOnlyReviewedConstraintRollbackFixtures()
    {
        const string uri="bolt://127.0.0.1:17687",constraint="oasis_owned_history_rollback_username";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);Assert.IsTrue(work.EndsWith(Path.Combine("TestResults","Neo4jCurrent"),StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual("B6D8083C906DD11E4A4F9CCFB6AE4F303952885168516BD6827250B307C03056",Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(work,"neo-identity-full-before-certification.trx")))));
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));using var session=driver.AsyncSession();
        var constraints=await(await session.RunAsync("SHOW CONSTRAINTS YIELD name,type,labelsOrTypes,properties WHERE name=$name RETURN name,type,labelsOrTypes,properties",new{name=constraint})).ToListAsync();
        Assert.AreEqual(1,constraints.Count);Assert.AreEqual("UNIQUENESS",constraints[0]["type"].As<string>());CollectionAssert.AreEqual(new[]{"Avatar"},constraints[0]["labelsOrTypes"].As<List<string>>());CollectionAssert.AreEqual(new[]{"Username"},constraints[0]["properties"].As<List<string>>());
        using var tx=await session.BeginTransactionAsync();
        var rows=await(await tx.RunAsync("MATCH (n) WHERE n:Avatar OR n:AvatarDetail OR n:OasisRecordVersion SET n.__reviewLock=true REMOVE n.__reviewLock RETURN n")).ToListAsync();
        Assert.AreEqual(2,rows.Count);var nodes=rows.Select(row=>row["n"].As<INode>()).ToArray();string? marker=null;var roles=new HashSet<string>();
        foreach(var node in nodes)
        {
            CollectionAssert.AreEqual(new[]{"Avatar"},node.Labels.ToArray());var record=OasisJson.DeserializeStored<Avatar>(node.Properties["SnapshotJson"].As<string>());
            var match=Regex.Match(record.Username,"^rollback-([ab])-([0-9a-f]{32})$");Assert.IsTrue(match.Success);Assert.IsTrue(roles.Add(match.Groups[1].Value));
            marker??=match.Groups[2].Value;Assert.AreEqual(marker,match.Groups[2].Value);Assert.AreEqual(record.Username,node.Properties["Username"].As<string>());
            Assert.AreEqual(record.Id.ToString(),node.Properties["Id"].As<string>());Assert.AreEqual(1,record.Version);Assert.AreNotEqual(Guid.Empty,record.VersionId);Assert.IsFalse(record.IsDeleted);
            if(match.Groups[1].Value=="a")Assert.AreEqual("original",record.FirstName);else Assert.IsTrue(string.IsNullOrEmpty(record.FirstName));
        }
        CollectionAssert.AreEquivalent(new[]{"a","b"},roles.ToArray());
        var ids=nodes.Select(node=>node.Properties["Id"].As<string>()).ToArray();
        Assert.AreEqual(0L,(await(await tx.RunAsync("MATCH (p)-[r]->(c) WHERE p.Id IN $ids OR c.Id IN $ids RETURN count(r) AS count",new{ids})).SingleAsync())["count"].As<long>());
        var archive=Path.Combine(work,"neo-identity-reviewed-rollback-fixture.json");Assert.IsFalse(File.Exists(archive));
        using(var output=new FileStream(archive,FileMode.CreateNew,FileAccess.Write,FileShare.None))
        {await System.Text.Json.JsonSerializer.SerializeAsync(output,new{marker,constraint,nodes=nodes.Select(node=>node.Properties).ToArray(),constraintType=constraints[0]["type"].As<string>()});output.Flush(true);}
        foreach(var node in nodes)
        {
            var removed=await(await tx.RunAsync("MATCH (n:Avatar {Id:$id}) WHERE properties(n)=$properties DETACH DELETE n RETURN count(n) AS count",new{id=node.Properties["Id"].As<string>(),properties=node.Properties.ToDictionary(pair=>pair.Key,pair=>pair.Value)})).SingleAsync();Assert.AreEqual(1L,removed["count"].As<long>());
        }
        await tx.CommitAsync();
        await(await session.RunAsync("DROP CONSTRAINT "+constraint)).ConsumeAsync();
        Assert.AreEqual(0,(await(await session.RunAsync("SHOW CONSTRAINTS YIELD name WHERE name=$name RETURN name",new{name=constraint})).ToListAsync()).Count);
    }
}
