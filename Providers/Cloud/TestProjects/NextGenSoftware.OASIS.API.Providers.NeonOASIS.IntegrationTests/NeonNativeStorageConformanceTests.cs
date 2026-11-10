using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Npgsql;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.NeonOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class NeonNativeStorageConformanceTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task CoreStorageSuiteAgainstNeon(bool sync)
    {
        var connection = Environment.GetEnvironmentVariable("OASIS_NEON_CONFORMANCE_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connection))
            Assert.Inconclusive("Set OASIS_NEON_CONFORMANCE_CONNECTION_STRING to a dedicated Neon test database.");

        var settings = new NpgsqlConnectionStringBuilder(connection);
        Assert.IsTrue(settings.Host is { } host && !host.Contains(',') &&
            host.EndsWith(".neon.tech", StringComparison.OrdinalIgnoreCase) &&
            host[..host.IndexOf('.')].EndsWith("-pooler", StringComparison.OrdinalIgnoreCase),
            "Native conformance requires the explicit Neon pooled endpoint so inspection and provider writes use the same host.");
        Assert.AreEqual("oasis_provider_test", settings.Database,
            "Native conformance requires the dedicated test database.");

        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts")))
            root = root.Parent;
        Assert.IsNotNull(root, "Run this test from the repository.");

        var directory = Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent",
            nameof(NeonOASIS), sync ? "sync" : "async");
        Directory.CreateDirectory(directory);
        using var lease = new FileStream(Path.Combine(Path.GetDirectoryName(directory)!, "environment.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        var reportPath = Path.Combine(directory, "report.json");
        var pendingPath = reportPath + ".next";
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(pendingPath),
            "Preserve the previous native report and resolve its fixture before running again.");

        var otherReportPath = Path.Combine(Path.GetDirectoryName(directory)!, sync ? "async" : "sync", "report.json");
        Assert.IsFalse(File.Exists(otherReportPath + ".next"), "Reconcile the other mode's interrupted checkpoint first.");
        if (File.Exists(otherReportPath))
        {
            await using var prior = File.OpenRead(otherReportPath);
            var priorReport = await JsonSerializer.DeserializeAsync<CoreStorageConformance.Report>(prior);
            Assert.IsTrue(priorReport?.FixtureCleanupComplete == true,
                "Reconcile the other mode's fixture before connecting to Neon.");
        }

        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            await using (var file = new FileStream(pendingPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(file, report);
                file.Flush(flushToDisk: true);
            }
            File.Move(pendingPath, reportPath, overwrite: true);
        }

        settings.SslMode = SslMode.VerifyFull;
        await using var inspection = new NpgsqlConnection(settings.ConnectionString);
        await inspection.OpenAsync();

        async Task RequireEmptyTables(bool requireSchema)
        {
            foreach (var table in new[] { "oasis_avatars", "oasis_avatar_details", "oasis_holons" })
            {
                await using var exists = new NpgsqlCommand("SELECT to_regclass($1) IS NOT NULL", inspection);
                exists.Parameters.AddWithValue(table);
                if ((bool)(await exists.ExecuteScalarAsync())! == false)
                {
                    Assert.IsFalse(requireSchema, $"Missing native table {table} after activation.");
                    continue;
                }

                await using var count = new NpgsqlCommand($"SELECT EXISTS (SELECT 1 FROM {table})", inspection);
                Assert.AreEqual(false, await count.ExecuteScalarAsync(), $"Expected an empty native table {table}.");
            }
        }

        await RequireEmptyTables(requireSchema: false);
        var provider = new NeonOASIS(connection!);
        var result = await CoreStorageConformance.RunAsync(
            provider, nameof(NeonOASIS), "native", sync, report => report.Succeeded ? Task.CompletedTask : Checkpoint(report),
            expectedDefaultDeletion: "absent");

        Assert.IsTrue(result.Succeeded, $"{result.CurrentCase}: {result.FailureType}");
        Assert.IsTrue(result.FixtureCleanupComplete);
        try
        {
            await RequireEmptyTables(requireSchema: true);
        }
        catch (Exception error)
        {
            result.CurrentCase = "native-cleanup-verification";
            result.FailureType = error.GetType().FullName ?? error.GetType().Name;
            result.Succeeded = false;
            result.FixtureCleanupComplete = false;
            await Checkpoint(result);
            throw;
        }
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, result.Passed.Count);
        await Checkpoint(result);
    }
}
