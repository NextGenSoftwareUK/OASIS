using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests;

[TestClass]
public class SuiHistoryTests
{
    [TestMethod]
    public async Task OriginalFailedSubmissionPreservesSignedGasDebitInProviderHistory()
    {
        var rpc = Environment.GetEnvironmentVariable("OASIS_SUI_TEST_RPC") ?? throw new InvalidOperationException("Real Sui RPC is required.");
        Assert.IsTrue(new Uri(rpc).IsLoopback);
        var path = Environment.GetEnvironmentVariable("OASIS_SUI_FAILED_TRANSACTION_JOURNAL")
            ?? throw new InvalidOperationException("Original failed transaction journal is required; this test never submits.");
        using var journal = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(path));
        var sender = journal.RootElement.GetProperty("sender").GetString()!;
        var digest = journal.RootElement.GetProperty("digest").GetString()!;
        var chain = journal.RootElement.GetProperty("chainId").GetString()!;
        using var provider = new SuiOASIS(rpc, "localnet", chainId: chain);
        var status = await provider.GetTransactionStatusAsync(digest);
        Assert.IsFalse(status.IsError, status.Message);
        Assert.AreEqual(Core.Managers.Bridge.Enums.BridgeTransactionStatus.Failed, status.Result);
        var history = await provider.GetTransactionsAsync(new GetWeb3TransactionsRequest { WalletAddress = sender });
        Assert.IsFalse(history.IsError, history.Message);
        var rows = history.Result.Where(row => row.NativeTransactionId == digest).ToList();
        Assert.IsTrue(rows.Count > 0, "Original failed transaction was dropped from provider history.");
        foreach (var row in rows)
        {
            Assert.AreEqual("Failed", row.NativeExecutionResult);
            Assert.AreEqual("WalletNetBalanceChange", row.NativeOperation);
        }
        var gas = rows.Single(row => row.AmountUnit.EndsWith("::sui::SUI:base-unit", StringComparison.Ordinal));
        Assert.IsTrue(System.Numerics.BigInteger.Parse(gas.RawAmount, System.Globalization.CultureInfo.InvariantCulture) < 0);
        Assert.AreEqual(TransactionType.Debit, gas.TransactionType);
        Assert.AreEqual(sender, gas.FromWalletAddress);
        Assert.IsNull(gas.ToWalletAddress, "Gas is not a fabricated transfer to another wallet.");
        Assert.IsTrue(gas.Amount > 0);
    }

    [TestMethod]
    public async Task LedgerHistoryIncludesIncomingAndOutgoingReceiptsAcrossPagesAndPropagatesFailure()
    {
        var rpc = Environment.GetEnvironmentVariable("OASIS_SUI_TEST_RPC") ?? throw new InvalidOperationException("Real Sui RPC is required.");
        Assert.IsTrue(new Uri(rpc).IsLoopback);
        using var wallets = new SuiOASIS(rpc, "localnet");
        var sender = await wallets.GenerateKeyPairAsync();
        var receiver = await wallets.GenerateKeyPairAsync();
        var unrelated = await wallets.GenerateKeyPairAsync();
        Assert.IsFalse(sender.IsError, sender.Message);
        Assert.IsFalse(receiver.IsError, receiver.Message);
        await SuiNativeTransferTests.FundAsync(sender.Result.WalletAddressLegacy);
        using var provider = new SuiOASIS(rpc, "localnet", privateKey: sender.Result.PrivateKey);
        var receipts = new List<string>();
        for (var i = 0; i < 53; i++)
        {
            var sent = await provider.SendTransactionAsync(sender.Result.WalletAddressLegacy, receiver.Result.WalletAddressLegacy, 0.001m);
            Assert.IsFalse(sent.IsError, sent.Message);
            receipts.Add(sent.Result.TransactionResult);
        }
        var received = await provider.GetTransactionsAsync(new GetWeb3TransactionsRequest { WalletAddress = receiver.Result.WalletAddressLegacy });
        Assert.IsFalse(received.IsError, received.Message);
        Assert.AreEqual(53, received.Result.Count, "Recipient-only history must include both sides of the page boundary.");
        Assert.AreEqual(53, received.Result.Select(item => item.TransactionId).Distinct().Count());
        foreach (var receipt in receipts)
        {
            var row = received.Result.Single(item => item.Description.Contains(receipt));
            Assert.AreEqual(0.001d, row.Amount);
            Assert.AreEqual(receipt, row.NativeTransactionId);
            Assert.AreEqual("1000000", row.RawAmount);
            Assert.AreEqual("WalletNetBalanceChange", row.NativeOperation);
            Assert.AreEqual("Success", row.NativeExecutionResult);
            Assert.AreEqual(TransactionType.Credit, row.TransactionType);
            Assert.AreEqual(sender.Result.WalletAddressLegacy, row.FromWalletAddress);
            Assert.AreEqual(receiver.Result.WalletAddressLegacy, row.ToWalletAddress);
            Assert.IsTrue(row.CreatedDate > DateTime.UtcNow.AddHours(-1));
        }
        var outgoing = await provider.GetTransactionsAsync(new GetWeb3TransactionsRequest { WalletAddress = sender.Result.WalletAddressLegacy });
        Assert.IsFalse(outgoing.IsError, outgoing.Message);
        foreach (var receipt in receipts)
            Assert.IsTrue(outgoing.Result.Any(item => item.Description.Contains(receipt)), "Sender receipt disappeared.");
        var empty = await provider.GetTransactionsAsync(new GetWeb3TransactionsRequest { WalletAddress = unrelated.Result.WalletAddressLegacy });
        Assert.IsFalse(empty.IsError, empty.Message);
        Assert.AreEqual(0, empty.Result.Count);
        using var wrongChain = new SuiOASIS(rpc, "localnet", chainId: "not-this-chain");
        Assert.IsTrue((await wrongChain.GetTransactionsAsync(new GetWeb3TransactionsRequest { WalletAddress = receiver.Result.WalletAddressLegacy })).IsError);
        using var unavailable = new SuiOASIS("http://127.0.0.1:1", "localnet");
        var failure = await unavailable.GetTransactionsAsync(new GetWeb3TransactionsRequest { WalletAddress = receiver.Result.WalletAddressLegacy });
        Assert.IsTrue(failure.IsError, "Unavailable RPC must not return successful empty history.");
        Assert.IsNull(failure.Result);
    }
}
