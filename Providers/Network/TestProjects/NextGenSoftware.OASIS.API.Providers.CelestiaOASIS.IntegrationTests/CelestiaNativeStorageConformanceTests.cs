using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.CelestiaOASIS.IntegrationTests;

/// <summary>Runs against the disposable devnet from fixture/start-celestia.sh (celestia-app + celestia-node bridge).</summary>
[TestClass, DoNotParallelize]
public sealed class CelestiaNativeStorageConformanceTests
{
    private static string Env(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException(name + " is required.");

    private static string Fixture()
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_NATIVE_STORAGE_FIXTURE");
        if (string.IsNullOrWhiteSpace(fixture))
            Assert.Inconclusive("Requires the disposable native Celestia devnet fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Env("RUNNER_TEMP")), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-native.");
        return directory;
    }

    // One namespace per mode ("oasis-sync", "oasis-asyn", "oasis-rest" as 10-byte IDs); the devnet starts empty at height 1.
    private static CelestiaOASIS Provider(string namespaceId, byte[] key)
        => new(Env("OASIS_CELESTIA_URL"), Env("OASIS_CELESTIA_TOKEN"), Convert.ToHexString(System.Text.Encoding.ASCII.GetBytes(namespaceId)), 1, key);

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task CoreStorageSuiteAgainstCelestia(bool synchronous)
    {
        var reportPath = Path.Combine(Fixture(), synchronous ? "sync.json" : "async.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        using var provider = Provider(synchronous ? "oasis-sync" : "oasis-asyn", RandomNumberGenerator.GetBytes(32));
        var report = await CoreStorageConformance.RunAsync(provider, provider.ProviderName, "native", synchronous,
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            });

        Assert.IsTrue(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
    }

    /// <summary>The key index lives only in memory; a new provider instance must rebuild it from the namespace on Celestia.</summary>
    [TestMethod]
    public async Task RestartedProviderRecoversRecordsAndDeletesFromCelestia()
    {
        Fixture();
        var key = RandomNumberGenerator.GetBytes(32);
        var holon = new Holon { Id = Guid.NewGuid(), Name = "celestia-restart-" + Guid.NewGuid().ToString("N") };
        var updatedName = holon.Name + "-updated";

        using (var writer = Provider("oasis-rest", key))
        {
            Assert.IsFalse((await writer.ActivateProviderAsync()).IsError);
            var saved = await writer.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsFalse(saved.IsError, saved.Message);
            holon.Name = updatedName;
            saved = await writer.SaveHolonAsync(holon, saveChildren: false);
            Assert.IsFalse(saved.IsError, saved.Message);
            await writer.DeActivateProviderAsync();
        }

        using (var wrongKey = Provider("oasis-rest", RandomNumberGenerator.GetBytes(32)))
            Assert.IsTrue((await wrongKey.ActivateProviderAsync()).IsError, "Records sealed with another key must not activate silently.");

        using (var restarted = Provider("oasis-rest", key))
        {
            Assert.IsFalse((await restarted.ActivateProviderAsync()).IsError);
            var loaded = await restarted.LoadHolonAsync(holon.Id, loadChildren: false);
            Assert.IsFalse(loaded.IsError, loaded.Message);
            Assert.AreEqual(updatedName, loaded.Result!.Name, "The restarted provider must see the newest record version.");
            var deleted = await restarted.DeleteHolonAsync(holon.Id);
            Assert.IsFalse(deleted.IsError, deleted.Message);
            await restarted.DeActivateProviderAsync();
        }

        using (var again = Provider("oasis-rest", key))
        {
            Assert.IsFalse((await again.ActivateProviderAsync()).IsError);
            var loaded = await again.LoadHolonAsync(holon.Id, loadChildren: false);
            Assert.IsTrue(loaded.IsError || loaded.Result == null, "A tombstoned record must stay deleted after a restart.");
            await again.DeActivateProviderAsync();
        }
    }
}
