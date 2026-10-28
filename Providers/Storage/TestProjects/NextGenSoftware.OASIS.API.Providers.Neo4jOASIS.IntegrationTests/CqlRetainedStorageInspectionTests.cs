using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass]
public sealed class CqlRetainedStorageInspectionTests
{
    private static Dictionary<string, int> ReviewedBoundaries() => new()
    {
        ["cosmic_cql_v1_web4_storage/Avatar"] = 0,
        ["cosmic_cql_v1_web4_storage/AvatarDetail"] = 0,
        ["cosmic_cql_v1_web4_storage/CosmicHolonOutbox"] = 0,
        ["cosmic_cql_v1_web4_storage/CosmicOperationFence"] = 7,
        ["cosmic_cql_v1_web4_storage/CosmicWriteReceipt"] = 7,
        ["cosmic_cql_v1_web4_storage/Holon"] = 146,
        ["cosmic_cql_v1_web4_usage/subscription_usage_outbox_v1"] = 267
    };

    private static void VerifyReviewedBoundaries(IReadOnlyDictionary<string, int> observed)
    {
        var expected = ReviewedBoundaries();
        if (observed.Count != expected.Count || expected.Any(pair => !observed.TryGetValue(pair.Key, out var count) || count != pair.Value))
            throw new InvalidDataException("Retained fixture collection boundaries changed; preserve all data.");
    }

    [DataTestMethod]
    [DataRow("unexpected-collection")]
    [DataRow("nonempty-avatar")]
    [DataRow("additional-fence")]
    [DataRow("additional-usage")]
    [DataRow("missing-holon")]
    public void RetainedCleanupBoundaryGuardRejectsForeignOrChangedScope(string change)
    {
        var observed = ReviewedBoundaries();
        switch (change)
        {
            case "unexpected-collection": observed.Add("cosmic_cql_v1_web4_storage/Unowned", 1); break;
            case "nonempty-avatar": observed["cosmic_cql_v1_web4_storage/Avatar"] = 1; break;
            case "additional-fence": observed["cosmic_cql_v1_web4_storage/CosmicOperationFence"] = 8; break;
            case "additional-usage": observed["cosmic_cql_v1_web4_usage/subscription_usage_outbox_v1"] = 268; break;
            case "missing-holon": observed.Remove("cosmic_cql_v1_web4_storage/Holon"); break;
            default: throw new ArgumentOutOfRangeException(nameof(change));
        }
        Assert.ThrowsException<InvalidDataException>(() => VerifyReviewedBoundaries(observed));
    }

