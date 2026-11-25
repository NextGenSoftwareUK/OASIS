using System.Numerics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.FilecoinOASIS.IntegrationTests;

/// <summary>
/// Shared core storage suite against a disposable local Lotus devnet (FEVM, chain 31415926)
/// started by Fixture/start-lotus-devnet.sh. Records are written by signed FEVM transactions
/// to a freshly deployed Web3CoreOASIS contract per mode; never a public network or real FIL.
/// </summary>
[TestClass, DoNotParallelize]
public sealed class FilecoinNativeStorageConformanceTests
{
    private static readonly BigInteger LotusDevnetChainId = new(31415926);

    private static string Env(string name) => Environment.GetEnvironmentVariable(name) is string value && !string.IsNullOrWhiteSpace(value)
        ? value : throw new InvalidOperationException(name + " is required.");

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task CoreStorageSuiteAgainstFilecoin(bool synchronous)
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_NATIVE_STORAGE_FIXTURE");
        if (string.IsNullOrWhiteSpace(fixture))
            Assert.Inconclusive("Requires the disposable native Filecoin (Lotus devnet) CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Env("RUNNER_TEMP")), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-native.");
        var reportPath = Path.Combine(directory, synchronous ? "sync.json" : "async.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        var endpoint = new Uri(Env("OASIS_FILECOIN_FEVM_RPC"));
        Assert.IsTrue(endpoint.IsLoopback && endpoint.Scheme == "http" && endpoint.UserInfo.Length == 0,
            "The native Filecoin binding only targets the loopback Lotus devnet.");
        var chainId = BigInteger.Parse(Env("OASIS_FILECOIN_FEVM_CHAIN_ID"));
        Assert.AreEqual(LotusDevnetChainId, chainId, "Only the Lotus local devnet chain identity is accepted.");
        _ = Env("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY");

        var provider = new FilecoinOASIS(endpoint.AbsoluteUri, Env("OASIS_FILECOIN_FEVM_PRIVATE_KEY"),
            Env("OASIS_FILECOIN_FEVM_CONTRACT_" + (synchronous ? "SYNC" : "ASYNC")), chainId);
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
