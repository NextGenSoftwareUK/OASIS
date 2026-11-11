using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.DragonflyOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class DragonflyCoreStorageConformanceTests
{
    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task SharedStorageAgainstDragonfly(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_DRAGONFLY_CI_FIXTURE");
        if (string.IsNullOrWhiteSpace(fixture))
            Assert.Inconclusive("Requires the disposable native Dragonfly CI fixture.");

        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var path = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(path));
        StringAssert.StartsWith(Path.GetFileName(path), "oasis-dragonfly.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(DragonflyOASIS.StorageKeyEnvironmentVariable)));

        var reportPath = Path.Combine(path, synchronous ? "sync.json" : "async.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));
        using var provider = new DragonflyOASIS("127.0.0.1:19691,abortConnect=true,connectRetry=0");
        Assert.AreEqual("DragonflyOASIS", provider.ProviderName);
        var result = await CoreStorageConformance.RunAsync(provider, provider.ProviderName, "native", synchronous,
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            },
            (id, sync) => sync ? Task.FromResult(provider.DeleteHolonSoft(id, false)) : provider.DeleteHolonSoftAsync(id, false),
            expectedDefaultDeletion: "soft");

        Assert.IsTrue(result.Succeeded, $"{result.CurrentCase}: {result.FailureType} {result.FailureContract}");
        Assert.IsTrue(result.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, result.Passed.Count);
    }
}
