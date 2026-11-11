using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;


namespace NextGenSoftware.OASIS.API.Providers.MemcachedOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class MemcachedNativeStorageConformanceTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task CoreStorageSuiteAgainstMemcached(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_MEMCACHED_CI_FIXTURE");
        if (string.IsNullOrEmpty(fixture))
            Assert.Inconclusive("Requires the disposable Memcached CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-memcached.");
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(directory));
        var database = synchronous ? "oasis_storage_sync" : "oasis_storage_async";
        var provider = new MemcachedOASIS("127.0.0.1", synchronous ? 11232 : 11231);
        var reportPath = Path.Combine(directory, $"{database}.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        var report = await CoreStorageConformance.RunAsync(provider, nameof(MemcachedOASIS), "native", synchronous,
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            });
        Assert.IsTrue(report.Succeeded,
            $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
    }
}
