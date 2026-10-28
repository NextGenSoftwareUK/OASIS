using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.DuckDBOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class DuckDbStorageConformanceTests
{
    [DataTestMethod, DataRow("async"), DataRow("sync")]
    public async Task LegacyRecordsAreRefusedWithoutHidingOrChangingThem(string mode)
    {
        var work = Path.Combine(FixtureDirectory(), mode);
        var path = Path.Combine(work, "initial-save-storage.duckdb"); Assert.IsTrue(File.Exists(path));
        var before = System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(path));
        using var report = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(work, "initial-save-report.json")));
        var owner = report.RootElement.GetProperty("OwnerId").GetGuid();
        await using (var connection = new DuckDB.NET.Data.DuckDBConnection("Data Source=" + path))
        {
            await connection.OpenAsync(); await using var command = connection.CreateCommand();
            command.CommandText = "SELECT id,doc FROM oasis_avatars";
            await using var rows = await command.ExecuteReaderAsync();
            Assert.IsTrue(await rows.ReadAsync()); Assert.AreEqual(owner, Guid.Parse(rows.GetString(0)));
            using var payload = System.Text.Json.JsonDocument.Parse(rows.GetString(1));
            Assert.AreEqual(owner, payload.RootElement.GetProperty("Id").GetGuid());
            Assert.IsFalse(await rows.ReadAsync());
        }
        var result = await new DuckDBOASIS("Data Source=" + path).ActivateProviderAsync();
        Assert.IsTrue(result.IsError); Assert.IsFalse(result.Result);
        Assert.IsInstanceOfType(result.Exception, typeof(InvalidOperationException));
        CollectionAssert.AreEqual(before, System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(path)));
    }

    [TestMethod]
    public async Task NativeUpdateAcknowledgesChangedRecord()
    {
        if (Environment.GetEnvironmentVariable("OASIS_DUCKDB_STORAGE_CONFORMANCE") != "1") Assert.Inconclusive("Native opt-in required.");
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.IsNotNull(root);
        var work = Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent", "DuckDBOASIS");
        var path = Path.Combine(work, "update-regression.duckdb");
        Assert.IsFalse(File.Exists(path) || File.Exists(path + ".wal"), "Preserve prior regression fixture.");
        var provider = new DuckDBOASIS("Data Source=" + path);
        var record = new NextGenSoftware.OASIS.API.Core.Holons.Holon { Id = Guid.NewGuid(), Name = "synthetic-original" };
        await File.WriteAllTextAsync(Path.Combine(work, "update-regression-intent.json"), System.Text.Json.JsonSerializer.Serialize(new { record.Id }));
        var activated = await provider.ActivateProviderAsync(); Assert.IsFalse(activated.IsError);
        var first = await provider.SaveHolonAsync(record, saveChildren: false); Assert.IsFalse(first.IsError, first.Message);
        record.Name = "synthetic-updated";
        var updated = await provider.SaveHolonAsync(record, saveChildren: false);
        Assert.IsFalse(updated.IsError, updated.Exception?.GetType().FullName + ": " + updated.Message);
        Assert.IsTrue(updated.IsSaved); Assert.AreEqual(1, updated.SavedCount);
        var loaded = await provider.LoadHolonAsync(record.Id, loadChildren: false);
        Assert.IsFalse(loaded.IsError); Assert.AreEqual(record.Name, loaded.Result.Name);
        var deleted = await provider.DeleteHolonAsync(record.Id);
        Assert.IsTrue(deleted.IsDeleted); Assert.IsFalse(deleted.IsError);
        var cleaned = await provider.DeleteHolonSoftAsync(record.Id, softDelete: false); Assert.IsFalse(cleaned.IsError); Assert.IsTrue(cleaned.IsDeleted);
    }

    [TestMethod]
    public async Task NativeConditionsRejectStaleOrMissingRecordsWithoutChangingThem()
    {
        var work = FixtureDirectory();
        var path = Path.Combine(work, "conditions.duckdb");
        Assert.IsFalse(File.Exists(path) || File.Exists(path + ".wal"), "Preserve prior condition fixture.");
        var backend = new DuckDbRecordBackend("Data Source=" + path);
        await backend.VerifyAsync();
        const string key = "synthetic/quote'☃";
        await backend.CreateOnlyAsync(key, "first");
        var first = await backend.ReadObservedAsync(key); Assert.IsNotNull(first);
        Assert.IsTrue(Guid.TryParse(first.Revision, out var revision) && revision != Guid.Empty);
        await backend.ReplaceUnchangedAsync(key, "second", first.Revision);
        var second = await backend.ReadObservedAsync(key); Assert.IsNotNull(second);
        Assert.AreNotEqual(first.Revision, second.Revision);
        await Assert.ThrowsExceptionAsync<NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException>(() => backend.ReplaceUnchangedAsync(key, "bad", first.Revision));
        await Assert.ThrowsExceptionAsync<NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException>(() => backend.DeleteUnchangedAsync(key, first.Revision));
        await Assert.ThrowsExceptionAsync<NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException>(() => backend.CreateOnlyAsync(key, "bad"));
        await Assert.ThrowsExceptionAsync<NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException>(() => backend.ReplaceUnchangedAsync("missing", "bad", first.Revision));
        await Assert.ThrowsExceptionAsync<NextGenSoftware.OASIS.API.Core.Helpers.StorageLifecycleConflictException>(() => backend.DeleteUnchangedAsync("missing", first.Revision));
        Assert.AreEqual("second", await backend.GetAsync(key));
        CollectionAssert.AreEqual(new[] { key }, (await backend.ListKeysAsync("synthetic/quote'")).ToArray());
        await backend.DeleteUnchangedAsync(key, second.Revision);
        Assert.IsNull(await backend.GetAsync(key));
        await backend.PutAsync(key, "third"); await backend.PutAsync(key, "fourth");
        Assert.AreEqual("fourth", await backend.GetAsync(key));
        await backend.DeleteAsync(key); await backend.DeleteAsync(key);
        Assert.AreEqual(0, (await backend.ListKeysAsync("")).Count);
    }

    [TestMethod]
    public async Task CompletedCanonicalFixturesHaveNoNativeRecordRows()
    {
        if (Environment.GetEnvironmentVariable("OASIS_DUCKDB_POSTRUN_INSPECTION") != "1") Assert.Inconclusive("Post-run inspection opt-in required.");
        var work = FixtureDirectory();
        foreach (var mode in new[] { "async", "sync" })
        {
            var report = System.Text.Json.JsonSerializer.Deserialize<CoreStorageConformance.Report>(await File.ReadAllTextAsync(Path.Combine(work, mode, "report.json")));
            Assert.IsTrue(report!.Succeeded && report.FixtureCleanupComplete);
            Assert.AreEqual(26, report.Passed.Count);
            await AssertNativeEmpty(Path.Combine(work, mode, "storage.duckdb"));
        }
    }

    [TestMethod]
    public async Task CompletedUpdateRegressionFixtureHasNoNativeRecordRows()
    {
        if (Environment.GetEnvironmentVariable("OASIS_DUCKDB_POSTRUN_INSPECTION") != "1") Assert.Inconclusive("Post-run inspection opt-in required.");
        var work = FixtureDirectory();
        await AssertNativeEmpty(Path.Combine(work, "update-regression.duckdb"));
    }

    private static string FixtureDirectory()
    {
        if (Environment.GetEnvironmentVariable("OASIS_DUCKDB_STORAGE_CONFORMANCE") != "1") Assert.Inconclusive("Native opt-in required.");
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.IsNotNull(root);
        return Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent", "DuckDBOASIS");
    }
    private static async Task AssertNativeEmpty(string path)
    {
        Assert.IsTrue(File.Exists(path), "Inspection must not initialize a missing database.");
        await using var connection = new DuckDB.NET.Data.DuckDBConnection("Data Source=" + path);
        await connection.OpenAsync(); await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM oasis_record_store";
        Assert.AreEqual(0L, Convert.ToInt64(await command.ExecuteScalarAsync()));
    }

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task ShippingDuckDbRunsSharedStorageSuite(bool synchronous)
    {
        if (Environment.GetEnvironmentVariable("OASIS_DUCKDB_STORAGE_CONFORMANCE") != "1")
            Assert.Inconclusive("Explicit isolated DuckDB fixture opt-in required.");
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts"))) root = root.Parent;
        Assert.IsNotNull(root);
        var work = Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent", "DuckDBOASIS", synchronous ? "sync" : "async");
        Directory.CreateDirectory(work);
        using var lease = new FileStream(Path.Combine(work, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var database = Path.Combine(work, "storage.duckdb");
        var report = Path.Combine(work, "report.json");
        Assert.IsFalse(File.Exists(database) || File.Exists(database + ".wal") || File.Exists(report) || File.Exists(report + ".next"),
            "Preserve old databases and checkpoints; no automatic reset or replay.");
        var checkpoint = new AtomicConformanceCheckpoint<CoreStorageConformance.Report>(report);
        var provider = new DuckDBOASIS("Data Source=" + database);
        var result = await CoreStorageConformance.RunAsync(provider, "DuckDBOASIS", "native", synchronous, checkpoint.WriteAsync,
            (id, sync) => sync ? Task.FromResult(provider.DeleteHolonSoft(id, softDelete: false)) : provider.DeleteHolonSoftAsync(id, softDelete: false), expectedDefaultDeletion: "soft");
        Assert.IsTrue(result.Succeeded, result.CurrentCase + ": " + result.FailureType + ": " + result.FailureContract);
        Assert.IsTrue(result.FixtureCleanupComplete);
    }
}
