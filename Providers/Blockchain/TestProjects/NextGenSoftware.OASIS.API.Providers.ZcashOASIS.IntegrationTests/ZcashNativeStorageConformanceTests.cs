using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.ZcashOASIS.IntegrationTests;

/// <summary>
/// Shared core storage suite against a disposable zcashd regtest node (address index on, a background miner) and an
/// offline Kubo node. The fixture funds a wallet-held transparent storage address from mature coinbase.
/// </summary>
[TestClass, DoNotParallelize]
public sealed class ZcashNativeStorageConformanceTests
{
    private static string Env(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException(name + " is required.");

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task CoreStorageSuiteAgainstZcashRegtest(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_NATIVE_STORAGE_FIXTURE");
        if (string.IsNullOrWhiteSpace(fixture))
            Assert.Inconclusive("Requires the disposable zcashd regtest CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Env("RUNNER_TEMP")), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-native.");
        var reportPath = Path.Combine(directory, synchronous ? "sync.json" : "async.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        var rpc = Env("OASIS_ZCASH_RPC");
        var kubo = Env("OASIS_ZCASH_KUBO");
        Assert.IsTrue(new Uri(rpc).IsLoopback && new Uri(kubo).IsLoopback, "The fixture endpoints must be local.");

        var provider = new ZcashOASIS(rpc, Env("OASIS_ZCASH_RPC_USER"), Env("OASIS_ZCASH_RPC_PASSWORD"), "regtest",
            storageAddress: Env("OASIS_ZCASH_STORAGE_ADDRESS"), kuboApi: kubo,
            encryptionKey: Convert.FromBase64String(Env("OASIS_ZCASH_STORAGE_KEY")));

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
                ("activate", async () =>
                {
                    var activation = await provider.ActivateProviderAsync();
                    return $"error={activation.IsError} result={activation.Result} {activation.Message} {activation.Exception}";
                }),
                ("avatar", async () => (await provider.LoadAvatarAsync(report.OwnerId)).Message),
                ("detail", async () => (await provider.LoadAvatarDetailAsync(report.OwnerId)).Message),
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
