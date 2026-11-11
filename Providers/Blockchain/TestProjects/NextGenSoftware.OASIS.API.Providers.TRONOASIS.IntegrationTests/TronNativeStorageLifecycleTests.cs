using System.Globalization;
using System.Text.Json;
using NextGenSoftware.OASIS.Providers.TestSupport;
using TronNet;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.IntegrationTests;

public class TronNativeStorageLifecycleTests
{
    [NativeLifecycleTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SharedStorageLifecycle(bool synchronous)
    {
        var endpoint = LoopbackEndpoint("OASIS_TRON_LIFECYCLE_RPC");
        var fullNode = LoopbackEndpoint("OASIS_TRON_LIFECYCLE_FULL_GRPC");
        var solidityNode = LoopbackEndpoint("OASIS_TRON_LIFECYCLE_SOLIDITY_GRPC");
        var network = Required("OASIS_TRON_LIFECYCLE_NETWORK");
        var chainId = Required("OASIS_TRON_LIFECYCLE_CHAIN_ID");
        var genesis = Required("OASIS_TRON_LIFECYCLE_GENESIS");
        RequireNonMainnetIdentity(network, chainId, genesis);

        var root = Required("OASIS_TRON_LIFECYCLE_REPORT_ROOT");
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "environment.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var path = Path.Combine(root, synchronous ? "sync.json" : "async.json");
        var pending = path + ".pending";
        Assert.False(File.Exists(path), "Preserve original fixture evidence.");
        Assert.False(File.Exists(Path.Combine(root, "sync.json.pending")) ||
            File.Exists(Path.Combine(root, "async.json.pending")),
            "Reconcile interrupted fixture evidence before starting either lifecycle mode.");

        var otherModePath = Path.Combine(root, synchronous ? "async.json" : "sync.json");
        if (File.Exists(otherModePath))
        {
            await using var previous = File.OpenRead(otherModePath);
            var previousReport = await JsonSerializer.DeserializeAsync<CoreStorageConformance.Report>(previous);
            Assert.True(previousReport?.FixtureCleanupComplete == true,
                "Reconcile the prior lifecycle fixture before starting another mode.");
        }

        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            await using (var journal = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(journal, report);
                journal.Flush(flushToDisk: true);
            }
            File.Move(pending, path, overwrite: true);
        }

        var fresh = Environment.GetEnvironmentVariable("OASIS_TRON_FRESH_LIFECYCLE") == "1";
        var contract = fresh ? null : Required("OASIS_TRON_LIFECYCLE_CONTRACT");
        var owner = fresh ? null : Required("OASIS_TRON_LIFECYCLE_OWNER");
        if (fresh)
        {
            const string treGenesis = "0000000000000000c93baa76a4a508f798a96f59156d9eb17ecede8ec845df2f";
            Assert.Equal(treGenesis, genesis);
            var signer = Required("OASIS_TRON_STORAGE_PRIVATE_KEY");
            owner = new TronECKey(signer, TronNetwork.MainNet).GetPublicAddress();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            await using var runtime = new TronSdkRuntime(fullNode, solidityNode, genesis, contractFeeLimitSun: 1000000000);
            await runtime.NativeTransfers.VerifyNetworkAsync(timeout.Token);
            var bytecode = Convert.FromHexString((await File.ReadAllTextAsync(
                Required("OASIS_TRON_FRESH_BYTECODE"), timeout.Token)).Trim());
            var deployment = await runtime.NativeTransfers.DeployBytecodeAsync(owner, bytecode, signer, timeout.Token);
            contract = deployment.ContractAddress;
            Assert.False(string.IsNullOrWhiteSpace(contract));
        }

        var provider = new TRONOASIS(endpoint.AbsoluteUri, network, chainId,
            fullNodeGrpcEndpoint: fullNode.AbsoluteUri,
            solidityNodeGrpcEndpoint: solidityNode.AbsoluteUri,
            genesisBlockId: genesis,
            storageContractAddress: contract,
            storageOwnerAddress: owner,
            contractFeeLimitSun: fresh ? 1000000000 : 100000000);
        var report = await CoreStorageConformance.RunAsync(provider, nameof(TRONOASIS), "native",
            synchronous, result => fresh && result.Succeeded ? Task.CompletedTask : Checkpoint(result),
            expectedDefaultDeletion: "absent");
        Assert.True(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}");
        Assert.True(report.FixtureCleanupComplete);
        Assert.Equal(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
        if (fresh)
        {
            report.CurrentCase = "native-contract-cleanup";
            report.Succeeded = false;
            report.FixtureCleanupComplete = false;
            await Checkpoint(report);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            await using var runtime = new TronSdkRuntime(fullNode, solidityNode, genesis);
            var raw = new TronStorageBackend(runtime.NativeTransfers, owner!, contract!,
                () => throw new InvalidOperationException("Native cleanup inspection must stay read-only."));
            Assert.Empty(await raw.ListKeysAsync(string.Empty, timeout.Token));
            report.CurrentCase = "complete";
            report.Succeeded = true;
            report.FixtureCleanupComplete = true;
            report.CleanupScope = "native-contract-empty";
            await Checkpoint(report);
        }
    }

    [Theory]
    [InlineData("mainnet", "1", false)]
    [InlineData("local", "728126428", false)]
    [InlineData("local", "0X02B6653DC", false)]
    [InlineData("local", "1", true)]
    [InlineData("local", "unknown", false)]
    public void MainnetAndInvalidIdentitiesAreRejectedBeforeStorage(
        string network, string chainId, bool mainnetGenesis)
    {
        var genesis = mainnetGenesis ? TronPublicHeaderMetadataSource.PublicGenesis : new string('1', 64);
        Assert.Throws<ArgumentException>(() => RequireNonMainnetIdentity(network, chainId, genesis));
    }

    private static void RequireNonMainnetIdentity(string network, string chainId, string genesis)
    {
        const uint mainnetChainId = 0x2b6653dc;
        var hex = chainId.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
        var value = hex ? chainId.AsSpan(2) : chainId.AsSpan();
        if (!uint.TryParse(value, hex ? NumberStyles.AllowHexSpecifier : NumberStyles.None,
                CultureInfo.InvariantCulture, out var numericChainId) || numericChainId == 0)
            throw new ArgumentException("A valid numeric TRON chain ID is required.", nameof(chainId));
        if (genesis.Length != 64 || !genesis.All(Uri.IsHexDigit))
            throw new ArgumentException("A 64-digit TRON genesis block ID is required.", nameof(genesis));
        if (network.Trim().Equals("mainnet", StringComparison.OrdinalIgnoreCase)
            || numericChainId == mainnetChainId
            || genesis.Equals(TronPublicHeaderMetadataSource.PublicGenesis, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("The native storage lifecycle requires a non-mainnet TRON fixture.");
    }

    private sealed class NativeLifecycleTheoryAttribute : TheoryAttribute
    {
        public NativeLifecycleTheoryAttribute()
        {
            if (Environment.GetEnvironmentVariable("OASIS_TRON_STORAGE_LIFECYCLE") != "1")
                Skip = "Explicitly enable the dedicated native TRON lifecycle fixture.";
        }
    }

    private static Uri LoopbackEndpoint(string name)
    {
        var endpoint = new Uri(Required(name), UriKind.Absolute);
        Assert.True(endpoint.IsLoopback && endpoint.Scheme == Uri.UriSchemeHttp &&
            string.IsNullOrEmpty(endpoint.UserInfo) && string.IsNullOrEmpty(endpoint.Query) &&
            string.IsNullOrEmpty(endpoint.Fragment), $"Configure a dedicated loopback HTTP endpoint for {name}.");
        return endpoint;
    }

    private static string Required(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return !string.IsNullOrWhiteSpace(value) ? value : throw new InvalidOperationException($"Missing {name}.");
    }
}
