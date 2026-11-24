using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS.IntegrationTests;

/// <summary>
/// Shared core storage suite against a disposable Bitcoin Core regtest node, Esplora and an offline Kubo node.
/// Publications are read from the mempool; the fixture mines once after each mode.
/// </summary>
[TestClass, DoNotParallelize]
public sealed class BitcoinNativeStorageConformanceTests
{
    private static string Env(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException(name + " is required.");

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task CoreStorageSuiteAgainstBitcoinRegtest(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_NATIVE_STORAGE_FIXTURE");
        if (string.IsNullOrWhiteSpace(fixture))
            Assert.Inconclusive("Requires the disposable Bitcoin regtest CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Env("RUNNER_TEMP")), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-native.");
        var api = Env("OASIS_BITCOIN_REGTEST_API");
        var container = Env("OASIS_BITCOIN_REGTEST_CONTAINER");
        Assert.IsTrue(Uri.TryCreate(api, UriKind.Absolute, out var uri) && uri.IsLoopback);
        _ = Env("OASIS_BITCOIN_KUBO_API");
        _ = Env("OASIS_BITCOIN_STORAGE_KEY");

        var network = NBitcoin.Network.RegTest;
        using var key = new NBitcoin.Key();
        var address = key.PubKey.GetAddress(NBitcoin.ScriptPubKeyType.Segwit, network).ToString();
        var minerAddress = (await BitcoinCliAsync(container, "-rpcwallet=oasis-regtest", "getnewaddress")).Trim();
        for (var output = 0; output < 40; output++)
            await BitcoinCliAsync(container, "-rpcwallet=oasis-regtest", "-named", "sendtoaddress", $"address={address}", "amount=0.5", "fee_rate=2");
        await BitcoinCliAsync(container, "generatetoaddress", "1", minerAddress);
        using (var esplora = new HttpClient { BaseAddress = new Uri(api.TrimEnd('/') + "/") })
        {
            // The suite must start from a wallet whose funding Esplora has fully indexed.
            for (var attempt = 0; ; attempt++)
            {
                using var utxos = JsonDocument.Parse(await esplora.GetStringAsync($"address/{address}/utxo"));
                if (utxos.RootElement.EnumerateArray().Count(utxo => utxo.GetProperty("status").GetProperty("confirmed").GetBoolean()) == 40) break;
                Assert.IsTrue(attempt < 120, "Esplora did not index the funded regtest wallet.");
                await Task.Delay(500);
            }
        }

        // No background mining: the provider reads unconfirmed publications from the mempool, and its wallet scan
        // correctly rejects a tip change mid-read. One suite mode publishes far fewer than Esplora's 50-entry mempool page.

        var reportPath = Path.Combine(directory, synchronous ? "sync.json" : "async.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));
        using var provider = new BitcoinOASIS(api, "regtest", key.GetWif(network).ToString(), null, 2m);
        CoreStorageConformance.Report report;
        try
        {
            report = await CoreStorageConformance.RunAsync(provider, provider.ProviderName, "native", synchronous,
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
                    ("detail", async () => (await provider.LoadAvatarDetailAsync(report.OwnerId)).Message),
                    ("holon", async () => (await provider.LoadHolonAsync(report.RootId, loadChildren: false)).Message),
                    ("all", async () => (await provider.LoadAllHolonsAsync(loadChildren: false)).Message),
                    ("delete-child", async () => { var d = await provider.DeleteHolonAsync(report.ChildId); return $"{d.IsError} {d.IsDeleted} {d.DeletedCount} {d.ErrorCode} {d.Message} {d.Exception?.GetType().Name}"; }),
                };
                foreach (var (name, probe) in probes)
                {
                    try { Console.WriteLine($"Diagnose({name}): {await probe()}"); }
                    catch (Exception error) { Console.WriteLine($"Diagnose({name}): {error.GetType().Name} {error.Message}"); }
                }
            }
        }
        finally
        {
            await BitcoinCliAsync(container, "generatetoaddress", "1", minerAddress);
        }

        Assert.IsTrue(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
    }

    private static async Task<string> BitcoinCliAsync(string container, params string[] arguments)
    {
        var start = new System.Diagnostics.ProcessStartInfo("docker") { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { "exec", container, "/srv/explorer/bitcoin/bin/bitcoin-cli",
            "-conf=/data/.bitcoin.conf", "-datadir=/data/bitcoin", "-regtest" }.Concat(arguments))
            start.ArgumentList.Add(argument);
        using var process = System.Diagnostics.Process.Start(start)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        Assert.AreEqual(0, process.ExitCode, await error);
        return await output;
    }
}
