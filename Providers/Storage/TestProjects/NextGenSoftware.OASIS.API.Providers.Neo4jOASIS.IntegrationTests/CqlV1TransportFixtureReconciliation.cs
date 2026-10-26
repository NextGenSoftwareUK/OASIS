using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
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
    public Task ReviewedSignedTransportEnumFixturePreservesOriginalsBeforeExactCleanup()
        => ReviewSignedTransportFixture(false);

    [TestMethod]
    public Task ReviewedSignedTransportBodyWriterFixturePreservesCommittedOriginalsBeforeExactCleanup()
        => ReviewSignedTransportFixture(true);

    private static async Task ReviewSignedTransportFixture(bool committedGrpc)
    {
        var hash = committedGrpc ? "E722AC607FAB11F2CC6BFD2DC140C081FFC0683FCCB41093DBEF1D62F05273DE"
            : "4859E1760E50FF1D9F7425A866BCF0145F0D8520F5BB58F04DFDD5D77E120858";
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        Assert.AreEqual("bolt://127.0.0.1:17687", Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        var manifest = Path.Combine(work, "cql-v1-web4-fixture.json");
        var original = await File.ReadAllBytesAsync(manifest);
        Assert.AreEqual(hash, Convert.ToHexString(SHA256.HashData(original)));
        using var document = JsonDocument.Parse(original);
        var fixture = document.RootElement;
        Assert.AreEqual("before-transport-http", fixture.GetProperty("stage").GetString());
        Assert.AreEqual("cosmic_cql_v1_web4_storage", fixture.GetProperty("storageDb").GetString());
        Assert.AreEqual("cosmic_cql_v1_web4_usage", fixture.GetProperty("usageDb").GetString());
        var sqlite = Path.Combine(work, "cql-v1-web4.sqlite");
        Assert.AreEqual(sqlite, fixture.GetProperty("sqlite").GetString());
        Guid[] Ids(string key) => fixture.GetProperty(key).EnumerateArray().Select(value => value.GetGuid()).ToArray();
        var ids = Ids("ids"); var scalars = Ids("scalarIds"); var rest = Ids("restIds"); var transports = Ids("transportIds");
        Assert.AreEqual(9, ids.Length); Assert.AreEqual(137, scalars.Length);
        Assert.AreEqual(3, rest.Length); Assert.AreEqual(3, transports.Length);
        var baseline = ids.Concat(scalars).Concat(rest).ToArray();
        var all = baseline.Concat(transports).ToArray();
        var alice = fixture.GetProperty("alice").GetGuid();
        var bob = fixture.GetProperty("bob").GetGuid();
        var outsider = fixture.GetProperty("outsider").GetGuid();
        var restActor = fixture.GetProperty("restActor").GetGuid();
        var actor = fixture.GetProperty("transportActor").GetGuid();
        var tag = fixture.GetProperty("tag").GetString();
        var conditionalOps = fixture.GetProperty("sharingOperations").EnumerateArray().Select(value => value.GetString()!).ToArray();
        var transportRequests = fixture.GetProperty("requests").EnumerateArray()
            .Where(value => value.TryGetProperty("key", out var key) && key.GetString()!.StartsWith("transport-", StringComparison.Ordinal)).ToArray();
        Assert.AreEqual(4, transportRequests.Length);
        Assert.IsTrue(transportRequests.All(value => value.GetProperty("provider").GetString() == "MongoDBOASIS"));
        var mongo = new MongoClient(uri);
        var operations = await mongo.GetDatabase("cosmic_cql_v1_web4_usage")
            .GetCollection<UsageOutboxOperation>("subscription_usage_outbox_v1")
            .Find(Builders<UsageOutboxOperation>.Filter.Empty).ToListAsync();
        Assert.AreEqual(382, operations.Count);
        Assert.IsTrue(operations.All(operation => operation.State == "settled" &&
            new[] { alice, bob, outsider, restActor, actor }.Select(value => value.ToString("D")).Contains(operation.UserId)));
        var transportRows = operations.Where(operation => operation.UserId == actor.ToString("D")).ToArray();
        Assert.AreEqual(4, transportRows.Length);
        Assert.AreEqual(committedGrpc ? 7 : 4, transportRows.Sum(row => row.ProviderAttempts.Count));
        Assert.AreEqual(committedGrpc ? 7 : 4, transportRows.Sum(row => row.Receipts.Count));
        foreach (var request in transportRequests)
        {
            var digest = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                new[] { "WEB4", actor.ToString("D"), request.GetProperty("key").GetString() })));
            var operationId = new Guid(digest.AsSpan(0, 16)).ToString("D");
            var row = transportRows.Single(value => value.Id == operationId);
            Assert.AreEqual(row.Receipts.Sum(receipt => receipt.ActualCostUsd), row.Settlement.ActualCostUsd);
            if (row.ProviderAttempts.Count == 0) Assert.IsFalse(string.IsNullOrEmpty(row.NoProviderExecutionReason));
        }
        var grpc = transportRows.Single(row => row.Endpoint.EndsWith("/oasis.web4.DataService/SaveHolon", StringComparison.Ordinal));
        Assert.AreEqual(committedGrpc ? 3 : 0, grpc.ProviderAttempts.Count);
        Assert.AreEqual(committedGrpc ? "succeeded" : "failed", grpc.Settlement.Outcome);
        Assert.AreEqual(committedGrpc ? 2 : 1, transportRows.Count(row => row.Settlement.Outcome == "succeeded"));

        var snapshots = new List<object>();
        foreach (var provider in new IOASISStorageProvider[] { new MongoProvider(uri, "cosmic_cql_v1_web4_storage"),
            new SqliteProvider("Data Source=" + sqlite), new Neo4jOASIS("bolt://127.0.0.1:17687", "neo4j", "oasis-local-test-only") })
        {
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            try
            {
                var loaded = await provider.LoadAllHolonsAsync(loadChildren: false);
                Assert.IsFalse(loaded.IsError, loaded.Message);
                var values = loaded.Result.ToArray();
                var isMongo = provider is MongoProvider;
                CollectionAssert.AreEquivalent(isMongo ? all : baseline, values.Select(value => value.Id).ToArray());
                foreach (var value in values)
                {
                    Assert.IsTrue(new[] { alice, bob, restActor, actor }.Contains(value.CreatedByAvatarId));
                    if (scalars.Contains(value.Id)) Assert.AreEqual(tag, value.MetaData["scalarFixture"].ToString());
                    if (ids.Take(5).Contains(value.Id) || rest.Contains(value.Id)) Assert.AreEqual(tag, value.MetaData["fixture"].ToString());
                    snapshots.Add(new { provider = provider.ProviderType.Name, value.Id, value.Version,
                        sha256 = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(value))) });
                }
                if (isMongo)
                {
                    Assert.AreEqual("GraphQL native create", values.Single(value => value.Id == transports[0]).Name);
                    Assert.AreEqual(actor, values.Single(value => value.Id == transports[0]).CreatedByAvatarId);
                    Assert.AreEqual(committedGrpc ? "gRPC acknowledged graph" : "transport root", values.Single(value => value.Id == transports[1]).Name);
                    Assert.AreEqual(transports[1], values.Single(value => value.Id == transports[2]).ParentHolonId);
                    var graph = await provider.LoadHolonAsync(transports[1], loadChildren: true);
                    Assert.IsFalse(graph.IsError, graph.Message);
                    CollectionAssert.AreEqual(new[] { transports[2] }, graph.Result.Children.Select(child => child.Id).ToArray());
                }
            }
            finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
        }
        var receipts = await mongo.GetDatabase("cosmic_cql_v1_web4_storage").GetCollection<BsonDocument>("CosmicWriteReceipt")
            .Find(Builders<BsonDocument>.Filter.Empty).ToListAsync();
        Assert.IsTrue(receipts.All(receipt => conditionalOps.Contains(receipt["_id"].AsString)));
        using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        await using var inspection = driver.AsyncSession();
        var neoReceipts = await (await inspection.RunAsync("MATCH(r:CosmicWriteReceipt) RETURN r.OperationId AS id")).ToListAsync();
        Assert.IsTrue(neoReceipts.All(row => conditionalOps.Contains(row["id"].As<string>())));
        Assert.AreEqual(hash, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(manifest))));
        await File.WriteAllTextAsync(Path.Combine(work, committedGrpc ? "joined-transport-bodywriter-reviewed-proof.json" : "joined-transport-reviewed-proof.json"), JsonSerializer.Serialize(new
        {
            hash, snapshots, operations = operations.Select(row => new { row.Id, row.State, row.UserId,
                row.ObservedOutcome, row.NoProviderExecutionReason, attempts = row.ProviderAttempts.Count, receipts = row.Receipts.Count })
        }));
        // All original operations and exact owned identities have been checked.
        // No operation is replayed; only this reviewed fixture is now disposable.
        var owned = all.Select(value => value.ToString("D")).ToArray();
        await (await inspection.RunAsync("MATCH(h:Holon) WHERE h.Id IN $ids DETACH DELETE h", new { ids = owned })).ConsumeAsync();
        await (await inspection.RunAsync("MATCH(v:OasisRecordVersion {Kind:'Holon'}) WHERE v.RecordId IN $ids DETACH DELETE v", new { ids = owned })).ConsumeAsync();
        await (await inspection.RunAsync("MATCH(r:CosmicWriteReceipt) WHERE r.OperationId IN $ops DETACH DELETE r", new { ops = conditionalOps })).ConsumeAsync();
        await mongo.DropDatabaseAsync("cosmic_cql_v1_web4_storage");
        await mongo.DropDatabaseAsync("cosmic_cql_v1_web4_usage");
        await File.WriteAllTextAsync(manifest, JsonSerializer.Serialize(new { stage = "successful-awaiting-runner-cleanup", sqlite }));
    }
}
