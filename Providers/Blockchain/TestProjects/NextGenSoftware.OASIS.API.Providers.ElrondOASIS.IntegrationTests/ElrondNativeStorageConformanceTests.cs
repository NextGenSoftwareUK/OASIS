using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.ElrondOASIS.IntegrationTests;

/// <summary>
/// Shared core storage suite against a disposable MultiversX chain simulator and an offline Kubo node. The fixture
/// funds a fresh Ed25519 storage account through the simulator's set-state endpoint; the workflow generates blocks.
/// </summary>
[TestClass, DoNotParallelize]
public sealed class ElrondNativeStorageConformanceTests
{
    private static string Env(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException(name + " is required.");

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task CoreStorageSuiteAgainstMultiversXChainSimulator(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_NATIVE_STORAGE_FIXTURE");
        if (string.IsNullOrWhiteSpace(fixture))
            Assert.Inconclusive("Requires the disposable MultiversX chain simulator CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Env("RUNNER_TEMP")), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-native.");
        var proxy = Env("OASIS_MULTIVERSX_PROXY");
        Assert.IsTrue(new Uri(proxy).IsLoopback);

        var secretKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var address = ElrondOASIS.AddressForSecretKey(secretKey);
        using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) })
        {
            var state = JsonSerializer.Serialize(new[] { new { address, balance = "1000000000000000000000", nonce = 0 } });
            using var funded = await http.PostAsync($"{proxy.TrimEnd('/')}/simulator/set-state", new StringContent(state, Encoding.UTF8, "application/json"));
            Assert.IsTrue(funded.IsSuccessStatusCode, await funded.Content.ReadAsStringAsync());
        }

        var reportPath = Path.Combine(directory, synchronous ? "sync.json" : "async.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));
        var provider = new ElrondOASIS(proxy, Env("OASIS_MULTIVERSX_CHAIN_ID"), secretKey, Env("OASIS_MULTIVERSX_KUBO"), RandomNumberGenerator.GetBytes(32));
        var report = await CoreStorageConformance.RunAsync(provider, provider.ProviderName, "native", synchronous,
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            });
        if (!report.Succeeded)
        {
            var activation = await provider.ActivateProviderAsync();
            Console.WriteLine($"Diagnose(activate): {activation.Message} | {activation.Exception}");
            var holon = await provider.LoadHolonAsync(report.RootId, loadChildren: false);
            Console.WriteLine($"Diagnose(holon): {holon.Message} | {holon.Exception}");
        }
        Assert.IsTrue(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
    }
}
