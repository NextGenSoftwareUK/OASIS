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