    [TestMethod]
    [TestCategory("Inspection")]
    public async Task RetainedMongoAndLocalSqliteInventoryPreservesDataAndReportsDurableOutcomes()
    {
        var root = Environment.GetEnvironmentVariable("OASIS_CQL_STORAGE_INSPECTION_ROOT")
            ?? throw new InvalidOperationException("Use the explicit retained-storage inspection runner.");
        var manifest = Path.Combine(root, "LocalState/COSMIC/Neo4jCurrent/cql-v1-web4-fixture.json");
        var bytes = await File.ReadAllBytesAsync(manifest);
        Assert.AreEqual("892A8AF780ED445887ECBD6C191A7577B053872A13E5CF35FE592A56899894C5", Convert.ToHexString(SHA256.HashData(bytes)));
        using var document = JsonDocument.Parse(bytes);
        var fixture = document.RootElement;
        var ids = new[] { "ids", "scalarIds", "restIds", "transportIds", "recoveryIds" }
            .SelectMany(group => fixture.GetProperty(group).EnumerateArray().Select(value => value.GetGuid())).ToHashSet();
        var operations = fixture.GetProperty("sharingOperations").EnumerateArray().Select(value => value.GetGuid()).ToHashSet();
        var usageKeys = new Dictionary<string, string>();
        var processUsageIds = new HashSet<string>();
        string? processActor = null;
        foreach (var request in fixture.GetProperty("requests").EnumerateArray())
        {
            if (request.TryGetProperty("root", out _) && request.TryGetProperty("actor", out var cursorActor))
            {
                Assert.IsNull(processActor, "Ambiguous process ownership entry.");
                processActor = cursorActor.GetGuid().ToString("D");
            }
            if (request.TryGetProperty("operationIds", out var processOperations))
            {
                Assert.IsNotNull(processActor, "Process completion lacks its journaled actor.");
                Assert.AreEqual(3, request.GetProperty("providers").GetInt32());
                Assert.AreEqual(3, request.GetProperty("pages").GetInt32());
                Assert.IsTrue(request.GetProperty("unchangedKey").GetBoolean());
                Assert.IsTrue(request.GetProperty("persistedProtectedKey").GetBoolean());
                Assert.AreEqual(3, processOperations.GetArrayLength());
                foreach (var value in processOperations.EnumerateArray())
                {
                    var id = value.GetGuid().ToString("D");
                    Assert.IsTrue(processUsageIds.Add(id));
                    Assert.IsTrue(usageKeys.TryAdd(id, processActor));
                }
            }
            if (!request.TryGetProperty("actor", out var actor)) continue;
            var hasOperation = request.TryGetProperty("operation", out var operation);
            var hasKey = request.TryGetProperty("key", out var key);
            if (!hasOperation && !hasKey) continue;
            Assert.IsFalse(hasOperation && hasKey, "Ambiguous journal request-key format.");
            var actorId = actor.GetGuid().ToString("D");
            // Independent check of WEB4's documented service/actor/key identity;
            // no request is resubmitted to discover its durable outcome.
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[] { "WEB4", actorId, (hasOperation ? operation : key).GetString()! })));
            usageKeys[new Guid(hash.AsSpan(0, 16)).ToString("D")] = actorId;
        }
        var client = new MongoClient("mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000");
        using var databases = await client.ListDatabaseNamesAsync();
        var databaseNames = await databases.ToListAsync();
        Assert.IsTrue(databaseNames.Contains("cosmic_cql_v1_web4_storage"));
        Assert.IsTrue(databaseNames.Contains("cosmic_cql_v1_web4_usage"));
        var collectionInventory = new List<object>();
        var collectionBoundaries = new Dictionary<string, int>();
        foreach (var databaseName in new[] { "cosmic_cql_v1_web4_storage", "cosmic_cql_v1_web4_usage" })
        {
            var database = client.GetDatabase(databaseName);
            using var names = await database.ListCollectionNamesAsync();
            var collections = await names.ToListAsync();
            Assert.IsTrue(collections.Count <= 32, "Unexpected collection inventory size.");
            foreach (var name in collections.OrderBy(value => value, StringComparer.Ordinal))
            {
                // Retain content fingerprints, never raw documents, signing
                // preparations or credentials. Every collection is included.
                var documents = await database.GetCollection<BsonDocument>(name).Find(FilterDefinition<BsonDocument>.Empty)
                    .Sort(Builders<BsonDocument>.Sort.Ascending("_id")).Limit(1001).ToListAsync();
                Assert.IsTrue(documents.Count <= 1000, "Collection inspection budget exceeded: " + name);
                using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                long byteCount = 0;
                foreach (var value in documents)
                {
                    var bson = value.ToBson();
                    byteCount += bson.Length;
                    Assert.IsTrue(byteCount <= 16 * 1024 * 1024, "Collection byte budget exceeded: " + name);
                    digest.AppendData(bson);
                }
                collectionInventory.Add(new { database = databaseName, collection = name, count = documents.Count,
                    bytes = byteCount, sha256 = Convert.ToHexString(digest.GetHashAndReset()) });
                collectionBoundaries.Add(databaseName + "/" + name, documents.Count);
            }
        }
        VerifyReviewedBoundaries(collectionBoundaries);
        var storage = client.GetDatabase("cosmic_cql_v1_web4_storage");
        var native = await storage.GetCollection<BsonDocument>("Holon").Find(FilterDefinition<BsonDocument>.Empty)
            .Project(Builders<BsonDocument>.Projection.Include("HolonId").Include("Version").Exclude("_id")).Limit(201).ToListAsync();
        Assert.IsTrue(native.Count <= 200, "Native inventory exceeds inspection budget.");
        var mongoRows = native.Select(row => new { id = Guid.Parse(row["HolonId"].AsString), version = row["Version"].ToInt32() }).ToArray();
        Assert.AreEqual(mongoRows.Length, mongoRows.Select(row => row.id).Distinct().Count());
        Assert.IsTrue(mongoRows.All(row => ids.Contains(row.id)), "Unexpected Mongo identity; preserve data.");
        var receiptDocuments = await storage.GetCollection<BsonDocument>("CosmicWriteReceipt")
            .Find(FilterDefinition<BsonDocument>.Empty).Limit(501).ToListAsync();
        Assert.IsTrue(receiptDocuments.Count <= 500);
        var fences = await storage.GetCollection<BsonDocument>("CosmicOperationFence")
            .Find(FilterDefinition<BsonDocument>.Empty).Limit(501).ToListAsync();
        CollectionAssert.AreEquivalent(receiptDocuments.Select(row => row["_id"].AsString).ToArray(),
            fences.Select(row => row["_id"].AsString).ToArray(), "Operation fences are not exactly the owned committed receipts.");
        Assert.IsTrue(fences.All(row => row["Generation"].ToInt64() > 0));
        var mongoReceipts = new List<object>();
        foreach (var receipt in receiptDocuments)
        {
            var operation = Guid.Parse(receipt["_id"].AsString);
            Assert.IsTrue(operations.Contains(operation), "Receipt outside declared sharing operations.");
            var json = receipt["ResultJson"].AsString;
            var acknowledged = ConditionalHolonReceipt.Read(json);
            using var envelope = JsonDocument.Parse(json);
            Assert.AreEqual(operation, envelope.RootElement.GetProperty("operation").GetGuid());
            Assert.AreEqual(receipt["Fingerprint"].AsString, envelope.RootElement.GetProperty("intent").GetString());
            Assert.IsTrue(acknowledged.All(row => ids.Contains(row.Id)));
            mongoReceipts.Add(new { operation, roots = acknowledged.Select(row => new { row.Id, row.Version }).ToArray() });
        }
        // Observe durable status only. Do not acquire a usage lease, initialize
        // indexes, settle, replay or classify an executing operation as absent.
        var usage = await client.GetDatabase("cosmic_cql_v1_web4_usage").GetCollection<BsonDocument>("subscription_usage_outbox_v1")
            .Find(FilterDefinition<BsonDocument>.Empty)
            .Project(Builders<BsonDocument>.Projection.Include("State").Include("ObservedOutcome").Include("UserId").Include("Service")
                .Include("Endpoint").Include("ProviderAttempts").Include("Receipts").Include("SettledAtUtc"))
            .Limit(1001).ToListAsync();
        Assert.IsTrue(usage.Count <= 1000, "Usage inventory exceeds inspection budget.");
        var usageRows = usage.Select(row => new
        {
            id = row["_id"].AsString,
            state = row.GetValue("State", BsonNull.Value).ToString(),
            outcome = row.GetValue("ObservedOutcome", BsonNull.Value).ToString(),
            actor = row.GetValue("UserId", BsonNull.Value).ToString(),
            endpoint = row.GetValue("Endpoint", BsonNull.Value).ToString(),
            attempts = row.GetValue("ProviderAttempts", new BsonArray()).AsBsonArray.Count,
            receipts = row.GetValue("Receipts", new BsonArray()).AsBsonArray.Count,
            receiptProviders = row.GetValue("Receipts", new BsonArray()).AsBsonArray.Select(value => value.AsBsonDocument["Provider"].AsString).ToArray(),
            settledAtPresent = !row.GetValue("SettledAtUtc", BsonNull.Value).IsBsonNull,
            bindingKind = processUsageIds.Contains(row["_id"].AsString) ? "completed-process-summary"
                : usageKeys.ContainsKey(row["_id"].AsString) ? "service-actor-key" : "unbound"
        }).ToArray();
        var unboundUsageIds = new List<string>();
        foreach (var row in usage)
        {
            if (usageKeys.TryGetValue(row["_id"].AsString, out var expectedActor))
                Assert.AreEqual(expectedActor, row["UserId"].AsString);
            else unboundUsageIds.Add(row["_id"].AsString);
            Assert.AreEqual("WEB4", row["Service"].AsString);
        }

        var sqlite = Path.Combine(root, "LocalState/COSMIC/Neo4jCurrent/cql-v1-web4.sqlite");
        var before = await File.ReadAllBytesAsync(sqlite);
        const string expectedSqlite = "7E1C6FBF19A56E00923CDC34B478FC8AE4833FDBA96F1D20E936FA57586992B2";
        Assert.AreEqual(expectedSqlite, Convert.ToHexString(SHA256.HashData(before)), "Local copied fixture changed; inspect provenance.");
        var sqliteRows = new List<object>();
        var sqliteReceipts = new List<object>();
        using (var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = sqlite, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ToString()))
        {
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT Id,Version FROM Holon ORDER BY Id LIMIT 201";
            using (var reader = await command.ExecuteReaderAsync())
                while (await reader.ReadAsync())
                {
                    var id = reader.GetGuid(0);
                    Assert.IsTrue(ids.Contains(id), "Unexpected SQLite identity.");
                    sqliteRows.Add(new { id, version = reader.GetInt32(1) });
                }
            Assert.IsTrue(sqliteRows.Count <= 200);
            command.CommandText = "SELECT OperationId,RequestHash,ResultJson FROM CosmicWriteReceipt ORDER BY OperationId LIMIT 501";
            using var receipts = await command.ExecuteReaderAsync();
            while (await receipts.ReadAsync())
            {
                var operation = receipts.GetGuid(0);
                Assert.IsTrue(operations.Contains(operation));
                var json = receipts.GetString(2);
                var outer = JObject.Parse(json, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
                var digestInput = new JObject(outer.Properties().Where(property => property.Name != "digest")
                    .Select(property => new JProperty(property.Name, property.Value.DeepClone())));
                Assert.AreEqual(outer.Value<string>("digest"), ConditionalHolonPlan.ComputeFingerprint(digestInput));
                using var receipt = JsonDocument.Parse(json);
                Assert.AreEqual(operation, receipt.RootElement.GetProperty("operationId").GetGuid());
                Assert.AreEqual(receipts.GetString(1), receipt.RootElement.GetProperty("intent").GetString());
                Assert.AreEqual("SQLLiteDBOASIS", receipt.RootElement.GetProperty("authority").GetString());
                var acknowledged = ConditionalHolonReceipt.Read(receipt.RootElement.GetProperty("graph").GetString()!);
                Assert.IsTrue(acknowledged.All(row => ids.Contains(row.Id)));
                sqliteReceipts.Add(new { operation, roots = acknowledged.Select(row => new { row.Id, row.Version }).ToArray() });
            }
            Assert.IsTrue(sqliteReceipts.Count <= 500);
        }
        CollectionAssert.AreEqual(before, await File.ReadAllBytesAsync(sqlite), "Read-only inspection changed the local SQLite file.");
        var output = Path.Combine(root, "TestResults/Neo4jCurrent/cql-retained-storage-inspection.json");
        await using var stream = new FileStream(output, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, new
        {
            observedUtc = DateTime.UtcNow, collectionInventory, mongoRows, mongoReceipts, usageRows, sqliteRows, sqliteReceipts,
            localSqliteSha256 = expectedSqlite, originalSqlitePathInspected = false,
            sqliteOuterReceiptDigestVerified = true, usageJournalBindingsVerified = unboundUsageIds.Count == 0,
            collectionBoundariesVerified = true, fenceReceiptBindingsVerified = true,
            unboundUsageIds,
            processSummaryBoundUsageCount = usageRows.Count(row => processUsageIds.Contains(row.id)),
            requestKeyBoundUsageCount = usageRows.Count(row => usageKeys.ContainsKey(row.id) && !processUsageIds.Contains(row.id)),
            journalUsageKeysWithoutObservedRow = usageKeys.Keys.Except(usageRows.Select(row => row.id)).OrderBy(id => id).ToArray(),
            originalIntentRecomputed = false, cleanupAuthorized = false
        }, new JsonSerializerOptions { WriteIndented = true });
        CollectionAssert.AreEqual(bytes, await File.ReadAllBytesAsync(manifest));
    }
}
