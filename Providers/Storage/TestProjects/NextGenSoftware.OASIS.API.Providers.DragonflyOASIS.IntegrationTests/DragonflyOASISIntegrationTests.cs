using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;
using StackExchange.Redis;

namespace NextGenSoftware.OASIS.API.Providers.DragonflyOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class DragonflyOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SharedStorageLifecycle(bool synchronous)
    {
        var root = Environment.GetEnvironmentVariable("OASIS_DRAGONFLY_LIFECYCLE_REPORT_ROOT");
        if (string.IsNullOrWhiteSpace(root))
            Assert.Inconclusive("Explicitly configure a dedicated Dragonfly instance and report directory.");

        var connection = Environment.GetEnvironmentVariable("DRAGONFLY_CONN");
        Assert.IsFalse(string.IsNullOrWhiteSpace(connection), "Configure the dedicated Dragonfly endpoint.");
        var options = ConfigurationOptions.Parse(connection!);
        Assert.AreEqual(1, options.EndPoints.Count, "Use one dedicated Dragonfly endpoint.");
        options.AllowAdmin = true;
        Directory.CreateDirectory(root!);
        using var lease = new FileStream(Path.Combine(root!, "environment.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var directory = Path.Combine(root!, synchronous ? "sync" : "async");
        Assert.IsFalse(Directory.Exists(directory), "Preserve prior fixture evidence.");

        using var native = await ConnectionMultiplexer.ConnectAsync(options);
        var info = await native.GetServer(options.EndPoints[0]).InfoAsync("server");
        Assert.IsTrue(info.SelectMany(group => group).Any(pair => pair.Key == "dragonfly_version" && !string.IsNullOrWhiteSpace(pair.Value)),
            "The endpoint must identify itself as Dragonfly.");

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

        var provider = new DragonflyOASIS(connection!);
        var result = await CoreStorageConformance.RunAsync(provider, nameof(DragonflyOASIS), "native", synchronous, Checkpoint,
            (id, sync) => sync ? Task.FromResult(provider.DeleteHolonSoft(id, false)) : provider.DeleteHolonSoftAsync(id, false),
            expectedDefaultDeletion: "soft");
        Assert.IsTrue(result.Succeeded, $"{result.CurrentCase}: {result.FailureType} {result.FailureContract}");
        Assert.IsTrue(result.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, result.Passed.Count);
    }
}
