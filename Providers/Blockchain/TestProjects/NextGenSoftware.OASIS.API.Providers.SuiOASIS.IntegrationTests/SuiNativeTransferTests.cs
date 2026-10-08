using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests;

[TestClass]
public class SuiNativeTransferTests
{
    private static string Required(string name) => Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException($"Real Sui test setting {name} is required; no skipped or mocked evidence.");

    internal static async Task FundAsync(string address)
    {
        var path = Path.Combine(Path.GetDirectoryName(Required("OASIS_SUI_SDK_BRIDGE"))!, "test-fund.mjs");
        var start = new ProcessStartInfo("node")
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.ArgumentList.Add(path);
        start.ArgumentList.Add(address); // Public address only, never signing credentials.
        using var process = Process.Start(start)!;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        var output = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var error = process.StandardError.ReadToEndAsync(timeout.Token);
        try
        {
            await process.WaitForExitAsync(timeout.Token);
            await output;
            Assert.AreEqual(0, process.ExitCode, await error);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited) process.Kill(entireProcessTree: true);
            throw;
        }
    }

    [TestMethod]
    public async Task OfficialSdkBackedProviderCommitsNativeTransferAndReadsConsensusStatus()
    {
        var rpc = Required("OASIS_SUI_TEST_RPC");
        Assert.IsTrue(new Uri(rpc).IsLoopback, "Test must not spend public-chain assets.");
        var walletProvider = new SuiOASIS(rpc, "localnet");
        try
        {
            var sender = await walletProvider.GenerateKeyPairAsync();
            var recipient = await walletProvider.GenerateKeyPairAsync();
            Assert.IsFalse(sender.IsError, sender.Message);
            Assert.IsFalse(recipient.IsError, recipient.Message);
            await FundAsync(sender.Result.WalletAddressLegacy);
            var provider = new SuiOASIS(rpc, "localnet", privateKey: sender.Result.PrivateKey);
            try
            {
                var before = await provider.GetBalanceAsync(new GetWeb3WalletBalanceRequest { WalletAddress = recipient.Result.WalletAddressLegacy });
                Assert.IsFalse(before.IsError, before.Message);
                var committed = await provider.SendTransactionAsync(sender.Result.WalletAddressLegacy, recipient.Result.WalletAddressLegacy, 1m);
                Assert.IsFalse(committed.IsError, committed.Message);
                Assert.IsFalse(string.IsNullOrWhiteSpace(committed.Result.TransactionResult));
                var status = await provider.GetTransactionStatusAsync(committed.Result.TransactionResult);
                Assert.IsFalse(status.IsError, status.Message);
                Assert.AreEqual(BridgeTransactionStatus.Completed, status.Result);
                var after = await provider.GetBalanceAsync(new GetWeb3WalletBalanceRequest { WalletAddress = recipient.Result.WalletAddressLegacy });
                Assert.IsFalse(after.IsError, after.Message);
                Assert.AreEqual(1d, after.Result - before.Result);
                var exactBalance = await provider.GetAccountBalanceAsync(recipient.Result.WalletAddressLegacy);
                Assert.IsFalse(exactBalance.IsError, exactBalance.Message);
                Assert.AreEqual((decimal)after.Result, exactBalance.Result);
                var wrongSender = await provider.SendTransactionAsync(recipient.Result.WalletAddressLegacy, sender.Result.WalletAddressLegacy, 1m);
                Assert.IsTrue(wrongSender.IsError);
                StringAssert.Contains(wrongSender.Message, "does not own sender");
            }
            finally { provider.Dispose(); }
            var wrongChain = new SuiOASIS(rpc, "localnet", chainId: "not-this-genesis");
            try
            {
                var rejected = await wrongChain.GetBalanceAsync(new GetWeb3WalletBalanceRequest { WalletAddress = recipient.Result.WalletAddressLegacy });
                Assert.IsTrue(rejected.IsError, "Chain failure must not be reported as a successful zero balance.");
                StringAssert.Contains(rejected.Message, "chain identifier mismatch");
                var exactRejected = await wrongChain.GetAccountBalanceAsync(recipient.Result.WalletAddressLegacy);
                Assert.IsTrue(exactRejected.IsError, "Decimal account balance must not hide a chain failure as zero.");
            }
            finally { wrongChain.Dispose(); }
        }
        finally { walletProvider.Dispose(); }
    }
}
