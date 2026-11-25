using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.TablelandOASIS.IntegrationTests;

/// <summary>
/// Runs the shared core storage suite against a disposable local Tableland network started by fixture/start-tableland.sh
/// (Hardhat chain 31337 + validator). Each mode uses its own funded Hardhat development wallet and its own new table.
/// </summary>
[TestClass, DoNotParallelize]
public sealed class TablelandNativeStorageConformanceTests
{
    private const long LocalChainId = 31337;
    private const string LocalRegistry = "0xe7f1725E7734CE288F8367e1Bb143E90bb3F0512";
    // Hardhat's publicly documented development accounts #1 and #2; they hold value only on the local chain,
    // and the provider refuses any RPC that does not report chain 31337.
    private const string AsyncWalletKey = "0x59c6995e998f97a5a0044966f0945389dc9e86dae88c7a8412f4603b6b78690d";
    private const string SyncWalletKey = "0x5de4111afa1a4b94908f83103eb1f1706367c2e68ca870fc3fb9a804cdab365a";

    private static string Env(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException(name + " is required.");

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task CoreStorageSuiteAgainstTableland(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_NATIVE_STORAGE_FIXTURE");
        if (string.IsNullOrWhiteSpace(fixture))
            Assert.Inconclusive("Requires the disposable local Tableland CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Env("RUNNER_TEMP")), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-native.");
        var reportPath = Path.Combine(directory, synchronous ? "sync.json" : "async.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        var connection = new TablelandConnection(new Uri("http://127.0.0.1:8545"), new Uri("http://127.0.0.1:8080/"),
            LocalRegistry, LocalChainId, synchronous ? SyncWalletKey : AsyncWalletKey);
        var table = await TablelandKeyValueBackend.CreateTableAsync(connection, synchronous ? "oasis_sync" : "oasis_async");
        var provider = new TablelandOASIS(connection, table, RandomNumberGenerator.GetBytes(32));
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
