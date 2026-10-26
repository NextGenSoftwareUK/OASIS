using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1FixtureReconciliationTests
{
    [TestMethod]
    public async Task ReviewedAccessSeedPreservesReadFailureBeforeExactCleanup()
    {
        const string hash = "195CA1DDE290A24150E3554C50033C013F8375B43FB034EEB32DBE2FCCF9A1FB";
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        Assert.AreEqual("bolt://127.0.0.1:17687", Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        var marker = Path.Combine(work, "cql-v1-web4-fixture.json");
        var bytes = await File.ReadAllBytesAsync(marker);
        Assert.AreEqual(hash, Convert.ToHexString(SHA256.HashData(bytes)));
        var failure = await File.ReadAllBytesAsync(Path.Combine(work, "neo4j-cql-v1-demo-current.trx"));
        Assert.AreEqual("437D2B709AB1A928BD56320980F9E60339D85CED0712EC0A05B105457BF7F7E7", Convert.ToHexString(SHA256.HashData(failure)));
        using var document = JsonDocument.Parse(bytes);
        var fixture = document.RootElement;
        Assert.AreEqual("before-create", fixture.GetProperty("stage").GetString());
        foreach (var name in new[] { "requests", "scalarWrites", "sharingOperations" })
            Assert.AreEqual(0, fixture.GetProperty(name).GetArrayLength());
        const string database = "cosmic_cql_v1_web4_storage";
        Assert.AreEqual(database, fixture.GetProperty("storageDb").GetString());
        Assert.AreEqual("cosmic_cql_v1_web4_usage", fixture.GetProperty("usageDb").GetString());
        var sqlite = Path.GetFullPath(Path.Combine(work, "cql-v1-web4.sqlite"));
        Assert.AreEqual(sqlite, fixture.GetProperty("sqlite").GetString());
        var ids = fixture.GetProperty("ids").EnumerateArray().Select(value => value.GetGuid()).ToArray();
        Assert.AreEqual(9, ids.Length);
        var names = new[] { "A-private", "A-index", "B-owned", "C-public", "D-owned", "public child", "private child", "owned child", "public behind private" };
        var snapshots = new List<object>();
        foreach (IOASISStorageProvider provider in new IOASISStorageProvider[]
        {
            new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(uri, database),
            new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS("Data Source=" + sqlite),
            new Neo4jOASIS("bolt://127.0.0.1:17687", "neo4j", "oasis-local-test-only")
        })
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            try
            {
                var read = await provider.LoadAllHolonsAsync(loadChildren: false);
                Assert.IsFalse(read.IsError, read.Message);
                var rows = read.Result.ToArray();
                CollectionAssert.AreEquivalent(ids, rows.Select(row => row.Id).ToArray());
                foreach (var row in rows)
                {
                    var index = Array.IndexOf(ids, row.Id);
                    Assert.AreEqual(names[index], row.Name);
                    Assert.AreEqual(fixture.GetProperty(index is 2 or 4 or 7 ? "alice" : "bob").GetGuid(), row.CreatedByAvatarId);
                    Assert.AreEqual(index is 3 or 5 or 8, row.IsPublic);
                    Assert.AreEqual(index < 5 ? Guid.Empty : index == 8 ? ids[6] : ids[3], row.ParentHolonId);
                    if (index < 5) Assert.AreEqual(fixture.GetProperty("tag").GetString(), row.MetaData["fixture"].ToString());
                    snapshots.Add(new { authority = provider.ProviderType.Value.ToString(), row.Id,
                        sha256 = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(StoredRecordJson.Serialize(row)))) });
                }
            }
            finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
        }
        var mongo = new MongoClient(uri);
        using (var databases = await mongo.ListDatabaseNamesAsync())
            Assert.IsFalse((await databases.ToListAsync()).Contains("cosmic_cql_v1_web4_usage"));
        using (var collections = await mongo.GetDatabase(database).ListCollectionNamesAsync())
            foreach (var name in await collections.ToListAsync())
                if (name != "Holon") Assert.AreEqual(0L, await mongo.GetDatabase(database).GetCollection<BsonDocument>(name).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        await using var session = driver.AsyncSession();
        var receipts = await (await session.RunAsync("MATCH (r:CosmicWriteReceipt) RETURN count(r) AS n")).SingleAsync();
        Assert.AreEqual(0L, receipts["n"].As<long>());
        await File.WriteAllTextAsync(Path.Combine(work, "cql-v1-access-seed-reconciliation.json"), JsonSerializer.Serialize(new { hash, snapshots, noReceipts = true, noUsage = true }));
        await File.WriteAllBytesAsync(Path.Combine(work, "cql-v1-access-seed-original.json"), bytes);
        Assert.AreEqual(hash, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(marker))));
        await (await session.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h", new { ids = ids.Select(value => value.ToString("D")).ToArray() })).ConsumeAsync();
        await (await session.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon'}) WHERE v.RecordId IN $ids DETACH DELETE v", new { ids = ids.Select(value => value.ToString("D")).ToArray() })).ConsumeAsync();
        await mongo.DropDatabaseAsync(database);
        await File.WriteAllTextAsync(marker, JsonSerializer.Serialize(new { stage = "successful-awaiting-runner-cleanup", sqlite }));
    }
}
