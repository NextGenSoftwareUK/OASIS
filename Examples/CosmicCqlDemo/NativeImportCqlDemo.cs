using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS;

namespace CosmicCqlDemo;

public sealed record NativeImportCqlDemoReport(int Created, int Unchanged, int Updated,
    int FirstActorRows, int SecondActorRows, bool DistinctActorIds, bool SourcePreserved, bool Cleaned);

/// <summary>Owned real-file fixture shared by the executable demo and native test.</summary>
public static class NativeImportCqlDemo
{
    public static async Task<NativeImportCqlDemoReport> RunAsync(string root, TextWriter output)
    {
        root = Path.GetFullPath(root);
        Check(File.Exists(Path.Combine(root, "Scripts/TestHosts/Invoke-WithProviderBuildLease.ps1")), "Use the existing task checkout.");
        var work = Path.Combine(root, "TestResults/NativeImportCqlCurrent");
        var state = Path.Combine(root, "LocalState/COSMIC/NativeImportCqlCurrent");
        Directory.CreateDirectory(work);
        Directory.CreateDirectory(state);
        using var lease = new FileStream(Path.Combine(state, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var journal = Path.Combine(state, "fixture.pending.json");
        var sourcePath = Path.Combine(work, "source.db");
        var targetPath = Path.Combine(work, "target.db");
        var reportPath = Path.Combine(work, "report.json");
        Check(!File.Exists(journal) && !File.Exists(reportPath), "Preserve prior fixture/evidence before another run.");
        foreach (var path in OwnedDatabaseFiles(sourcePath, targetPath))
            Check(!File.Exists(path), "Reconcile original database files before another write.");

        var firstActor = Guid.NewGuid();
        var secondActor = Guid.NewGuid();
        using (var file = new FileStream(journal, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await JsonSerializer.SerializeAsync(file, new { sourcePath, targetPath, firstActor, secondActor, phase = "before-owned-create" });
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
        var provider = new SQLLiteDBOASIS($"Data Source={targetPath};Pooling=False")
        { NativeImportConnectionString = $"Data Source={sourcePath};Pooling=False" };
        NativeImportCqlDemoReport report;
        try
        {
            Check(!(await provider.ActivateProviderAsync()).IsError, "Target activation failed.");
            var dna = new OASISDNA { OASIS = new NextGenSoftware.OASIS.API.DNA.OASIS
            { HyperDriveMode = HyperDriveModes.V2, StorageProviders = new StorageProviderSettings { LogSwitchingProviders = false } } };
            dna.OASIS.SubscriptionConfig.PlanType = "Gold";
            var registry = new ProviderManager(null, dna)
            { IsAutoFailOverEnabled = false, IsAutoReplicationEnabled = false, IsAutoLoadBalanceEnabled = false };
            registry.RegisterProvider(provider);
            var manager = new HolonManager(provider, dna, registry);
            report = await VerifyWorkflowAsync(manager, provider, provider, firstActor, secondActor, sourcePath, SourceSql, output);
        }
        finally { Check(!(await provider.DeActivateProviderAsync()).IsError, "Target teardown failed; preserve original fixture."); }

        // Every listed file was absent before this journaled synthetic fixture.
        // Delete only these exact paths after provider shutdown; never a directory.
        foreach (var path in OwnedDatabaseFiles(sourcePath, targetPath))
        {
            if (File.Exists(path)) File.Delete(path);
            Check(!File.Exists(path), "Owned database cleanup incomplete.");
        }
        File.Delete(journal);
        report = report with { Cleaned = true };
        await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        await output.WriteLineAsync("\nVerified: stable re-import, no-op revisions, changed-row update, private per-avatar IDs and exact cleanup.\n");
        return report;
    }

    // The same scenario proves either local or cross-provider persistence.
    // Fixture hosts own their resources; this method owns import/query assertions.
    internal static async Task<NativeImportCqlDemoReport> VerifyWorkflowAsync(
        HolonManager manager, INativeDataImportProvider source, ICosmicQueryStorageProvider target,
        Guid firstActor, Guid secondActor, string sourcePath, Func<string, Task> sourceSql, TextWriter output)
    {
        var first = await NativeImportCqlWalkthrough.RunAsync(manager, source, target, "coffees", firstActor, output);
        Check(first.Import.HolonsCreated == 3 && first.Rows.Count == 3, "First import must create three queryable holons.");
        foreach (var row in first.Rows)
        {
            var nativeKey = row.MetaData[NativeRecordHolonMapper.KeyMetaDataKey].ToString()!;
            Check(row.Id == NativeRecordHolonMapper.StableHolonId(firstActor, source.NativeDataProviderType, "coffees", nativeKey), "Imported identity differs from native provenance.");
            Check(row.CreatedByAvatarId == firstActor, "Save must acknowledge the importing actor.");
        }
        var originalVersions = first.Rows.ToDictionary(row => row.Id, row => row.Version);
        var sourceHash = Hash(sourcePath);
        var unchanged = await NativeImportCqlWalkthrough.RunAsync(manager, source, target, "coffees", firstActor, output);
        Check(unchanged.Import.HolonsUnchanged == 3 && unchanged.Import.HolonsCreated == 0, "Unchanged re-import must not write duplicates/revisions.");
        Check(unchanged.Rows.All(row => row.Version == originalVersions[row.Id]), "Unchanged re-import changed a revision.");
        Check(sourceHash == Hash(sourcePath), "Import must not mutate the native application database.");

        // The application changes its own table; importing again updates the
        // same holon rather than allocating a new identity.
        await sourceSql("UPDATE coffees SET stock=18 WHERE id=1;");
        sourceHash = Hash(sourcePath);
        var updated = await NativeImportCqlWalkthrough.RunAsync(manager, source, target, "coffees", firstActor, output);
        Check(updated.Import.HolonsUpdated == 1 && updated.Import.HolonsUnchanged == 2 && updated.Rows.Count == 3, "Only the changed native row must update.");
        Check(updated.Rows.Single(row => row.Name == "Espresso").Id == first.Rows.Single(row => row.Name == "Espresso").Id, "Re-import changed stable identity.");

        // Another avatar imports the same source into disjoint private holons.
        var other = await NativeImportCqlWalkthrough.RunAsync(manager, source, target, "coffees", secondActor, output);
        Check(other.Import.HolonsCreated == 3 && other.Rows.Count == 3, "Second actor must receive three independent holons.");
        Check(!other.Rows.Select(row => row.Id).Intersect(updated.Rows.Select(row => row.Id)).Any(), "Actors share imported identities.");
        Check(other.Rows.All(row => row.CreatedByAvatarId == secondActor), "Second actor ownership differs.");
        Check(sourceHash == Hash(sourcePath), "Import changed the native application database.");
        var firstAgain = await NativeImportCqlWalkthrough.RunAsync(manager, source, target, "coffees", firstActor, output);
        Check(firstAgain.Import.HolonsUnchanged == 3 && firstAgain.Rows.Count == 3 &&
            firstAgain.Rows.Select(row => row.Id).OrderBy(id => id).SequenceEqual(updated.Rows.Select(row => row.Id).OrderBy(id => id)),
            "First actor must still see only its own three holons after the second import.");
        return new(first.Import.HolonsCreated, unchanged.Import.HolonsUnchanged, updated.Import.HolonsUpdated,
            firstAgain.Rows.Count, other.Rows.Count, true, true, false);
    }

    private static IEnumerable<string> OwnedDatabaseFiles(params string[] paths)
        => paths.SelectMany(path => new[] { path, path + "-wal", path + "-shm", path + "-journal" });
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    private static void Check(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }
}
