using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;
using Xunit.Abstractions;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class CosmicChildFixtureReconciliationTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", "OwnedFixtureReconciliation")]
    public async Task InspectOriginalChildFixtureBeforeRemovingOnlyOwnedDatabase()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.."));
        using var lease = File.Open(Path.Combine(workspace, "TestResults/SubscriptionUsageCurrent/cosmic-child-environment.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        const string name = "cosmic_child_traversal_fixture";
        var client = new MongoClient(uri); var database = client.GetDatabase(name);
        var rows = await database.GetCollection<BsonDocument>("Holon").Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        Assert.Equal(3, rows.Count);
        var root = Assert.Single(rows.Where(row => row["Name"] == "owned cosmic parent"));
        var child = Assert.Single(rows.Where(row => row["Name"] == "owned cosmic child"));
        var grand = Assert.Single(rows.Where(row => row["Name"] == "owned cosmic grandchild"));
        Assert.Equal(root["HolonId"], child["ParentHolonId"]);
        Assert.Equal(child["HolonId"], grand["ParentHolonId"]);
        Assert.Equal(child["HolonId"], root["Children"].AsBsonArray.Single()["_id"]);
        Assert.Equal(grand["HolonId"], child["Children"].AsBsonArray.Single()["_id"]);
        using var collections = await database.ListCollectionNamesAsync();
        foreach (var collection in await collections.ToListAsync())
            if (collection != "Holon") Assert.Equal(0, await database.GetCollection<BsonDocument>(collection)
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        foreach (var row in rows)
            output.WriteLine(new BsonDocument { { "Id", row["HolonId"] }, { "Name", row["Name"] },
                { "Parent", row["ParentHolonId"] }, { "BsonSHA256", Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(row.ToBson())) } }.ToJson());
        // Exact absent-before-created fixture, independent native inspection; no save or replay.
        await client.DropDatabaseAsync(name);
    }
}
