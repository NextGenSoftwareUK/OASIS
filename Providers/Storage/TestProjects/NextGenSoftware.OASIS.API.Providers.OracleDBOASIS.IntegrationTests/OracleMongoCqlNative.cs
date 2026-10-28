using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Providers.OracleDBOASIS.IntegrationTests;

public sealed partial class OracleCqlNativeTests
{
    private static async Task VerifyMongoAsync(string root, OracleDBOASIS oracle, Guid[] oracleIds)
    {
        const string database = "cosmic_cql_mongo_oracle_demo";
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        var journal = Path.Combine(root, "LocalState/COSMIC/OracleCqlCurrent/mongo.pending.json");
        Assert.IsFalse(File.Exists(journal), "Preserve original Mongo fixture; no replay.");
        var client = new MongoClient(uri);
        using (var names = await client.ListDatabaseNamesAsync())
            Assert.IsFalse((await names.ToListAsync()).Contains(database));

        var actor = Guid.NewGuid();
        var records = oracleIds.Take(2).Append(Guid.NewGuid()).Select((id, index) => new Holon
        {
            Id = id, Name = "COSMIC owned Mongo Oracle copy " + index, CreatedByAvatarId = actor
        }).ToArray();
        using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(file, new { database, actor, ids = records.Select(row => row.Id) });
            file.Flush(true);
        }

        var mongo = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(uri, database)
        { IsVersionControlEnabled = false };
        try
        {
            Assert.IsFalse((await mongo.ActivateProviderAsync()).IsError);
            foreach (var record in records)
                Assert.IsFalse((await mongo.SaveHolonAsync(record, saveChildren: false)).IsError);

            using var output = new StringWriter();
            var dna = new NextGenSoftware.OASIS.API.DNA.OASISDNA
            {
                OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                {
                    HyperDriveMode = NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,
                    StorageProviders = new NextGenSoftware.OASIS.API.DNA.StorageProviderSettings { LogSwitchingProviders = false }
                }
            };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            var registry = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
            registry.RegisterProvider(oracle);
            registry.RegisterProvider(mongo);
            var manager = new HolonManager(null, dna, registry);
            foreach (var selection in new[] { oracleIds, oracleIds.Take(2).ToArray() })
            {
                var result = await CosmicCqlDemo.IdentityFederationWalkthrough.RunAsync(
                    manager, mongo, oracle, selection, records.Select(row => row.Id), oracleIds, actor, output);
                Assert.AreEqual(selection.Length + 2, result.ObservedInputRows);
                Assert.AreEqual(selection.Length + 2, result.Rows.Count);
                Assert.AreEqual(2, result.Rows.Count(row => row.Selected.Provider == ProviderType.MongoDBOASIS));
                Assert.AreEqual(selection.Length, result.Rows.Count(row => row.Selected.Provider == ProviderType.OracleDBOASIS));
                Assert.IsFalse(result.Rows.Any(row => row.Selected.Row.Id == records[2].Id));
            }

            // This exact database was absent before intent publication. Verify its
            // complete physical ownership before dropping only this test database.
            var native = client.GetDatabase(database);
            using (var names = await native.ListCollectionNamesAsync())
                CollectionAssert.AreEquivalent(new[] { "Avatar", "AvatarDetail", "CosmicHolonOutbox", "Holon" },
                    (await names.ToListAsync()).ToArray());
            foreach (var empty in new[] { "Avatar", "AvatarDetail", "CosmicHolonOutbox" })
                Assert.AreEqual(0L, await native.GetCollection<BsonDocument>(empty).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            Assert.AreEqual(3L, await native.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
            foreach (var record in records)
            {
                var loaded = await mongo.LoadHolonAsync(record.Id, loadChildren: false);
                Assert.IsFalse(loaded.IsError);
                Assert.AreEqual(actor, loaded.Result.CreatedByAvatarId);
                Assert.AreEqual(record.Name, loaded.Result.Name);
            }
            Assert.IsFalse((await mongo.DeActivateProviderAsync()).IsError);
            await client.DropDatabaseAsync(database);
            using (var names = await client.ListDatabaseNamesAsync())
                Assert.IsFalse((await names.ToListAsync()).Contains(database));
            File.Delete(journal);
        }
        finally { await mongo.DeActivateProviderAsync(); }
    }
}
