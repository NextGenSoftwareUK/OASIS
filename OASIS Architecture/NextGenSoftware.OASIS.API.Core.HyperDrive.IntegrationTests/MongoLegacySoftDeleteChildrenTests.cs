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
    public async Task LegacySoftDeleteRetainsEmbeddedChildren()
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

            foreach (var synchronous in new[] { false, true })
            foreach (var byKey in new[] { false, true })
            {
                var root = new Holon { Id = Guid.NewGuid(), Name = "Legacy parent", IsActive = true };
                var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = root.Id, Name = "Retained child", IsActive = true };
                var saved = await provider.SaveHolonAsync(root, saveChildren: false);
                Assert.False(saved.IsError, saved.Message);
                var childSaved = await provider.SaveHolonAsync(child, saveChildren: false);
                Assert.False(childSaved.IsError, childSaved.Message);
                var embedded = await records.UpdateOneAsync(row => row.HolonId == root.Id,
                    Builders<NativeHolon>.Update.Set(row => row.Children, new List<IHolon> { childSaved.Result }));
                Assert.Equal(1L, embedded.MatchedCount);
                var key = saved.Result.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS];

                var deleted = synchronous
                    ? byKey ? manager.DeleteHolon(key, actor) : manager.DeleteHolon(root.Id, actor)
                    : byKey ? await manager.DeleteHolonAsync(key, actor) : await manager.DeleteHolonAsync(root.Id, actor);
                Assert.False(deleted.IsError, deleted.Message);
                var stored = await records.Find(row => row.HolonId == root.Id).SingleAsync();
                Assert.Equal(actor, stored.DeletedByAvatarId);
                Assert.Equal(child.Id, Assert.Single(stored.Children).Id);
            }
        }
        finally
        {
            await client.DropDatabaseAsync(database);
        }
    }
}
