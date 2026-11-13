using System.Text.Json;
using CosmicCqlDemo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.OASISBootLoader;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass, DoNotParallelize]
public class Web3OwnedCqlMongoNativeTests
{
    [TestMethod]
    public async Task ReconcileOriginalV2MongoFixtureAndVerifyMissingReadContract()
    {
        if (Environment.GetEnvironmentVariable("OASIS_EVM_V2_MONGO_RECONCILE") != "1")
            Assert.Inconclusive("Explicit original-fixture reconciliation required.");
        var root = Environment.GetEnvironmentVariable("OASIS_EVM_V2_WORKTREE")!;
        var state = Path.Combine(root, "LocalState", "COSMIC", "ArbitrumV2MongoCurrent");
        Assert.IsFalse(File.Exists(Path.Combine(state, "fixture.cleaned.json")));
        using var intent = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(state, "fixture.intent.json")));
        const string database = "cosmic_cql_mongo_arbitrum_demo";
        Assert.AreEqual(database, intent.RootElement.GetProperty("database").GetString());
        var actor = intent.RootElement.GetProperty("actor").GetGuid();
        var ids = intent.RootElement.GetProperty("ids").EnumerateArray().Select(x => x.GetGuid()).ToArray();
        Assert.AreEqual(2, ids.Length);
        var mongo = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(
            "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000", database);
        Assert.IsFalse((await mongo.ActivateProviderAsync()).IsError);
        try
        {
            for (var index = 0; index < ids.Length; index++)
            {
                using var acknowledgement = JsonDocument.Parse(await File.ReadAllTextAsync(
                    Path.Combine(state, $"save-{index}.acknowledged.json")));
                Assert.AreEqual(ids[index], acknowledgement.RootElement.GetProperty("id").GetGuid());
                var observed = await mongo.LoadHolonAsync(ids[index], loadChildren: false);
                if (observed.IsError)
                    Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, observed.ErrorCode);
                else
                {
                    Assert.IsNotNull(observed.Result);
                    Assert.AreEqual(actor, observed.Result.CreatedByAvatarId);
                    Assert.AreEqual("Owned V2 paired copy " + index, observed.Result.Name);
                    Assert.IsFalse((await mongo.DeleteHolonAsync(ids[index])).IsError);
                }
                var absent = await mongo.LoadHolonAsync(ids[index], loadChildren: false);
                var syncAbsent = mongo.LoadHolon(ids[index], loadChildren: false);
                foreach (var read in new[] { absent, syncAbsent })
                {
                    Assert.IsTrue(read.IsError);
                    Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, read.ErrorCode);
                    Assert.IsNull(read.Result);
                    Assert.IsFalse(read.IsLoaded);
                    Assert.AreEqual(0, read.LoadedCount);
                }
            }
            using var receipt = new FileStream(Path.Combine(state, "fixture.cleaned.json"), FileMode.CreateNew);
            await JsonSerializer.SerializeAsync(receipt, new { reconciled = true, missingSyncAsyncVerified = 2 });
            receipt.Flush(true);
        }
        finally { Assert.IsFalse((await mongo.DeActivateProviderAsync()).IsError); }
    }

    [TestMethod]
    public async Task ExistingV2ArbitrumAndOwnedMongoCopiesFederateFiveToFour()
    {
        if (Environment.GetEnvironmentVariable("OASIS_EVM_V2_MONGO") != "1")
            Assert.Inconclusive("Explicit retained paired environment required.");

        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string database = "cosmic_cql_mongo_arbitrum_demo";
        var root = Environment.GetEnvironmentVariable("OASIS_EVM_V2_WORKTREE")
            ?? throw new InvalidOperationException("Select this worktree explicitly.");
        var state = Path.Combine(root, "LocalState", "COSMIC", "ArbitrumV2MongoCurrent");
        Directory.CreateDirectory(state);
        var intent = Path.Combine(state, "fixture.intent.json");
        Assert.IsFalse(File.Exists(intent), "Original paired outcome exists; reconcile without replay.");
        Assert.IsFalse(Directory.EnumerateFiles(state).Any(), "Preserve orphaned fixture receipts before dispatch.");
        var seedPath = Path.Combine(root, "LocalState", "COSMIC", "EvmCql", "Arbitrum", "demo-seed.json");
        var original = await File.ReadAllBytesAsync(seedPath);
        using var seed = JsonDocument.Parse(original);
        var actor = seed.RootElement.GetProperty("Actor").GetGuid();
        var ids = seed.RootElement.GetProperty("Ids").EnumerateArray().Select(x => x.GetGuid()).ToArray();
        Assert.AreEqual(3, ids.Length);
        Assert.AreEqual(3, ids.Distinct().Count());
        Assert.IsFalse(ids.Contains(Guid.Empty));
        Assert.AreNotEqual(Guid.Empty, actor);
        var client = new MongoClient(uri);
        var db = client.GetDatabase(database);
        // Reuse the historical dedicated namespace, but never adopt existing rows.
        using (var collections = await db.ListCollectionNamesAsync())
            foreach (var collection in await collections.ToListAsync())
                Assert.AreEqual(0L, await db.GetCollection<BsonDocument>(collection)
                    .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty), "Preserve pre-existing data.");

        using (var file = new FileStream(intent, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(file, new { database, actor, ids = ids.Take(2).ToArray() });
            file.Flush(true);
        }
        var provider = (Web3CoreOASISBaseProvider)EvmProviderFactory.CreateArbitrum(
            new ArbitrumOASISProviderSettings
            {
                ConnectionString = "http://127.0.0.1:18547", ChainId = 31338,
                ChainPrivateKey = "0x4f3edf983ac636a65a842ce7c78d9aa706d3b113bce9c46f30d7d21715b23b1d",
                ContractAddress = seed.RootElement.GetProperty("Contract").GetString()!
            }, HyperDriveModes.V2, null!);
        var mongo = new NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.MongoDBOASIS(uri, database)
            { IsVersionControlEnabled = false };
        try
        {
            var activation = await provider.ActivateProviderAsync();
            Assert.IsFalse(activation.IsError, activation.Message);
            Assert.IsFalse((await mongo.ActivateProviderAsync()).IsError);
            for (var index = 0; index < 2; index++)
            {
                var row = new Holon { Id = ids[index], Name = "Owned V2 paired copy " + index,
                    CreatedByAvatarId = actor, IsActive = true, IsNewHolon = true };
                var saved = await mongo.SaveHolonAsync(row, saveChildren: false);
                Assert.IsFalse(saved.IsError, saved.Message);
                Assert.IsTrue(saved.IsSaved);
                await File.WriteAllTextAsync(Path.Combine(state, $"save-{index}.acknowledged.json"),
                    JsonSerializer.Serialize(new { id = row.Id, version = row.Version }));
            }
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                { HyperDriveMode = HyperDriveModes.V2 } };
            var manager = new HolonManager(null, dna, new ProviderManager(null, dna));
            using var output = new StringWriter();
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            var all = await EvmV2OwnedWalkthrough.RunWithMongoAsync(manager, mongo, provider,
                ids, ids.Take(2), ids, actor, output, deadline.Token);
            Assert.AreEqual(5, all.Rows.Count);
            Assert.AreEqual(3, all.Rows.Count(x => x.Selected.Provider == ProviderType.ArbitrumOASIS));
            Assert.AreEqual(2, all.Rows.Count(x => x.Selected.Provider == ProviderType.MongoDBOASIS));
            var subset = await EvmV2OwnedWalkthrough.RunWithMongoAsync(manager, mongo, provider,
                ids.Take(2), ids.Take(2), ids, actor, output, deadline.Token);
            Assert.AreEqual(4, subset.Rows.Count);
            CollectionAssert.AreEqual(original, await File.ReadAllBytesAsync(seedPath));

            // Only clean this exact acknowledged fixture after full acceptance.
            foreach (var id in ids.Take(2))
            {
                var observed = await mongo.LoadHolonAsync(id, loadChildren: false);
                Assert.IsFalse(observed.IsError);
                Assert.AreEqual(actor, observed.Result.CreatedByAvatarId);
                StringAssert.StartsWith(observed.Result.Name, "Owned V2 paired copy ");
                var deleted = await mongo.DeleteHolonAsync(id);
                Assert.IsFalse(deleted.IsError, deleted.Message);
                var absent = await mongo.LoadHolonAsync(id, loadChildren: false);
                Assert.IsTrue(absent.IsError);
                Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, absent.ErrorCode);
            }
            await File.WriteAllTextAsync(Path.Combine(state, "fixture.cleaned.json"),
                JsonSerializer.Serialize(new { full = 5, subset = 4, chainWrites = 0, mongoCopiesRemoved = 2 }));
        }
        finally
        {
            Assert.IsFalse((await mongo.DeActivateProviderAsync()).IsError);
            Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
        }
    }
}
