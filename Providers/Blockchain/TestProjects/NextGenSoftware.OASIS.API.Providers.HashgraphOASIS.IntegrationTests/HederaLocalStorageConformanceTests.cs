using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public sealed class HederaLocalStorageConformanceTests
{
    [TestMethod]
    public async Task CoreStorageSuiteAgainstHieroConsensus()
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_HEDERA_LOCAL_FIXTURE");
        if (string.IsNullOrEmpty(fixture))
            Assert.Inconclusive("Requires the disposable local Hiero CI fixture.");

        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var mode = Environment.GetEnvironmentVariable("OASIS_HEDERA_LOCAL_MODE");
        Assert.IsTrue(mode is "async" or "sync");
        var synchronous = mode == "sync";
        var path = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!),
            Path.GetDirectoryName(path));
        StringAssert.StartsWith(Path.GetFileName(path), "oasis-hedera.");
        var contract = Environment.GetEnvironmentVariable("OASIS_HEDERA_LOCAL_CONTRACT_ID")!;
        var key = Environment.GetEnvironmentVariable("OASIS_HEDERA_LOCAL_SIGNING_KEY")!;
        var encryptionKey = Environment.GetEnvironmentVariable("OASIS_HEDERA_LOCAL_ENCRYPTION_KEY")!;
        var nodes = new Dictionary<string, string> { ["127.0.0.1:50211"] = "0.0.3" };
        var configuration = new HederaStorageConfiguration
        {
            ContractId = contract,
            OperatorAccountId = "0.0.2",
            SigningKeyType = "der",
            SigningKeyResolver = () => key,
            EncryptionKeyResolver = () => Convert.FromBase64String(encryptionKey),
            GasLimit = 4_000_000,
            MaxFeeTinybars = 10_000_000_000
        };
        var provider = new ObservedProvider(nodes, configuration);
        var raw = new HederaStorageBackend("local", contract, configuration.OperatorAccountId,
            configuration.SigningKeyType, configuration.SigningKeyResolver, configuration.GasLimit,
            configuration.MaxFeeTinybars, nodes);

        Assert.AreEqual(0, (await raw.ListKeysAsync("")).Count, "The fresh native contract must be empty.");
        var reportPath = Path.Combine(path, synchronous ? "core-storage-sync.json" : "core-storage-async.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));
        var report = await CoreStorageConformance.RunAsync(provider, nameof(HashgraphOASIS), "native", synchronous,
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            }, expectedDefaultDeletion: "absent");
        Assert.IsTrue(report.Succeeded,
            $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed. {provider.DetailFailure}");
        Assert.IsTrue(report.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
        Assert.AreEqual(0, (await raw.ListKeysAsync("")).Count, "Native storage must be empty after the suite.");
    }

    private sealed class ObservedProvider(Dictionary<string, string> nodes, HederaStorageConfiguration configuration)
        : HashgraphOASIS("http://127.0.0.1:5551", "local", "298",
            consensusNodes: nodes, storageConfiguration: configuration)
    {
        public string? DetailFailure { get; private set; }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail detail)
        {
            var result = await base.SaveAvatarDetailAsync(detail).ConfigureAwait(false);
            for (var error = result.Exception; error != null; error = error.InnerException)
            {
                DetailFailure += $" {error.GetType().Name}";
                if (error is HederaNativeMutationReconciliationException native)
                    DetailFailure += $" stage={native.Evidence.Stage} status={native.Evidence.NativeStatus}";
            }
            return result;
        }
    }
}
