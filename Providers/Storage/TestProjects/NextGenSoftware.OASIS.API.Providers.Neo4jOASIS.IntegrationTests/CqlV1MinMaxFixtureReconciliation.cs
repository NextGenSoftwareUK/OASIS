using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1FixtureReconciliationTests
{
    [TestMethod]
    public async Task ReviewedMinMaxSwaggerFixtureVerifiesAllTerminalOutcomesAndOwnedRowsBeforeCleanup()
    {
        const string hash = "AC9E6BCCD1AF2A90133F85E32DB7DFD1A7CEA31E28C79EAA90E2908F3ADB2565";
        const string evidenceHash = "76BF29D58AFBACE4140246061A735B234DB0AEF966B618E2B1FD419CAB3480AB";
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string storageDb = "cosmic_cql_v1_web4_storage", usageDb = "cosmic_cql_v1_web4_usage";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        Assert.AreEqual("bolt://127.0.0.1:17687", Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        var manifest = Path.Combine(work, "cql-v1-web4-fixture.json");
        var original = await File.ReadAllBytesAsync(manifest);
        Assert.AreEqual(hash, Convert.ToHexString(SHA256.HashData(original)));
        using var document = JsonDocument.Parse(original);
        var fixture = document.RootElement;
        Assert.AreEqual("before-http-request", fixture.GetProperty("stage").GetString());
        Assert.AreEqual(storageDb, fixture.GetProperty("storageDb").GetString());
        Assert.AreEqual(usageDb, fixture.GetProperty("usageDb").GetString());
        var sqlite = Path.GetFullPath(Path.Combine(work, "cql-v1-web4.sqlite"));
        Assert.AreEqual(sqlite, fixture.GetProperty("sqlite").GetString());
        var ids = fixture.GetProperty("ids").EnumerateArray().Select(x => x.GetGuid()).ToArray();
        var scalarIds = fixture.GetProperty("scalarIds").EnumerateArray().Select(x => x.GetGuid()).ToArray();
        Assert.AreEqual(9, ids.Length); Assert.AreEqual(137, scalarIds.Length);
        var allIds = ids.Concat(scalarIds).ToArray();
        Assert.AreEqual(146, allIds.Distinct().Count());
        var alice = fixture.GetProperty("alice").GetGuid(); var bob = fixture.GetProperty("bob").GetGuid();
        var tag = fixture.GetProperty("tag").GetString();
        var providerNames = new[] { "MongoDBOASIS", "SQLLiteDBOASIS", "Neo4jOASIS" };
        CollectionAssert.AreEquivalent(providerNames.SelectMany(p => scalarIds.Select(id => p + ":" + id.ToString("D"))).ToArray(),
            fixture.GetProperty("scalarWrites").EnumerateArray().Select(x => x.GetString()).ToArray());

        var nativeEvidence = await File.ReadAllBytesAsync(Path.Combine(work, "cql-v1-web4-native-evidence.json"));
        Assert.AreEqual(evidenceHash, Convert.ToHexString(SHA256.HashData(nativeEvidence)));
        using var proof = JsonDocument.Parse(nativeEvidence);
        Assert.AreEqual(180, proof.RootElement.GetProperty("settledOperations").GetInt32());
        Assert.AreEqual(175, proof.RootElement.GetProperty("nativeDatabaseQueryAttempts").GetInt32());
        Assert.AreEqual(tag, proof.RootElement.GetProperty("tag").GetString());
        CollectionAssert.AreEqual(ids.Take(5).ToArray(), proof.RootElement.GetProperty("ids").EnumerateArray().Select(x => x.GetGuid()).ToArray());

        // The reviewed journal records client intent and host admission separately.
        // Five auth probes and one oversized request never reserve an operation.
        var journal = fixture.GetProperty("requests").EnumerateArray().ToArray();
        Assert.AreEqual(396, journal.Length);
        var authKeys = journal.Select(x => x.GetProperty("operation").GetString()).Distinct().Take(5).ToArray();
        const string oversizedKey = "c7c7fbbb-8982-4c87-84a6-62f642236c48";
        Assert.AreEqual(oversizedKey, journal[^4].GetProperty("operation").GetString());
        string OperationId(Guid actor, string key)
        {
            var canonical = JsonSerializer.Serialize(new[] { "WEB4", actor.ToString("D"), key });
            return new Guid(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)).AsSpan(0, 16)).ToString("D");
        }
        var expected = journal.Where(x => x.GetProperty("actor").GetGuid() != Guid.Empty
                && !authKeys.Contains(x.GetProperty("operation").GetString())
                && x.GetProperty("operation").GetString() != oversizedKey)
            .Select(x => OperationId(x.GetProperty("actor").GetGuid(), x.GetProperty("operation").GetString()!)).Distinct().ToArray();
        Assert.AreEqual(180, expected.Length);
        var mongo = new MongoClient(uri);
        var operations = await mongo.GetDatabase(usageDb).GetCollection<UsageOutboxOperation>("subscription_usage_outbox_v1")
            .Find(Builders<UsageOutboxOperation>.Filter.Empty).ToListAsync();
        CollectionAssert.AreEquivalent(expected, operations.Select(x => x.Id).ToArray());
        Assert.AreEqual(175, operations.Sum(x => x.ProviderAttempts.Count));
        Assert.AreEqual(175, operations.Sum(x => x.Receipts.Count));
        foreach (var operation in operations)
        {
            Assert.AreEqual("WEB4", operation.Service); Assert.AreEqual("settled", operation.State);
            if (operation.Receipts.Count == 0)
            {
                Assert.AreEqual(0, operation.ProviderAttempts.Count); Assert.AreEqual("failed", operation.ObservedOutcome);
                Assert.IsFalse(string.IsNullOrEmpty(operation.NoProviderExecutionReason));
            }
            else
            {
                Assert.AreEqual(1, operation.ProviderAttempts.Count); Assert.AreEqual(1, operation.Receipts.Count);
                Assert.AreEqual(0.002m, operation.Receipts.Single().ActualCostUsd);
                Assert.AreEqual(operation.ProviderAttempts.Single().ClientRequestId, operation.Receipts.Single().ClientRequestId);
            }
        }

        using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        using var inspection = driver.AsyncSession();
        var native = await (await inspection.RunAsync("MATCH (h:Holon) RETURN h.Id AS id")).ToListAsync();
        var owned = allIds.Select(x => x.ToString("D")).ToArray();
        CollectionAssert.AreEquivalent(owned, native.Select(x => x["id"].As<string>()).ToArray());
        var names = new[] { "A-private", "A-index", "B-owned", "C-public", "D-owned", "public child", "private child", "owned child", "public behind private" };
        var snapshots = new List<object>();
        foreach (var provider in new IOASISStorageProvider[] { new MongoProvider(uri, storageDb), new SqliteProvider("Data Source=" + sqlite),
            new Neo4jOASIS("bolt://127.0.0.1:17687", "neo4j", "oasis-local-test-only") })
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            try
            {
                var loaded = await provider.LoadAllHolonsAsync(loadChildren: false);
                Assert.IsFalse(loaded.IsError, loaded.Message + loaded.Exception);
                CollectionAssert.AreEquivalent(allIds, loaded.Result.Select(x => x.Id).ToArray());
                foreach (var holon in loaded.Result)
                {
                    var index = Array.IndexOf(ids, holon.Id);
                    if (index >= 0)
                    {
                        Assert.AreEqual(names[index], holon.Name);
                        Assert.AreEqual(index is 2 or 4 or 7 ? alice : bob, holon.CreatedByAvatarId);
                        Assert.AreEqual(index is 3 or 5 or 8, holon.IsPublic);
                        Assert.AreEqual(index < 5 ? Guid.Empty : index == 8 ? ids[6] : ids[3], holon.ParentHolonId);
                        if (index < 5)
                        {
                            Assert.AreEqual(tag, holon.MetaData["fixture"].ToString());
                            var extremum = index == 0 ? long.MinValue : index == 1 ? long.MaxValue : -9007199254740993L + index;
                            Assert.AreEqual(extremum, long.Parse(holon.MetaData["extremum"].ToString()!, CultureInfo.InvariantCulture));
                        }
                    }
                    else
                    {
                        Assert.IsTrue(scalarIds.Contains(holon.Id));
                        Assert.AreEqual("Z-owned scalar " + holon.Id.ToString("D"), holon.Name);
                        Assert.AreEqual(alice, holon.CreatedByAvatarId); Assert.IsFalse(holon.IsPublic);
                        Assert.AreEqual(Guid.Empty, holon.ParentHolonId); Assert.AreEqual(tag, holon.MetaData["scalarFixture"].ToString());
                    }
                    snapshots.Add(new { provider = provider.ProviderType.Name, holon.Id,
                        hash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new { holon.Id, holon.Name, holon.CreatedByAvatarId, holon.IsPublic, holon.ParentHolonId, holon.Version, holon.MetaData }))) });
                }
            }
            finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
        }
        // No destructive action occurs until every store and durable outcome
        // has been checked and the original ownership journal is unchanged.
        Assert.AreEqual(hash, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(manifest))));
        await File.WriteAllBytesAsync(Path.Combine(work, "cql-v1-web4-before-minmax-reconciliation.json"), original);
        await File.WriteAllTextAsync(Path.Combine(work, "cql-v1-web4-reviewed-minmax-reconciliation.json"),
            JsonSerializer.Serialize(new { hash, evidenceHash, snapshots, operations = operations.Select(x => new { x.Id, x.State, attempts = x.ProviderAttempts.Count, receipts = x.Receipts.Count }) }));
        await (await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h", new { ids = owned })).ConsumeAsync();
        await (await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon'}) WHERE v.RecordId IN $ids DETACH DELETE v", new { ids = owned })).ConsumeAsync();
        await mongo.DropDatabaseAsync(storageDb); await mongo.DropDatabaseAsync(usageDb);
        await File.WriteAllTextAsync(manifest, JsonSerializer.Serialize(new { stage = "successful-awaiting-runner-cleanup", sqlite }));
    }
}
