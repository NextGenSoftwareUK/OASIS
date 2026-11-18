using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.AvatarProfile;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class AvatarProfileRequestContextNativeTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, true, true)]
    public async Task ProfileRecordsStayOnTheirServingStoreWithoutChangingGlobalRouting(
        bool hyperDriveV2, bool selectSqlite, bool missingSelectedDetail)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != uri)
            throw new InvalidOperationException("The owned loopback MongoDB runner is required.");
        var database = "profile_scope_" + Guid.NewGuid().ToString("N");
        var client = new MongoClient(uri);
        Assert.DoesNotContain(database, await (await client.ListDatabaseNamesAsync()).ToListAsync());
        var directory = Directory.CreateTempSubdirectory("oasis-profile-scope-");
        try
        {
            var mongo = new MongoDBOASIS(uri, database);
            using var sqlite = new SQLLiteDBOASIS($"Data Source={Path.Combine(directory.FullName, "scope.db")};Pooling=False");
            IOASISStorageProvider[] stores = [mongo, sqlite];
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            {
                HyperDriveMode = hyperDriveV2 ? HyperDriveModes.V2 : HyperDriveModes.Legacy,
                StorageProviders = new() { ProviderMethodCallTimeOutSeconds = 3 },
                Security = new() { SecretKey = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N") },
                SubscriptionConfig = new() { PlanType = "Gold" }
            } };
            var providers = new ProviderManager(null, dna);
            var id = Guid.NewGuid();
            var username = "scope-" + id.ToString("N");
            var hash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"));
            var now = DateTime.UtcNow;
            var quests = new Dictionary<ProviderType, Guid>();
            foreach (var store in stores)
            {
                Assert.False((await store.ActivateProviderAsync()).IsError);
                providers.RegisterProvider(store);
                var label = store.ProviderType.Value.ToString();
                var seeded = await store.SaveAvatarAsync(new Avatar
                {
                    Id = id, Username = username, Email = username + "@example.test", Title = label,
                    FirstName = "Original", Password = hash, IsActive = true, CreatedDate = now,
                    Version = 1, VersionId = Guid.NewGuid(), AvatarType = new EnumValue<AvatarType>(AvatarType.User),
                    RefreshTokens = [new RefreshToken { Token = label, Created = now, Expires = now.AddDays(1) }]
                });
                Assert.False(seeded.IsError, seeded.Message);
                quests[store.ProviderType.Value] = Guid.NewGuid();
                if (missingSelectedDetail && ReferenceEquals(store, sqlite)) continue;
                var detail = await store.SaveAvatarDetailAsync(new AvatarDetail
                {
                    Id = id, Username = username, Email = username + "@example.test", Address = label,
                    ActiveQuestId = quests[store.ProviderType.Value], ActiveObjectiveId = Guid.NewGuid(),
                    Karma = 37, XP = 42, CreatedDate = now, Version = 1, VersionId = Guid.NewGuid()
                });
                Assert.False(detail.IsError, detail.Message);
            }
            var configured = providers.PublishRoutingOverrides(new ProviderRoutingOverrides(
                autoFailoverEnabled: true, autoReplicationEnabled: false, autoLoadBalancingEnabled: false,
                failoverProviders: stores.Select(store => store.ProviderType.Value)), mongo.ProviderType.Value);
            Assert.False(configured.IsError, configured.Message);
            var before = providers.CaptureRoutingPolicy().Result;
            var accounts = new AvatarManager(null, dna, providers);
            var selected = stores[selectSqlite ? 1 : 0];
            var other = stores[selectSqlite ? 0 : 1];
            var opened = await accounts.CreateProfileRequestContextAsync(id, selected.ProviderType.Value);
            if (missingSelectedDetail)
            {
                Assert.True(opened.IsError);
                Assert.Null(opened.Result);
                var remaining = await mongo.LoadAvatarDetailAsync(id);
                Assert.False(remaining.IsError, remaining.Message);
                Assert.Equal(mongo.ProviderType.Value.ToString(), remaining.Result.Address);
            }
            else
            {
                Assert.False(opened.IsError, opened.Message);
                var context = opened.Result;
                Assert.Equal(selected.ProviderType.Value, context.ProviderType);
                Assert.Equal(context.ProviderType, context.Policy.PreferredProvider);
                Assert.False(context.Policy.AutoFailoverEnabled);
                Assert.False(context.Policy.AutoLoadBalancingEnabled);
                Assert.Equal(before.AutoReplicationEnabled, context.Policy.AutoReplicationEnabled);
                Assert.Equal(before.ReplicationProviders, context.Policy.ReplicationProviders);
                Assert.Equal(selected.ProviderType.Value.ToString(), context.Avatar.Title);
                Assert.Equal(selected.ProviderType.Value.ToString(), context.Detail.Address);
                Assert.Equal(quests[selected.ProviderType.Value], context.Detail.ActiveQuestId);
                var patch = new AvatarProfilePatch { Title = "Edited", Address = "Edited" };
                var applied = AvatarProfileChanges.Apply(patch, context.Avatar, context.Detail);
                Assert.False(applied.IsError, applied.Message);
                var detailSave = await context.Manager.SaveAvatarDetailAsync(context.Detail, providerType: context.ProviderType);
                Assert.False(detailSave.IsError, detailSave.Message);
                Assert.True(detailSave.IsSaved);
                var accountSave = await context.Manager.SaveAvatarAsync(context.Avatar, providerType: context.ProviderType);
                Assert.False(accountSave.IsError, accountSave.Message);
                Assert.True(accountSave.IsSaved);
                var savedAccount = await selected.LoadAvatarAsync(id);
                var savedDetail = await selected.LoadAvatarDetailAsync(id);
                Assert.False(savedAccount.IsError, savedAccount.Message);
                Assert.False(savedDetail.IsError, savedDetail.Message);
                Assert.Equal("Edited", savedAccount.Result.Title);
                Assert.Equal("Edited", savedDetail.Result.Address);
                Assert.Equal(hash, savedAccount.Result.Password);
                Assert.Single(savedAccount.Result.RefreshTokens);
                Assert.Equal(quests[selected.ProviderType.Value], savedDetail.Result.ActiveQuestId);
                Assert.Equal(37, savedDetail.Result.Karma);
                Assert.Equal(42, savedDetail.Result.XP);
                var untouchedAccount = await other.LoadAvatarAsync(id);
                var untouchedDetail = await other.LoadAvatarDetailAsync(id);
                Assert.False(untouchedAccount.IsError, untouchedAccount.Message);
                Assert.False(untouchedDetail.IsError, untouchedDetail.Message);
                Assert.Equal(other.ProviderType.Value.ToString(), untouchedAccount.Result.Title);
                Assert.Equal(other.ProviderType.Value.ToString(), untouchedDetail.Result.Address);
                var json = System.Text.Json.JsonSerializer.Serialize(context);
                Assert.DoesNotContain(hash, json);
                Assert.DoesNotContain("RefreshTokens", json);
                Assert.DoesNotContain("Manager", json);
                Assert.DoesNotContain("Avatar", json);
                Assert.DoesNotContain("Detail", json);
            }
            var after = providers.CaptureRoutingPolicy().Result;
            Assert.Equal(before.AutoFailoverEnabled, after.AutoFailoverEnabled);
            Assert.Equal(before.AutoReplicationEnabled, after.AutoReplicationEnabled);
            Assert.Equal(before.AutoLoadBalancingEnabled, after.AutoLoadBalancingEnabled);
            Assert.Equal(before.PreferredProvider, after.PreferredProvider);
            Assert.Equal(before.FailoverProviders, after.FailoverProviders);
            Assert.Equal(before.ReplicationProviders, after.ReplicationProviders);
            Assert.Equal(before.LoadBalancingProviders, after.LoadBalancingProviders);
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
                Assert.StartsWith("oasis-profile-scope-", directory.Name);
                directory.Delete(recursive: true);
                Assert.False(Directory.Exists(path));
            }
        }
    }

    [Fact]
    public void ProfilePreparationOwnsMutableAccountSaveCollections()
    {
        var avatar = new Avatar
        {
            Id = Guid.NewGuid(), MetaData = new() { ["label"] = "original" },
            ProviderUniqueStorageKey = new() { [ProviderType.MongoDBOASIS] = "original" },
            PreviousVersionProviderUniqueStorageKey = new() { [ProviderType.MongoDBOASIS] = "previous" },
            ProviderMetaData = new() { [ProviderType.MongoDBOASIS] = new() { ["label"] = "original" } },
            ProviderUsername = new() { [ProviderType.MongoDBOASIS] = "original" },
            RefreshTokens = [new RefreshToken { Token = "original" }]
        };
        avatar.RefreshTokens[0].Avatar = avatar;
        var detached = AvatarAuthResponse.DetachForProfile((IAvatar)avatar);
        detached.MetaData["label"] = "changed";
        detached.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = "changed";
        detached.PreviousVersionProviderUniqueStorageKey[ProviderType.MongoDBOASIS] = "changed";
        detached.ProviderMetaData[ProviderType.MongoDBOASIS]["label"] = "changed";
        detached.ProviderUsername[ProviderType.MongoDBOASIS] = "changed";
        detached.RefreshTokens[0].Token = "changed";
        detached.RefreshTokens.Add(new RefreshToken());
        Assert.Equal("original", avatar.MetaData["label"]);
        Assert.Equal("original", avatar.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]);
        Assert.Equal("previous", avatar.PreviousVersionProviderUniqueStorageKey[ProviderType.MongoDBOASIS]);
        Assert.Equal("original", avatar.ProviderMetaData[ProviderType.MongoDBOASIS]["label"]);
        Assert.Equal("original", avatar.ProviderUsername[ProviderType.MongoDBOASIS]);
        Assert.Single(avatar.RefreshTokens);
        Assert.Equal("original", avatar.RefreshTokens[0].Token);
        Assert.Same(detached, detached.RefreshTokens[0].Avatar);
        var detail = new AvatarDetail { Id = avatar.Id, MetaData = new() { ["label"] = "original" } };
        var detachedDetail = AvatarAuthResponse.DetachForProfile((IAvatarDetail)detail);
        detachedDetail.MetaData["label"] = "changed";
        Assert.Equal("original", detail.MetaData["label"]);
    }
}
