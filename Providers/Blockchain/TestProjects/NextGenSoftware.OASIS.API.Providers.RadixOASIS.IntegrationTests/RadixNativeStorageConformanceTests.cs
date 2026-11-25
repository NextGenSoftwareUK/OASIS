using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Providers.RadixOASIS.Infrastructure.Entities;
using NextGenSoftware.OASIS.Providers.TestSupport;

namespace NextGenSoftware.OASIS.API.Providers.RadixOASIS.IntegrationTests;

/// <summary>
/// The shared core storage suite against a real local Radix Babylon network (fixture/start-radix-fixture.sh). Each mode
/// gets a fresh key and account (funded by the localnet faucet), publishes the OASIS package and instantiates its own
/// OasisKeyValueStore owned by that key, then runs every check through the provider.
/// </summary>
[TestClass, DoNotParallelize]
public sealed class RadixNativeStorageConformanceTests
{
    private const byte LocalnetId = 240;
    private const string LocalnetName = "localnet";

    private static string Env(string name) => Environment.GetEnvironmentVariable(name) ?? throw new InvalidOperationException(name + " is required.");

    private static string FixtureDirectory()
    {
        var fixture = Environment.GetEnvironmentVariable("OASIS_NATIVE_STORAGE_FIXTURE");
        if (string.IsNullOrWhiteSpace(fixture))
            Assert.Inconclusive("Requires the disposable native Radix localnet CI fixture.");
        Assert.AreEqual("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var directory = Path.GetFullPath(fixture!);
        Assert.AreEqual(Path.GetFullPath(Env("RUNNER_TEMP")), Path.GetDirectoryName(directory));
        StringAssert.StartsWith(Path.GetFileName(directory), "oasis-native.");
        return directory;
    }

    /// <summary>Fresh funded key with its own OasisKeyValueStore; returns the provider config for it.</summary>
    internal static async Task<RadixOASISConfig> CreateStoreAsync()
    {
        var privateKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var packageDirectory = Env("OASIS_RADIX_PACKAGE_DIR");
        using var ledger = new RadixLedgerClient(new Uri(Env("OASIS_RADIX_CORE_API")), new Uri(Env("OASIS_RADIX_ENGINE_STATE_API")),
            LocalnetId, LocalnetName, privateKey);
        await ledger.FundFromFaucetAsync();
        var package = await ledger.PublishPackageAsync(
            await File.ReadAllBytesAsync(Path.Combine(packageDirectory, "oasis_storage.wasm")),
            await File.ReadAllBytesAsync(Path.Combine(packageDirectory, "oasis_storage.rpd")));
        var component = await ledger.InstantiateKeyValueStoreAsync(package);
        return new RadixOASISConfig
        {
            NetworkId = LocalnetId,
            NetworkName = LocalnetName,
            CoreApiUri = Env("OASIS_RADIX_CORE_API"),
            EngineStateApiUri = Env("OASIS_RADIX_ENGINE_STATE_API"),
            AccountAddress = ledger.AccountAddress,
            PrivateKey = privateKey,
            KeyValueStoreComponentAddress = component,
            StorageEncryptionKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant()
        };
    }

    [DataTestMethod, DataRow(false), DataRow(true)]
    public async Task CoreStorageSuiteAgainstRadixLocalnet(bool synchronous)
    {
        var directory = FixtureDirectory();
        var reportPath = Path.Combine(directory, synchronous ? "sync.json" : "async.json");
        Assert.IsFalse(File.Exists(reportPath) || File.Exists(reportPath + ".next"));

        using var provider = new RadixOASIS(await CreateStoreAsync());
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

/// <summary>
/// Restart persistence: phase "write" saves a holon into a fresh store and records its config in the fixture; the
/// workflow then restarts the node container and phase "read" loads the holon through a new provider instance.
/// </summary>
[TestClass, DoNotParallelize]
public sealed class RadixNativeStoragePersistenceTests
{
    [TestMethod]
    public async Task HolonSurvivesNodeRestart()
    {
        var phase = Environment.GetEnvironmentVariable("OASIS_RADIX_PERSISTENCE_PHASE");
        if (phase is not ("write" or "read"))
            Assert.Inconclusive("Run by the Radix native storage workflow with OASIS_RADIX_PERSISTENCE_PHASE=write|read.");
        var fixture = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_NATIVE_STORAGE_FIXTURE")!);
        StringAssert.StartsWith(Path.GetFileName(fixture), "oasis-native.");
        var probePath = Path.Combine(fixture, "persistence-probe.json");

        if (phase == "write")
        {
            Assert.IsFalse(File.Exists(probePath));
            var config = await RadixNativeStorageConformanceTests.CreateStoreAsync();
            var holon = new Holon { Id = Guid.NewGuid(), Name = "radix-restart-" + Guid.NewGuid().ToString("N") };
            using var provider = new RadixOASIS(config);
            Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
            var saved = await provider.SaveHolonAsync(holon);
            Assert.IsFalse(saved.IsError, saved.Message);
            await File.WriteAllTextAsync(probePath, JsonSerializer.Serialize(new Probe(config, holon.Id, holon.Name)));
            return;
        }

        var probe = JsonSerializer.Deserialize<Probe>(await File.ReadAllTextAsync(probePath))!;
        using var restarted = new RadixOASIS(probe.Config);
        var activated = await restarted.ActivateProviderAsync();
        Assert.IsFalse(activated.IsError, activated.Message);
        var loaded = await restarted.LoadHolonAsync(probe.HolonId);
        Assert.IsFalse(loaded.IsError, loaded.Message);
        Assert.AreEqual(probe.Name, loaded.Result.Name);
        Assert.IsFalse((await restarted.DeleteHolonAsync(probe.HolonId)).IsError);
    }

    private sealed record Probe(RadixOASISConfig Config, Guid HolonId, string Name);
}
