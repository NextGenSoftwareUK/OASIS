using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using Neo4j.Driver;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using MongoProvider = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS;
using SqliteProvider = NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

[TestClass]
public sealed class CqlV1FixtureReconciliationTests
{
    [TestMethod]
    public Task ReviewedRejectedStringFixtureContainsOnlySettledOwnedOperationsBeforeExactCleanup()
        =>Reconcile("1ECEF8404C4CF8E5E368E02D69D0B88EE8457995C36B3C183FBB8014D9EB1E0C",false);

    [TestMethod]
    public Task ReviewedGraphOrderingFixtureContainsOnlySettledOwnedRecordsBeforeExactCleanup()
        =>Reconcile("6CA92E59C526CFD4592D07D70ED39DD821C97D4A81E16D86AFCE7590B4C1A87B",true);

    private static async Task Reconcile(string expectedHash,bool graphFixture)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string storageDb = "cosmic_cql_v1_web4_storage", usageDb = "cosmic_cql_v1_web4_usage";
        Assert.AreEqual(uri, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        Assert.AreEqual("bolt://127.0.0.1:17687", Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_URI"));
        var work = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_OWNED_NEO4J_WORK")!);
        var manifest = Path.Combine(work, "cql-v1-web4-fixture.json");
        var bytes = await File.ReadAllBytesAsync(manifest);
        Assert.AreEqual(expectedHash, Convert.ToHexString(SHA256.HashData(bytes)), "Reviewed manifest must remain unchanged.");
        using var json = JsonDocument.Parse(bytes);
        var fixture = json.RootElement;
        Assert.AreEqual("before-http-request", fixture.GetProperty("stage").GetString());
        Assert.AreEqual(storageDb, fixture.GetProperty("storageDb").GetString());
        Assert.AreEqual(usageDb, fixture.GetProperty("usageDb").GetString());
        var sqlite = Path.GetFullPath(Path.Combine(work, "cql-v1-web4.sqlite"));
        Assert.AreEqual(sqlite, fixture.GetProperty("sqlite").GetString());
        var ids = fixture.GetProperty("ids").EnumerateArray().Select(id => id.GetGuid()).ToArray();
        var names = graphFixture ? new[]{"A-private","A-index","B-owned","C-public","D-owned",
            "public child","private child","owned child","public behind private"}
            : new[]{"A-private","A-index","B-owned","C-public","D-owned"};
        Assert.AreEqual(names.Length,ids.Length);
        Assert.AreEqual(names.Length, ids.Distinct().Count());
        var alice = fixture.GetProperty("alice").GetGuid();
        var bob = fixture.GetProperty("bob").GetGuid();
        var tag = fixture.GetProperty("tag").GetString();
        var mongo = new MongoClient(uri);
        var operations = await mongo.GetDatabase(usageDb).GetCollection<UsageOutboxOperation>("subscription_usage_outbox_v1")
            .Find(Builders<UsageOutboxOperation>.Filter.Empty).ToListAsync();
        // The first five requests were anonymous, bad signature/issuer, expired, and
        // mismatched signed claims. The final prefix request was a settled pre-I/O rejection.
        var expectedOperations = fixture.GetProperty("requests").EnumerateArray().Skip(5).Select(request =>
        {
            var canonical = JsonSerializer.Serialize(new[] { "WEB4", request.GetProperty("actor").GetGuid().ToString("D"),
                request.GetProperty("operation").GetString()! });
            return new Guid(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(canonical)).AsSpan(0, 16)).ToString("D");
        }).Distinct().ToArray();
        CollectionAssert.AreEquivalent(expectedOperations, operations.Select(operation => operation.Id).ToArray());
        foreach (var operation in operations)
        {
            Assert.AreEqual("WEB4", operation.Service);
            Assert.AreEqual("settled", operation.State, "No indeterminate operation may be discarded.");
            if (operation.Receipts.Count == 0)
            {
                Assert.AreEqual(0, operation.ProviderAttempts.Count);
                Assert.IsFalse(string.IsNullOrEmpty(operation.NoProviderExecutionReason));
                Assert.AreEqual("failed", operation.ObservedOutcome);
            }
            else
            {
                Assert.AreEqual(1, operation.Receipts.Count);
                Assert.AreEqual(1, operation.ProviderAttempts.Count);
                Assert.AreEqual(0.002m, operation.Receipts.Single().ActualCostUsd);
            }
        }

        using var driver = GraphDatabase.Driver("bolt://127.0.0.1:17687", AuthTokens.Basic("neo4j", "oasis-local-test-only"));
        await using var inspection = driver.AsyncSession();
        var native = await (await inspection.RunAsync("MATCH (h:Holon) RETURN h.Id AS id")).ToListAsync();
        CollectionAssert.AreEquivalent(ids.Select(id => id.ToString("D")).ToArray(), native.Select(row => row["id"].As<string>()).ToArray());
        var providers = new IOASISStorageProvider[] { new MongoProvider(uri, storageDb),
            new SqliteProvider("Data Source=" + sqlite), new Neo4jOASIS("bolt://127.0.0.1:17687", "neo4j", "oasis-local-test-only") };
        var snapshots = new List<object>();
        foreach (var provider in providers)
        {
            var activation = await provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Message);
            try
            {
                var all=await provider.LoadAllHolonsAsync(loadChildren:false);
                Assert.IsFalse(all.IsError,all.Message);
                CollectionAssert.AreEquivalent(ids,all.Result.Select(row=>row.Id).ToArray());
                for (var i = 0; i < ids.Length; i++)
                {
                    var loaded = await provider.LoadHolonAsync(ids[i], loadChildren: false);
                    Assert.IsFalse(loaded.IsError, loaded.Message);
                    var holon = loaded.Result;
                    Assert.AreEqual(ids[i], holon.Id);
                    Assert.AreEqual(names[i], holon.Name);
                    Assert.AreEqual(i is 2 or 4 or 7 ? alice : bob, holon.CreatedByAvatarId);
                    Assert.AreEqual(i is 3 or 5 or 8, holon.IsPublic);
                    Assert.AreEqual(i<5?Guid.Empty:i==8?ids[6]:ids[3],holon.ParentHolonId);
                    if(i<5)Assert.AreEqual(tag,holon.MetaData["fixture"].ToString());
                    else Assert.IsFalse(holon.MetaData.ContainsKey("fixture"));
                    if(i==6)Assert.AreEqual(alice.ToString("D"),holon.MetaData["SHARED_WITH"].ToString());
                    snapshots.Add(new { provider = provider.ProviderType.Name, holon.Id,
                        hash = Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new
                        { holon.Id, holon.Name, holon.CreatedByAvatarId, holon.IsPublic, holon.Version, holon.MetaData }))) });
                }
            }
            finally { Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError); }
        }
        // All ownership and terminal-state checks finish before the first destructive action.
        await File.WriteAllTextAsync(Path.Combine(work, "cql-v1-web4-reviewed-reconciliation.json"),
            JsonSerializer.Serialize(new { expectedHash, snapshots, operations = operations.Select(op => new { op.Id, op.State, receipts = op.Receipts.Count }) }));
        var ownedIds = ids.Select(id => id.ToString("D")).ToArray();
        await (await inspection.RunAsync("MATCH (h:Holon) WHERE h.Id IN $ids DETACH DELETE h", new { ids = ownedIds })).ConsumeAsync();
        await (await inspection.RunAsync("MATCH (v:OasisRecordVersion {Kind:'Holon'}) WHERE v.RecordId IN $ids DETACH DELETE v", new { ids = ownedIds })).ConsumeAsync();
        await mongo.DropDatabaseAsync(storageDb);
        await mongo.DropDatabaseAsync(usageDb);
        await File.WriteAllTextAsync(manifest, JsonSerializer.Serialize(new { stage = "successful-awaiting-runner-cleanup", sqlite }));
    }
}
