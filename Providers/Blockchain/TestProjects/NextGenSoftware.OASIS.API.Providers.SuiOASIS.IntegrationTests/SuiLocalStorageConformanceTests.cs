using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class SuiLocalStorageConformanceTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task CoreStorageSuiteAgainstOfficialSui(bool synchronous)
    {
        var deployment = Environment.GetEnvironmentVariable("OASIS_SUI_CI_STORAGE_DEPLOYMENT");
        if (string.IsNullOrEmpty(deployment))
            Assert.Inconclusive("Requires the fresh Sui CI fixture.");

        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetDirectoryName(Path.GetFullPath(deployment!))!;
        StringAssert.EndsWith(directory, "/TestResults/SuiRuntime");
        using var evidence = JsonDocument.Parse(await File.ReadAllTextAsync(deployment!));
        var fixture = evidence.RootElement;
        Assert.AreEqual("ready", fixture.GetProperty("state").GetString());
        var encryptionKey = Environment.GetEnvironmentVariable("OASIS_SUI_STORAGE_ENCRYPTION_KEY")!;
        using var provider = new SuiOASIS("http://127.0.0.1:9000", "localnet",
            fixture.GetProperty("chainId").GetString()!, fixture.GetProperty("packageAddress").GetString()!,
            fixture.GetProperty("privateKey").GetString()!, fixture.GetProperty("storageObjectId").GetString()!,
            fixture.GetProperty("currencyObjectId").GetString()!, () => Convert.FromBase64String(encryptionKey));

        var path = Path.Combine(directory, synchronous ? "core-storage-sync.json" : "core-storage-async.json");
        Assert.IsFalse(File.Exists(path) || File.Exists(path + ".next"));
        var otherPath = Path.Combine(directory, synchronous ? "core-storage-async.json" : "core-storage-sync.json");
        Assert.IsFalse(File.Exists(otherPath + ".next"));
        if (File.Exists(otherPath))
        {
            var prior = JsonSerializer.Deserialize<CoreStorageConformance.Report>(await File.ReadAllTextAsync(otherPath));
            Assert.IsTrue(prior?.FixtureCleanupComplete == true);
        }
        var report = await CoreStorageConformance.RunAsync(provider, nameof(SuiOASIS), "native", synchronous,
            async checkpoint =>
            {
                await File.WriteAllTextAsync(path + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(path + ".next", path, overwrite: true);
            }, expectedDefaultDeletion: "absent");
        Assert.IsTrue(report.Succeeded,
            $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
    }
}
