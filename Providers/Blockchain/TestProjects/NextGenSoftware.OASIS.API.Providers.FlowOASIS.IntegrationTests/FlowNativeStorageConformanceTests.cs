using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.FlowOASIS.IntegrationTests;

/// <summary>
/// Shared core storage suite against a disposable local Flow emulator. The fixture deploys OasisKv to the emulator service
/// account, which then holds the contract's Admin resource and signs every write; each mode gets a fresh encryption key.
/// </summary>
[TestClass, DoNotParallelize]
public sealed class FlowNativeStorageConformanceTests
{
    private static string Env(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException(name + " is required.");

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task CoreStorageSuiteAgainstFlowEmulator(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_NATIVE_STORAGE_FIXTURE");
        if (string.IsNullOrWhiteSpace(fixture))
            Assert.Inconclusive("Requires the disposable Flow emulator CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Env("RUNNER_TEMP")), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-native.");
        var accessNode = Env("OASIS_FLOW_ACCESS_NODE");
        Assert.IsTrue(new Uri(accessNode).IsLoopback);

        var reportPath = Path.Combine(directory, synchronous ? "sync.json" : "async.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));
        var account = Env("OASIS_FLOW_SERVICE_ADDRESS");
        var provider = new FlowOASIS(accessNode, account, account, Env("OASIS_FLOW_SERVICE_KEY"), RandomNumberGenerator.GetBytes(32));
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
