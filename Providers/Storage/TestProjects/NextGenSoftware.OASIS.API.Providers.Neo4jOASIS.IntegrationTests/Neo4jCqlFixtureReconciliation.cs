using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class Neo4jCqlFixtureReconciliation
{
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    [TestMethod]
    public Task ReviewedGuidEmptyCompilerFailureFixtureIsUnchangedBeforeExactCleanup()
        => Reconcile("53ADBD3857DDA6FB0CCC23ED7927DE6261EA04AAD78E89393AC5EB6D324A7657",
            "does not invoke methods, getters or arbitrary client expressions", "cql-guid-empty-original-failure.trx",
            "cql-guid-empty-reviewed-fixture.json", "cql-guid-empty-preserved-inspection.csv");

    [TestMethod]
    public Task ReviewedIntegerFieldCompilerFailureFixtureIsUnchangedBeforeExactCleanup()
        => Reconcile("69442A3CCAD6FA2FB80B1FA39221670AD8DDF5F19617435C8F0AEC9EB90FF756",
            "field comparisons require the same underlying CLR scalar type", "cql-integer-field-original-failure.trx",
            "cql-reviewed-fixture.json", "cql-preserved-inspection.csv");

    [TestMethod]
    public async Task ReviewedInterruptedEnumFixtureIsUnchangedBeforeExactCleanup()
    {
        Assert.AreEqual("bolt://127.0.0.1:17687",Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!;
        var reviewPath=Path.Combine(work,"cql-enum-interrupted-reviewed-fixture.json");
        Assert.AreEqual("AC8C07837842A0F5997B1F3AE72D9CB7DF8657D5090CFCF771D3335DCC31F25E",Hash(await File.ReadAllBytesAsync(reviewPath)));
        using var review=JsonDocument.Parse(await File.ReadAllTextAsync(reviewPath));var root=review.RootElement;
        Assert.AreEqual("interrupted-run-no-fresh-native-report",root.GetProperty("reason").GetString());
        var manifestPath=Path.Combine(work,"cql-native-fixture.json");
        Assert.AreEqual(root.GetProperty("manifestSha256").GetString(),Hash(await File.ReadAllBytesAsync(manifestPath)));
        Assert.AreEqual(root.GetProperty("inspectionSha256").GetString(),Hash(await File.ReadAllBytesAsync(Path.Combine(work,"cql-enum-interrupted-inspection.csv"))));
        Assert.AreEqual(root.GetProperty("runnerLogSha256").GetString(),Hash(await File.ReadAllBytesAsync(Path.Combine(work,"../CoreCurrent/neo-cql-enum-native-final-runner.log"))));
        Assert.IsTrue(File.GetLastWriteTimeUtc(Path.Combine(work,"neo4j-cql-regression-current.trx"))<File.GetLastWriteTimeUtc(manifestPath),"No fresh original completion report was published.");
        await CleanupReviewedFixture(work,manifestPath,root);
    }

    private async Task Reconcile(string reportHash,string expectedFailure,string reportFile,string reviewFile,string inspectionFile)
    {
        const string uri="bolt://127.0.0.1:17687";
        Assert.AreEqual(uri,Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work=Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!;
        var manifestPath=Path.Combine(work,"cql-native-fixture.json");
        using var review=JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(work,reviewFile)));
        var root=review.RootElement;
        Assert.AreEqual(reportHash,root.GetProperty("sourceReportSha256").GetString());
        var reportPath=Path.Combine(work,reportFile);
        Assert.AreEqual(reportHash,Hash(await File.ReadAllBytesAsync(reportPath)));
        Assert.AreEqual(root.GetProperty("manifestSha256").GetString(),Hash(await File.ReadAllBytesAsync(manifestPath)));
        Assert.AreEqual(root.GetProperty("inspectionSha256").GetString(),Hash(await File.ReadAllBytesAsync(Path.Combine(work,inspectionFile))));
        var report=XDocument.Load(reportPath);
        var failed=report.Descendants().Where(node=>node.Name.LocalName=="UnitTestResult" && (string?)node.Attribute("outcome")=="Failed").Single();
        Assert.AreEqual("NativeCosmicCqlPredicatesProjectionUpdatesAndHistoricalUpgrade",(string?)failed.Attribute("testName"));
        StringAssert.Contains(failed.Descendants().Single(node=>node.Name.LocalName=="Message").Value,expectedFailure);
        await CleanupReviewedFixture(work,manifestPath,root);
    }

    private static async Task CleanupReviewedFixture(string work,string manifestPath,JsonElement root)
    {
        const string uri="bolt://127.0.0.1:17687";
        var expected=root.GetProperty("records").EnumerateArray().ToDictionary(row=>row.GetProperty("id").GetString()!);
        Assert.AreEqual(5,expected.Count);
        using var manifest=JsonDocument.Parse(await File.ReadAllTextAsync(manifestPath));
        Assert.AreEqual("before-create",manifest.RootElement.GetProperty("stage").GetString());
        CollectionAssert.AreEquivalent(expected.Keys.ToArray(),manifest.RootElement.GetProperty("ids").EnumerateArray().Select(value=>value.GetString()).ToArray());
        using var driver=GraphDatabase.Driver(uri,AuthTokens.Basic("neo4j","oasis-local-test-only"));
        using var session=driver.AsyncSession();
        using var transaction=await session.BeginTransactionAsync(config=>config.WithTimeout(TimeSpan.FromSeconds(30)));
        var ids=expected.Keys.ToArray();
        var rows=await(await transaction.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids SET h.__oasisHolonLock=true REMOVE h.__oasisHolonLock "
            +"WITH h OPTIONAL MATCH (h)-[p:HAS_CQL_PROJECTION]->(h) RETURN h,collect(p) AS projections",new{ids})).ToListAsync();
        Assert.AreEqual(5,rows.Count);
        foreach(var row in rows)
        {
            var node=row["h"].As<INode>();var id=node["Id"].As<string>();var original=expected[id];
            var snapshot=node["SnapshotJson"].As<string>();
            Assert.AreEqual(original.GetProperty("snapshotSha256").GetString(),Hash(Encoding.UTF8.GetBytes(snapshot)));
            Assert.AreEqual(original.GetProperty("name").GetString(),node["Name"].As<string>());
            Assert.AreEqual(original.GetProperty("revision").GetString(),node["ProviderRevisionId"].As<string>());
            using var body=JsonDocument.Parse(snapshot);
            Assert.AreEqual(id,body.RootElement.GetProperty("Id").GetString());
            Assert.AreEqual(1,body.RootElement.GetProperty("Version").GetInt32());
            Assert.AreEqual(original.GetProperty("versionId").GetString(),body.RootElement.GetProperty("VersionId").GetString());
            var projections=row["projections"].As<List<IRelationship>>();Assert.AreEqual(1,projections.Count);
            Assert.AreEqual(1L,projections[0]["SchemaVersion"].As<long>());
            Assert.AreEqual(id,projections[0]["f_Id"].As<string>());
            Assert.AreEqual(node["ProviderRevisionId"],projections[0]["ProviderRevisionId"]);
        }
        async Task<long> Count(string query)=>(await(await transaction.RunAsync(query,new{ids})).SingleAsync())["count"].As<long>();
        Assert.AreEqual(0L,await Count("MATCH (h:Holon)-[r]-(other) WHERE h.Id IN $ids AND (type(r)<>'HAS_CQL_PROJECTION' OR startNode(r)<>endNode(r)) RETURN count(r) AS count"));
        Assert.AreEqual(0L,await Count("MATCH (v:OasisRecordVersion {Kind:'Holon'}) WHERE v.RecordId IN $ids RETURN count(v) AS count"));
        Assert.AreEqual(0L,await Count("MATCH (r:CosmicWriteReceipt) WHERE any(id IN $ids WHERE r.ResultJson CONTAINS id) RETURN count(r) AS count"));
        Assert.AreEqual(5L,await Count("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h RETURN count(h) AS count"));
        await transaction.CommitAsync();
        Assert.AreEqual(0L,(await(await session.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids RETURN count(h) AS count",new{ids})).SingleAsync())["count"].As<long>());
        File.Delete(manifestPath);
    }
}
