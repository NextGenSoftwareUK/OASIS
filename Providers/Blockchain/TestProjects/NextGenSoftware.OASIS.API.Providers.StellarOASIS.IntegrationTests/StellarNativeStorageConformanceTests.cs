using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.StellarOASIS.IntegrationTests;

/// <summary>
/// Shared core storage suite against a disposable Stellar quickstart network (standalone) and an offline Kubo node.
/// The fixture funds a fresh Ed25519 storage account through the local friendbot.
/// </summary>
[TestClass, DoNotParallelize]
public sealed class StellarNativeStorageConformanceTests
{
    private const string StandalonePassphrase = "Standalone Network ; February 2017";
    private static string Env(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException(name + " is required.");

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task CoreStorageSuiteAgainstStellarStandalone(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_NATIVE_STORAGE_FIXTURE");
        if (string.IsNullOrWhiteSpace(fixture))
            Assert.Inconclusive("Requires the disposable Stellar standalone CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Env("RUNNER_TEMP")), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-native.");
        var horizon = Env("OASIS_STELLAR_HORIZON");
        Assert.IsTrue(new Uri(horizon).IsLoopback);

        var privateKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var account = StellarOASIS.AccountIdForPrivateKey(privateKey);
        using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) })
        {
            using var funded = await http.GetAsync($"{horizon.TrimEnd('/')}/friendbot?addr={account}");
            Assert.IsTrue(funded.IsSuccessStatusCode, await funded.Content.ReadAsStringAsync());
        }

        var reportPath = Path.Combine(directory, synchronous ? "sync.json" : "async.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));
        var provider = new StellarOASIS(horizon, StandalonePassphrase, privateKey, Env("OASIS_STELLAR_KUBO"), RandomNumberGenerator.GetBytes(32));
        Assert.AreEqual(account, provider.StorageAccountId);
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
