using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.StarknetOASIS.IntegrationTests;

/// <summary>
/// Shared core storage suite against a disposable starknet-devnet and an offline Kubo node. The workflow fixture
/// (SdkBridge/devnet-setup.mjs) deploys the OasisKv contract and a funded, deployed storage account.
/// </summary>
[TestClass, DoNotParallelize]
public sealed class StarknetNativeStorageConformanceTests
{
    private static string Env(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException(name + " is required.");

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task CoreStorageSuiteAgainstStarknetDevnet(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_NATIVE_STORAGE_FIXTURE");
        if (string.IsNullOrWhiteSpace(fixture))
            Assert.Inconclusive("Requires the disposable starknet-devnet CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Env("RUNNER_TEMP")), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-native.");
        var rpc = Env("OASIS_STARKNET_RPC");
        Assert.IsTrue(new Uri(rpc).IsLoopback);

        var reportPath = Path.Combine(directory, synchronous ? "sync.json" : "async.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));
        var provider = new StarknetOASIS(Env("OASIS_STARKNET_CHAIN_ID"), rpc, Env("OASIS_STARKNET_CONTRACT"), Env("OASIS_STARKNET_ACCOUNT_CLASS"),
            Env("OASIS_STARKNET_STORAGE_KEY"), Env("OASIS_STARKNET_KUBO"), RandomNumberGenerator.GetBytes(32));
        var report = await CoreStorageConformance.RunAsync(provider, provider.ProviderName, "native", synchronous,
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            });
        if (!report.Succeeded)
        {
            var probes = new (string, Func<Task<string>>)[]
            {
                ("activate", async () => { var r = await provider.ActivateProviderAsync(); return r.Message + " | " + r.Exception; }),
                ("avatar", async () => (await provider.LoadAvatarAsync(report.OwnerId)).Message),
                ("holon", async () => (await provider.LoadHolonAsync(report.RootId, loadChildren: false)).Message),
                ("all", async () => (await provider.LoadAllHolonsAsync(loadChildren: false)).Message),
            };
            foreach (var (name, probe) in probes)
            {
                try { Console.WriteLine($"Diagnose({name}): {await probe()}"); }
                catch (Exception error) { Console.WriteLine($"Diagnose({name}): {error.GetType().Name} {error.Message}"); }
            }
        }
        Assert.IsTrue(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
    }
}
