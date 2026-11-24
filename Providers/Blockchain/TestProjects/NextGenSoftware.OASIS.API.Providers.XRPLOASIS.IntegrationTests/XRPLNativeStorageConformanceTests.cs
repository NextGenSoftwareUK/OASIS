using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.XRPLOASIS.IntegrationTests;

/// <summary>
/// Shared core storage suite against a disposable rippled in standalone mode and an offline Kubo node. The fixture
/// funds a fresh Ed25519 storage account from the standalone genesis account and closes ledgers in the background.
/// </summary>
[TestClass, DoNotParallelize]
public sealed class XRPLNativeStorageConformanceTests
{
    // Well-known standalone-mode genesis account (never valid on any public network).
    private const string GenesisSecret = "snoPBrXtMeMyMHUVTgbuqAfg1SUTb";
    private static string Env(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException(name + " is required.");

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task CoreStorageSuiteAgainstXrplStandalone(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_NATIVE_STORAGE_FIXTURE");
        if (string.IsNullOrWhiteSpace(fixture))
            Assert.Inconclusive("Requires the disposable XRPL standalone CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Env("RUNNER_TEMP")), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-native.");
        var rpc = Env("OASIS_XRPL_RPC");
        Assert.IsTrue(new Uri(rpc).IsLoopback);

        using var http = new HttpClient { BaseAddress = new Uri(rpc) };
        var privateKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var address = XRPLOASIS.AddressForPrivateKey(privateKey);
        var funded = await RpcAsync(http, "submit", new JsonObject
        {
            ["secret"] = GenesisSecret,
            ["tx_json"] = new JsonObject { ["TransactionType"] = "Payment", ["Account"] = "rHb9CJAWyB4rj91VRWn96DkukG4bwdtyTh", ["Destination"] = address, ["Amount"] = "1000000000" }
        });
        Assert.AreEqual("tesSUCCESS", funded.GetProperty("engine_result").GetString(), funded.GetRawText());
        await RpcAsync(http, "ledger_accept", new JsonObject());

        using var closer = new CancellationTokenSource();
        var closing = Task.Run(async () =>
        {
            while (!closer.IsCancellationRequested)
            {
                await RpcAsync(http, "ledger_accept", new JsonObject());
                try { await Task.Delay(400, closer.Token); } catch (OperationCanceledException) { }
            }
        });

        var reportPath = Path.Combine(directory, synchronous ? "sync.json" : "async.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));
        var provider = new XRPLOASIS(rpc, privateKey, Env("OASIS_XRPL_KUBO"), RandomNumberGenerator.GetBytes(32));
        Assert.AreEqual(address, provider.StorageAddress);
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
                    ("holon", async () => (await provider.LoadHolonAsync(report.RootId, loadChildren: false)).Message),
                    ("all", async () => (await provider.LoadAllHolonsAsync(loadChildren: false)).Message),
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
            closer.Cancel();
            await closing;
        }

        Assert.IsTrue(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
    }

    private static async Task<JsonElement> RpcAsync(HttpClient http, string method, JsonObject parameters)
    {
        var body = new JsonObject { ["method"] = method, ["params"] = new JsonArray(parameters) }.ToJsonString();
        using var response = await http.PostAsync("", new StringContent(body, Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("result").Clone();
    }
}
