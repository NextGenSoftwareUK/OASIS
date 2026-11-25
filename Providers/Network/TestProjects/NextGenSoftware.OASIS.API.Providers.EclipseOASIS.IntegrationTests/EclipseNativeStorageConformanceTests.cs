using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Solnet.Wallet.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.EclipseOASIS.IntegrationTests;

/// <summary>
/// Shared native core-storage suite against a disposable local SVM ledger (Agave solana-test-validator),
/// which serves the same JSON-RPC and runtime that Eclipse executes. Each mode signs with its own funded
/// disposable account, so the two runs never share on-chain record history. No public endpoint is accepted.
/// </summary>
[TestClass, DoNotParallelize]
public sealed class EclipseNativeStorageConformanceTests
{
    private static string Env(string name) => Environment.GetEnvironmentVariable(name) is string value && !string.IsNullOrWhiteSpace(value)
        ? value : throw new InvalidOperationException(name + " is required.");

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task CoreStorageSuiteAgainstLocalSvmLedger(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_NATIVE_STORAGE_FIXTURE");
        if (string.IsNullOrWhiteSpace(fixture))
            Assert.Inconclusive("Requires the disposable native SVM ledger fixture.");
        var directory = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Env("RUNNER_TEMP")), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-native.");
        var mode = synchronous ? "sync" : "async";
        var reportPath = Path.Combine(directory, mode + ".json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"), "Preserve the prior report.");

        var endpoint = new Uri(Env("OASIS_ECLIPSE_RPC_URL"), UriKind.Absolute);
        Assert.IsTrue(endpoint.IsLoopback, "Local SVM ledger required; no public-chain endpoint.");
        await RequireLedgerIdentity(endpoint, Env("OASIS_ECLIPSE_GENESIS"));
        var key = PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_SOLANA_STORAGE_ENCRYPTION_KEY");
        CryptographicOperations.ZeroMemory(key);

        var provider = await OpenSigner(endpoint, Path.Combine(directory, mode + "-signer.json"));
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

    private static async Task RequireLedgerIdentity(Uri endpoint, string genesis)
    {
        using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(5) };
        using var request = new StringContent("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"getGenesisHash\"}", Encoding.UTF8, "application/json");
        using var response = await http.PostAsync(endpoint, request);
        response.EnsureSuccessStatusCode();
        using var identity = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        Assert.AreEqual(genesis, identity.RootElement.GetProperty("result").GetString(), "Fresh ledger identity differs.");
    }

    private static async Task<EclipseOASIS> OpenSigner(Uri endpoint, string keypairPath)
    {
        int[]? keypair;
        await using (var file = File.OpenRead(keypairPath))
            keypair = await JsonSerializer.DeserializeAsync<int[]>(file);
        Assert.IsNotNull(keypair);
        Assert.AreEqual(64, keypair.Length);
        var secret = Array.ConvertAll(keypair, value => checked((byte)value));
        try
        {
            return new EclipseOASIS(endpoint.ToString(), Encoders.Base58.EncodeData(secret), Encoders.Base58.EncodeData(secret[32..]));
        }
        finally
        {
            Array.Clear(keypair);
            CryptographicOperations.ZeroMemory(secret);
        }
    }
}
