using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using Xunit;
using NativeHolon = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Holon;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class MongoLegacySoftDeleteChildrenTests
{
    [Fact]
    public Task LegacySoftDeleteRetainsEmbeddedChildren() => VerifyLegacyWritesAsync(typed: false, updateFile: false);

    [Fact]
    public Task TypedLegacyWritesRetainEmbeddedChildren() => VerifyLegacyWritesAsync(typed: true, updateFile: false);

    [Fact]
    public Task FileMetadataUpdateRetainsEmbeddedChildren() => VerifyLegacyWritesAsync(typed: false, updateFile: true);

    private static async Task VerifyLegacyWritesAsync(bool typed, bool updateFile)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(uri, Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var database = "legacy_soft_delete_" + Guid.NewGuid().ToString("N");
        var client = new MongoClient(uri);
        var provider = new MongoDBOASIS(uri, database);
        try
        {
            var activation = await provider.ActivateProviderAsync();
            Assert.False(activation.IsError, activation.Message);
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
                { HyperDriveMode = HyperDriveModes.Legacy, StorageProviders = new StorageProviderSettings() } };
            var providers = new ProviderManager(null, dna)
                { IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false, IsAutoReplicationEnabled = false };
            providers.RegisterProvider(provider);
            var selected = providers.SetAndActivateCurrentStorageProvider(provider);
            Assert.False(selected.IsError, selected.Message);
            var manager = new HolonManager(provider, dna, providers);
            var records = client.GetDatabase(database).GetCollection<NativeHolon>("Holon");
            var actor = Guid.NewGuid();

            async Task<(Guid Root, Guid Child, string Key)> SeedGraphAsync()
            {
                var root = new Holon { Id = Guid.NewGuid(), Name = "Legacy parent", IsActive = true, CreatedByAvatarId = actor };
                root.MetaData["oasisFileType"] = "file";
                var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, Name = "Retained child", IsActive = true };
                var saved = await provider.SaveHolonAsync(root, saveChildren: false);
                Assert.False(saved.IsError, saved.Message);
                var childSaved = await provider.SaveHolonAsync(child, saveChildren: false);
                Assert.False(childSaved.IsError, childSaved.Message);
                var embedded = await records.UpdateOneAsync(row => row.HolonId == root.Id,
                    Builders<NativeHolon>.Update.Set(row => row.Children, new List<IHolon> { childSaved.Result }));
                Assert.Equal(1L, embedded.MatchedCount);
                return (root.Id, child.Id, saved.Result.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]);
            }

            if (updateFile)
            {
                var graph = await SeedGraphAsync();
                var files = new FilesManager(provider, dna, providers);
                var updated = await files.UpdateFileMetadataAsync(actor, graph.Root,
                    new Dictionary<string, object> { ["fileName"] = "updated.dat" });
                Assert.False(updated.IsError, updated.Message);
                Assert.True(updated.Result);
                var stored = await records.Find(row => row.HolonId == graph.Root).SingleAsync();
                Assert.Equal("updated.dat", stored.MetaData["fileName"].ToString());
                Assert.Equal(graph.Child, Assert.Single(stored.Children).Id);
                return;
            }

            foreach (var synchronous in new[] { false, true })
            foreach (var byKey in new[] { false, true })
            {
                var graph = await SeedGraphAsync();
                if (typed)
                {
                    var deleted = (synchronous, byKey) switch
                    {
                        (true, true) => manager.DeleteHolon<Holon>(graph.Key, actor),
                        (true, false) => manager.DeleteHolon<Holon>(graph.Root, actor),
                        (false, true) => await manager.DeleteHolonAsync<Holon>(graph.Key, actor),
                        _ => await manager.DeleteHolonAsync<Holon>(graph.Root, actor)
                    };
                    Assert.False(deleted.IsError, deleted.Message);
                }
                else
                {
                    var deleted = (synchronous, byKey) switch
                    {
                        (true, true) => manager.DeleteHolon(graph.Key, actor),
                        (true, false) => manager.DeleteHolon(graph.Root, actor),
                        (false, true) => await manager.DeleteHolonAsync(graph.Key, actor),
                        _ => await manager.DeleteHolonAsync(graph.Root, actor)
                    };
                    Assert.False(deleted.IsError, deleted.Message);
                }
                var stored = await records.Find(row => row.HolonId == graph.Root).SingleAsync();
                Assert.Equal(actor.ToString(), stored.DeletedByAvatarId);
                Assert.Equal(graph.Child, Assert.Single(stored.Children).Id);
            }
        }
        finally
        {
            await client.DropDatabaseAsync(database);
        }
    }
}
