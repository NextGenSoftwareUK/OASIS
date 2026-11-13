using System.Text.Json;
using Microsoft.Data.Sqlite;
using MongoDB.Bson;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

namespace CosmicCqlDemo;

/// <summary>Real native SQLite records imported into a separately owned Mongo target.</summary>
public static class NativeImportMongoDemo
{
    public static async Task<NativeImportCqlDemoReport> RunAsync(string root, TextWriter output)
    {
        root = Path.GetFullPath(root);
        Check(File.Exists(Path.Combine(root, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1")), "Use the existing task checkout.");
        const string uri = "mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        const string database = "cosmic_native_import_cql_demo";
        var work = Path.Combine(root, "TestResults/NativeImportMongoCurrent");
        var state = Path.Combine(root, "LocalState/COSMIC/NativeImportMongoCurrent");
        Directory.CreateDirectory(work);
        Directory.CreateDirectory(state);
        using var lease = new FileStream(Path.Combine(state, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var journal = Path.Combine(state, "fixture.pending.json");
        var sourcePath = Path.Combine(work, "source.db");
        var unusedAdapterPath = Path.Combine(work, "unused-adapter.db");
        var reportPath = Path.Combine(work, "report.json");
        Check(!File.Exists(journal) && !File.Exists(reportPath), "Preserve prior import outcome before another run.");
        foreach (var path in Files(sourcePath, unusedAdapterPath)) Check(!File.Exists(path), "Reconcile original source files first.");
        var client = new MongoClient(uri);
        using (var names = await client.ListDatabaseNamesAsync())
            Check(!(await names.ToListAsync()).Contains(database), "The dedicated target database must be absent before intent publication.");
        var firstActor = Guid.NewGuid();
        var secondActor = Guid.NewGuid();
        using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(file, new { database, sourcePath, unusedAdapterPath, firstActor, secondActor, phase = "before-owned-create" });
            file.Flush(true);
        }
        async Task SourceSql(string sql)
        {
            await using var connection = new SqliteConnection($"Data Source={sourcePath};Pooling=False");
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            await command.ExecuteNonQueryAsync();
        }
        await SourceSql("CREATE TABLE coffees(id INTEGER PRIMARY KEY,name TEXT,stock INTEGER); INSERT INTO coffees VALUES(1,'Espresso',19),(2,'Latte',0),(3,'Tea',25);");
        var source = new SQLLiteDBOASIS($"Data Source={unusedAdapterPath};Pooling=False")
        { NativeImportConnectionString = $"Data Source={sourcePath};Pooling=False" };
        var target = new MongoDBOASIS(uri, database) { IsVersionControlEnabled = false };
        var active = false;
        NativeImportCqlDemoReport report;
        try
        {
            Check(!(await target.ActivateProviderAsync()).IsError, "Mongo target activation failed.");
            active = true;
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false } } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            var registry = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
            registry.RegisterProvider(target);
            var manager = new HolonManager(target, dna, registry);

            // Import reads SQLite; Save and every CQL read explicitly use Mongo.
            // This is a target change, not a replica or a managed query substitute.
            report = await NativeImportCqlDemo.VerifyWorkflowAsync(manager, source, target,
                firstActor, secondActor, sourcePath, SourceSql, output);

            // Validate the complete newly created Mongo scope before exact teardown.
            var db = client.GetDatabase(database);
            using (var names = await db.ListCollectionNamesAsync())
                Check((await names.ToListAsync()).Order().SequenceEqual(new[] { "Avatar", "AvatarDetail", "CosmicHolonOutbox", "Holon" }), "Unexpected target collections; preserve fixture.");
            foreach (var empty in new[] { "Avatar", "AvatarDetail", "CosmicHolonOutbox" })
                Check(await db.GetCollection<BsonDocument>(empty).CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty) == 0, "Unexpected target records; preserve fixture.");
            Check(await db.GetCollection<BsonDocument>("Holon").CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty) == 6, "The target must contain exactly six owned holons.");
            foreach (var actor in new[] { firstActor, secondActor })
            foreach (var key in new[] { "[1]", "[2]", "[3]" })
            {
                var id = NativeRecordHolonMapper.StableHolonId(actor, ProviderType.SQLLiteDBOASIS, "coffees", key);
                var loaded = await target.LoadHolonAsync(id, loadChildren: false);
                Check(!loaded.IsError && loaded.Result != null && loaded.Result.CreatedByAvatarId == actor, "Target ownership differs from original intent.");
                Check(loaded.Result.MetaData[NativeRecordHolonMapper.KeyMetaDataKey].ToString() == key &&
                    loaded.Result.MetaData[NativeRecordHolonMapper.SourceMetaDataKey].ToString() == "coffees" &&
                    loaded.Result.MetaData[NativeRecordHolonMapper.ProviderMetaDataKey].ToString() == "SQLLiteDBOASIS", "Native source provenance differs.");
            }
            Check(!(await target.DeActivateProviderAsync()).IsError, "Target shutdown failed.");
            active = false;
            await client.DropDatabaseAsync(database);
            using (var names = await client.ListDatabaseNamesAsync())
                Check(!(await names.ToListAsync()).Contains(database), "Owned target cleanup incomplete.");
        }
        finally
        {
            try { if (active) Check(!(await target.DeActivateProviderAsync()).IsError, "Preserve original target outcome after shutdown failure."); }
            finally { source.Dispose(); }
        }
        foreach (var path in Files(sourcePath, unusedAdapterPath))
        {
            if (File.Exists(path)) File.Delete(path);
            Check(!File.Exists(path), "Owned source cleanup incomplete.");
        }
        File.Delete(journal);
        report = report with { Cleaned = true };
        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(new
        {
            source = "SQLite native table", target = "MongoDBOASIS", physicalTargetRows = 6,
            workflow = report, exactCleanupVerified = true
        }, new JsonSerializerOptions { WriteIndented = true }));
        return report;
    }

    private static IEnumerable<string> Files(params string[] paths)
        => paths.SelectMany(path => new[] { path, path + "-wal", path + "-shm", path + "-journal" });
    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
