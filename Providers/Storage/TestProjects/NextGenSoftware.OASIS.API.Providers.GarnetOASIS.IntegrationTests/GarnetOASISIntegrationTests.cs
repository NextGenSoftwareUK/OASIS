using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;
using StackExchange.Redis;

namespace NextGenSoftware.OASIS.API.Providers.GarnetOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class GarnetOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SharedStorageLifecycle(bool synchronous)
    {
        var root = Environment.GetEnvironmentVariable("OASIS_GARNET_LIFECYCLE_REPORT_ROOT");
        if (string.IsNullOrWhiteSpace(root))
            Assert.Inconclusive("Explicitly configure a dedicated Garnet instance and report directory.");

        var connection = Environment.GetEnvironmentVariable("GARNET_CONN");
        Assert.IsFalse(string.IsNullOrWhiteSpace(connection), "Configure the dedicated Garnet endpoint.");
        var options = ConfigurationOptions.Parse(connection!);
        Assert.AreEqual(1, options.EndPoints.Count, "Use one dedicated Garnet endpoint.");
        options.AllowAdmin = true;
        var directory = Path.Combine(root!, synchronous ? "sync" : "async");
        Assert.IsFalse(Directory.Exists(directory), "Preserve prior fixture evidence.");

        using var native = await ConnectionMultiplexer.ConnectAsync(options);
        var info = await native.GetServer(options.EndPoints[0]).InfoAsync("server");
        Assert.IsTrue(info.SelectMany(group => group).Any(pair => pair.Key == "garnet_version"),
            "The endpoint must identify itself as Garnet.");

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

        var provider = new GarnetOASIS(connection!);
        var result = await CoreStorageConformance.RunAsync(provider, nameof(GarnetOASIS), "native", synchronous, Checkpoint);
        Assert.IsTrue(result.Succeeded, $"{result.CurrentCase}: {result.FailureType} {result.FailureContract}");
        Assert.IsTrue(result.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, result.Passed.Count);
    }
}
