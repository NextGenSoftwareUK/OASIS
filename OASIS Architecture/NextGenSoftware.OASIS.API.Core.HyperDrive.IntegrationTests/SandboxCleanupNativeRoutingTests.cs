using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class SandboxCleanupNativeRoutingTests
{
    [Fact]
    public async Task FailedNativeAvatarPurgeRetainsLaterReplicaForNextSweep()
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != uri)
            throw new InvalidOperationException("The owned loopback MongoDB runner is required.");
        const string app = "owned-cleanup-stop";
        var database = "hyperdrive_cleanup_stop_" + Guid.NewGuid().ToString("N");
        var client = new MongoClient(uri);
        Assert.DoesNotContain(database, await (await client.ListDatabaseNamesAsync()).ToListAsync());
        var directory = Directory.CreateTempSubdirectory("oasis-cleanup-stop-");
        var mongo = new MongoDBOASIS(uri + "&appName=" + app, database);
        SQLLiteDBOASIS? sqlite = null;
        var admin = client.GetDatabase("admin");
        try
        {
            sqlite = new SQLLiteDBOASIS($"Data Source={Path.Combine(directory.FullName, "cleanup.db")};Pooling=False");
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings(),
                SubscriptionConfig = new() { PlanType = "Gold" } } };
            var providers = new ProviderManager(null, dna) {
                IsAutoFailOverEnabled = false, IsAutoLoadBalanceEnabled = false, IsAutoReplicationEnabled = false };
            var stores = new IOASISStorageProvider[] { mongo, sqlite };
            var id = Guid.NewGuid();
            foreach (var store in stores)
            {
                providers.RegisterProvider(store);
                var activated = await store.ActivateProviderAsync();
                Assert.False(activated.IsError, activated.Message);
                var saved = await store.SaveAvatarAsync(new Avatar { Id = id, Username = "sandbox-" + id.ToString("N"),
                    Email = id.ToString("N") + "@sandbox.example.org", AvatarType = new EnumValue<AvatarType>(AvatarType.User),
                    IsActive = true, CreatedDate = DateTime.UtcNow });
                Assert.False(saved.IsError, saved.Message);
                Assert.Equal(id, (await store.LoadAvatarAsync(id)).Result.Id);
            }
            var router = new OASISHyperDrive(providers);
            var targets = stores.Select(store => store.ProviderType.Value).ToArray();
            var managers = new Dictionary<ProviderType, HolonManager>();
            foreach (var target in targets)
            {
                var context = new HolonManager(null, dna, providers).CreateRequestContext(
                    new ProviderRoutingOverrides(false, false, false), target);
                Assert.False(context.IsError, context.Message);
                managers.Add(target, context.Result.Manager);
            }
            var cleanup = new SandboxAvatarCleanup(async () => {
                var loaded = await router.RouteRequestToProviderAsync<IEnumerable<IAvatar>>(
                    new StorageOperationRequest { Operation = "LoadAllAvatars", PreferredProvider = ProviderType.MongoDBOASIS,
                        RequirePreferredProvider = true, AutoReplicate = false }, ProviderType.MongoDBOASIS);
                Assert.False(loaded.IsError, loaded.Message);
                return loaded.Result.ToList();
            }, async (avatar, target) => {
                var loaded = await managers[target].LoadAllHolonsAsync(HolonType.All, loadChildren: false,
                    recursive: false, loadChildrenFromProvider: false, providerType: target, avatarId: avatar, includePublic: false);
                Assert.False(loaded.IsError, loaded.Message);
                return loaded.Result.ToList();
            }, async (holon, avatar, target) => {
                var deleted = await managers[target].DeleteHolonAsync(holon, avatar, softDelete: false, providerType: target);
                return !deleted.IsError && deleted.Result?.Id == holon;
            }, async (avatar, target) => {
                var deleted = await router.RouteRequestToProviderAsync<bool>(new StorageOperationRequest {
                    Operation = "DeleteAvatar", AvatarId = avatar, SoftDelete = false, PreferredProvider = target,
                    RequirePreferredProvider = true, AutoReplicate = false }, target);
                return !deleted.IsError && deleted.Result;
            }, targets, "sandbox-", "sandbox.example.org", TimeSpan.FromHours(24), 1,
                () => DateTime.UtcNow.AddDays(2));

            await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" },
                { "mode", "alwaysOn" }, { "data", new BsonDocument { { "failCommands", new BsonArray { "delete" } },
                    { "appName", app }, { "errorCode", 2 } } } });
            try
            {
                var failed = await cleanup.SweepAsync();
                Assert.Equal(0, failed.AvatarsRemoved);
                Assert.Equal(1, failed.AvatarsKept);
                Assert.Contains("MongoDBOASIS", Assert.Single(failed.Failures));
                Assert.Equal(id, (await sqlite.LoadAvatarAsync(id)).Result.Id);
                Assert.Equal(id, (await mongo.LoadAvatarAsync(id)).Result.Id);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
            }
            var completed = await cleanup.SweepAsync();
            Assert.Equal(1, completed.AvatarsRemoved);
            Assert.Equal(0, completed.AvatarsKept);
            Assert.Empty(completed.Failures);
            foreach (var store in stores)
            {
                var absent = await store.LoadAvatarAsync(id);
                Assert.True(!absent.IsError || absent.ErrorCode == OASISResultErrorCodes.RecordNotFound, absent.Message);
                Assert.Null(absent.Exception);
                Assert.Null(absent.Result);
            }
        }
        finally
        {
            try { await mongo.DeActivateProviderAsync(); sqlite?.Dispose(); await client.DropDatabaseAsync(database);
                Assert.DoesNotContain(database, await (await client.ListDatabaseNamesAsync()).ToListAsync()); }
            finally { directory.Delete(recursive: true); }
        }
    }
}
