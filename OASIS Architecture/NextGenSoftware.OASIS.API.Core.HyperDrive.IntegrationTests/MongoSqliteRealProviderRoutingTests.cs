using FluentAssertions;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class MongoSqliteRealProviderRoutingTests
{
    private static readonly string[] QueryKeys = { "CreatedByAvatarId", "Active", "HolonType", "STARNETHolonId", "_versionStamp", "data",
        "Username", "Email", "Name", "Description", "ParentId", "ParentProviderKey", "SearchQuery", "parkType", "env",
        "web3TokenId", "NFT.MintedByAvatarId", "NFT.MintWalletAddress", "NFT.ParentWeb4NFTId", "GEONFT.PlacedByAvatarId",
        "GEONFT.MintedByAvatarId", "GEONFT.OriginalOASISNFT.MintWalletAddress", "GEONFT.LatLong", "Status", "ActiveQuestId",
        "ActiveObjectiveId", "ParentMissionId", "Avatar", "AppRegion" };
    [Theory]
    [InlineData(true, true)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task HolonReplicationUsesBothRealPriorityStores(bool synchronous, bool mongoPrimary)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        if (Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI") != uri)
            throw new InvalidOperationException("Owned reusable Mongo runner required; refusing other databases.");
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName,"Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1"))) root = root.Parent;
        if (root == null) throw new InvalidOperationException("Existing checkout required.");
        var directory = Path.Combine(root.FullName,"TestResults/HyperDriveMongoSqliteCurrent");
        Directory.CreateDirectory(directory);
        using var lease = new FileStream(Path.Combine(directory,"environment.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        var dbPath = Path.Combine(directory,"owned.db");
        const string databaseName = "hyperdrive_mongo_sqlite_fixture";
        var client = new MongoClient(uri);
        using var names = await client.ListDatabaseNamesAsync();
        if (File.Exists(dbPath) || (await names.ToListAsync()).Contains(databaseName))
            throw new InvalidOperationException("Existing fixture requires inspection; refusing overwrite or cleanup.");
        try
        {
            var mongo = new MongoDBOASIS(uri,databaseName);
            using var sqlite = new SQLLiteDBOASIS($"Data Source={dbPath};Pooling=False");
            (await mongo.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings() } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            var manager = new ProviderManager(null,dna) { IsAutoReplicationEnabled = true };
            manager.RegisterProvider(mongo);
            manager.RegisterProvider(sqlite);
            manager.SetAndReplaceAutoReplicationListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) }).IsError.Should().BeFalse();
            var holon = new Holon { Id = Guid.NewGuid(), Name = "priority-replication" };
            var request = new StorageOperationRequest { Operation = "SaveHolon", Payload = holon, SaveChildren = false, PreferredProvider = mongoPrimary ? ProviderType.MongoDBOASIS : ProviderType.SQLLiteDBOASIS };
            var router = new OASISHyperDrive(manager);
            var result = synchronous ? router.RouteRequest<IHolon>(request) : await router.RouteRequestAsync<IHolon>(request);
            result.IsError.Should().BeFalse(result.Message);
            result.IsWarning.Should().BeFalse(string.Join("; ",manager.LastReplicationDiagnostic.Attempts.Select(x=>x.Message)));
            (await mongo.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Name.Should().Be(holon.Name);
            (await sqlite.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Name.Should().Be(holon.Name);
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            holon.Name = "priority-updated";
            holon.Version = 3;
            holon.MetaData["Email"] = "priority@test.local";
            foreach (var key in QueryKeys.Where(key=>key!="Email")) holon.MetaData[key] = "query-value:" + key;
            var updated = synchronous ? router.RouteRequest<IHolon>(request) : await router.RouteRequestAsync<IHolon>(request);
            updated.IsError.Should().BeFalse(updated.Message);
            updated.IsWarning.Should().BeFalse(string.Join("; ",manager.LastReplicationDiagnostic.Attempts.Select(x=>x.Message)));
            foreach (var loaded in new[] { await mongo.LoadHolonAsync(holon.Id,loadChildren:false), await sqlite.LoadHolonAsync(holon.Id,loadChildren:false) })
            {
                loaded.IsError.Should().BeFalse(loaded.Message);
                loaded.Result.Name.Should().Be("priority-updated");
                loaded.Result.Version.Should().Be(3);
                loaded.Result.MetaData["Email"].ToString().Should().Be("priority@test.local");
            }
            var balanceManager = new ProviderManager(null,dna) { IsAutoLoadBalanceEnabled = true };
            foreach (var key in QueryKeys)
            foreach (var query in new[] {
                synchronous ? mongo.LoadHolonsByMetaData(key,holon.MetaData[key].ToString(),loadChildren:false) : await mongo.LoadHolonsByMetaDataAsync(key,holon.MetaData[key].ToString(),loadChildren:false),
                synchronous ? sqlite.LoadHolonsByMetaData(key,holon.MetaData[key].ToString(),loadChildren:false) : await sqlite.LoadHolonsByMetaDataAsync(key,holon.MetaData[key].ToString(),loadChildren:false) })
            {
                query.IsError.Should().BeFalse(query.Message);
                query.Result.Select(x=>x.Id).Should().Equal(new[] { holon.Id }, "metadata key {0} must remain queryable", key);
            }
            balanceManager.RegisterProvider(mongo);
            foreach (var mode in new[] { MetaKeyValuePairMatchMode.All, MetaKeyValuePairMatchMode.Any })
            foreach (var mismatchCount in new[] { 0, 1, 2 })
            foreach (var requestedVersion in new[] { 0, 2, 3 })
            {
                var pairs = new Dictionary<string,string> { ["Email"] = mismatchCount == 2 ? "not-matching" : "priority@test.local",
                    ["parkType"] = mismatchCount > 0 ? "not-matching" : holon.MetaData["parkType"].ToString() };
                foreach (var query in new[] {
                    synchronous ? mongo.LoadHolonsByMetaData(pairs,mode,loadChildren:false,version:requestedVersion) : await mongo.LoadHolonsByMetaDataAsync(pairs,mode,loadChildren:false,version:requestedVersion),
                    synchronous ? sqlite.LoadHolonsByMetaData(pairs,mode,loadChildren:false,version:requestedVersion) : await sqlite.LoadHolonsByMetaDataAsync(pairs,mode,loadChildren:false,version:requestedVersion) })
                {
                    query.IsError.Should().BeFalse(query.Message);
                    if (requestedVersion != 2 && (mismatchCount == 0 || (mode == MetaKeyValuePairMatchMode.Any && mismatchCount == 1)))
                        query.Result.Select(x=>x.Id).Should().Equal(holon.Id);
                    else query.Result.Should().BeEmpty();
                }
            }
            balanceManager.RegisterProvider(sqlite);
            foreach (var provider in new IOASISStorageProvider[] { mongo, sqlite })
            {
                var mismatch = synchronous ? provider.LoadHolonsByMetaData("Email","priority@test.local",loadChildren:false,version:2) : await provider.LoadHolonsByMetaDataAsync("Email","priority@test.local",loadChildren:false,version:2);
                mismatch.IsError.Should().BeFalse(mismatch.Message);
                mismatch.Result.Should().BeEmpty();
                var wrongType = synchronous ? provider.LoadHolonsByMetaData("Email","priority@test.local",HolonType.Quest,loadChildren:false,version:3) : await provider.LoadHolonsByMetaDataAsync("Email","priority@test.local",HolonType.Quest,loadChildren:false,version:3);
                wrongType.IsError.Should().BeFalse(wrongType.Message);
                wrongType.Result.Should().BeEmpty();
            }
            balanceManager.SetAndReplaceAutoLoadBalanceListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) }).IsError.Should().BeFalse();
            var timer = Stopwatch.StartNew();
            (await mongo.LoadHolonAsync(holon.Id,loadChildren:false)).IsError.Should().BeFalse();
            timer.Stop();
            var mongoMs = Math.Max(timer.Elapsed.TotalMilliseconds,0.001);
            timer.Restart();
            (await sqlite.LoadHolonAsync(holon.Id,loadChildren:false)).IsError.Should().BeFalse();
            timer.Stop();
            var sqliteMs = Math.Max(timer.Elapsed.TotalMilliseconds,0.001);
            balanceManager.PerformanceMonitor.RecordRequest(ProviderType.MongoDBOASIS,true,mongoMs);
            balanceManager.PerformanceMonitor.RecordRequest(ProviderType.SQLLiteDBOASIS,true,sqliteMs);
            var balancedRouter = new OASISHyperDrive(balanceManager);
            var read = new StorageOperationRequest { Operation = "LoadHolon", HolonId = holon.Id, LoadChildren = false };
            var balanced = synchronous ? balancedRouter.RouteRequest<IHolon>(read,LoadBalancingStrategy.Performance) : await balancedRouter.RouteRequestAsync<IHolon>(read,LoadBalancingStrategy.Performance);
            balanced.IsError.Should().BeFalse(balanced.Message);
            balanced.Result.Id.Should().Be(holon.Id);
            balanceManager.PerformanceMonitor.GetMetrics(mongoMs <= sqliteMs ? ProviderType.MongoDBOASIS : ProviderType.SQLLiteDBOASIS).TotalRequests.Should().BeGreaterThan(1);
            var delete = new StorageOperationRequest { Operation = "DeleteHolon", HolonId = holon.Id, SoftDelete = false, PreferredProvider = request.PreferredProvider };
            var deleted = synchronous ? router.RouteRequest<IHolon>(delete) : await router.RouteRequestAsync<IHolon>(delete);
            deleted.IsError.Should().BeFalse(deleted.Message);
            deleted.IsWarning.Should().BeFalse(string.Join("; ",manager.LastReplicationDiagnostic.Attempts.Select(x=>x.Message)));
            (await mongo.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Should().BeNull();
            (await sqlite.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Should().BeNull();
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = "priority-avatar", Email = "priority-avatar@test.local", FirstName = "Initial", AvatarType = new EnumValue<AvatarType>(AvatarType.User) };
            var avatarRequest = new StorageOperationRequest { Operation = "SaveAvatar", Payload = avatar, PreferredProvider = request.PreferredProvider };
            foreach (var updating in new[] { false, true })
            {
                if (updating) { avatar.FirstName = "Updated"; avatar.Email = "priority-updated@test.local"; }
                var saved = synchronous ? router.RouteRequest<IAvatar>(avatarRequest) : await router.RouteRequestAsync<IAvatar>(avatarRequest);
                saved.IsError.Should().BeFalse(saved.Message);
                saved.IsWarning.Should().BeFalse(string.Join("; ",manager.LastReplicationDiagnostic.Attempts.Select(x=>x.Message)));
                foreach (var loaded in new[] { await mongo.LoadAvatarAsync(avatar.Id), await sqlite.LoadAvatarAsync(avatar.Id),
                    await mongo.LoadAvatarByUsernameAsync(avatar.Username), await sqlite.LoadAvatarByEmailAsync(avatar.Email) })
                {
                    loaded.IsError.Should().BeFalse(loaded.Message);
                    loaded.Result.Id.Should().Be(avatar.Id);
                    loaded.Result.Email.Should().Be(avatar.Email);
                    loaded.Result.FirstName.Should().Be(avatar.FirstName);
                }
            }
            var detail = new AvatarDetail { Id = avatar.Id, Username = avatar.Username, Email = avatar.Email, Karma = 345, XP = 123 };
            detail.Stats.HP.Current = 37;
            detail.Stats.Mana.Current = 42;
            detail.Stats.Stamina.Current = 53;
            detail.MetaData["park-source"] = "priority-park";
            var item = new InventoryItem { Id = Guid.NewGuid(), Name = "priority-seed", Quantity = 3 };
            item.MetaData["quest-source"] = "priority-tree";
            detail.Inventory.Add(item);
            var detailRequest = new StorageOperationRequest { Operation = "SaveAvatarDetail", Payload = detail, PreferredProvider = request.PreferredProvider };
            foreach (var updating in new[] { false, true })
            {
                if (updating) { detail.Karma = 456; detail.XP = 234; detail.Stats.HP.Current = 38; item.Quantity = 4; }
                var saved = synchronous ? router.RouteRequest<IAvatarDetail>(detailRequest) : await router.RouteRequestAsync<IAvatarDetail>(detailRequest);
                saved.IsError.Should().BeFalse(saved.Message);
                saved.IsWarning.Should().BeFalse(string.Join("; ",manager.LastReplicationDiagnostic.Attempts.Select(x=>x.Message)));
                foreach (var loaded in new[] { await mongo.LoadAvatarDetailAsync(detail.Id), await sqlite.LoadAvatarDetailAsync(detail.Id) })
                {
                    loaded.IsError.Should().BeFalse(loaded.Message);
                    loaded.Result.Karma.Should().Be(detail.Karma);
                    loaded.Result.XP.Should().Be(detail.XP);
                    loaded.Result.Stats.HP.Current.Should().Be(detail.Stats.HP.Current);
                    loaded.Result.Stats.Mana.Current.Should().Be(42);
                    loaded.Result.Stats.Stamina.Current.Should().Be(53);
                    loaded.Result.MetaData["park-source"].ToString().Should().Be("priority-park");
                    loaded.Result.Inventory.Single().Id.Should().Be(item.Id);
                    loaded.Result.Inventory.Single().Quantity.Should().Be(item.Quantity);
                    loaded.Result.Inventory.Single().MetaData["quest-source"].ToString().Should().Be("priority-tree");
                }
            }
            var freshMongo = new MongoDBOASIS(uri,databaseName);
            using (var freshSqlite = new SQLLiteDBOASIS($"Data Source={dbPath};Pooling=False"))
            {
                (await freshMongo.ActivateProviderAsync()).IsError.Should().BeFalse();
                (await freshSqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
                foreach (var loaded in new[] { await freshMongo.LoadAvatarDetailAsync(detail.Id), await freshSqlite.LoadAvatarDetailAsync(detail.Id) })
                {
                    loaded.IsError.Should().BeFalse(loaded.Message);
                    loaded.Result.Karma.Should().Be(456);
                    loaded.Result.XP.Should().Be(234);
                    loaded.Result.Stats.HP.Current.Should().Be(38);
                    loaded.Result.Inventory.Single().Id.Should().Be(item.Id);
                    loaded.Result.Inventory.Single().Quantity.Should().Be(4);
                    loaded.Result.MetaData["park-source"].ToString().Should().Be("priority-park");
                }
                (await freshMongo.DeActivateProviderAsync()).IsError.Should().BeFalse();
            }
            const string failureApp = "oasis-v2-owned-failure";
            var failureMongo = new MongoDBOASIS(uri + "&appName=" + failureApp,databaseName);
            (await failureMongo.ActivateProviderAsync()).IsError.Should().BeFalse();
            var failManager = new ProviderManager(null,dna) { IsAutoFailOverEnabled = true };
            failManager.RegisterProvider(failureMongo);
            failManager.RegisterProvider(sqlite);
            failManager.SetAndReplaceAutoFailOverListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) }).IsError.Should().BeFalse();
            failManager.SetAndActivateCurrentStorageProvider(failureMongo).IsError.Should().BeFalse();
            var admin = client.GetDatabase("admin");
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "find" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var failRouter = new OASISHyperDrive(failManager);
                var detailRead = new StorageOperationRequest { Operation = "LoadAvatarDetail", AvatarId = detail.Id };
                var recovered = synchronous ? failRouter.RouteRequest<IAvatarDetail>(detailRead) : await failRouter.RouteRequestAsync<IAvatarDetail>(detailRead);
                recovered.IsError.Should().BeFalse(recovered.Message);
                recovered.Result.Karma.Should().Be(456);
                failManager.LastFailoverDiagnostic.SelectedProvider.Should().Be(ProviderType.SQLLiteDBOASIS);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                await failureMongo.DeActivateProviderAsync();
            }
            (await failureMongo.ActivateProviderAsync()).IsError.Should().BeFalse();
            var partialManager = new ProviderManager(null,dna) { IsAutoReplicationEnabled = true };
            partialManager.RegisterProvider(failureMongo);
            partialManager.RegisterProvider(sqlite);
            partialManager.SetAndReplaceAutoReplicationListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS), new EnumValue<ProviderType>(ProviderType.MongoDBOASIS) }).IsError.Should().BeFalse();
            var primaryOnly = new Holon { Id = Guid.NewGuid(), Name = "mongo-replica-rejected" };
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "insert" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var partialRouter = new OASISHyperDrive(partialManager);
                var partialRequest = new StorageOperationRequest { Operation = "SaveHolon", Payload = primaryOnly, SaveChildren = false, PreferredProvider = ProviderType.SQLLiteDBOASIS };
                var partial = synchronous ? partialRouter.RouteRequest<IHolon>(partialRequest) : await partialRouter.RouteRequestAsync<IHolon>(partialRequest);
                partial.IsError.Should().BeFalse(partial.Message);
                partial.IsWarning.Should().BeTrue();
                partialManager.LastReplicationDiagnostic.FailedCount.Should().Be(1);
                partialManager.LastReplicationDiagnostic.SucceededCount.Should().Be(0);
                (await sqlite.LoadHolonAsync(primaryOnly.Id,loadChildren:false)).Result.Name.Should().Be(primaryOnly.Name);
                (await mongo.LoadHolonAsync(primaryOnly.Id,loadChildren:false)).Result.Should().BeNull();
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                await failureMongo.DeActivateProviderAsync();
            }
            var avatarDelete = new StorageOperationRequest { Operation = "DeleteAvatar", AvatarId = avatar.Id, SoftDelete = false, PreferredProvider = request.PreferredProvider };
            var avatarDeleted = synchronous ? router.RouteRequest<bool>(avatarDelete) : await router.RouteRequestAsync<bool>(avatarDelete);
            avatarDeleted.IsError.Should().BeFalse(avatarDeleted.Message);
            avatarDeleted.IsWarning.Should().BeFalse(string.Join("; ",manager.LastReplicationDiagnostic.Attempts.Select(x=>x.Message)));
            (await mongo.LoadAvatarAsync(avatar.Id)).Result.Should().BeNull();
            (await sqlite.LoadAvatarAsync(avatar.Id)).Result.Should().BeNull();
            (await mongo.LoadAvatarDetailAsync(detail.Id)).Result.Should().BeNull();
            (await sqlite.LoadAvatarDetailAsync(detail.Id)).Result.Should().BeNull();
            using (var control = new DataContext($"Data Source={dbPath};Pooling=False")) control.Database.ExecuteSqlRaw("DROP TABLE Holon;");
            manager.IsAutoReplicationEnabled = false;
            manager.IsAutoFailOverEnabled = true;
            manager.SetAndReplaceAutoFailOverListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS), new EnumValue<ProviderType>(ProviderType.MongoDBOASIS) }).IsError.Should().BeFalse();
            manager.SetAndActivateCurrentStorageProvider(sqlite).IsError.Should().BeFalse();
            var fallbackHolon = new Holon { Id = Guid.NewGuid(), Name = "real-mongo-fallback" };
            var fallbackRequest = new StorageOperationRequest { Operation = "SaveHolon", Payload = fallbackHolon, SaveChildren = false };
            var fallback = synchronous ? router.RouteRequest<IHolon>(fallbackRequest) : await router.RouteRequestAsync<IHolon>(fallbackRequest);
            fallback.IsError.Should().BeFalse(fallback.Message);
            (await mongo.LoadHolonAsync(fallbackHolon.Id,loadChildren:false)).Result.Name.Should().Be(fallbackHolon.Name);
            manager.LastFailoverDiagnostic.SelectedProvider.Should().Be(ProviderType.MongoDBOASIS);
            await mongo.DeActivateProviderAsync();
        }
        finally
        {
            await client.DropDatabaseAsync(databaseName);
            foreach (var path in new[] { dbPath, dbPath + "-wal", dbPath + "-shm" })
                if (File.Exists(path)) File.Delete(path);
        }
    }
}
