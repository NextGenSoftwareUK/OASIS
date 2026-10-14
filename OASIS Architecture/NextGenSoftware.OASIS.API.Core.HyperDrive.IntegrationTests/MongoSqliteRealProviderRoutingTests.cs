using FluentAssertions;
using MongoDB.Driver;
using MongoDB.Bson;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
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
            foreach (var searchProvider in new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS })
            {
                var searchRequest = new StorageOperationRequest { Operation = "Search", LoadChildren = false, PreferredProvider = searchProvider,
                    SearchParams = new SearchParams { SearchOnlyForCurrentAvatar = false,
                        SearchGroups = new() { new SearchTextGroup { SearchQuery = "priority-updated", SearchHolons = true,
                            HolonType = holon.HolonType, HolonSearchParams = new SearchHolonParams { Name = true } } } } };
                var searched = synchronous ? balancedRouter.RouteRequest<ISearchResults>(searchRequest) : await balancedRouter.RouteRequestAsync<ISearchResults>(searchRequest);
                searched.IsError.Should().BeFalse(searched.Message);
                searched.Result.SearchResultHolons.Select(h => h.Id).Should().Equal(holon.Id);
                searched.Result.SearchResultAvatars.Should().BeEmpty();
            }
            var searchCounts = new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS }
                .ToDictionary(provider => provider, provider => balanceManager.PerformanceMonitor.GetMetrics(provider).TotalRequests);
            var roundRobinSearch = new StorageOperationRequest { Operation = "Search", LoadChildren = false,
                SearchParams = new SearchParams { SearchOnlyForCurrentAvatar = false,
                    SearchGroups = new() { new SearchTextGroup { SearchQuery = "priority-updated", SearchHolons = true,
                        HolonType = holon.HolonType, HolonSearchParams = new SearchHolonParams { Name = true } } } } };
            for (var index = 0; index < 6; index++)
            {
                var searched = synchronous ? balancedRouter.RouteRequest<ISearchResults>(roundRobinSearch, LoadBalancingStrategy.RoundRobin)
                    : await balancedRouter.RouteRequestAsync<ISearchResults>(roundRobinSearch, LoadBalancingStrategy.RoundRobin);
                searched.IsError.Should().BeFalse(searched.Message);
                searched.Result.SearchResultHolons.Select(h => h.Id).Should().Equal(holon.Id);
            }
            foreach (var provider in searchCounts.Keys)
                balanceManager.PerformanceMonitor.GetMetrics(provider).TotalRequests.Should().Be(searchCounts[provider] + 3,
                    "six real round-robin search requests must execute three times on each provider");
            var delete = new StorageOperationRequest { Operation = "DeleteHolon", HolonId = holon.Id, SoftDelete = false, PreferredProvider = request.PreferredProvider };
            var deleted = synchronous ? router.RouteRequest<IHolon>(delete) : await router.RouteRequestAsync<IHolon>(delete);
            deleted.IsError.Should().BeFalse(deleted.Message);
            deleted.IsWarning.Should().BeFalse(string.Join("; ",manager.LastReplicationDiagnostic.Attempts.Select(x=>x.Message)));
            (await mongo.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Should().BeNull();
            (await sqlite.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Should().BeNull();
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            var avatar = new Avatar { Id = Guid.NewGuid(), Username = "priority-avatar", Email = "priority-avatar@test.local", FirstName = "Initial",
                IsActive = false, Verified = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc), AvatarType = new EnumValue<AvatarType>(AvatarType.User) };
            avatar.VerificationToken = "priority-verification-owned";
            avatar.ResetToken = "priority-reset-owned";
            const string collectionRefreshToken = "priority-refresh-owned";
            avatar.RefreshToken = null;
            avatar.RefreshTokens = new List<RefreshToken> { new RefreshToken { Id = 7, Token = collectionRefreshToken,
                CreatedByIp = "127.0.0.1", Expires = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc) } };
            var avatarRequest = new StorageOperationRequest { Operation = "SaveAvatar", Payload = avatar, PreferredProvider = request.PreferredProvider };
            foreach (var updating in new[] { false, true })
            {
                if (updating) { avatar.FirstName = "Updated"; avatar.Email = "priority-updated@test.local"; avatar.IsActive = true; avatar.Verified = null; avatar.Version = 3; }
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
                    loaded.Result.IsActive.Should().Be(avatar.IsActive);
                    loaded.Result.Verified.Should().Be(avatar.Verified);
                    loaded.Result.IsVerified.Should().Be(avatar.IsVerified);
                    loaded.Result.RefreshToken.Should().BeNull();
                    loaded.Result.RefreshTokens.Should().ContainSingle();
                    loaded.Result.RefreshTokens[0].Token.Should().Be(collectionRefreshToken);
                    loaded.Result.RefreshTokens[0].Id.Should().Be(7);
                    loaded.Result.RefreshTokens[0].CreatedByIp.Should().Be("127.0.0.1");
                    loaded.Result.RefreshTokens[0].Expires.Should().Be(avatar.RefreshTokens[0].Expires);
                }
            }
            foreach (var tokenProvider in new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS })
            foreach (var operation in new[] { "LoadAvatarByVerificationToken", "LoadAvatarByResetToken", "LoadAvatarByRefreshToken" })
            foreach (var requestedVersion in new[] { 0, 2, 3 })
            {
                var tokenRequest = new StorageOperationRequest { Operation = operation, PreferredProvider = tokenProvider, Version = requestedVersion,
                    VerificationToken = avatar.VerificationToken, ResetToken = avatar.ResetToken, RefreshToken = collectionRefreshToken };
                var beforeTokenRead = manager.PerformanceMonitor.GetMetrics(tokenProvider).TotalRequests;
                var loaded = synchronous ? router.RouteRequest<IAvatar>(tokenRequest) : await router.RouteRequestAsync<IAvatar>(tokenRequest);
                loaded.IsError.Should().BeFalse(loaded.Message);
                loaded.IsLoaded.Should().Be(requestedVersion != 2, "{0} on {1}, version {2}, result {3}, message {4}",
                    operation, tokenProvider, requestedVersion, loaded.Result?.Id, loaded.Message);
                if (requestedVersion == 2) loaded.Result.Should().BeNull();
                else loaded.Result.Id.Should().Be(avatar.Id);
                manager.PerformanceMonitor.GetMetrics(tokenProvider).TotalRequests.Should().Be(beforeTokenRead + 1);
            }
            foreach (var invalid in new[] { null, "", " " })
            foreach (var provider in new IOASISStorageProvider[] { mongo, sqlite })
            foreach (var invalidRead in new[] {
                synchronous ? provider.LoadAvatarByVerificationToken(invalid) : await provider.LoadAvatarByVerificationTokenAsync(invalid),
                synchronous ? provider.LoadAvatarByResetToken(invalid) : await provider.LoadAvatarByResetTokenAsync(invalid),
                synchronous ? provider.LoadAvatarByRefreshToken(invalid) : await provider.LoadAvatarByRefreshTokenAsync(invalid)
            }) {
                invalidRead.IsError.Should().BeTrue();
                invalidRead.IsLoaded.Should().BeFalse();
                invalidRead.Result.Should().BeNull();
                invalidRead.Exception.Should().BeAssignableTo<ArgumentException>();
            }
            const string rotatedRefreshToken = "priority-rotated-refresh-owned";
            foreach (var provider in new IOASISStorageProvider[] { mongo, sqlite })
            {
                foreach (var duplicateId in new[] { Guid.NewGuid(), Guid.NewGuid() })
                {
                    var duplicate = new Avatar { Id = duplicateId, Username = duplicateId.ToString(), Email = duplicateId + "@ambiguous.invalid",
                        VerificationToken = "owned-ambiguous-verification", ResetToken = "owned-ambiguous-reset",
                        RefreshTokens = new() { new RefreshToken { Token = "owned-ambiguous-refresh" } },
                        AvatarType = new EnumValue<AvatarType>(AvatarType.User) };
                    var saved = synchronous ? provider.SaveAvatar(duplicate) : await provider.SaveAvatarAsync(duplicate);
                    saved.IsError.Should().BeFalse(saved.Message);
                }
                foreach (var ambiguous in new[] {
                    synchronous ? provider.LoadAvatarByVerificationToken("owned-ambiguous-verification") : await provider.LoadAvatarByVerificationTokenAsync("owned-ambiguous-verification"),
                    synchronous ? provider.LoadAvatarByResetToken("owned-ambiguous-reset") : await provider.LoadAvatarByResetTokenAsync("owned-ambiguous-reset"),
                    synchronous ? provider.LoadAvatarByRefreshToken("owned-ambiguous-refresh") : await provider.LoadAvatarByRefreshTokenAsync("owned-ambiguous-refresh")
                }) {
                    ambiguous.IsError.Should().BeTrue();
                    ambiguous.IsLoaded.Should().BeFalse();
                    ambiguous.Result.Should().BeNull();
                    ambiguous.Exception.Should().BeOfType<InvalidOperationException>();
                }
            }
            avatar.RefreshTokens = new List<RefreshToken> { new RefreshToken { Id = 7, Token = rotatedRefreshToken,
                CreatedByIp = "127.0.0.2", Expires = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc) } };
            var rotatedSave = synchronous ? router.RouteRequest<IAvatar>(avatarRequest) : await router.RouteRequestAsync<IAvatar>(avatarRequest);
            rotatedSave.IsError.Should().BeFalse(rotatedSave.Message);
            rotatedSave.IsWarning.Should().BeFalse();
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
            foreach (var tokenProvider in new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS })
            foreach (var token in new[] { collectionRefreshToken, rotatedRefreshToken })
            {
                var readRotated = new StorageOperationRequest { Operation = "LoadAvatarByRefreshToken", RefreshToken = token,
                    PreferredProvider = tokenProvider, Version = 3 };
                var loaded = synchronous ? router.RouteRequest<IAvatar>(readRotated) : await router.RouteRequestAsync<IAvatar>(readRotated);
                loaded.IsError.Should().BeFalse(loaded.Message);
                if (token == collectionRefreshToken) { loaded.IsLoaded.Should().BeFalse(); loaded.Result.Should().BeNull(); }
                else {
                    loaded.Result.Id.Should().Be(avatar.Id);
                    loaded.Result.RefreshTokens.Should().ContainSingle();
                    loaded.Result.RefreshTokens[0].Id.Should().Be(7);
                    loaded.Result.RefreshTokens[0].Token.Should().Be(rotatedRefreshToken);
                    loaded.Result.RefreshTokens[0].CreatedByIp.Should().Be("127.0.0.2");
                }
            }
            foreach (var searchProvider in new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS })
            {
                var emailSearch = new StorageOperationRequest { Operation = "Search", LoadChildren = false, PreferredProvider = searchProvider,
                    SearchParams = new SearchParams { SearchOnlyForCurrentAvatar = false,
                        SearchGroups = new() { new SearchTextGroup { SearchQuery = avatar.Email.ToUpperInvariant(), SearchAvatars = true,
                            AvatarSearchParams = new NextGenSoftware.OASIS.API.Core.Objects.Search.Avatrar.SearchAvatarParams { Email = true } } } } };
                var beforeSearch = manager.PerformanceMonitor.GetMetrics(searchProvider).TotalRequests;
                var searched = synchronous ? router.RouteRequest<ISearchResults>(emailSearch) : await router.RouteRequestAsync<ISearchResults>(emailSearch);
                searched.IsError.Should().BeFalse(searched.Message);
                searched.Result.SearchResultAvatars.Select(a => a.Id).Should().Equal(avatar.Id);
                searched.Result.SearchResultHolons.Should().BeEmpty();
                manager.PerformanceMonitor.GetMetrics(searchProvider).TotalRequests.Should().Be(beforeSearch + 1);
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
            foreach (var collectionName in new[] { "Avatar", "AvatarDetail" })
            {
                var collection = client.GetDatabase(databaseName).GetCollection<BsonDocument>(collectionName);
                var existingRecord = await collection.Find(FilterDefinition<BsonDocument>.Empty).FirstOrDefaultAsync();
                var duplicateRecord = existingRecord.DeepClone().AsBsonDocument;
                duplicateRecord["_id"] = existingRecord["_id"].IsObjectId ? new BsonObjectId(ObjectId.GenerateNewId()) : new BsonString(ObjectId.GenerateNewId().ToString());
                Func<Task> duplicateInsert = () => collection.InsertOneAsync(duplicateRecord);
                var duplicateError = await duplicateInsert.Should().ThrowAsync<MongoWriteException>();
                duplicateError.Which.WriteError.Category.Should().Be(ServerErrorCategory.DuplicateKey);
                var identityIndex = collectionName == "Avatar" ? "oasis_avatar_public_identity" : "oasis_avatar_detail_public_identity";
                await collection.Indexes.DropOneAsync(identityIndex);
                await collection.InsertOneAsync(duplicateRecord);
                try
                {
                    for (var activationAttempt = 0; activationAttempt < 2; activationAttempt++)
                    {
                        var duplicateActivation = synchronous ? mongo.ActivateProvider() : await mongo.ActivateProviderAsync();
                        duplicateActivation.IsError.Should().BeTrue();
                        duplicateActivation.Exception.Should().BeOfType<MongoCommandException>();
                        mongo.IsProviderActivated.Should().BeFalse();
                    }
                    (await collection.CountDocumentsAsync(new BsonDocument("_id", duplicateRecord["_id"]))).Should().Be(1);
                    (await collection.CountDocumentsAsync(new BsonDocument("_id", existingRecord["_id"]))).Should().Be(1);
                }
                finally
                {
                    await collection.DeleteOneAsync(new BsonDocument("_id", duplicateRecord["_id"]));
                }
                (synchronous ? mongo.ActivateProvider() : await mongo.ActivateProviderAsync()).IsError.Should().BeFalse();
            }
            var staleDetail = new AvatarDetail { Id = Guid.NewGuid(), Username = "secondary-only-detail", Karma = 999 };
            (await sqlite.SaveAvatarDetailAsync(staleDetail)).IsError.Should().BeFalse();
            var absenceManager = new ProviderManager(null,dna) { IsAutoFailOverEnabled = true };
            absenceManager.RegisterProvider(mongo);
            absenceManager.RegisterProvider(sqlite);
            absenceManager.SetAndReplaceAutoFailOverListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) }).IsError.Should().BeFalse();
            absenceManager.SetAndActivateCurrentStorageProvider(mongo).IsError.Should().BeFalse();
            var absenceRouter = new OASISHyperDrive(absenceManager);
            var absenceRequest = new StorageOperationRequest { Operation = "LoadAvatarDetail", AvatarId = staleDetail.Id, PreferredProvider = ProviderType.MongoDBOASIS };
            var authoritativeAbsence = synchronous ? absenceRouter.RouteRequest<IAvatarDetail>(absenceRequest) : await absenceRouter.RouteRequestAsync<IAvatarDetail>(absenceRequest);
            authoritativeAbsence.IsError.Should().BeFalse(authoritativeAbsence.Message);
            authoritativeAbsence.Result.Should().BeNull();
            (await sqlite.LoadAvatarDetailAsync(staleDetail.Id)).Result.Karma.Should().Be(999);
            foreach (var selector in new[] { "email", "username", "key" })
            {
                var selectorAvatar = new Avatar { Id = Guid.NewGuid(), Version = 3, Username = "mongo-selector-" + selector, Email = selector + "@mongo-selector.local",
                    AvatarType = new EnumValue<AvatarType>(AvatarType.User) };
                selectorAvatar.VerificationToken = "owned-verification-" + selector;
                selectorAvatar.ResetToken = "owned-reset-" + selector;
                selectorAvatar.RefreshTokens = new List<RefreshToken> { new RefreshToken { Token = "owned-refresh-" + selector } };
                var savedSelector = await mongo.SaveAvatarAsync(selectorAvatar);
                savedSelector.IsError.Should().BeFalse(savedSelector.Message);
                var selectorDetail = new AvatarDetail { Id = selectorAvatar.Id, Version = 3, Username = "independent-" + selector, Email = "detail-" + selector + "@mongo-selector.local", Karma = 123 };
                (await mongo.SaveAvatarDetailAsync(selectorDetail)).IsError.Should().BeFalse();
                var localKey = savedSelector.Result.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS];
                foreach (var tokenVersion in new[] { 0, 3, 2 })
                {
                    foreach (var tokenRead in new[] {
                        synchronous ? mongo.LoadAvatarByVerificationToken(selectorAvatar.VerificationToken, tokenVersion) : await mongo.LoadAvatarByVerificationTokenAsync(selectorAvatar.VerificationToken, tokenVersion),
                        synchronous ? mongo.LoadAvatarByResetToken(selectorAvatar.ResetToken, tokenVersion) : await mongo.LoadAvatarByResetTokenAsync(selectorAvatar.ResetToken, tokenVersion),
                        synchronous ? mongo.LoadAvatarByRefreshToken("owned-refresh-" + selector, tokenVersion) : await mongo.LoadAvatarByRefreshTokenAsync("owned-refresh-" + selector, tokenVersion) })
                    {
                        tokenRead.IsError.Should().BeFalse(tokenRead.Message);
                        if (tokenVersion == 2) tokenRead.Result.Should().BeNull();
                        else tokenRead.Result.Id.Should().Be(selectorAvatar.Id);
                    }
                }
                foreach (var listVersion in new[] { 0, 3, 2 })
                {
                    var avatarList = synchronous ? mongo.LoadAllAvatars(listVersion) : await mongo.LoadAllAvatarsAsync(listVersion);
                    var detailList = synchronous ? mongo.LoadAllAvatarDetails(listVersion) : await mongo.LoadAllAvatarDetailsAsync(listVersion);
                    avatarList.IsError.Should().BeFalse(avatarList.Message);
                    detailList.IsError.Should().BeFalse(detailList.Message);
                    avatarList.Result.Any(a => a.Id == selectorAvatar.Id).Should().Be(listVersion != 2);
                    detailList.Result.Any(a => a.Id == selectorDetail.Id).Should().Be(listVersion != 2);
                }
                foreach (var requestedVersion in new[] { 0, 3, 2 })
                {
                    foreach (var detailRead in new[] {
                        synchronous ? mongo.LoadAvatarDetail(selectorDetail.Id, requestedVersion) : await mongo.LoadAvatarDetailAsync(selectorDetail.Id, requestedVersion),
                        synchronous ? mongo.LoadAvatarDetailByUsername(selectorDetail.Username, requestedVersion) : await mongo.LoadAvatarDetailByUsernameAsync(selectorDetail.Username, requestedVersion),
                        synchronous ? mongo.LoadAvatarDetailByEmail(selectorDetail.Email, requestedVersion) : await mongo.LoadAvatarDetailByEmailAsync(selectorDetail.Email, requestedVersion) })
                    {
                        detailRead.IsError.Should().BeFalse(detailRead.Message);
                        if (requestedVersion == 2) detailRead.Result.Should().BeNull();
                        else detailRead.Result.Karma.Should().Be(123);
                    }
                }
                foreach (var requestedVersion in new[] { 0, 3, 2 })
                {
                    foreach (var versionRead in new[] {
                        synchronous ? mongo.LoadAvatar(selectorAvatar.Id, requestedVersion) : await mongo.LoadAvatarAsync(selectorAvatar.Id, requestedVersion),
                        synchronous ? mongo.LoadAvatarByUsername(selectorAvatar.Username, requestedVersion) : await mongo.LoadAvatarByUsernameAsync(selectorAvatar.Username, requestedVersion),
                        synchronous ? mongo.LoadAvatarByEmail(selectorAvatar.Email, requestedVersion) : await mongo.LoadAvatarByEmailAsync(selectorAvatar.Email, requestedVersion) })
                    {
                        versionRead.IsError.Should().BeFalse(versionRead.Message);
                        if (requestedVersion == 2) versionRead.Result.Should().BeNull();
                        else versionRead.Result.Id.Should().Be(selectorAvatar.Id);
                    }
                }
                var keyRead = synchronous ? mongo.LoadAvatarByProviderKey(localKey) : await mongo.LoadAvatarByProviderKeyAsync(localKey);
                keyRead.IsError.Should().BeFalse(keyRead.Message);
                keyRead.Result.Should().NotBeNull();
                keyRead.Result.Id.Should().Be(selectorAvatar.Id);
                (synchronous ? mongo.LoadAvatarByProviderKey(localKey, 3) : await mongo.LoadAvatarByProviderKeyAsync(localKey, 3)).Result.Id.Should().Be(selectorAvatar.Id);
                (synchronous ? mongo.LoadAvatarByProviderKey(localKey, 2) : await mongo.LoadAvatarByProviderKeyAsync(localKey, 2)).Result.Should().BeNull();
                (synchronous ? mongo.LoadAvatarByProviderKey(selectorAvatar.Username) : await mongo.LoadAvatarByProviderKeyAsync(selectorAvatar.Username)).Result.Should().BeNull();
                var selectorDeleted = selector == "email"
                    ? (synchronous ? mongo.DeleteAvatarByEmail(selectorAvatar.Email, true) : await mongo.DeleteAvatarByEmailAsync(selectorAvatar.Email, true))
                    : selector == "username"
                        ? (synchronous ? mongo.DeleteAvatarByUsername(selectorAvatar.Username, true) : await mongo.DeleteAvatarByUsernameAsync(selectorAvatar.Username, true))
                        : (synchronous ? mongo.DeleteAvatar(localKey, true) : await mongo.DeleteAvatarAsync(localKey, true));
                selectorDeleted.IsError.Should().BeFalse(selectorDeleted.Message);
                selectorDeleted.Result.Should().BeTrue();
                (await mongo.LoadAvatarDetailAsync(selectorAvatar.Id)).Result.DeletedDate.Should().NotBe(DateTime.MinValue);
                var hardDeleted = selector == "email"
                    ? (synchronous ? mongo.DeleteAvatarByEmail(selectorAvatar.Email, false) : await mongo.DeleteAvatarByEmailAsync(selectorAvatar.Email, false))
                    : selector == "username"
                        ? (synchronous ? mongo.DeleteAvatarByUsername(selectorAvatar.Username, false) : await mongo.DeleteAvatarByUsernameAsync(selectorAvatar.Username, false))
                        : (synchronous ? mongo.DeleteAvatar(localKey, false) : await mongo.DeleteAvatarAsync(localKey, false));
                hardDeleted.IsError.Should().BeFalse(hardDeleted.Message);
                hardDeleted.Result.Should().BeTrue();
                (await mongo.LoadAvatarAsync(selectorAvatar.Id)).Result.Should().BeNull();
                (await mongo.LoadAvatarDetailAsync(selectorAvatar.Id)).Result.Should().BeNull();
                var missingDeleted = selector == "email"
                    ? (synchronous ? mongo.DeleteAvatarByEmail(selectorAvatar.Email, false) : await mongo.DeleteAvatarByEmailAsync(selectorAvatar.Email, false))
                    : selector == "username"
                        ? (synchronous ? mongo.DeleteAvatarByUsername(selectorAvatar.Username, false) : await mongo.DeleteAvatarByUsernameAsync(selectorAvatar.Username, false))
                        : (synchronous ? mongo.DeleteAvatar(localKey, false) : await mongo.DeleteAvatarAsync(localKey, false));
                missingDeleted.IsError.Should().BeTrue();
                missingDeleted.Result.Should().BeFalse();
            }
            var softAvatar = new Avatar { Id = Guid.NewGuid(), Username = "mongo-soft-avatar", Email = "mongo-soft@test.local",
                AvatarType = new EnumValue<AvatarType>(AvatarType.User) };
            (await mongo.SaveAvatarAsync(softAvatar)).IsError.Should().BeFalse();
            var softDetail = new AvatarDetail { Id = softAvatar.Id, Username = "independent-mongo-detail", Karma = 123 };
            (await mongo.SaveAvatarDetailAsync(softDetail)).IsError.Should().BeFalse();
            var softDeleted = synchronous ? mongo.DeleteAvatar(softAvatar.Id, true) : await mongo.DeleteAvatarAsync(softAvatar.Id, true);
            softDeleted.IsError.Should().BeFalse(softDeleted.Message);
            softDeleted.Result.Should().BeTrue();
            var softStoredAvatar = (await mongo.LoadAvatarAsync(softAvatar.Id)).Result;
            var softStoredDetail = (await mongo.LoadAvatarDetailAsync(softAvatar.Id)).Result;
            softStoredAvatar.DeletedDate.Should().NotBe(DateTime.MinValue);
            softStoredDetail.DeletedDate.Should().Be(softStoredAvatar.DeletedDate);
            softStoredDetail.Karma.Should().Be(123);
            var failureMongo = new MongoDBOASIS(uri + "&appName=" + failureApp,databaseName);
            (await failureMongo.ActivateProviderAsync()).IsError.Should().BeFalse();
            var failManager = new ProviderManager(null,dna) { IsAutoFailOverEnabled = true };
            failManager.RegisterProvider(failureMongo);
            failManager.RegisterProvider(sqlite);
            failManager.SetAndReplaceAutoFailOverListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) }).IsError.Should().BeFalse();
            failManager.SetAndActivateCurrentStorageProvider(failureMongo).IsError.Should().BeFalse();
            var admin = client.GetDatabase("admin");
            var searchProbe = new Holon { Id = Guid.NewGuid(), Name = "owned-search-failure-probe" };
            var saveSearchProbe = new StorageOperationRequest { Operation = "SaveHolon", Payload = searchProbe, SaveChildren = false,
                PreferredProvider = request.PreferredProvider };
            var probeSaved = synchronous ? router.RouteRequest<IHolon>(saveSearchProbe) : await router.RouteRequestAsync<IHolon>(saveSearchProbe);
            probeSaved.IsError.Should().BeFalse(probeSaved.Message);
            probeSaved.IsWarning.Should().BeFalse();
            failManager.SetAndActivateCurrentStorageProvider(failureMongo).IsError.Should().BeFalse();
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "aggregate" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var searchFailureRequest = new StorageOperationRequest { Operation = "Search", LoadChildren = false,
                    PreferredProvider = ProviderType.MongoDBOASIS, SearchParams = new SearchParams { SearchOnlyForCurrentAvatar = false,
                        SearchGroups = new() { new SearchTextGroup { SearchQuery = searchProbe.Name, SearchHolons = true,
                            HolonType = searchProbe.HolonType, HolonSearchParams = new SearchHolonParams { Name = true } } } } };
                var searchFailureRouter = new OASISHyperDrive(failManager);
                var recoveredSearch = synchronous ? searchFailureRouter.RouteRequest<ISearchResults>(searchFailureRequest)
                    : await searchFailureRouter.RouteRequestAsync<ISearchResults>(searchFailureRequest);
                recoveredSearch.IsError.Should().BeFalse(recoveredSearch.Message);
                recoveredSearch.Result.SearchResultHolons.Select(h => h.Id).Should().Equal(searchProbe.Id);
                failManager.LastFailoverDiagnostic.Should().NotBeNull("the Mongo command must fail before SQLite is selected");
                failManager.LastFailoverDiagnostic.SelectedProvider.Should().Be(ProviderType.SQLLiteDBOASIS);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
            }
            foreach (var rejectDetailUpdate in new[] { false, true })
            {
                try
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                        { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                        { "data", new BsonDocument { { "failCommands", new BsonArray { "update" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                    });
                    if (rejectDetailUpdate)
                    {
                        var attempted = (await mongo.LoadAvatarDetailAsync(detail.Id)).Result;
                        attempted.Karma = 999;
                        var rejectedUpdate = synchronous ? failureMongo.SaveAvatarDetail(attempted) : await failureMongo.SaveAvatarDetailAsync(attempted);
                        rejectedUpdate.IsError.Should().BeTrue();
                        rejectedUpdate.Exception.Should().BeOfType<MongoCommandException>();
                        (await mongo.LoadAvatarDetailAsync(detail.Id)).Result.Karma.Should().Be(456);
                    }
                    else
                    {
                        var attempted = (await mongo.LoadAvatarAsync(avatar.Id)).Result;
                        attempted.FirstName = "rejected-update";
                        var rejectedUpdate = synchronous ? failureMongo.SaveAvatar(attempted) : await failureMongo.SaveAvatarAsync(attempted);
                        rejectedUpdate.IsError.Should().BeTrue();
                        rejectedUpdate.Exception.Should().BeOfType<MongoCommandException>();
                        (await mongo.LoadAvatarAsync(avatar.Id)).Result.FirstName.Should().Be("Updated");
                    }
                }
                finally
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                }
            }
            foreach (var rejectDetailLookup in new[] { false, true })
            {
                try
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                        { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                        { "data", new BsonDocument { { "failCommands", new BsonArray { "find" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                    });
                    if (rejectDetailLookup)
                    {
                        var attempted = (await mongo.LoadAvatarDetailAsync(detail.Id)).Result;
                        attempted.Karma = 998;
                        var rejected = synchronous ? failureMongo.SaveAvatarDetail(attempted) : await failureMongo.SaveAvatarDetailAsync(attempted);
                        rejected.IsError.Should().BeTrue();
                        rejected.Exception.Should().BeOfType<MongoCommandException>();
                        (await mongo.LoadAvatarDetailAsync(detail.Id)).Result.Karma.Should().Be(456);
                    }
                    else
                    {
                        var attempted = (await mongo.LoadAvatarAsync(avatar.Id)).Result;
                        attempted.FirstName = "rejected-preflight";
                        var rejected = synchronous ? failureMongo.SaveAvatar(attempted) : await failureMongo.SaveAvatarAsync(attempted);
                        rejected.IsError.Should().BeTrue();
                        rejected.Exception.Should().BeOfType<MongoCommandException>();
                        (await mongo.LoadAvatarAsync(avatar.Id)).Result.FirstName.Should().Be("Updated");
                    }
                }
                finally
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                }
            }
            foreach (var rejectDetailInsert in new[] { false, true })
            {
                var rejectedId = Guid.NewGuid();
                try
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                        { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                        { "data", new BsonDocument { { "failCommands", new BsonArray { "insert" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                    });
                    if (rejectDetailInsert)
                    {
                        var rejectedRecord = new AvatarDetail { Id = rejectedId, Username = "rejected-create-detail" };
                        var rejectedInsert = synchronous ? failureMongo.SaveAvatarDetail(rejectedRecord) : await failureMongo.SaveAvatarDetailAsync(rejectedRecord);
                        rejectedInsert.IsError.Should().BeTrue();
                        rejectedInsert.Exception.Should().BeOfType<MongoCommandException>();
                        (await mongo.LoadAvatarDetailAsync(rejectedId)).Result.Should().BeNull();
                    }
                    else
                    {
                        var rejectedRecord = new Avatar { Id = rejectedId, Username = "rejected-create-avatar", AvatarType = new EnumValue<AvatarType>(AvatarType.User) };
                        var rejectedInsert = synchronous ? failureMongo.SaveAvatar(rejectedRecord) : await failureMongo.SaveAvatarAsync(rejectedRecord);
                        rejectedInsert.IsError.Should().BeTrue();
                        rejectedInsert.Exception.Should().BeOfType<MongoCommandException>();
                        (await mongo.LoadAvatarAsync(rejectedId)).Result.Should().BeNull();
                    }
                }
                finally
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                }
            }
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", "alwaysOn" },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "update" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var singleWriteAvatar = new Avatar { Id = Guid.NewGuid(), Username = "single-write-avatar", AvatarType = new EnumValue<AvatarType>(AvatarType.User),
                    CreatedProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) };
                (synchronous ? failureMongo.SaveAvatar(singleWriteAvatar) : await failureMongo.SaveAvatarAsync(singleWriteAvatar)).IsError.Should().BeFalse();
                var singleWriteDetail = new AvatarDetail { Id = singleWriteAvatar.Id, Username = singleWriteAvatar.Username, Karma = 123,
                    CreatedProviderType = new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) };
                (synchronous ? failureMongo.SaveAvatarDetail(singleWriteDetail) : await failureMongo.SaveAvatarDetailAsync(singleWriteDetail)).IsError.Should().BeFalse();
                var storedAvatar = await client.GetDatabase(databaseName).GetCollection<API.Providers.MongoDBOASIS.Entities.Avatar>("Avatar").Find(x => x.HolonId == singleWriteAvatar.Id).FirstOrDefaultAsync();
                var storedDetail = await client.GetDatabase(databaseName).GetCollection<API.Providers.MongoDBOASIS.Entities.AvatarDetail>("AvatarDetail").Find(x => x.HolonId == singleWriteAvatar.Id).FirstOrDefaultAsync();
                storedAvatar.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS].Should().Be(storedAvatar.Id);
                storedDetail.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS].Should().Be(storedDetail.Id);
                storedAvatar.CreatedProviderType.Value.Should().Be(ProviderType.SQLLiteDBOASIS);
                storedDetail.CreatedProviderType.Value.Should().Be(ProviderType.SQLLiteDBOASIS);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
            }
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "createIndexes" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var failedActivation = synchronous ? failureMongo.ActivateProvider() : await failureMongo.ActivateProviderAsync();
                failedActivation.IsError.Should().BeTrue();
                failedActivation.Exception.Should().BeOfType<MongoCommandException>();
                failureMongo.IsProviderActivated.Should().BeFalse();
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
            }
            (synchronous ? failureMongo.ActivateProvider() : await failureMongo.ActivateProviderAsync()).IsError.Should().BeFalse();
            failureMongo.IsProviderActivated.Should().BeTrue();
            foreach (var detailsList in new[] { false, true })
            {
                try
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                        { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                        { "data", new BsonDocument { { "failCommands", new BsonArray { "find" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                    });
                    if (detailsList)
                    {
                        var listFailure = synchronous ? failureMongo.LoadAllAvatarDetails() : await failureMongo.LoadAllAvatarDetailsAsync();
                        listFailure.IsError.Should().BeTrue();
                        listFailure.Exception.Should().BeOfType<MongoCommandException>();
                        listFailure.Result.Should().BeNull();
                    }
                    else
                    {
                        var listFailure = synchronous ? failureMongo.LoadAllAvatars() : await failureMongo.LoadAllAvatarsAsync();
                        listFailure.IsError.Should().BeTrue();
                        listFailure.Exception.Should().BeOfType<MongoCommandException>();
                        listFailure.Result.Should().BeNull();
                    }
                }
                finally
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                }
            }
            foreach (var detailSelector in new[] { "id", "username", "email" })
            {
                try
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                        { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                        { "data", new BsonDocument { { "failCommands", new BsonArray { "find" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                    });
                    var detailFailure = detailSelector == "id"
                        ? (synchronous ? failureMongo.LoadAvatarDetail(detail.Id) : await failureMongo.LoadAvatarDetailAsync(detail.Id))
                        : detailSelector == "username"
                            ? (synchronous ? failureMongo.LoadAvatarDetailByUsername(detail.Username) : await failureMongo.LoadAvatarDetailByUsernameAsync(detail.Username))
                            : (synchronous ? failureMongo.LoadAvatarDetailByEmail(detail.Email) : await failureMongo.LoadAvatarDetailByEmailAsync(detail.Email));
                    detailFailure.IsError.Should().BeTrue();
                    detailFailure.Exception.Should().BeOfType<MongoCommandException>();
                    detailFailure.Result.Should().BeNull();
                }
                finally
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                }
            }
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "find" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var keyFailure = synchronous ? failureMongo.LoadAvatarByProviderKey(avatar.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS])
                    : await failureMongo.LoadAvatarByProviderKeyAsync(avatar.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS]);
                keyFailure.IsError.Should().BeTrue();
                keyFailure.Exception.Should().BeOfType<MongoCommandException>();
                keyFailure.Result.Should().BeNull();
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
            }
            foreach (var selector in new[] { "email", "username", "key" })
            {
                try
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                        { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                        { "data", new BsonDocument { { "failCommands", new BsonArray { "find" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                    });
                    var selectorFailure = selector == "email"
                        ? (synchronous ? failureMongo.DeleteAvatarByEmail(avatar.Email, false) : await failureMongo.DeleteAvatarByEmailAsync(avatar.Email, false))
                        : selector == "username"
                            ? (synchronous ? failureMongo.DeleteAvatarByUsername(avatar.Username, false) : await failureMongo.DeleteAvatarByUsernameAsync(avatar.Username, false))
                            : (synchronous ? failureMongo.DeleteAvatar(avatar.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS], false) : await failureMongo.DeleteAvatarAsync(avatar.ProviderUniqueStorageKey[ProviderType.MongoDBOASIS], false));
                    selectorFailure.IsError.Should().BeTrue();
                    selectorFailure.Result.Should().BeFalse();
                    selectorFailure.Exception.Should().BeOfType<MongoCommandException>();
                    (await mongo.LoadAvatarAsync(avatar.Id)).Result.Should().NotBeNull();
                    (await mongo.LoadAvatarDetailAsync(detail.Id)).Result.Karma.Should().Be(456);
                }
                finally
                {
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                }
            }
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
            (await failureMongo.ActivateProviderAsync()).IsError.Should().BeFalse();
            try
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("skip",1) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "delete" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var rejectedDelete = synchronous ? failureMongo.DeleteAvatar(avatar.Id, false) : await failureMongo.DeleteAvatarAsync(avatar.Id, false);
                rejectedDelete.IsError.Should().BeTrue();
                rejectedDelete.Result.Should().BeFalse();
                rejectedDelete.Exception.Should().NotBeNull();
                (await mongo.LoadAvatarAsync(avatar.Id)).Result.Should().NotBeNull();
                (await mongo.LoadAvatarDetailAsync(detail.Id)).Result.Karma.Should().Be(456);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                await failureMongo.DeActivateProviderAsync();
            }
            (await failureMongo.ActivateProviderAsync()).IsError.Should().BeFalse();
            API.Providers.MongoDBOASIS.MongoTransactionReconciliationException commitEvidence = null;
            try
            {
                var commitManager = new ProviderManager(null,dna) { IsAutoFailOverEnabled = true };
                commitManager.RegisterProvider(failureMongo);
                commitManager.RegisterProvider(sqlite);
                commitManager.SetAndReplaceAutoFailOverListForProviders(new[] { new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS) }).IsError.Should().BeFalse();
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument {
                    { "configureFailPoint", "failCommand" }, { "mode", new BsonDocument("times",1) },
                    { "data", new BsonDocument { { "failCommands", new BsonArray { "commitTransaction" } }, { "appName", failureApp }, { "errorCode", 2 } } }
                });
                var commitRequest = new StorageOperationRequest { Operation = "DeleteAvatar", AvatarId = avatar.Id, SoftDelete = false, PreferredProvider = ProviderType.MongoDBOASIS };
                var commitRouter = new OASISHyperDrive(commitManager);
                var rejectedCommit = synchronous ? commitRouter.RouteRequest<bool>(commitRequest) : await commitRouter.RouteRequestAsync<bool>(commitRequest);
                commitEvidence = rejectedCommit.Exception as API.Providers.MongoDBOASIS.MongoTransactionReconciliationException;
                rejectedCommit.IsError.Should().BeTrue();
                rejectedCommit.Result.Should().BeFalse();
                commitEvidence.Should().NotBeNull();
                commitEvidence.TransactionNumber.Should().BeGreaterThanOrEqualTo(0);
                commitEvidence.InnerException.Should().NotBeNull();
                (await sqlite.LoadAvatarAsync(avatar.Id)).Result.Should().NotBeNull();
                (await sqlite.LoadAvatarDetailAsync(detail.Id)).Result.Karma.Should().Be(456);
            }
            finally
            {
                await admin.RunCommandAsync<BsonDocument>(new BsonDocument { { "configureFailPoint", "failCommand" }, { "mode", "off" } });
                if (commitEvidence != null)
                    await admin.RunCommandAsync<BsonDocument>(new BsonDocument("killSessions", new BsonArray { BsonDocument.Parse(commitEvidence.SessionId) }));
                await failureMongo.DeActivateProviderAsync();
            }
            (await mongo.LoadAvatarAsync(avatar.Id)).Result.Should().NotBeNull();
            (await mongo.LoadAvatarDetailAsync(detail.Id)).Result.Karma.Should().Be(456);
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
            var searchFallbackRequest = new StorageOperationRequest { Operation = "Search", LoadChildren = false,
                PreferredProvider = ProviderType.SQLLiteDBOASIS, SearchParams = new SearchParams { SearchOnlyForCurrentAvatar = false,
                    SearchGroups = new() { new SearchTextGroup { SearchQuery = "real-mongo-fallback", SearchHolons = true,
                        HolonType = fallbackHolon.HolonType, HolonSearchParams = new SearchHolonParams { Name = true } } } } };
            var searchFallback = synchronous ? router.RouteRequest<ISearchResults>(searchFallbackRequest) : await router.RouteRequestAsync<ISearchResults>(searchFallbackRequest);
            searchFallback.IsError.Should().BeFalse(searchFallback.Message);
            searchFallback.Result.SearchResultHolons.Select(h => h.Id).Should().Equal(fallbackHolon.Id);
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
