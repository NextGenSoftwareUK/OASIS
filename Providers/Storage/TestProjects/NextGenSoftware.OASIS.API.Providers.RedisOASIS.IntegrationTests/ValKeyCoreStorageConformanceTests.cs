using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Provider = NextGenSoftware.OASIS.API.Providers.ValKeyOASIS.ValKeyOASIS;

namespace NextGenSoftware.OASIS.API.Providers.RedisOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public sealed class ValKeyCoreStorageConformanceTests
{
    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task SharedStorageAgainstValKey(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_VALKEY_CI_FIXTURE");
        if (string.IsNullOrWhiteSpace(fixture))
            Assert.Inconclusive("Requires the disposable native Valkey CI fixture.");

        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var path = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(path));
        StringAssert.StartsWith(Path.GetFileName(path), "oasis-valkey.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(Provider.StorageKeyEnvironmentVariable)));

        var reportPath = Path.Combine(path, synchronous ? "sync.json" : "async.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));
        using var provider = new Provider("127.0.0.1:19679,abortConnect=true,connectRetry=0");
        Assert.AreEqual("ValKeyOASIS", provider.ProviderName);
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
