using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass]
public sealed class CqlRetainedGraphInspectionTests
{
    [TestMethod]
    [TestCategory("Inspection")]
    public async Task TypedDriverValidatesRetainedCanonicalVersionsAndReceiptBindingsWithoutWrites()
    {
        var root = Environment.GetEnvironmentVariable("OASIS_CQL_GRAPH_INSPECTION_ROOT")
            ?? throw new InvalidOperationException("Use the explicit retained-graph inspection runner.");
        var manifest = Path.Combine(root, "LocalState/COSMIC/Neo4jCurrent/cql-v1-web4-fixture.json");
        var bytes = await File.ReadAllBytesAsync(manifest);
        const string expected = "892A8AF780ED445887ECBD6C191A7577B053872A13E5CF35FE592A56899894C5";
        Assert.AreEqual(expected, Convert.ToHexString(SHA256.HashData(bytes)));
        using var fixtureDocument = JsonDocument.Parse(bytes);
        var fixture = fixtureDocument.RootElement;
        var ids = new[] { "ids", "scalarIds", "restIds", "transportIds", "recoveryIds" }
            .SelectMany(group => fixture.GetProperty(group).EnumerateArray().Select(value => value.GetGuid())).ToHashSet();
        Assert.AreEqual(157, ids.Count);
        // scalarWrites contains provider:recordId seed acknowledgements, not
        // conditional operation IDs. Only sharingOperations owns these receipts.
        var operations = fixture.GetProperty("sharingOperations").EnumerateArray()
            .Select(value => value.GetGuid()).ToHashSet();

        // Direct reads deliberately avoid provider activation (which can own
        // schema writes). The typed driver carries canonical JSON losslessly.
        using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        await using var session = driver.AsyncSession(config => config.WithDefaultAccessMode(AccessMode.Read));
        var rows = await (await session.RunAsync("MATCH (h:Holon) RETURN h ORDER BY h.Id LIMIT 201")).ToListAsync();
        Assert.IsTrue(rows.Count <= 200, "Inspection budget exceeded; no truncated inventory claim.");
        var current = new Dictionary<Guid, Holon>();
        var snapshots = new List<object>();
        foreach (var row in rows)
        {
            var node = row["h"].As<INode>();
            var id = Guid.Parse(node["Id"].As<string>());
            Assert.IsTrue(ids.Contains(id), "Unexpected native identity; preserve all state.");
            var json = node["SnapshotJson"].As<string>();
            var holon = OasisJson.DeserializeStored<Holon>(json);
            Assert.IsNotNull(holon);
            Assert.AreEqual(id, holon.Id, "Canonical/native identity binding differs.");
            Assert.IsTrue(holon.Version >= 0);
            Assert.IsTrue(current.TryAdd(id, holon), "Duplicate native identity.");
            snapshots.Add(new { id, holon.Version, holon.VersionId, digest = Digest(json) });
        }
        var receiptRows = await (await session.RunAsync("MATCH (r:CosmicWriteReceipt) RETURN r ORDER BY r.OperationId LIMIT 501")).ToListAsync();
        Assert.IsTrue(receiptRows.Count <= 500, "Receipt inspection budget exceeded.");
        var receipts = new List<object>();
        var seenOperations = new HashSet<Guid>();
        foreach (var row in receiptRows)
        {
            var node = row["r"].As<INode>();
            var operation = Guid.Parse(node["OperationId"].As<string>());
            Assert.IsTrue(operations.Contains(operation), "Unexpected receipt operation.");
            Assert.IsTrue(seenOperations.Add(operation), "Duplicate receipt operation.");
            Assert.IsFalse(node.Properties.ContainsKey("__cosmicClaim"), "Unfinished native operation claim.");
            var json = node["ResultJson"].As<string>();
            // Validate the existing canonical receipt digest/graph contract,
            // then independently bind its envelope to the native operation.
            var acknowledged = ConditionalHolonReceipt.Read(json);
            using var document = JsonDocument.Parse(json);
            Assert.AreEqual(operation, document.RootElement.GetProperty("operation").GetGuid());
            Assert.AreEqual(node["Fingerprint"].As<string>(), document.RootElement.GetProperty("intent").GetString());
            foreach (var holon in acknowledged)
            {
                Assert.IsTrue(ids.Contains(holon.Id), "Receipt acknowledges an undeclared identity.");
                if (current.TryGetValue(holon.Id, out var live))
                    Assert.IsTrue(live.Version >= holon.Version, "Current canonical version precedes its durable acknowledgement.");
            }
            receipts.Add(new { operation, digest = Digest(json), roots = acknowledged.Select(h => new { h.Id, h.Version, h.VersionId }).ToArray() });
        }

        var path = Path.Combine(root, "TestResults/Neo4jCurrent/cql-retained-canonical-inspection.json");
        // CreateNew preserves prior evidence. The runner archives by digest.
        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(output, new
        {
            snapshotSha256 = expected, observedUtc = DateTime.UtcNow, declaredIds = ids.Count,
            snapshots, receipts, missingIds = ids.Except(current.Keys).OrderBy(id => id).ToArray(),
            canonicalVersionsVerified = true, receiptEnvelopeBindingsVerified = true,
            originalRequestFingerprintRecomputed = false, mongoOutcomesVerified = false,
            sqliteOutcomesVerified = false, cleanupAuthorized = false
        }, new JsonSerializerOptions { WriteIndented = true });
        CollectionAssert.AreEqual(bytes, await File.ReadAllBytesAsync(manifest), "Inspection changed the fixture manifest.");
    }

    private static string Digest(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
