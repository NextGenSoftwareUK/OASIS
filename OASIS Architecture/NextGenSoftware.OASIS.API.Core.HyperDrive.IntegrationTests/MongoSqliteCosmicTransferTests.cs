using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.Utilities;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    [Theory]
    [InlineData(false, false, false)] [InlineData(false, false, true)]
    [InlineData(false, true, false)] [InlineData(false, true, true)]
    [InlineData(true, false, false)] [InlineData(true, false, true)]
    [InlineData(true, true, false)] [InlineData(true, true, true)]
    public async Task CosmicTransferCopiesCleanNativeGraphWithoutRewritingItsSource(bool synchronous, bool mongoSource, bool v2)
    {
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI").Should().Be(uri);
        var workspace = new DirectoryInfo(AppContext.BaseDirectory);
        while (workspace != null && !File.Exists(Path.Combine(workspace.FullName, "Scripts/TestHosts/Run-ReusableUsageRecoveryTests.ps1"))) workspace = workspace.Parent;
        workspace.Should().NotBeNull();
        var directory = Path.Combine(workspace!.FullName, "TestResults/HyperDriveMongoSqliteCurrent");
        Directory.CreateDirectory(directory);
        using var lease = File.Open(Path.Combine(directory, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var dbPath = Path.Combine(directory, "owned.db");
        const string databaseName = "hyperdrive_mongo_sqlite_fixture";
        var client = new MongoClient(uri);
        using (var names = await client.ListDatabaseNamesAsync())
            if (File.Exists(dbPath) || (await names.ToListAsync()).Contains(databaseName))
                throw new InvalidOperationException("Existing fixture requires inspection; refusing overwrite or cleanup.");
        MongoDBOASIS? mongo = null; SQLLiteDBOASIS? sqlite = null; var complete = false;
        try
        {
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode = v2 ? HyperDriveModes.V2 : HyperDriveModes.Legacy,
                StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false } } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            mongo = new MongoDBOASIS(uri, databaseName, dna); sqlite = new SQLLiteDBOASIS($"Data Source={dbPath};Pooling=False");
            (await mongo.ActivateProviderAsync()).IsError.Should().BeFalse();
            (await sqlite.ActivateProviderAsync()).IsError.Should().BeFalse();
            IOASISStorageProvider source = mongoSource ? mongo : sqlite;
            IOASISStorageProvider destination = mongoSource ? sqlite : mongo;
            var child = new Holon { Id = Guid.NewGuid(), Name = "owned transfer child", IsPublic = true };
            var root = new Holon { Id = Guid.NewGuid(), Name = "owned transfer root", Description = "copied from a clean source",
                IsPublic = true, CreatedByAvatarId = Guid.NewGuid(), CreatedDate = DateTime.UtcNow,
                Children = new List<IHolon> { child } };
            child.ParentHolonId = root.Id; root.MetaData["amount"] = 2.5m; child.MetaData["purpose"] = "child transfer evidence";
            var seeded = await source.SaveHolonAsync(root, saveChildrenOnProvider: true);
            seeded.IsError.Should().BeFalse(seeded.Message + seeded.Exception);
            var providers = new ProviderManager(null, dna) { IsAutoReplicationEnabled = true,
                IsAutoLoadBalanceEnabled = true, IsAutoFailOverEnabled = true };
            providers.RegisterProvider(mongo); providers.RegisterProvider(sqlite);
            providers.SetAndActivateCurrentStorageProvider(source).IsError.Should().BeFalse();
            providers.SetAndReplaceAutoReplicationListForProviders(new[] { new EnumValue<ProviderType>(source.ProviderType.Value) }).IsError.Should().BeFalse();
            var manager = new HolonManager(source, dna, providers);
            var sourceBefore = (await source.LoadHolonAsync(root.Id, loadChildrenFromProvider: true)).Result;
            var revision = sourceBefore.VersionId; var modified = sourceBefore.ModifiedDate;
            var sent = synchronous
                ? manager.SendHolon<Holon>(root.Id, Guid.NewGuid(), source.ProviderType.Value, destination.ProviderType.Value)
                : await manager.SendHolonAsync<Holon>(root.Id, Guid.NewGuid(), source.ProviderType.Value, destination.ProviderType.Value);
            sent.IsError.Should().BeFalse(sent.Message + sent.Exception); sent.IsSaved.Should().BeTrue();
            sent.Result.Id.Should().Be(root.Id); sent.Result.HasHolonChanged().Should().BeFalse();
            sent.Result.Children.Should().ContainSingle().Which.Id.Should().Be(child.Id);
            var target = await destination.LoadHolonAsync(root.Id, loadChildrenFromProvider: true);
            target.IsError.Should().BeFalse(target.Message + target.Exception);
            target.Result.Name.Should().Be(root.Name); target.Result.Description.Should().Be(root.Description);
            target.Result.IsPublic.Should().BeTrue();
            System.Text.Json.JsonSerializer.SerializeToElement(target.Result.MetaData["amount"]).GetDecimal().Should().Be(2.5m);
            var targetChild = target.Result.Children.Should().ContainSingle().Which;
            System.Text.Json.JsonSerializer.SerializeToElement(targetChild.MetaData["purpose"]).GetString().Should().Be("child transfer evidence");
            target.Result.VersionId.Should().Be(sent.Result.VersionId);
            var sourceAfter = await source.LoadHolonAsync(root.Id, loadChildrenFromProvider: true);
            sourceAfter.IsError.Should().BeFalse(sourceAfter.Message + sourceAfter.Exception);
            sourceAfter.Result.VersionId.Should().Be(revision); sourceAfter.Result.ModifiedDate.Should().Be(modified);
            sourceAfter.Result.Children.Should().ContainSingle().Which.Id.Should().Be(child.Id);
            providers.IsAutoReplicationEnabled.Should().BeTrue("the transfer override must not change runtime policy");
            using var control = new DataContext($"Data Source={dbPath};Pooling=False");
            (await control.Holons.AsNoTracking().CountAsync()).Should().Be(2);
            var mongoRows = client.GetDatabase(databaseName).GetCollection<BsonDocument>("Holon");
            (await mongoRows.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty)).Should().Be(2);
            complete = true;
        }
        finally
        {
            if (mongo != null) await mongo.DeActivateProviderAsync();
            sqlite?.Dispose();
            if (complete)
            {
                await client.DropDatabaseAsync(databaseName);
                foreach (var path in new[] { dbPath, dbPath + "-wal", dbPath + "-shm" })
                    if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
