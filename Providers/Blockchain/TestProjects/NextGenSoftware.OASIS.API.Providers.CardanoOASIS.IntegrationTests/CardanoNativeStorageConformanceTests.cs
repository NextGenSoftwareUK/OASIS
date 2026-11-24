using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.CardanoOASIS.IntegrationTests;

/// <summary>
/// Shared core storage suite against a disposable Yaci DevKit Cardano devnet (Blockfrost-compatible Yaci Store) and an
/// offline Kubo node. The fixture tops up a fresh Ed25519 storage address through the devnet admin API.
/// </summary>
[TestClass, DoNotParallelize]
public sealed class CardanoNativeStorageConformanceTests
{
    private static string Env(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException(name + " is required.");

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task CoreStorageSuiteAgainstCardanoDevnet(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_NATIVE_STORAGE_FIXTURE");
        if (string.IsNullOrWhiteSpace(fixture))
            Assert.Inconclusive("Requires the disposable Cardano devnet CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Env("RUNNER_TEMP")), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-native.");
        var api = Env("OASIS_CARDANO_API");
        Assert.IsTrue(new Uri(api).IsLoopback);

        var signingKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var address = CardanoOASIS.AddressForSigningKey(signingKey, "devnet");
        using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) })
        {
            using var topup = await http.PostAsJsonAsync(Env("OASIS_CARDANO_TOPUP_URL"), new { address, adaAmount = 10000 });
            Assert.IsTrue(topup.IsSuccessStatusCode, await topup.Content.ReadAsStringAsync());
            for (var attempt = 0; ; attempt++)
            {
                using var utxos = await http.GetAsync($"{api.TrimEnd('/')}/addresses/{address}/utxos");
                if (utxos.IsSuccessStatusCode && (await utxos.Content.ReadAsStringAsync()).Contains("lovelace")) break;
                Assert.IsTrue(attempt < 120, "The devnet did not fund the storage address.");
                await Task.Delay(500);
            }
        }

        var reportPath = Path.Combine(directory, synchronous ? "sync.json" : "async.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));
        var provider = new CardanoOASIS(api, null, "devnet", signingKey, Env("OASIS_CARDANO_KUBO"), RandomNumberGenerator.GetBytes(32));
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
