using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class ScopedProviderLifecycleNativeTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ScopeSelectionKeepsBorrowedProvidersActiveAndHostSelectionUnchanged(bool synchronous)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != uri)
            throw new InvalidOperationException("The owned loopback MongoDB runner is required.");
        var database = "scope_lifecycle_" + Guid.NewGuid().ToString("N");
        var client = new MongoClient(uri);
        Assert.DoesNotContain(database, await (await client.ListDatabaseNamesAsync()).ToListAsync());
        var directory = Directory.CreateTempSubdirectory("oasis-scope-lifecycle-");
        try
        {
            var mongo = new MongoDBOASIS(uri, database);
            using var sqlite = new SQLLiteDBOASIS($"Data Source={Path.Combine(directory.FullName, "lifecycle.db")};Pooling=False");
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                StorageProviders = new() { ProviderMethodCallTimeOutSeconds = 3 },
                Security = new() { SecretKey = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N") },
                SubscriptionConfig = new() { PlanType = "Gold" }
            } };
            var host = new ProviderManager(null, dna);
            var id = Guid.NewGuid();
            IOASISStorageProvider[] stores = [mongo, sqlite];
            foreach (var store in stores)
            {
                Assert.False((await store.ActivateProviderAsync()).IsError);
                host.RegisterProvider(store);
                var seed = await store.SaveAvatarAsync(new Avatar
                {
                    Id = id, Username = "scope-" + id.ToString("N"), Email = id.ToString("N") + "@example.test",
                    Title = store.ProviderType.Value.ToString(), IsActive = true,
                    CreatedDate = DateTime.UtcNow, Version = 1, VersionId = Guid.NewGuid()
                });
                Assert.False(seed.IsError, seed.Message);
            }
            var configured = host.PublishRoutingOverrides(new ProviderRoutingOverrides(
                autoFailoverEnabled: true, autoReplicationEnabled: false, autoLoadBalancingEnabled: false,
                failoverProviders: stores.Select(store => store.ProviderType.Value)), mongo.ProviderType.Value);
            Assert.False(configured.IsError, configured.Message);
            var created = host.CreateRoutingScope(new ProviderRoutingOverrides(autoFailoverEnabled: false), sqlite.ProviderType.Value);
            Assert.False(created.IsError, created.Message);
            var scope = created.Result;
            // The inherited failover list starts with Mongo. Default must still
            // resolve the scope's captured SQLite preference, not that first entry.
            var selectedDefault = synchronous ? scope.SetAndActivateCurrentStorageProvider()
                : await scope.SetAndActivateCurrentStorageProviderAsync();
            Assert.False(selectedDefault.IsError, selectedDefault.Message);
            Assert.Same(sqlite, selectedDefault.Result);
            foreach (var store in stores)
            {
                var selected = synchronous ? scope.SetAndActivateCurrentStorageProvider(store.ProviderType.Value, false)
                    : await scope.SetAndActivateCurrentStorageProviderAsync(store.ProviderType.Value, false);
                Assert.False(selected.IsError, selected.Message);
                Assert.Same(store, selected.Result);
                Assert.Same(store, scope.CurrentStorageProvider);
                foreach (var borrowed in stores)
                {
                    Assert.True(borrowed.IsProviderActivated);
                    var read = await borrowed.LoadAvatarAsync(id);
                    Assert.False(read.IsError, read.Message);
                    Assert.Equal(borrowed.ProviderType.Value.ToString(), read.Result.Title);
                }
                Assert.Same(mongo, host.CurrentStorageProvider);
                Assert.Same(mongo, host.DefaultGlobalStorageProvider);
                Assert.Equal(mongo.ProviderType.Value, host.CurrentStorageProviderType.Value);
            }
            var shutdown = synchronous ? scope.DeActivateProvider(mongo) : await scope.DeActivateProviderAsync(mongo);
            Assert.True(shutdown.IsError);
            Assert.Equal("HYPERDRIVE_SCOPED_PROVIDER_SHUTDOWN_DENIED", shutdown.ErrorCode);
            Assert.True(mongo.IsProviderActivated);
            var global = synchronous ? scope.SetAndActivateCurrentStorageProvider(sqlite.ProviderType.Value, true)
                : await scope.SetAndActivateCurrentStorageProviderAsync(sqlite.ProviderType.Value, true);
            Assert.True(global.IsError);
            Assert.Equal("HYPERDRIVE_SCOPED_GLOBAL_SELECTION_DENIED", global.ErrorCode);
            Assert.Same(sqlite, scope.CurrentStorageProvider);
            Assert.Same(mongo, host.CurrentStorageProvider);
            var repeatedActivation = synchronous ? scope.ActivateProvider(mongo) : await scope.ActivateProviderAsync(mongo);
            Assert.False(repeatedActivation.IsError, repeatedActivation.Message);
            Assert.True(repeatedActivation.Result);
            Assert.Equal("The shared provider is already active.", repeatedActivation.Message);
            Assert.False((await mongo.LoadAvatarAsync(id)).IsError);
        }
        finally
        {
            try
            {
                await client.DropDatabaseAsync(database);
                Assert.DoesNotContain(database, await (await client.ListDatabaseNamesAsync()).ToListAsync());
            }
            finally
            {
                var path = Path.GetFullPath(directory.FullName);
                var tempRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                Assert.True(path.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase));
                Assert.StartsWith("oasis-scope-lifecycle-", directory.Name);
                directory.Delete(recursive: true);
                Assert.False(Directory.Exists(path));
            }
        }
    }
}
