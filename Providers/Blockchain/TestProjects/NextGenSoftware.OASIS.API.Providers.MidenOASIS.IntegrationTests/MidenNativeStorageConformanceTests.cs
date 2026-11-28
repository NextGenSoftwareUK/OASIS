using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.MidenOASIS.IntegrationTests;

/// <summary>
/// Shared core storage suite against a disposable Miden local network (miden-node v0.18.3 compose: sequencer, validators, prover, funding service) and an
/// offline Kubo node. The fixture creates and funds the public storage account that carries the oasis::kv component.
/// </summary>
[TestClass, DoNotParallelize]
public sealed class MidenNativeStorageConformanceTests
{
    private static string Env(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException(name + " is required.");

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task CoreStorageSuiteAgainstMidenLocalNetwork(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_NATIVE_STORAGE_FIXTURE");
        if (string.IsNullOrWhiteSpace(fixture))
            Assert.Inconclusive("Requires the disposable Miden local network CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Env("RUNNER_TEMP")), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-native.");
        var reportPath = Path.Combine(directory, synchronous ? "sync.json" : "async.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        var rpc = Env("OASIS_MIDEN_RPC_URL");
        var kubo = Env("OASIS_MIDEN_KUBO");
        Assert.IsTrue(new Uri(rpc).IsLoopback && new Uri(kubo).IsLoopback, "The fixture endpoints must be local.");

        // The shipping BootLoader uses this constructor; storage settings come from the secret/environment store.
        using var provider = new MidenOASIS(network: "local");

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

    [TestMethod]
    public async Task SdkStoreReuseAndWorkerOwnershipOnLocalNetwork()
    {
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var fixture = Path.GetFullPath(Env("OASIS_NATIVE_STORAGE_FIXTURE"));
        Assert.AreEqual(Path.GetFullPath(Env("RUNNER_TEMP")), Path.GetDirectoryName(fixture));
        StringAssert.StartsWith(Path.GetFileName(fixture), "oasis-native.");
        Assert.IsTrue(new Uri(Env("OASIS_MIDEN_RPC_URL")).IsLoopback);
        Assert.IsTrue(new Uri(Env("OASIS_MIDEN_KUBO")).IsLoopback);
        var storeFile = Path.GetFullPath(Env("OASIS_MIDEN_SDK_STORE_FILE"));
        StringAssert.StartsWith(storeFile, fixture + Path.DirectorySeparatorChar);

        var holon = new NextGenSoftware.OASIS.API.Core.Holons.Holon
        {
            Id = Guid.NewGuid(),
            CreatedByAvatarId = Guid.NewGuid(),
            Name = "miden-owned-sdk-cache"
        };
        using var first = new MidenOASIS(network: "local");
        var activated = await first.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Message);
        Assert.IsTrue(File.Exists(storeFile), "The real SDK store must be inside the owned fixture.");
        var saved = await first.SaveHolonAsync(holon, saveChildren: false);
        Assert.IsTrue(saved.IsSaved && !saved.IsError, saved.Message);
        var version = saved.Result.Version;

        using (var crashed = System.Diagnostics.Process.GetProcessById(first.SdkProcessId
            ?? throw new InvalidOperationException("The real SDK worker was not started.")))
        {
            crashed.Kill(entireProcessTree: true);
            Assert.IsTrue(crashed.WaitForExit(10_000));
        }
        var interrupted = await first.LoadHolonAsync(holon.Id, loadChildren: false);
        Assert.IsTrue(interrupted.IsError, "A lost SDK session must fail explicitly.");
        // This is a new read operation. The failed operation is never replayed.
        var resumed = await first.LoadHolonAsync(holon.Id, loadChildren: false);
        Assert.IsFalse(resumed.IsError, resumed.Message);
        Assert.AreEqual(holon.Name, resumed.Result.Name);
        Assert.AreEqual(version, resumed.Result.Version);
        using (var owned = System.Diagnostics.Process.GetProcessById(first.SdkProcessId
            ?? throw new InvalidOperationException("The SDK session did not initialize again.")))
        {
            first.Dispose();
            Assert.IsTrue(owned.WaitForExit(10_000), "Disposing the provider must stop its worker.");
        }
        Assert.IsNull(first.SdkProcessId);
        Assert.IsTrue(File.Exists(storeFile), "The SDK journal is retained until the owned fixture is removed.");
        var homeStores = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".miden", "stores");
        Assert.IsFalse(Directory.Exists(homeStores) && Directory.GetDirectories(homeStores, "oasis-miden-*").Length != 0,
            "CI SDK stores must remain inside the disposable fixture, not the home directory.");

        using var second = new MidenOASIS(network: "local");
        activated = await second.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Message);
        var reopened = await second.LoadHolonAsync(holon.Id, loadChildren: false);
        Assert.IsFalse(reopened.IsError, reopened.Message);
        Assert.AreEqual(holon.Name, reopened.Result.Name);
        Assert.AreEqual(version, reopened.Result.Version);
        var deleted = await second.DeleteHolonAsync(holon.Id);
        Assert.IsTrue(deleted.IsDeleted && !deleted.IsError, deleted.Message);
        using (var owned = System.Diagnostics.Process.GetProcessById(second.SdkProcessId
            ?? throw new InvalidOperationException("The reopened SDK worker was not started.")))
        {
            second.Dispose();
            Assert.IsTrue(owned.WaitForExit(10_000));
        }
    }

}
