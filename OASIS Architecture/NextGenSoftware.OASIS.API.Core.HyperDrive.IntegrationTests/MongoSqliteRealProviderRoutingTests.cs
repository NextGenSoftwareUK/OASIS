using FluentAssertions;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class MongoSqliteRealProviderRoutingTests
{
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
            var delete = new StorageOperationRequest { Operation = "DeleteHolon", HolonId = holon.Id, SoftDelete = false, PreferredProvider = request.PreferredProvider };
            var deleted = synchronous ? router.RouteRequest<IHolon>(delete) : await router.RouteRequestAsync<IHolon>(delete);
            deleted.IsError.Should().BeFalse(deleted.Message);
            deleted.IsWarning.Should().BeFalse(string.Join("; ",manager.LastReplicationDiagnostic.Attempts.Select(x=>x.Message)));
            (await mongo.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Should().BeNull();
            (await sqlite.LoadHolonAsync(holon.Id,loadChildren:false)).Result.Should().BeNull();
            manager.LastReplicationDiagnostic.SucceededCount.Should().Be(1);
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
