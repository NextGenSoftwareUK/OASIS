using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;


namespace NextGenSoftware.OASIS.API.Providers.MilvusOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class MilvusNativeStorageConformanceTests
{
    [TestMethod]
    public async Task CoreStorageSuiteAgainstMilvus()
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_MILVUS_CI_FIXTURE");
        if (string.IsNullOrEmpty(fixture))
            Assert.Inconclusive("Requires the disposable Milvus CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-milvus.");
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(directory));
        var mode = Environment.GetEnvironmentVariable("OASIS_MILVUS_CI_MODE");
        Assert.IsTrue(mode is "sync" or "async");
        var synchronous = mode == "sync";
        var database = $"oasis_storage_{mode}";
        var provider = new MilvusOASIS("http://127.0.0.1:19531");
        var reportPath = Path.Combine(directory, $"{database}.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        var report = await CoreStorageConformance.RunAsync(provider, nameof(MilvusOASIS), "native", synchronous,
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
