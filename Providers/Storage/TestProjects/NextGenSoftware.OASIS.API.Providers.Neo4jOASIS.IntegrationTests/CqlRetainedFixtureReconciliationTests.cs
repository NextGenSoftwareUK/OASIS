using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass]
public sealed class CqlRetainedFixtureReconciliationTests
{
    [TestMethod]
    [TestCategory("Reconciliation")]
    public async Task ReconcileReviewedRetainedThreeDatabaseFixtureAfterArchivingEvidence()
    {
        var root = Environment.GetEnvironmentVariable("OASIS_CQL_STORAGE_INSPECTION_ROOT")!;
        Assert.AreEqual("892A8AF7-reviewed-fixture", Environment.GetEnvironmentVariable("OASIS_CQL_RECONCILE_FIXTURE"));
        var state = Path.Combine(root, "LocalState/COSMIC/Neo4jCurrent");
        var evidence = Path.Combine(root, "TestResults/Neo4jCurrent");
        var source = Path.Combine(state, "cql-v1-web4-fixture.json");
        var storageProof = Path.Combine(evidence, "cql-retained-storage-inspection.json");
        var graphProof = Path.Combine(evidence, "cql-retained-canonical-inspection.json");
        Assert.AreEqual("892A8AF780ED445887ECBD6C191A7577B053872A13E5CF35FE592A56899894C5", Hash(await File.ReadAllBytesAsync(source)));
        Assert.AreEqual("405C3E91DCDD2785E82A9BAC2D39B3724AD14DD3AC75A5E74BFE9A197324ADF3", Hash(await File.ReadAllBytesAsync(storageProof)));
        Assert.AreEqual("603441A27681EF5DB1F41A27E2D41FAF944D48433392D97E271E022DB65CC97B", Hash(await File.ReadAllBytesAsync(graphProof)));
        using var storageDocument = JsonDocument.Parse(await File.ReadAllBytesAsync(storageProof));
        using var graphDocument = JsonDocument.Parse(await File.ReadAllBytesAsync(graphProof));
        var proof = storageDocument.RootElement;
        Assert.IsTrue(proof.GetProperty("usageJournalBindingsVerified").GetBoolean());
        Assert.IsTrue(proof.GetProperty("collectionBoundariesVerified").GetBoolean());
        Assert.IsTrue(proof.GetProperty("fenceReceiptBindingsVerified").GetBoolean());
        Assert.IsTrue(proof.GetProperty("usageRows").EnumerateArray().All(row => row.GetProperty("state").GetString() == "settled" && row.GetProperty("settledAtPresent").GetBoolean()));
        var sqlite = Path.Combine(state, "cql-v1-web4.sqlite");
        var sqliteBytes = await File.ReadAllBytesAsync(sqlite);
        Assert.AreEqual("7E1C6FBF19A56E00923CDC34B478FC8AE4833FDBA96F1D20E936FA57586992B2", Hash(sqliteBytes));
        var archive = Path.Combine(state, "ReconciledFixture892A8AF7");
        Assert.IsFalse(Directory.Exists(archive), "Preserve prior/uncertain reconciliation; no automatic retry.");
        Directory.CreateDirectory(archive);
        var journal = Path.Combine(archive, "reconciliation.json");
        var removed = new List<string>();
        var graphCommitted = false;
        async Task Stage(string phase)
        {
            var next = journal + ".next";
            await using (var stream = new FileStream(next, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(stream, new { phase, utc = DateTime.UtcNow, graphCommitted, removedDatabases = removed });
                stream.Flush(true);
            }
            File.Move(next, journal, true);
        }
        await Stage("preflight-no-mutations");
        foreach (var file in new[] { source, storageProof, graphProof })
            await ArchiveBytes(Path.GetFileName(file) + ".gz", await File.ReadAllBytesAsync(file));
        await ArchiveBytes("local-sqlite-copy.sqlite.gz", sqliteBytes);

        var mongo = new MongoClient("mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000");
        var expectedCollections = proof.GetProperty("collectionInventory").EnumerateArray().ToArray();
        async Task<Dictionary<string, List<BsonDocument>>> ValidateMongo()
        {
            var observed = new Dictionary<string, List<BsonDocument>>();
            foreach (var db in new[] { "cosmic_cql_v1_web4_storage", "cosmic_cql_v1_web4_usage" })
            {
                using var names = await mongo.GetDatabase(db).ListCollectionNamesAsync();
                foreach (var collection in await names.ToListAsync())
                    observed.Add(db + "/" + collection, await mongo.GetDatabase(db).GetCollection<BsonDocument>(collection)
                        .Find(FilterDefinition<BsonDocument>.Empty).Sort(Builders<BsonDocument>.Sort.Ascending("_id")).Limit(1001).ToListAsync());
            }
            CqlRetainedStorageInspectionTests.VerifyReviewedBoundaries(observed.ToDictionary(pair => pair.Key, pair => pair.Value.Count));
            foreach (var expected in expectedCollections)
            {
                var key = expected.GetProperty("database").GetString() + "/" + expected.GetProperty("collection").GetString();
                var fingerprint = CqlRetainedStorageInspectionTests.FingerprintDocuments(observed[key]);
                Assert.AreEqual(expected.GetProperty("sha256").GetString(), fingerprint.Sha256, "Mongo content changed: " + key);
                Assert.AreEqual(expected.GetProperty("bytes").GetInt64(), fingerprint.Bytes);
            }
            return observed;
        }
        var mongoDocuments = await ValidateMongo();
        foreach (var collection in mongoDocuments)
        {
            using var bytes = new MemoryStream();
            foreach (var row in collection.Value) await bytes.WriteAsync(row.ToBson());
            await ArchiveBytes(collection.Key.Replace('/', '-') + ".bson.gz", bytes.ToArray());
        }

        using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        await using var session = driver.AsyncSession();
        await using var transaction = await session.BeginTransactionAsync();
        var nodes = await (await transaction.RunAsync("MATCH(h:Holon) RETURN h ORDER BY h.Id LIMIT 201")).ToListAsync();
        var receipts = await (await transaction.RunAsync("MATCH(r:CosmicWriteReceipt) RETURN r ORDER BY r.OperationId LIMIT 501")).ToListAsync();
        var expectedNodes = graphDocument.RootElement.GetProperty("snapshots").EnumerateArray().ToDictionary(row => row.GetProperty("id").GetString()!);
        var expectedReceipts = graphDocument.RootElement.GetProperty("receipts").EnumerateArray().ToDictionary(row => row.GetProperty("operation").GetString()!);
        Assert.AreEqual(expectedNodes.Count, nodes.Count);
        Assert.AreEqual(expectedReceipts.Count, receipts.Count);
        CollectionAssert.AreEquivalent(expectedNodes.Keys.ToArray(), nodes.Select(row => row["h"].As<INode>()["Id"].As<string>()).ToArray());
        CollectionAssert.AreEquivalent(expectedReceipts.Keys.ToArray(), receipts.Select(row => row["r"].As<INode>()["OperationId"].As<string>()).ToArray());
        foreach (var node in nodes.Select(row => row["h"].As<INode>()))
            Assert.AreEqual(expectedNodes[node["Id"].As<string>()].GetProperty("digest").GetString(), Hash(Encoding.UTF8.GetBytes(node["SnapshotJson"].As<string>())));
        foreach (var receipt in receipts.Select(row => row["r"].As<INode>()))
        {
            Assert.IsFalse(receipt.Properties.ContainsKey("__cosmicClaim"));
            Assert.AreEqual(expectedReceipts[receipt["OperationId"].As<string>()].GetProperty("digest").GetString(), Hash(Encoding.UTF8.GetBytes(receipt["ResultJson"].As<string>())));
            using var envelope = JsonDocument.Parse(receipt["ResultJson"].As<string>());
            Assert.AreEqual(receipt["Fingerprint"].As<string>(), envelope.RootElement.GetProperty("intent").GetString());
        }
        var history = await (await transaction.RunAsync("MATCH(v:OasisRecordVersion {Kind:'Holon'}) RETURN count(v) AS n")).SingleAsync();
        Assert.AreEqual(0L, history["n"].As<long>(), "History changed; preserve it.");
        var edges = await (await transaction.RunAsync("MATCH(h:Holon)-[r]-(other) RETURN r,other")).ToListAsync();
        foreach (var edge in edges)
        {
            var other = edge["other"].As<INode>();
            Assert.IsTrue(other.Labels.Contains("Holon") && other.Properties.TryGetValue("Id", out var id) && expectedNodes.ContainsKey((string)id), "Relationship escapes owned fixture.");
        }
        await ArchiveBytes("graph.json.gz", JsonSerializer.SerializeToUtf8Bytes(new
        {
            holons = nodes.Select(row => row["h"].As<INode>()).Select(node => new { node.Id, node.Labels, node.Properties }).ToArray(),
            receipts = receipts.Select(row => row["r"].As<INode>()).Select(node => new { node.Id, node.Labels, node.Properties }).ToArray(),
            relationships = edges.Select(row => row["r"].As<IRelationship>()).DistinctBy(edge => edge.Id)
                .Select(edge => new { edge.Id, edge.Type, edge.StartNodeId, edge.EndNodeId, edge.Properties }).ToArray()
        }));
        var backupFiles = Directory.GetFiles(archive, "*.gz").OrderBy(path => path, StringComparer.Ordinal).ToArray();
        var backupManifest = new List<object>();
        foreach (var file in backupFiles) backupManifest.Add(new { file = Path.GetFileName(file), bytes = new FileInfo(file).Length, sha256 = Hash(await File.ReadAllBytesAsync(file)) });
        await File.WriteAllTextAsync(Path.Combine(archive, "archive-manifest.json"), JsonSerializer.Serialize(backupManifest, new JsonSerializerOptions { WriteIndented = true }));
        await Stage("archives-complete-before-graph-commit");
        foreach (var node in nodes.Select(row => row["h"].As<INode>()))
        {
            var result = await (await transaction.RunAsync("MATCH(h:Holon {Id:$id,SnapshotJson:$json}) DETACH DELETE h", new { id = node["Id"].As<string>(), json = node["SnapshotJson"].As<string>() })).ConsumeAsync();
            Assert.AreEqual(1, result.Counters.NodesDeleted);
        }
        foreach (var receipt in receipts.Select(row => row["r"].As<INode>()))
        {
            var result = await (await transaction.RunAsync("MATCH(r:CosmicWriteReceipt {OperationId:$id,ResultJson:$json}) DELETE r", new { id = receipt["OperationId"].As<string>(), json = receipt["ResultJson"].As<string>() })).ConsumeAsync();
            Assert.AreEqual(1, result.Counters.NodesDeleted);
        }
        await transaction.CommitAsync();
        graphCommitted = true;
        await Stage("graph-committed-before-mongo-revalidation");
        _ = await ValidateMongo();
        foreach (var database in new[] { "cosmic_cql_v1_web4_storage", "cosmic_cql_v1_web4_usage" })
        {
            await Stage("before-drop-" + database);
            await mongo.DropDatabaseAsync(database);
            removed.Add(database);
            await Stage("dropped-" + database);
        }
        using var remainingDatabases = await mongo.ListDatabaseNamesAsync();
        Assert.IsFalse((await remainingDatabases.ToListAsync()).Intersect(removed).Any());
        var remaining = await (await session.RunAsync("MATCH(h:Holon) RETURN count(h) AS n")).SingleAsync();
        Assert.AreEqual(0L, remaining["n"].As<long>());
        CollectionAssert.AreEqual(sqliteBytes, await File.ReadAllBytesAsync(sqlite));
        Assert.AreEqual("892A8AF780ED445887ECBD6C191A7577B053872A13E5CF35FE592A56899894C5", Hash(await File.ReadAllBytesAsync(source)));
        await Stage("completed-original-and-local-sqlite-preserved");

        async Task ArchiveBytes(string name, byte[] bytes)
        {
            var path = Path.Combine(archive, name);
            await using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            await using (var gzip = new GZipStream(stream, CompressionLevel.SmallestSize)) await gzip.WriteAsync(bytes);
            await using var input = File.OpenRead(path);
            await using var decoded = new GZipStream(input, CompressionMode.Decompress);
            using var restored = new MemoryStream();
            await decoded.CopyToAsync(restored);
            CollectionAssert.AreEqual(bytes, restored.ToArray(), "Archive read-back differs; no mutation permitted.");
        }
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
}
