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
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Serialization;
using MongoDB.Bson.Serialization;
using NativeHolon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;
using NativeHelper = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers.DataHelper;

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
        {
            var sourceReceipts = await client.GetDatabase("cosmic_committed_replica_source_fixture").GetCollection<BsonDocument>("CosmicWriteReceipt")
                .WithReadConcern(ReadConcern.Majority).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
            if (sourceReceipts != 0) await InspectCommittedReplicaFenceProbe(client);
            else await InspectCapturedTopologyProbe(client);
            return;
        }

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

    private async Task InspectCommittedReplicaFenceProbe(MongoClient client)
    {
        var names = new[] { "cosmic_committed_replica_source_fixture", "cosmic_committed_replica_target_fixture" };
        var receiptSets = new List<BsonDocument>[] { new(), new() }; var rowSets = new List<BsonDocument>[] { new(), new() };
        var latest = new Dictionary<Guid, IHolon>[] { new(), new() };
        for (var index = 0; index < names.Length; index++)
        {
            var db = client.GetDatabase(names[index]); using var cursor = await db.ListCollectionNamesAsync(); var collections = await cursor.ToListAsync();
            Assert.Contains("CosmicOwnedFixture", collections);
            var receipts = receiptSets[index] = await db.GetCollection<BsonDocument>("CosmicWriteReceipt").WithReadConcern(ReadConcern.Majority)
                .Find(FilterDefinition<BsonDocument>.Empty).ToListAsync(); Assert.Equal(3, receipts.Count);
            foreach (var receipt in receipts)
            {
                using var body = System.Text.Json.JsonDocument.Parse(receipt["ResultJson"].AsString);
                Assert.Equal(receipt["_id"].AsString, body.RootElement.GetProperty("operation").GetString());
                Assert.Equal(receipt["Fingerprint"].AsString, body.RootElement.GetProperty("intent").GetString());
                foreach (var node in HolonGraphWritePlan.Create(ConditionalHolonReceipt.Read(receipt["ResultJson"].AsString), true, true, 0, 0).Nodes)
                {
                    var row = node.Source; Assert.StartsWith("owned ", row.Name);
                    if (!latest[index].TryGetValue(row.Id, out var previous) || row.Version > previous.Version) latest[index][row.Id] = row;
                }
                output.WriteLine($"Inspected {names[index]} operation {receipt["_id"].AsString}, receipt SHA256={Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(receipt.ToBson()))}");
            }
            Assert.Equal(2, latest[index].Count);
            var rows = rowSets[index] = await db.GetCollection<BsonDocument>("Holon").WithReadConcern(ReadConcern.Majority)
                .Find(FilterDefinition<BsonDocument>.Empty).ToListAsync(); Assert.Equal(index == 0 ? 4 : 2, rows.Count);
            foreach (var row in rows)
            {
                var value = NativeHelper.ConvertMongoEntityToOASISHolon(BsonSerializer.Deserialize<NativeHolon>(row), false);
                if (latest[index].TryGetValue(value.Id, out var acknowledged))
                {
                    Assert.Equal(acknowledged.Name, value.Name); Assert.Equal(acknowledged.VersionId, value.VersionId); Assert.Equal(acknowledged.Version, value.Version);
                    Assert.Equal(CosmicConcurrency.GetProviderRevisionId(acknowledged, ProviderType.MongoDBOASIS), CosmicConcurrency.GetProviderRevisionId(value, ProviderType.MongoDBOASIS));
                }
                else { Assert.Equal(0, index); Assert.Contains(value.Name, new[] { "owned captured root", "owned captured child" }); Assert.Equal(0, value.Version); }
            }
            var fences = await db.GetCollection<BsonDocument>("CosmicOperationFence").WithReadConcern(ReadConcern.Majority)
                .Find(FilterDefinition<BsonDocument>.Empty).ToListAsync(); Assert.Equal(3, fences.Count);
            Assert.Equal(receipts.Select(row => row["_id"].AsString).OrderBy(value => value), fences.Select(row => row["_id"].AsString).OrderBy(value => value));
            Assert.All(fences, row => { Assert.Equal(3, row.ElementCount); Assert.True(row["Generation"].ToInt64() >= 1); Assert.Equal(BsonType.DateTime, row["CreatedAtUtc"].BsonType); });
            foreach (var collection in collections.Except(new[] { "Holon", "CosmicWriteReceipt", "CosmicOperationFence", "CosmicReplicaHead" }))
                Assert.Equal(0, await db.GetCollection<BsonDocument>(collection).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        }
        var sourceRoot = Assert.Single(latest[0].Values, value => value.Version == 3); Assert.Equal("owned source third", sourceRoot.Name);
        var targetReceipt = Assert.Single(receiptSets[1], receipt => ConditionalHolonReceipt.Read(receipt["ResultJson"].AsString).Single().Version == 3);
        var request = new CommittedHolonReplicaRequest(Guid.Parse(targetReceipt["_id"].AsString), "owned mongo source", ProviderType.MongoDBOASIS,
            sourceRoot.CreatedByAvatarId, ConditionalHolonOperation.Save, new[] { sourceRoot }, saveChildren: false);
        var plan = CommittedHolonReplicaPlan.Create(request, ProviderType.MongoDBOASIS);
        var originalCommitted = Assert.Single(ConditionalHolonReceipt.Read(targetReceipt["ResultJson"].AsString, plan));
        Assert.Equal(sourceRoot.VersionId, originalCommitted.VersionId);
        var targetDb = client.GetDatabase(names[1]);
        var heads = await targetDb.GetCollection<BsonDocument>("CosmicReplicaHead").WithReadConcern(ReadConcern.Majority)
            .Find(FilterDefinition<BsonDocument>.Empty).ToListAsync(); Assert.Equal(2, heads.Count);
        foreach (var head in heads)
        {
            Assert.Equal("owned mongo source", head["SourceId"].AsString); Assert.Equal(ProviderType.MongoDBOASIS.ToString(), head["SourceAuthority"].AsString);
            Assert.False(head["HardDeleted"].AsBoolean);
            Assert.Equal(head["SnapshotDigest"].AsString, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(head["SnapshotJson"].AsString))));
            var snapshot = StoredRecordJson.DeserializeStoredHolon(head["SnapshotJson"].AsString); var expected = latest[1][snapshot.Id];
            Assert.Equal(expected.VersionId, snapshot.VersionId); Assert.Equal(head["ProviderRevisionId"].AsString, CosmicConcurrency.GetProviderRevisionId(expected, ProviderType.MongoDBOASIS)!.Value.ToString("D"));
            Assert.Equal(latest[0][snapshot.Id].VersionId, snapshot.VersionId);
        }
        output.WriteLine("Majority-visible source/target rows, original committed third-version receipt, replica heads and operation fences verified without replay.");
        // Both exact, previously absent owned fixture databases are verified before either removal.
        foreach (var name in names) await client.DropDatabaseAsync(name);
    }

}
