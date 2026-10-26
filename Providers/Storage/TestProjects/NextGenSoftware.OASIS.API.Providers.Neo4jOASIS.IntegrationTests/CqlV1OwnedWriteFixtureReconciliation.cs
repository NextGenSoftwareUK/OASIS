using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1FixtureReconciliationTests
{
    [TestMethod]
    public async Task ReviewedOwnedWriteBooleanAssertionPreservesProofBeforeExactCleanup()
    {
        const string hash = "CC64F69E5607C5011DCE0D857F1FF6C3AD9611FA5372594C14A3868E30C87D73";
        const string mongoUri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(mongoUri, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        Assert.AreEqual("bolt://127.0.0.1:17687", Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        var manifest = Path.Combine(work, "cql-v1-web4-fixture.json");
        var bytes = await File.ReadAllBytesAsync(manifest);
        Assert.AreEqual(hash, Convert.ToHexString(SHA256.HashData(bytes)));
        using var document = JsonDocument.Parse(bytes);
        var fixture = document.RootElement;
        Assert.AreEqual("before-owned-graph-write", fixture.GetProperty("stage").GetString());
        Assert.AreEqual("cosmic_cql_v1_web4_storage", fixture.GetProperty("storageDb").GetString());
        Assert.AreEqual("cosmic_cql_v1_web4_usage", fixture.GetProperty("usageDb").GetString());
        var sqlite = Path.GetFullPath(Path.Combine(work, "cql-v1-web4.sqlite"));
        Assert.AreEqual(sqlite, fixture.GetProperty("sqlite").GetString());
        var ids = fixture.GetProperty("ids").EnumerateArray().Select(value => value.GetGuid()).ToArray();
        Assert.AreEqual(9, ids.Length);
        Assert.AreEqual(9, ids.Distinct().Count());
        var ops = fixture.GetProperty("sharingOperations").EnumerateArray().Select(value => value.GetString()!).ToArray();
        Assert.AreEqual(20, ops.Length);
        Assert.AreEqual(20, ops.Distinct().Count());
        var requests = fixture.GetProperty("requests").EnumerateArray().ToArray();
        Assert.AreEqual(20, requests.Length);
        CollectionAssert.AreEqual(ops, requests.Select(value => value.GetProperty("operation").GetString()).ToArray());
        for (var i = 0; i < requests.Length; i++)
            Assert.AreEqual(i < 10 ? "MongoDBOASIS" : "SQLLiteDBOASIS", requests[i].GetProperty("provider").GetString());
        Assert.AreEqual(0, fixture.GetProperty("scalarWrites").GetArrayLength());
        var alice = fixture.GetProperty("alice").GetGuid();
        var bob = fixture.GetProperty("bob").GetGuid();
        var tag = fixture.GetProperty("tag").GetString();
        var mongo = new MongoClient(mongoUri);
        var storage = mongo.GetDatabase("cosmic_cql_v1_web4_storage");
        var usage = mongo.GetDatabase("cosmic_cql_v1_web4_usage");
        Assert.AreEqual(0L, await usage.GetCollection<UsageOutboxOperation>("subscription_usage_outbox_v1")
            .CountDocumentsAsync(Builders<UsageOutboxOperation>.Filter.Empty), "The HTTP host never started.");
        var receipts = await storage.GetCollection<BsonDocument>("CosmicWriteReceipt")
            .Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
        Assert.AreEqual(5, receipts.Count);
        foreach (var receipt in receipts)
        {
            CollectionAssert.Contains(ops.Take(10).ToArray(), receipt["_id"].AsString);
            var acknowledged = ConditionalHolonReceipt.Read(receipt["ResultJson"].AsString);
            Assert.AreEqual(ids[0], acknowledged.Single().Id);
            Assert.AreEqual(bob, acknowledged.Single().CreatedByAvatarId);
        }
        using (var connection = new SqliteConnection("Data Source=" + sqlite + ";Mode=ReadOnly"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT OperationId,ResultJson FROM CosmicWriteReceipt";
            using var reader = command.ExecuteReader();
            var count = 0;
            while (reader.Read())
            {
                CollectionAssert.Contains(ops.Skip(10).ToArray(), reader.GetString(0));
                using var receipt = JsonDocument.Parse(reader.GetString(1));
                Assert.AreEqual(bob, receipt.RootElement.GetProperty("actor").GetGuid());
                Assert.AreEqual("SQLLiteDBOASIS", receipt.RootElement.GetProperty("authority").GetString());
                Assert.AreEqual(ids[0], ConditionalHolonReceipt.Read(receipt.RootElement.GetProperty("graph").GetString()!).Single().Id);
                count++;
            }
            Assert.AreEqual(5, count);
        }
        using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        await using var inspection = driver.AsyncSession();
        var native = await (await inspection.RunAsync("MATCH(h:Holon) RETURN h.Id AS id")).ToListAsync();
        CollectionAssert.AreEquivalent(ids.Select(id => id.ToString("D")).ToArray(), native.Select(row => row["id"].As<string>()).ToArray());
        var neoReceipts = await (await inspection.RunAsync("MATCH(r:CosmicWriteReceipt) WHERE r.OperationId IN $ops RETURN count(r) AS n", new { ops })).SingleAsync();
        Assert.AreEqual(0L, neoReceipts["n"].As<long>());
        var names = new[] { "A-private", "A-index", "B-owned", "C-public", "D-owned",
            "public child", "private child", "owned child", "public behind private" };
        var snapshots = new List<object>();
        foreach (var provider in new IOASISStorageProvider[] { new MongoProvider(mongoUri, "cosmic_cql_v1_web4_storage"),
            new SqliteProvider("Data Source=" + sqlite), new Neo4jOASIS("bolt://127.0.0.1:17687", "neo4j", "oasis-local-test-only") })
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            try
            {
                var all = await provider.LoadAllHolonsAsync(loadChildren: false);
                Assert.IsFalse(all.IsError, all.Message);
                CollectionAssert.AreEquivalent(ids, all.Result.Select(value => value.Id).ToArray());
                foreach (var holon in all.Result)
                {
                    var i = Array.IndexOf(ids, holon.Id);
                    Assert.AreEqual(names[i], holon.Name);
                    Assert.AreEqual(i is 2 or 4 or 7 ? alice : bob, holon.CreatedByAvatarId);
                    Assert.AreEqual(i is 3 or 5 or 8, holon.IsPublic);
                    if (i < 5) Assert.AreEqual(tag, holon.MetaData["fixture"].ToString());
                    if (i == 0 && provider.ProviderType.Value != Core.Enums.ProviderType.Neo4jOASIS)
                        Assert.IsTrue(JsonSerializer.SerializeToElement(holon.MetaData["owned-write-control"]).GetBoolean());
                    snapshots.Add(new { provider = provider.ProviderType.Name, holon.Id,
                        digest = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(holon))) });
                }
            }
            finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
        }
        // All exact ownership and terminal receipt checks precede destructive cleanup.
        await File.WriteAllBytesAsync(Path.Combine(work, "owned-write-boolean-reviewed-manifest.json"), bytes);
        await File.WriteAllTextAsync(Path.Combine(work, "owned-write-boolean-reviewed-proof.json"),
            JsonSerializer.Serialize(new { hash, snapshots, mongoReceipts = receipts.Count, sqliteReceipts = 5, httpOperations = 0 }));
        var owned = ids.Select(id => id.ToString("D")).ToArray();
        await (await inspection.RunAsync("MATCH(h:Holon) WHERE h.Id IN $ids DETACH DELETE h", new { ids = owned })).ConsumeAsync();
        await (await inspection.RunAsync("MATCH(v:OasisRecordVersion {Kind:'Holon'}) WHERE v.RecordId IN $ids DETACH DELETE v", new { ids = owned })).ConsumeAsync();
        await mongo.DropDatabaseAsync("cosmic_cql_v1_web4_storage");
        await mongo.DropDatabaseAsync("cosmic_cql_v1_web4_usage");
        await File.WriteAllTextAsync(manifest, JsonSerializer.Serialize(new { stage = "successful-awaiting-runner-cleanup", sqlite }));
    }
}
