using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Xunit;
using Xunit.Abstractions;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

public sealed class CosmicConditionalFixtureReconciliationTests(ITestOutputHelper output)
{
    [Fact]
    [Trait("Category", "OwnedFixtureReconciliation")]
    public async Task IndependentlyInspectRetainedConditionalFixtureBeforeOwnedCleanup()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri, Environment.GetEnvironmentVariable("OASIS_MONGO_REPLICA_SET_CONNECTION"));
        var workspace = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../.."));
        using var lease = File.Open(Path.Combine(workspace, "TestResults/SubscriptionUsageCurrent/cosmic-conditional-environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        const string name = "cosmic_conditional_fixture";
        var client = new MongoClient(uri); var database = client.GetDatabase(name);
        using var known = await client.ListDatabaseNamesAsync();
        if (!(await known.ToListAsync()).Contains(name))
        { await InspectCapturedTopologyProbe(client); return; }

        var receipts = await database.GetCollection<BsonDocument>("CosmicWriteReceipt").WithReadConcern(ReadConcern.Majority)
            .Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        Assert.NotEmpty(receipts);
        var identities = new HashSet<Guid>(); var uncertainReceipts = 0;
        foreach (var receipt in receipts)
        {
            var roots = ConditionalHolonReceipt.Read(receipt["ResultJson"].AsString);
            var graph = HolonGraphWritePlan.Create(roots, true, true, 0, 0);
            foreach (var node in graph.Nodes) { Assert.StartsWith("owned ", node.Snapshot.Name); identities.Add(node.Snapshot.Id); }
            if (roots.Any(root => root.Name == "owned uncertain root")) uncertainReceipts++;
            output.WriteLine(new BsonDocument { { "Operation", receipt["_id"] }, { "Fingerprint", receipt["Fingerprint"] },
                { "RootNames", new BsonArray(roots.Select(root => root.Name)) },
                { "ReceiptSHA256", Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(receipt.ToBson())) } }.ToJson());
        }
        var rows = await database.GetCollection<BsonDocument>("Holon").WithReadConcern(ReadConcern.Majority)
            .Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
        foreach (var row in rows)
        {
            Assert.StartsWith("owned ", row["Name"].AsString); Assert.Contains(Guid.Parse(row["HolonId"].AsString), identities);
            output.WriteLine(new BsonDocument { { "Id", row["HolonId"] }, { "Name", row["Name"] },
                { "RecordSHA256", Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(row.ToBson())) } }.ToJson());
        }
        using var collections = await database.ListCollectionNamesAsync();
        foreach (var collection in await collections.ToListAsync())
            if (collection != "Holon" && collection != "CosmicWriteReceipt")
                Assert.Equal(0, await database.GetCollection<BsonDocument>(collection).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        output.WriteLine($"Verified rows={rows.Count}, receipts={receipts.Count}, majority-visible uncertain-commit receipts={uncertainReceipts}.");
        // This exact database was absent before the failed fixture. No save/transaction is replayed.
        await client.DropDatabaseAsync(name);
    }
    private async Task InspectCapturedTopologyProbe(MongoClient client)
    {
        // This narrowly identifies the retained read-once failure before any conditional
        // source commit. Any different state is preserved for separate reconciliation.
        var names = new[] { "cosmic_committed_replica_source_fixture", "cosmic_committed_replica_target_fixture" };
        for (var index = 0; index < names.Length; index++)
        {
            var database = client.GetDatabase(names[index]);
            using var cursor = await database.ListCollectionNamesAsync(); var collections = await cursor.ToListAsync();
            Assert.Contains("CosmicOwnedFixture", collections);
            var rows = await database.GetCollection<BsonDocument>("Holon").WithReadConcern(ReadConcern.Majority)
                .Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
            Assert.Equal(index == 0 ? 1 : 0, rows.Count);
            if (index == 0)
            {
                var row = Assert.Single(rows); Assert.Equal("owned captured root", row["Name"].AsString);
                Assert.True(Guid.TryParse(row["HolonId"].AsString, out _));
                Assert.Equal(0, row["Version"].AsInt32); Assert.Empty(row["Children"].AsBsonArray);
                output.WriteLine("Retained captured root SHA256=" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(row.ToBson())));
            }
            foreach (var collection in collections.Where(value => value != "Holon"))
                Assert.Equal(0, await database.GetCollection<BsonDocument>(collection).WithReadConcern(ReadConcern.Majority)
                    .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            output.WriteLine($"Verified exact owned captured-topology probe {names[index]}: rows={rows.Count}, no populated auxiliary collections.");
        }
        // Both exact fixed fixtures were independently inspected before either removal.
        foreach (var name in names) await client.DropDatabaseAsync(name);
    }

}
