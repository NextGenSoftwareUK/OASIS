using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class CosmicCqlRetainedInspection
{
    [Fact]
    [Trait("Category", "OwnedFixtureReconciliation")]
    public async Task InspectRetainedCqlFixtureWithoutMutation()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var work = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../..", "TestResults/SubscriptionUsageCurrent"));
        using var lease = File.Open(Path.Combine(work, "cosmic-cql-environment.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        var client = new MongoClient(uri);
        var names = await (await client.ListDatabaseNamesAsync()).ToListAsync();
        Assert.Contains("cosmic_cql_fixture", names);
        var db = client.GetDatabase("cosmic_cql_fixture");
        var snapshots = new List<object>();
        long bytes = 0;
        foreach (var name in (await (await db.ListCollectionNamesAsync()).ToListAsync()).OrderBy(x => x, StringComparer.Ordinal))
        {
            var collection = db.GetCollection<BsonDocument>(name);
            var rows = await collection.Find(FilterDefinition<BsonDocument>.Empty).Sort(new BsonDocument("_id", 1)).Limit(201).ToListAsync();
            Assert.True(rows.Count <= 200, "Inspection stops at the explicit per-collection bound; no mutation occurs.");
            var encoded = rows.Select(row => row.ToJson()).ToArray();
            bytes += encoded.Sum(row => (long)System.Text.Encoding.UTF8.GetByteCount(row));
            Assert.True(bytes <= 4 * 1024 * 1024, "Inspection exceeds its artifact bound; no mutation occurs.");
            snapshots.Add(new { collection = name, count = rows.Count, rows = encoded });
        }
        var artifact = Path.Combine(work, "mongo-cql-retained-inspection.json");
        await File.WriteAllTextAsync(artifact, JsonSerializer.Serialize(snapshots));
        await File.WriteAllTextAsync(Path.Combine(work, "mongo-cql-retained-inspection-sha256.txt"),
            Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(artifact))));
        // This method deliberately performs no update, delete, drop or provider activation.
    }
    [Fact]
    [Trait("Category", "OwnedFixtureReconciliation")]
    public async Task CleanupReviewedEmptyRetainedCqlFixture()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string hash = "C640207F460B4F0A9F9EF804E5A0823F1583E35BD58AC3513D89517CED7EF5C1";
        Assert.Equal(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var work = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../..", "TestResults/SubscriptionUsageCurrent"));
        using var lease = File.Open(Path.Combine(work, "cosmic-cql-environment.lock"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
        var path = Path.Combine(work, "mongo-cql-retained-inspection.json");
        var original = await File.ReadAllBytesAsync(path);
        Assert.Equal(hash, Convert.ToHexString(SHA256.HashData(original)));
        var expected = new[] { "Avatar", "AvatarDetail", "CosmicHolonOutbox", "CosmicOwnedFixture", "Holon" };
        using var proof = JsonDocument.Parse(original);
        Assert.Equal(expected, proof.RootElement.EnumerateArray().Select(row => row.GetProperty("collection").GetString()));
        foreach (var row in proof.RootElement.EnumerateArray())
        {
            Assert.Equal(0, row.GetProperty("count").GetInt32());
            Assert.Equal(0, row.GetProperty("rows").GetArrayLength());
        }
        var client = new MongoClient(uri);
        var db = client.GetDatabase("cosmic_cql_fixture");
        async Task VerifyEmpty()
        {
            Assert.Equal(expected, (await (await db.ListCollectionNamesAsync()).ToListAsync()).OrderBy(x => x, StringComparer.Ordinal));
            foreach (var name in expected)
                Assert.Equal(0, await db.GetCollection<BsonDocument>(name).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        }
        await VerifyEmpty();
        Assert.Equal(hash, Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(path))));
        await VerifyEmpty();
        // The fixed test marker, exact empty collection set and exclusive fixture
        // lease prove no saved node or pending outbox outcome is being discarded.
        await client.DropDatabaseAsync("cosmic_cql_fixture");
        Assert.DoesNotContain("cosmic_cql_fixture", await (await client.ListDatabaseNamesAsync()).ToListAsync());
        await File.WriteAllTextAsync(Path.Combine(work, "mongo-cql-retained-empty-cleanup.json"),
            JsonSerializer.Serialize(new { inspectionSha256 = hash, database = "cosmic_cql_fixture", records = 0, outboxOperations = 0, removed = true }));
    }

}
