using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlV1FixtureReconciliationTests
{
    [TestMethod]
    public Task ReviewedShippingRestPreparationFixturePreservesTerminalEvidenceBeforeExactCleanup()
        => ReviewShippingRestFixture(false);

    [TestMethod]
    public Task ReviewedShippingRestGraphProjectionFixturePreservesChildrenBeforeExactCleanup()
        => ReviewShippingRestFixture(true);

    private static async Task ReviewShippingRestFixture(bool graphProjection)
    {
        var hash = graphProjection
            ? "7A7912DA384210819E6AAC6BA871ADEF4BC10A752EE241F191FDB259C502991B"
            : "9857D397D00B79429E57B8FDA1F866AF9EEDFB9088C098BB2C1654F52A3503B3";
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        Assert.AreEqual("bolt://127.0.0.1:17687", Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        var manifest = Path.Combine(work, "cql-v1-web4-fixture.json");
        var original = await File.ReadAllBytesAsync(manifest);
        Assert.AreEqual(hash, Convert.ToHexString(SHA256.HashData(original)));
        using var document = JsonDocument.Parse(original);
        var fixture = document.RootElement;
        Assert.AreEqual("before-rest-http", fixture.GetProperty("stage").GetString());
        var storageDb = fixture.GetProperty("storageDb").GetString()!;
        var usageDb = fixture.GetProperty("usageDb").GetString()!;
        Assert.AreEqual("cosmic_cql_v1_web4_storage", storageDb);
        Assert.AreEqual("cosmic_cql_v1_web4_usage", usageDb);
        var sqlite = Path.Combine(work, "cql-v1-web4.sqlite");
        Assert.AreEqual(sqlite, fixture.GetProperty("sqlite").GetString());
        var ids = fixture.GetProperty("ids").EnumerateArray().Select(value => value.GetGuid()).ToArray();
        var scalarIds = fixture.GetProperty("scalarIds").EnumerateArray().Select(value => value.GetGuid()).ToArray();
        var restIds = fixture.GetProperty("restIds").EnumerateArray().Select(value => value.GetGuid()).ToArray();
        Assert.AreEqual(9, ids.Length); Assert.AreEqual(137, scalarIds.Length); Assert.AreEqual(3, restIds.Length);
        var baseIds = ids.Concat(scalarIds).ToArray();
        var allIds = baseIds.Concat(restIds).ToArray();
        Assert.AreEqual(149, allIds.Distinct().Count());
        var restActor = fixture.GetProperty("restActor").GetGuid();
        var alice = fixture.GetProperty("alice").GetGuid();
        var bob = fixture.GetProperty("bob").GetGuid();
        var outsider = fixture.GetProperty("outsider").GetGuid();
        var tag = fixture.GetProperty("tag").GetString();
        var conditionalOps = fixture.GetProperty("sharingOperations").EnumerateArray().Select(value => value.GetString()!).ToArray();
        var restRequests = fixture.GetProperty("requests").EnumerateArray()
            .Where(value => value.TryGetProperty("key", out _)).ToArray();
        Assert.AreEqual(graphProjection ? 8 : 5, restRequests.Length);
        Assert.IsTrue(restRequests.All(value => value.GetProperty("provider").GetString() == "MongoDBOASIS"));
        var mongo = new MongoClient(uri);
        var usage = mongo.GetDatabase(usageDb);
        var operations = await usage.GetCollection<UsageOutboxOperation>("subscription_usage_outbox_v1")
            .Find(Builders<UsageOutboxOperation>.Filter.Empty).ToListAsync();
        Assert.AreEqual(graphProjection ? 353 : 350, operations.Count);
        Assert.IsTrue(operations.All(operation => operation.State == "settled" &&
            new[] { alice, bob, outsider, restActor }.Select(value => value.ToString("D")).Contains(operation.UserId)));
        var rest = operations.Where(operation => operation.UserId == restActor.ToString("D")).ToArray();
        Assert.AreEqual(graphProjection ? 8 : 5, rest.Length);
        Assert.AreEqual(graphProjection ? 13 : 4, rest.Sum(operation => operation.ProviderAttempts.Count));
        Assert.AreEqual(graphProjection ? 13 : 4, rest.Sum(operation => operation.Receipts.Count));
        foreach (var request in restRequests)
        {
            var identityBytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                new[] { "WEB4", restActor.ToString("D"), request.GetProperty("key").GetString() })));
            var operationId = new Guid(identityBytes.AsSpan(0, 16)).ToString("D");
            var row = rest.Single(operation => operation.Id == operationId);
            Assert.AreEqual(row.Receipts.Sum(receipt => receipt.ActualCostUsd), row.Settlement.ActualCostUsd);
            if (row.ProviderAttempts.Count == 0)
                Assert.IsFalse(string.IsNullOrWhiteSpace(row.NoProviderExecutionReason));
            else
            {
                Assert.IsTrue(row.ProviderAttempts.Count >= 1 && row.ProviderAttempts.Count <= 3);
                Assert.AreEqual(row.ProviderAttempts.Count, row.Receipts.Count);
            }
        }
        var snapshots = new List<object>();
        foreach (var provider in new IOASISStorageProvider[] { new MongoProvider(uri, storageDb),
            new SqliteProvider("Data Source=" + sqlite), new Neo4jOASIS("bolt://127.0.0.1:17687", "neo4j", "oasis-local-test-only") })
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            try
            {
                var loaded = await provider.LoadAllHolonsAsync(loadChildren: false);
                Assert.IsFalse(loaded.IsError, loaded.Message);
                var values = loaded.Result.ToArray();
                var isMongo = provider is MongoProvider;
                CollectionAssert.AreEquivalent(isMongo ? allIds : baseIds, values.Select(value => value.Id).ToArray());
                foreach (var value in values)
                {
                    Assert.IsTrue(new[] { alice, bob, restActor }.Contains(value.CreatedByAvatarId));
                    if (scalarIds.Contains(value.Id))
                        Assert.AreEqual(tag, value.MetaData["scalarFixture"].ToString());
                    if (ids.Take(5).Contains(value.Id) || restIds.Contains(value.Id))
                        Assert.AreEqual(tag, value.MetaData["fixture"].ToString());
                    snapshots.Add(new { provider = provider.ProviderType.Name, value.Id, value.Version,
                        sha256 = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value))) });
                }
                Assert.AreEqual(bob, values.Single(value => value.Id == ids[0]).CreatedByAvatarId);
                if (isMongo)
                {
                    var root = values.Single(value => value.Id == restIds[0]);
                    Assert.AreEqual("REST root", root.Name);
                    Assert.AreEqual("first", root.MetaData["rest-write"].ToString());
                    Assert.AreEqual("REST second", values.Single(value => value.Id == restIds[1]).Name);
                    Assert.AreEqual(restIds[0], values.Single(value => value.Id == restIds[2]).ParentHolonId);
                    if (graphProjection)
                    {
                        var graph = await provider.LoadHolonAsync(restIds[0], loadChildren: true);
                        Assert.IsFalse(graph.IsError, graph.Message);
                        CollectionAssert.AreEqual(new[] { restIds[2] }, graph.Result.Children.Select(child => child.Id).ToArray());
                        Assert.AreEqual("committed", values.Single(value => value.Id == restIds[1]).MetaData["rest-batch"].ToString());
                    }
                    // Reproduce the planner's observation guard without executing
                    // a save or replaying any original HTTP operation.
                    var detached = StoredRecordJson.Project<Holon>(root);
                    detached.Children = new List<IHolon> { StoredRecordJson.Project<Holon>(values.Single(value => value.Id == ids[0])) };
                    var error = Assert.ThrowsException<InvalidOperationException>(() => HolonChangeTracking.ValidateChildSelectionForSave(detached));
                    StringAssert.Contains(error.Message, "fully observed");
                }
            }
            finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
        }
        var receipts = await mongo.GetDatabase(storageDb).GetCollection<BsonDocument>("CosmicWriteReceipt")
            .Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
        Assert.IsTrue(receipts.All(receipt => conditionalOps.Contains(receipt["_id"].AsString)));
        using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        await using var inspection = driver.AsyncSession();
        var neoReceipts = await (await inspection.RunAsync("MATCH(r:CosmicWriteReceipt) RETURN r.OperationId AS id")).ToListAsync();
        Assert.IsTrue(neoReceipts.All(row => conditionalOps.Contains(row["id"].As<string>())));
        Assert.AreEqual(hash, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(manifest))));
        await File.WriteAllTextAsync(Path.Combine(work, graphProjection ? "joined-depth-reviewed-proof.json" : "joined-rest-reviewed-proof.json"), JsonSerializer.Serialize(new
        {
            hash, snapshots, operations = operations.Select(operation => new { operation.Id, operation.State,
                operation.UserId, operation.ObservedOutcome, operation.NoProviderExecutionReason,
                attempts = operation.ProviderAttempts.Count, receipts = operation.Receipts.Count })
        }));
        // Every exact identity and terminal outcome is verified above. Only the
        // owner-created fixture resources are now disposable.
        var owned = allIds.Select(value => value.ToString("D")).ToArray();
        await (await inspection.RunAsync("MATCH(h:Holon) WHERE h.Id IN $ids DETACH DELETE h", new { ids = owned })).ConsumeAsync();
        await (await inspection.RunAsync("MATCH(v:OasisRecordVersion {Kind:'Holon'}) WHERE v.RecordId IN $ids DETACH DELETE v", new { ids = owned })).ConsumeAsync();
        await (await inspection.RunAsync("MATCH(r:CosmicWriteReceipt) WHERE r.OperationId IN $ops DETACH DELETE r", new { ops = conditionalOps })).ConsumeAsync();
        await mongo.DropDatabaseAsync(storageDb);
        await mongo.DropDatabaseAsync(usageDb);
        await File.WriteAllTextAsync(manifest, JsonSerializer.Serialize(new { stage = "successful-awaiting-runner-cleanup", sqlite }));
    }
}
