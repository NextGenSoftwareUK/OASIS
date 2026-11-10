using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class NearSharedLifecycleTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SharedStorageLifecycle(bool synchronous)
    {
        var root = Environment.GetEnvironmentVariable("OASIS_NEAR_LIFECYCLE_REPORT_ROOT");
        if (string.IsNullOrWhiteSpace(root))
            Assert.Inconclusive("Explicitly admit a dedicated writable NEAR fixture and report directory.");

        var provider = new NEAROASIS(
            Required("NEAROASIS_RPCENDPOINT"), Required("NEAROASIS_NETWORKID"),
            Required("NEAROASIS_CHAINID"), Required("NEAROASIS_CONTRACTADDRESS"),
            Required("NEAROASIS_ACCOUNTID"), Required("NEAROASIS_PRIVATEKEY"));
        var directory = Path.Combine(root!, synchronous ? "sync" : "async");
        Assert.IsFalse(Directory.Exists(directory), "Preserve the existing fixture evidence.");
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

        var result = await CoreStorageConformance.RunAsync(provider, nameof(NEAROASIS), "native", synchronous, Checkpoint);
        Assert.IsTrue(result.Succeeded, $"{result.CurrentCase}: {result.FailureType} {result.FailureContract}");
        Assert.IsTrue(result.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, result.Passed.Count);
    }

    private static string Required(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException($"Missing {name}.");
    }
}
