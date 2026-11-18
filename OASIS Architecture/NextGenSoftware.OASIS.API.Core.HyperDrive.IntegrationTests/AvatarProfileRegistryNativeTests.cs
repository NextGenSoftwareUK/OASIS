using System.Reflection;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class AvatarProfileRegistryNativeTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task ProfileReadFreezesPolicyAndRejectsAnAuthorityReplacedAfterItsAccountRead(
        bool hyperDriveV2, bool useSqlite)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != uri)
            throw new InvalidOperationException("The owned loopback MongoDB runner is required.");
        var suffix = Guid.NewGuid().ToString("N");
        var databases = new[] { "profile_registry_old_" + suffix, "profile_registry_new_" + suffix };
        var client = new MongoClient(uri);
        var existingNames = await (await client.ListDatabaseNamesAsync()).ToListAsync();
        foreach (var database in databases) Assert.DoesNotContain(database, existingNames);
        var directory = Directory.CreateTempSubdirectory("oasis-profile-registry-");
        try
        {
            using var sqliteOld = useSqlite ? new SQLLiteDBOASIS($"Data Source={Path.Combine(directory.FullName, "old.db")};Pooling=False") : null;
            using var sqliteNew = useSqlite ? new SQLLiteDBOASIS($"Data Source={Path.Combine(directory.FullName, "new.db")};Pooling=False") : null;
            IOASISStorageProvider original = useSqlite ? sqliteOld! : new MongoDBOASIS(uri, databases[0]);
            IOASISStorageProvider replacement = useSqlite ? sqliteNew! : new MongoDBOASIS(uri, databases[1]);
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                HyperDriveMode = hyperDriveV2 ? HyperDriveModes.V2 : HyperDriveModes.Legacy,
                StorageProviders = new()
                {
                    ProviderMethodCallTimeOutSeconds = 3, ActivateProviderTimeOutSeconds = 3,
                    DectivateProviderTimeOutSeconds = 3
                },
                Security = new() { SecretKey = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N") },
                SubscriptionConfig = new() { PlanType = "Gold" }
            } };
            var host = new ProviderManager(null, dna);
            var id = Guid.NewGuid();
            var username = "registry-" + suffix;
            var originalQuest = Guid.NewGuid();
            foreach (var pair in new[] { (Store: original, Label: "Original"), (Store: replacement, Label: "Replacement") })
            {
                Assert.False((await pair.Store.ActivateProviderAsync()).IsError);
                var saved = await pair.Store.SaveAvatarAsync(new Avatar
                {
                    Id = id, Username = username, Email = username + "@example.test", Title = pair.Label,
                    IsActive = true, CreatedDate = DateTime.UtcNow, Version = 1, VersionId = Guid.NewGuid(),
                    AvatarType = new EnumValue<AvatarType>(AvatarType.User)
                });
                Assert.False(saved.IsError, saved.Message);
                var detail = await pair.Store.SaveAvatarDetailAsync(new AvatarDetail
                {
                    Id = id, Username = username, Email = username + "@example.test", Address = pair.Label,
                    ActiveQuestId = ReferenceEquals(pair.Store, original) ? originalQuest : Guid.NewGuid(),
                    Karma = 37, XP = 42, CreatedDate = DateTime.UtcNow, Version = 1, VersionId = Guid.NewGuid()
                });
                Assert.False(detail.IsError, detail.Message);
            }
            Assert.True(host.RegisterProvider(original));
            Assert.False(host.PublishRoutingOverrides(new ProviderRoutingOverrides(false, false, false), original.ProviderType.Value).IsError);
            var accounts = new AvatarManager(null, dna, host);
            var opened = await ReadAcrossHostChange(accounts, host, id, original.ProviderType.Value, () =>
            {
                var changed = host.PublishRoutingOverrides(new ProviderRoutingOverrides(
                    autoFailoverEnabled: true, autoReplicationEnabled: true, autoLoadBalancingEnabled: true,
                    failoverProviders: [original.ProviderType.Value], replicationProviders: [original.ProviderType.Value],
                    loadBalancingProviders: [original.ProviderType.Value]), original.ProviderType.Value);
                Assert.False(changed.IsError, changed.Message);
            });
            Assert.False(opened.IsError, opened.Message);
            var context = opened.Result;
            Assert.Equal("Original", context.Avatar.Title);
            Assert.Equal("Original", context.Detail.Address);
            Assert.Equal(originalQuest, context.Detail.ActiveQuestId);
            Assert.False(context.Policy.AutoReplicationEnabled);
            Assert.Empty(context.Policy.ReplicationProviders);
            Assert.Empty(context.Policy.FailoverProviders);
            context.Detail.Address = "Edited original";
            var persisted = await context.Manager.SaveAvatarDetailAsync(context.Detail, providerType: context.ProviderType);
            Assert.False(persisted.IsError, persisted.Message);
            Assert.True(persisted.IsSaved);
            Assert.Same(original, host.CurrentStorageProvider);
            Assert.True(host.CaptureRoutingPolicy().Result.AutoReplicationEnabled);

            Assert.False(host.PublishRoutingOverrides(new ProviderRoutingOverrides(false, false, false), original.ProviderType.Value).IsError);
            var withdrawn = await ReadAcrossHostChange(accounts, host, id, original.ProviderType.Value, () =>
            {
                Assert.True(host.UnRegisterProvider(original));
                Assert.True(host.RegisterProvider(replacement));
                var changed = host.PublishRoutingOverrides(new ProviderRoutingOverrides(
                    autoFailoverEnabled: true, autoReplicationEnabled: true, autoLoadBalancingEnabled: true,
                    failoverProviders: [replacement.ProviderType.Value], replicationProviders: [replacement.ProviderType.Value],
                    loadBalancingProviders: [replacement.ProviderType.Value]), replacement.ProviderType.Value);
                Assert.False(changed.IsError, changed.Message);
            });
            Assert.True(withdrawn.IsError);
            Assert.Equal("AVATAR_PROFILE_AUTHORITY_CHANGED", withdrawn.ErrorCode);
            Assert.Null(withdrawn.Result);
            Assert.False(original.IsProviderActivated, "The profile caller must not reopen a provider withdrawn by its host.");
            // Fixture-owner inspection of the retired store, after the failed
            // operation has drained. This is not profile recovery or a retry.
            Assert.False((await original.ActivateProviderAsync()).IsError);
            var oldDetail = await original.LoadAvatarDetailAsync(id);
            var newDetail = await replacement.LoadAvatarDetailAsync(id);
            Assert.False(oldDetail.IsError, oldDetail.Message);
            Assert.False(newDetail.IsError, newDetail.Message);
            Assert.Equal("Edited original", oldDetail.Result.Address);
            Assert.Equal(originalQuest, oldDetail.Result.ActiveQuestId);
            Assert.Equal("Replacement", newDetail.Result.Address);
            Assert.Same(replacement, host.CurrentStorageProvider);
            Assert.Same(replacement, host.GetStorageProvider(replacement.ProviderType.Value));
            Assert.True(host.CaptureRoutingPolicy().Result.AutoReplicationEnabled);
            // A distinct, explicit new request observes the new configuration.
            var fresh = await accounts.CreateProfileRequestContextAsync(id);
            Assert.False(fresh.IsError, fresh.Message);
            Assert.Equal("Replacement", fresh.Result.Avatar.Title);
            Assert.Equal("Replacement", fresh.Result.Detail.Address);
            Assert.True(fresh.Result.Policy.AutoReplicationEnabled);

        }
        finally
        {
            try
            {
                foreach (var database in databases) await client.DropDatabaseAsync(database);
                var remainingNames = await (await client.ListDatabaseNamesAsync()).ToListAsync();
                foreach (var database in databases) Assert.DoesNotContain(database, remainingNames);
            }
            finally
            {
                var path = Path.GetFullPath(directory.FullName);
                var tempRoot = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
                Assert.True(path.StartsWith(tempRoot, StringComparison.OrdinalIgnoreCase));
                Assert.StartsWith("oasis-profile-registry-", directory.Name);
                directory.Delete(recursive: true);
                Assert.False(Directory.Exists(path));
            }
        }
    }

    private static async Task<NextGenSoftware.OASIS.Common.OASISResult<AvatarProfileRequestContext>> ReadAcrossHostChange(
        AvatarManager accounts, ProviderManager host, Guid id, ProviderType provider, Action change)
    {
        // Hold only post-I/O metrics synchronization on this host's own monitor.
        // The real SDK read completes first; no substitute provider or response.
        var metricsGate = typeof(PerformanceMonitor).GetField("_lockObject", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(host.PerformanceMonitor)!;
        using var trace = AuthenticationOperationTrace.Begin();
        Task<NextGenSoftware.OASIS.Common.OASISResult<AvatarProfileRequestContext>>? pending = null;
        try
        {
            lock (metricsGate)
            {
                pending = Task.Run(() => accounts.CreateProfileRequestContextAsync(id, provider));
                Assert.True(SpinWait.SpinUntil(() => pending.IsCompleted ||
                    trace.Snapshot.Routing == AuthenticationOperationTrace.RoutingStage.ProviderMetrics, TimeSpan.FromSeconds(10)),
                    "The actual account read must reach post-I/O metrics before changing the host registry.");
                Assert.False(pending.IsCompleted);
                Assert.Equal(AuthenticationOperationTrace.RoutingStage.ProviderMetrics, trace.Snapshot.Routing);
                Assert.Equal("LoadAvatar", trace.Snapshot.Operation);
                change();
            }
            return await pending;
        }
        finally
        {
            // Release the lock and drain the operation before fixture disposal,
            // including assertion failures in the coordination or mutation.
            if (pending != null) await pending;
        }
    }
}
