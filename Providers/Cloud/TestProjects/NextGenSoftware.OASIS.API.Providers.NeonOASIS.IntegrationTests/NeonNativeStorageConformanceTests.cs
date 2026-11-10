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
            host.EndsWith(".neon.tech", StringComparison.OrdinalIgnoreCase),
            "Native conformance requires a Neon endpoint.");
        Assert.AreEqual("oasis_provider_test", settings.Database,
            "Native conformance requires the dedicated test database.");

        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "Scripts", "TestHosts")))
            root = root.Parent;
        Assert.IsNotNull(root, "Run this test from the repository.");

        var directory = Path.Combine(root.FullName, "TestResults", "ProviderConformanceCurrent",
            nameof(NeonOASIS), sync ? "sync" : "async");
        Directory.CreateDirectory(directory);
        using var lease = new FileStream(Path.Combine(directory, "environment.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

        var reportPath = Path.Combine(directory, "report.json");
        var pendingPath = reportPath + ".next";
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(pendingPath),
            "Preserve the previous native report and resolve its fixture before running again.");

        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            await using (var file = new FileStream(pendingPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(file, report);
                file.Flush(flushToDisk: true);
            }
            File.Move(pendingPath, reportPath, overwrite: true);
        }

        var provider = new NeonOASIS(connection!);
        var result = await CoreStorageConformance.RunAsync(
            provider, nameof(NeonOASIS), "native", sync, Checkpoint, expectedDefaultDeletion: "absent");

        Assert.IsTrue(result.Succeeded, $"{result.CurrentCase}: {result.FailureType}");
        Assert.IsTrue(result.FixtureCleanupComplete);
        Assert.AreEqual(26, result.Passed.Count);
    }
}
