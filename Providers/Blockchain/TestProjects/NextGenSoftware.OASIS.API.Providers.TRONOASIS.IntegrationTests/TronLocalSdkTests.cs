using TronNet;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.IntegrationTests;

/// <summary>Requires the single reusable, loopback-only pinned oasis-tron-tre instance.</summary>
public sealed class TronLocalSdkTests
{
    private const string Genesis = "0000000000000000c93baa76a4a508f798a96f59156d9eb17ecede8ec845df2f";
    // Public local fixture keys. Never send these to a public network.
    private static readonly string SenderKey = new string('0', 63) + "1";
    private static readonly string RecipientKey = new string('0', 63) + "2";

    [Fact]
    public async Task SignedContractStorageCommitsOnReusableLocalTronNode()
    {
        using var bounded = new CancellationTokenSource(TimeSpan.FromSeconds(55));
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"), new Uri("http://127.0.0.1:50052"), Genesis);
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Scripts/TestHosts/compile-tron-store.cjs"))) directory = directory.Parent;
        Assert.NotNull(directory);
        var artifacts = Path.Combine(directory!.FullName, "TestResults/TronRuntime");
        var deployment = Path.Combine(artifacts, "local-store-address.txt");
        string contract;
        if (File.Exists(deployment)) contract = (await File.ReadAllTextAsync(deployment, bounded.Token)).Trim();
        else
        {
            var bytecode = Convert.FromHexString((await File.ReadAllTextAsync(Path.Combine(artifacts, "store-bytecode.txt"), bounded.Token)).Trim());
            var deployed = await runtime.NativeTransfers.DeployBytecodeAsync(new TronECKey(SenderKey, TronNetwork.MainNet).GetPublicAddress(), bytecode, SenderKey, bounded.Token);
            contract = deployed.ContractAddress;
            await File.WriteAllTextAsync(deployment, contract, bounded.Token);
        }
        var store = new TronStorageBackend(runtime.NativeTransfers, new TronECKey(SenderKey, TronNetwork.MainNet).GetPublicAddress(), contract, () => SenderKey);
        await store.VerifyAsync(bounded.Token);
        const string key = "reusable-sdk-storage-test";
        try
        {
            await store.PutAsync(key, "TRON 🌳 exact storage", bounded.Token);
            Assert.Equal("TRON 🌳 exact storage", await store.GetAsync(key, bounded.Token));
            Assert.Contains(key, await store.ListKeysAsync("reusable-", bounded.Token));
        }
        finally { await store.DeleteAsync(key, bounded.Token); }
        Assert.Null(await store.GetAsync(key, bounded.Token));
    }

    [Fact]
    public async Task NativeTransferUsesSdkSigningAndSolidifiedReceiptOnPinnedLocalChain()
    {
        using var bounded = new CancellationTokenSource(TimeSpan.FromSeconds(50));
        using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:9090/"), Timeout = TimeSpan.FromSeconds(5) };
        using var node = System.Text.Json.JsonDocument.Parse(await http.GetStringAsync("wallet/getnodeinfo", bounded.Token));
        Assert.Equal("4.8.2", node.RootElement.GetProperty("configNodeInfo").GetProperty("codeVersion").GetString());
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"), new Uri("http://127.0.0.1:50052"), Genesis);
        var sender = new TronECKey(SenderKey, TronNetwork.MainNet).GetPublicAddress();
        var recipient = new TronECKey(RecipientKey, TronNetwork.MainNet).GetPublicAddress();
        var before = await runtime.NativeTransfers.GetNativeBalanceAsync(sender, bounded.Token);
        Assert.True(before > 1m);
        var hash = await runtime.NativeTransfers.SendAsync(sender, recipient, 1m, SenderKey, "OASIS reusable local SDK test", bounded.Token);
        Assert.Matches("\\A[0-9a-fA-F]{64}\\z", hash);
        Assert.Equal(NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed,
            await runtime.NativeTransfers.GetTransactionStatusAsync(hash, bounded.Token));
        Assert.True(await runtime.NativeTransfers.GetNativeBalanceAsync(sender, bounded.Token) <= before - 1m);
        Assert.True(await runtime.NativeTransfers.GetNativeBalanceAsync(recipient, bounded.Token) >= 1m);
    }
}
