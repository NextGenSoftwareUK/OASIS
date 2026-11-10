using System.Text.Json;
using NextGenSoftware.OASIS.Providers.TestSupport;
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
        Assert.False(network.Equals("mainnet", StringComparison.OrdinalIgnoreCase));
        var chainId = Required("OASIS_TRON_LIFECYCLE_CHAIN_ID");
        Assert.False(chainId.Equals("0x2b6653dc", StringComparison.OrdinalIgnoreCase));

        var root = Required("OASIS_TRON_LIFECYCLE_REPORT_ROOT");
        Directory.CreateDirectory(root);
        using var lease = new FileStream(Path.Combine(root, "environment.lock"),
            FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        var path = Path.Combine(root, synchronous ? "sync.json" : "async.json");
        var pending = path + ".pending";
        Assert.False(File.Exists(path) || File.Exists(pending), "Preserve original fixture evidence.");

        async Task Checkpoint(CoreStorageConformance.Report report)
        {
            await using (var journal = new FileStream(pending, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(journal, report);
                journal.Flush(flushToDisk: true);
            }
            File.Move(pending, path, overwrite: true);
        }

        var provider = new TRONOASIS(endpoint.AbsoluteUri, network, chainId,
            fullNodeGrpcEndpoint: fullNode.AbsoluteUri,
            solidityNodeGrpcEndpoint: solidityNode.AbsoluteUri,
            genesisBlockId: Required("OASIS_TRON_LIFECYCLE_GENESIS"),
            storageContractAddress: Required("OASIS_TRON_LIFECYCLE_CONTRACT"),
            storageOwnerAddress: Required("OASIS_TRON_LIFECYCLE_OWNER"));
        var report = await CoreStorageConformance.RunAsync(provider, nameof(TRONOASIS), "native",
            synchronous, Checkpoint, expectedDefaultDeletion: "absent");
        Assert.True(report.Succeeded, $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}");
        Assert.True(report.FixtureCleanupComplete);
        Assert.Equal(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
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
