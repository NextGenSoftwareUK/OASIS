using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.TONOASIS.IntegrationTests;

[TestClass]
[DoNotParallelize]
public class TonNativeLifecycleTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task SharedStorageLifecycle(bool synchronous)
    {
        var root = Environment.GetEnvironmentVariable("OASIS_TON_NATIVE_ROOT");
        if (string.IsNullOrWhiteSpace(root))
            Assert.Inconclusive("Set the explicit native TVM fixture configuration.");

        var node = Environment.GetEnvironmentVariable("OASIS_TON_NODE");
        var hash = Environment.GetEnvironmentVariable("OASIS_TON_CODE_HASH");
        var worker = Path.Combine(root!, "Providers/Blockchain/NextGenSoftware.OASIS.API.Providers.TONOASIS/SdkBridge/native-worker.cjs");
        var binding = Path.Combine(root!, "TestResults/TonCqlCurrent/compiled/TonHolonStore_TonHolonStore.cjs");
        Assert.IsTrue(File.Exists(node) && File.Exists(worker) && File.Exists(binding) && !string.IsNullOrWhiteSpace(hash),
            "The configured native TVM fixture inputs are incomplete.");

        var mode = synchronous ? "sync" : "async";
        var directory = Path.Combine(root!, "TestResults/TonProviderLifecycle", mode);
        Assert.IsFalse(Directory.Exists(directory), "Preserve any existing fixture and its evidence.");
        Directory.CreateDirectory(directory);

        var options = new TonSandboxStorageOptions(node!, worker, binding, directory, Guid.NewGuid(), hash!, ReadOnly: false);
        await new TonSandboxStorageBackend(options).CreateSourceAsync();
        var key = SHA256.HashData("OASIS disposable TON lifecycle fixture"u8);
        using var provider = new TONOASIS(options, () => (byte[])key.Clone());
        var reportPath = Path.Combine(directory, "conformance.json");
        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            var pendingPath = reportPath + ".pending";
            await using (var stream = new FileStream(pendingPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(stream, report);
                stream.Flush(flushToDisk: true);
            }
            File.Move(pendingPath, reportPath, overwrite: true);
        }

        var result = await CoreStorageConformance.RunAsync(provider, nameof(TONOASIS), "native", synchronous, Checkpoint);
        Assert.IsTrue(result.Succeeded, $"{result.CurrentCase}: {result.FailureType}");
        Assert.IsTrue(result.FixtureCleanupComplete);
        Assert.AreEqual(CoreStorageConformance.ChecksPerMode, result.Passed.Count);
    }
}
