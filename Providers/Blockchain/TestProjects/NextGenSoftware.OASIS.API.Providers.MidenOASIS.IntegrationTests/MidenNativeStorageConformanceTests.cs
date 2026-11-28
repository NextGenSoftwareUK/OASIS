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
        var provider = new MidenOASIS(network: "local");

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
}
