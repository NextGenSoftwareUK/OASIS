using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class NeoOrdinaryRoleSeedReconciliation
{
    [TestMethod]
    public async Task ReviewedOrdinaryRoleSeedWasRejectedBeforeAnyNativeWrite()
    {
        Assert.AreEqual("bolt://127.0.0.1:17687", Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work = Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!;
        var marker = Path.Combine(work, "cql-graph-fixture.json");
        var bytes = await File.ReadAllBytesAsync(marker);
        Assert.AreEqual("F2AC06ABA5ECAD50567DDD0D546A66620AEF71E05F434B63F90250BD63D495B0",
            Convert.ToHexString(SHA256.HashData(bytes)));
        var report = Path.Combine(work, "ordinary-admission-original-red.trx");
        Assert.AreEqual("7B548E5C6C30AB8F4491CFD02781D21E9BCCB32B1477D77EE1B8E9B0FE8B60E5",
            Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(report))));
        var xml = XDocument.Load(report);
        XNamespace ns = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";
        var failure = xml.Descendants(ns + "UnitTestResult").Single(row =>
            (string?)row.Attribute("testName") == "NativeDescendantDoesNotTraverseAnUnlistedDeclaredBranch");
        Assert.AreEqual("Failed", (string?)failure.Attribute("outcome"));
        StringAssert.Contains(failure.Element(ns + "Output")!.Element(ns + "ErrorInfo")!.Element(ns + "Message")!.Value,
            "Persisted child-role membership must be fully observed");
        using var manifest = JsonDocument.Parse(bytes);
        Assert.AreEqual("before-create", manifest.RootElement.GetProperty("stage").GetString());
        Assert.AreEqual(0, manifest.RootElement.GetProperty("operations").GetArrayLength());
        var ids = manifest.RootElement.GetProperty("ids").EnumerateArray().Select(value => value.GetString()!).ToArray();
        CollectionAssert.AreEquivalent(new[] { "7be80d01-dfe5-4086-9807-b60437692491", "e1913fc0-5ab9-44bf-8a9f-aee71d027c54",
            "4bc390ad-5d18-4668-bf67-16961a514c92", "c54b1bcf-89e3-41b4-bb78-a9b5d2b53d72" }, ids);
        using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var session = driver.AsyncSession();
        var nodes = await (await session.RunAsync("MATCH (n) WHERE n.Id IN $ids OR n.RecordId IN $ids OR n.ParentHolonId IN $ids "
            + "OR any(id IN $ids WHERE n.SnapshotJson CONTAINS id OR n.ResultJson CONTAINS id) RETURN count(n) AS count", new { ids })).SingleAsync();
        Assert.AreEqual(0L, nodes["count"].As<long>());
        var edges = await (await session.RunAsync("MATCH ()-[r]->() WHERE r.f_Id IN $ids OR r.f_ParentHolonId IN $ids RETURN count(r) AS count", new { ids })).SingleAsync();
        Assert.AreEqual(0L, edges["count"].As<long>());
        // No database deletion is necessary or authorized by this proof: nothing was written.
        await File.WriteAllBytesAsync(Path.Combine(work, "ordinary-role-unwritten-marker.json"), bytes);
        await File.WriteAllTextAsync(Path.Combine(work, "ordinary-role-unwritten-proof.json"),
            JsonSerializer.Serialize(new { ids, nodes = 0, edges = 0, noMutationSubmitted = true }));
        Assert.AreEqual(Convert.ToHexString(SHA256.HashData(bytes)),
            Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(marker))));
        File.Delete(marker);
    }
}