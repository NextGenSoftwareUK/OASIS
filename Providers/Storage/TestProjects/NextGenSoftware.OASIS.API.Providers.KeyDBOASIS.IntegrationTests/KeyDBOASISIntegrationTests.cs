using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;
using StackExchange.Redis;

namespace NextGenSoftware.OASIS.API.Providers.KeyDBOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class KeyDBOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SharedStorageLifecycle(bool synchronous)
    {
        var root = Environment.GetEnvironmentVariable("OASIS_KEYDB_LIFECYCLE_REPORT_ROOT");
        if (string.IsNullOrWhiteSpace(root))
            Assert.Inconclusive("Explicitly configure a dedicated KeyDB instance and report directory.");

        var connection = Environment.GetEnvironmentVariable("KEYDB_CONN");
        Assert.IsFalse(string.IsNullOrWhiteSpace(connection), "Configure the dedicated KeyDB endpoint.");
        var options = ConfigurationOptions.Parse(connection!);
        Assert.AreEqual(1, options.EndPoints.Count, "Use one dedicated KeyDB endpoint.");
        options.AllowAdmin = true;
        Directory.CreateDirectory(root!);
        using var lease = new FileStream(Path.Combine(root!, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var directory = Path.Combine(root!, synchronous ? "sync" : "async");
        Assert.IsFalse(Directory.Exists(directory), "Preserve prior fixture evidence.");

        using var native = await ConnectionMultiplexer.ConnectAsync(options);
        var info = await native.GetServer(options.EndPoints[0]).InfoAsync("server");
        Assert.IsTrue(info.SelectMany(group => group).Any(pair => pair.Key == "executable" && pair.Value.EndsWith("/keydb-server", StringComparison.Ordinal)),
            "The endpoint must identify itself as KeyDB.");

        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "report.json");
        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            var pending = path + ".pending";
            await using (var stream = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, report);
                stream.Flush(flushToDisk: true);
            }
            File.Move(pending, path, overwrite: true);
        }

        var provider = new KeyDBOASIS(connection!);
        var result = await CoreStorageConformance.RunAsync(provider, nameof(KeyDBOASIS), "native", synchronous, Checkpoint,
            (id, sync) => sync ? Task.FromResult(provider.DeleteHolonSoft(id, false)) : provider.DeleteHolonSoftAsync(id, false),
            expectedDefaultDeletion: "soft");
        Assert.IsTrue(result.Succeeded, $"{result.CurrentCase}: {result.FailureType} {result.FailureContract}");
        Assert.IsTrue(result.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, result.Passed.Count);
    }
}
