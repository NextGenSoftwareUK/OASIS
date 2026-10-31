using System.Security.Cryptography;
using System.Text.Json;
using System.Xml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1FixtureReconciliationTests
{
    [TestMethod]
    public async Task ReviewedUnobservedRoleSeedVerifiesPartialCreationBeforeExactCleanup()
    {
        const string markerHash = "E062B6BA6D240DE26CCD74D2F7DF0B4A9C2B923C569EEBA0BC7175BEA7ADA73F";
        const string failureHash = "9E2D50C429A563CDC95D21DEE4106D0C8A8D3B5F9D66463552EF4377F4A3AEA8";
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string storageDb = "cosmic_cql_v1_web4_storage";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        Assert.AreEqual("bolt://127.0.0.1:17687", Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        var marker = Path.Combine(work, "cql-v1-web4-fixture.json");
        var original = await File.ReadAllBytesAsync(marker);
        Assert.AreEqual(markerHash, Convert.ToHexString(SHA256.HashData(original)));
        var failure = await File.ReadAllBytesAsync(Path.Combine(work, "neo4j-cql-v1-demo-current.trx"));
        Assert.AreEqual(failureHash, Convert.ToHexString(SHA256.HashData(failure)));
        var failureXml = new XmlDocument();
        using var failureStream = new MemoryStream(failure);
        failureXml.Load(failureStream);
        StringAssert.Contains(failureXml.InnerText, "Persisted child-role membership must be fully observed");

        using var document = JsonDocument.Parse(original);
        var fixture = document.RootElement;
        Assert.AreEqual("before-create", fixture.GetProperty("stage").GetString());
        Assert.AreEqual(storageDb, fixture.GetProperty("storageDb").GetString());
        Assert.AreEqual("cosmic_cql_v1_web4_usage", fixture.GetProperty("usageDb").GetString());
        Assert.AreEqual(0, fixture.GetProperty("requests").GetArrayLength());
        Assert.AreEqual(0, fixture.GetProperty("scalarWrites").GetArrayLength());
        var sqlite = Path.GetFullPath(Path.Combine(work, "cql-v1-web4.sqlite"));
        Assert.AreEqual(sqlite, fixture.GetProperty("sqlite").GetString());
        var ids = fixture.GetProperty("ids").EnumerateArray().Select(item => item.GetGuid()).ToArray();
        var scalarIds = fixture.GetProperty("scalarIds").EnumerateArray().Select(item => item.GetGuid()).ToArray();
        Assert.AreEqual(9, ids.Length);
        Assert.AreEqual(137, scalarIds.Length);
        Assert.AreEqual(146, ids.Concat(scalarIds).Distinct().Count());
        var alice = fixture.GetProperty("alice").GetGuid();
        var bob = fixture.GetProperty("bob").GetGuid();
        var tag = fixture.GetProperty("tag").GetString();
        var names = new[] { "A-private", "A-index", "B-owned", "C-public", "D-owned",
            "public child", "private child", "owned child", "public behind private" };
        var snapshots = new List<object>();

        // Mongo finished all five acknowledged seed saves. SQLite acknowledged
        // the first three roots, then rejected the fourth before transaction entry.
        foreach (var entry in new (IOASISStorageProvider Provider, int Count)[]
        {
            (new MongoProvider(uri, storageDb), 9),
            (new SqliteProvider("Data Source=" + sqlite), 3)
        })
        {
            Assert.IsFalse((await entry.Provider.ActivateProviderAsync()).IsError);
            try
            {
                var loaded = await entry.Provider.LoadAllHolonsAsync(loadChildren: false);
                Assert.IsFalse(loaded.IsError, loaded.Message);
                var rows = loaded.Result.ToArray();
                CollectionAssert.AreEquivalent(ids.Take(entry.Count).ToArray(), rows.Select(row => row.Id).ToArray());
                foreach (var row in rows)
                {
                    var index = Array.IndexOf(ids, row.Id);
                    Assert.AreEqual(names[index], row.Name);
                    Assert.AreEqual(index is 2 or 4 or 7 ? alice : bob, row.CreatedByAvatarId);
                    Assert.AreEqual(index is 3 or 5 or 8, row.IsPublic);
                    Assert.AreEqual(index < 5 ? Guid.Empty : index == 8 ? ids[6] : ids[3], row.ParentHolonId);
                    if (index < 5) Assert.AreEqual(tag, row.MetaData["fixture"].ToString());
                    else Assert.IsFalse(row.MetaData.ContainsKey("fixture"));
                    snapshots.Add(new { provider = entry.Provider.ProviderType.Name, row.Id, row.Version,
                        sha256 = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(StoredRecordJson.Serialize(row)))) });
                }
            }
            finally { Assert.IsFalse((await entry.Provider.DeActivateProviderAsync()).IsError); }
        }

        var mongo = new MongoClient(uri);
        using (var databases = await mongo.ListDatabaseNamesAsync())
            Assert.IsFalse((await databases.ToListAsync()).Contains("cosmic_cql_v1_web4_usage"),
                "No HTTP/outbox initialization was reached; preserve any unexpected usage database.");
        using (var collections = await mongo.GetDatabase(storageDb).ListCollectionNamesAsync())
            foreach (var name in await collections.ToListAsync())
                if (name != "Holon")
                    Assert.AreEqual(0L, await mongo.GetDatabase(storageDb).GetCollection<BsonDocument>(name)
                        .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty), "Unexpected non-holon data: " + name);
        using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        await using var session = driver.AsyncSession();
        var neoRows = await (await session.RunAsync("MATCH (h:Holon) RETURN count(h) AS n")).SingleAsync();
        Assert.AreEqual(0L, neoRows["n"].As<long>(), "The Neo4j seed phase was never reached.");

        // Preserve the original intent and observations before deleting only the
        // exact database whose absent baseline and owned IDs were verified above.
        await File.WriteAllBytesAsync(Path.Combine(work, "cql-v1-role-seed-original.json"), original);
        await File.WriteAllTextAsync(Path.Combine(work, "cql-v1-role-seed-reconciliation.json"),
            JsonSerializer.Serialize(new { markerHash, failureHash, snapshots, neoRows = 0, usageInitialized = false }));
        Assert.AreEqual(markerHash, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(marker))));
        await mongo.DropDatabaseAsync(storageDb);
        await File.WriteAllTextAsync(marker,
            JsonSerializer.Serialize(new { stage = "successful-awaiting-runner-cleanup", sqlite }));
    }
}
