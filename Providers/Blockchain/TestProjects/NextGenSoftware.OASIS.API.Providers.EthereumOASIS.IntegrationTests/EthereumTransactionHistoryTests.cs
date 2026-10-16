using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task NativeTransactionHistoryUsesActualBlocksReceiptsAndExactAmounts(bool synchronous)
    {
        Assert.AreEqual(new BigInteger(31337), ChainId, "No public-chain transfers permitted.");
        var (provider, _) = await CreateActivatedProviderAsync();
        try
        {
            var account = new Account(PrivateKey, ChainId);
            var web3 = new Web3(account, Host);
            const string recipient = "0x2222222222222222222222222222222222222222";
            var amount = BigInteger.Parse("1234567890123456789");
            var hash = await web3.TransactionManager.SendTransactionAsync(new TransactionInput {
                From = account.Address, To = recipient, Value = new HexBigInteger(amount), Gas = new HexBigInteger(21000) });
            var receipt = await web3.TransactionManager.TransactionReceiptService.PollForReceiptAsync(hash);
            Assert.AreEqual(BigInteger.One, receipt.Status.Value);
            var height = (ulong)receipt.BlockNumber.Value;
            foreach (var outgoing in new[] { false, true })
            {
                var request = new GetWeb3TransactionsRequest {
                    WalletAddress = outgoing ? account.Address.ToLowerInvariant() : recipient,
                    FromBlock = height, ToBlock = height };
                var result = synchronous ? provider.GetTransactions(request) : await provider.GetTransactionsAsync(request);
                Assert.IsFalse(result.IsError, result.Message + " " + result.Exception);
                Assert.IsTrue(result.IsLoaded);
                Assert.AreEqual(1, result.LoadedCount);
                var transaction = result.Result.Single();
                Assert.AreEqual(hash, transaction.NativeTransactionId);
                Assert.AreEqual(amount.ToString(System.Globalization.CultureInfo.InvariantCulture), transaction.RawAmount);
                Assert.AreEqual("wei", transaction.AmountUnit);
                Assert.AreEqual("succeeded", transaction.NativeExecutionResult);
                Assert.AreEqual(Guid.Empty, transaction.TransactionId, "Do not fabricate GUIDs for native hashes.");
                Assert.AreEqual(outgoing ? TransactionType.Debit : TransactionType.Credit, transaction.TransactionType);
                Assert.AreEqual(DateTimeKind.Utc, transaction.CreatedDate.Kind);
                var balanceRequest = new GetWeb3WalletBalanceRequest { WalletAddress = request.WalletAddress };
                var balance = synchronous ? provider.GetBalance(balanceRequest) : await provider.GetBalanceAsync(balanceRequest);
                Assert.IsFalse(balance.IsError, balance.Message);
                Assert.IsTrue(balance.IsLoaded);
                Assert.AreEqual(1, balance.LoadedCount);
                var nativeBalance = await web3.Eth.GetBalance.SendRequestAsync(request.WalletAddress);
                Assert.AreEqual(double.Parse(Nethereum.Util.UnitConversion.Convert.FromWei(nativeBalance.Value).ToString(System.Globalization.CultureInfo.InvariantCulture),System.Globalization.CultureInfo.InvariantCulture), balance.Result);
            }
            var empty = await provider.GetTransactionsAsync(new GetWeb3TransactionsRequest {
                WalletAddress = "0x3333333333333333333333333333333333333333", FromBlock = height, ToBlock = height });
            Assert.IsFalse(empty.IsError, empty.Message);
            Assert.IsTrue(empty.IsLoaded);
            Assert.AreEqual(0, empty.Result.Count);
            var future = await provider.GetTransactionsAsync(new GetWeb3TransactionsRequest {
                WalletAddress = recipient, FromBlock = height + 10000, ToBlock = height + 10000 });
            Assert.IsTrue(future.IsError);
            Assert.IsNull(future.Result);
            Assert.IsFalse(future.IsLoaded);
        }
        finally { await provider.DeActivateProviderAsync(); }
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task NativeHistoryReportsRevertedRequestedValueWithoutApplyingIt(bool synchronous)
    {
        Assert.AreEqual(new BigInteger(31337), ChainId, "No public-chain transfers permitted.");
        var (provider, contract) = await CreateActivatedProviderAsync();
        var account = new Account(PrivateKey, ChainId);
        var web3 = new Web3(account, Host);
        var automineDisabled = false;
        try
        {
            var before = await web3.Eth.GetBalance.SendRequestAsync(contract);
            await web3.Client.SendRequestAsync<bool>(new Nethereum.JsonRpc.Client.RpcRequest(1, "evm_setAutomine", false));
            automineDisabled = true;
            var hash = await web3.TransactionManager.SendTransactionAsync(new TransactionInput {
                From = account.Address, To = contract, Value = new HexBigInteger(1234), Gas = new HexBigInteger(21000) });
            await web3.Client.SendRequestAsync<string>(new Nethereum.JsonRpc.Client.RpcRequest(2, "evm_mine"));
            var receipt = await web3.TransactionManager.TransactionReceiptService.PollForReceiptAsync(hash);
            Assert.AreEqual(BigInteger.Zero, receipt.Status.Value, "Storage contract must reject plain ETH.");
            var height = (ulong)receipt.BlockNumber.Value;
            var request = new GetWeb3TransactionsRequest { WalletAddress = account.Address, FromBlock = height, ToBlock = height };
            var result = synchronous ? provider.GetTransactions(request) : await provider.GetTransactionsAsync(request);
            Assert.IsFalse(result.IsError, result.Message + " " + result.Exception);
            Assert.IsTrue(result.IsLoaded);
            var failed = result.Result.Single();
            Assert.AreEqual(hash, failed.NativeTransactionId);
            Assert.AreEqual("1234", failed.RawAmount);
            Assert.AreEqual("reverted", failed.NativeExecutionResult);
            StringAssert.Contains(failed.NativeError, "not applied");
            Assert.AreEqual(before.Value, (await web3.Eth.GetBalance.SendRequestAsync(contract)).Value);
        }
        finally
        {
            if (automineDisabled)
                await web3.Client.SendRequestAsync<bool>(new Nethereum.JsonRpc.Client.RpcRequest(3, "evm_setAutomine", true));
            await provider.DeActivateProviderAsync();
        }
    }

    [TestMethod]
    public async Task TransactionHistoryInvalidRequestsFailBeforeActivation()
    {
        var provider = new EthereumOASIS("http://127.0.0.1:1", PrivateKey, ChainId, "0x" + new string('1', 40));
        foreach (var request in new GetWeb3TransactionsRequest[] {
            null!, new() {WalletAddress="invalid"},
            new() {WalletAddress="0x"+new string('2',40),FromBlock=5,ToBlock=4},
            new() {WalletAddress="0x"+new string('2',40),FromBlock=0,ToBlock=1000} })
        {
            var result = await provider.GetTransactionsAsync(request);
            Assert.IsTrue(result.IsError);
            Assert.IsNull(result.Result);
            Assert.IsInstanceOfType<ArgumentException>(result.Exception);
            Assert.IsFalse(provider.IsProviderActivated);
        }
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task BalanceActivationFailurePreservesOriginalCause(bool synchronous)
    {
        var provider = new EthereumOASIS(null!, PrivateKey, ChainId, "0x" + new string('1', 40));
        var request = new GetWeb3WalletBalanceRequest { WalletAddress = "0x" + new string('2', 40) };
        var result = synchronous ? provider.GetBalance(request) : await provider.GetBalanceAsync(request);
        Assert.IsTrue(result.IsError);
        Assert.IsInstanceOfType<InvalidOperationException>(result.Exception);
        Assert.IsFalse(result.IsLoaded);
        Assert.IsFalse(provider.IsProviderActivated);
        StringAssert.Contains(result.Message, "requires an RPC URI");
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task InvalidBalanceRequestIsRejectedBeforeActivation(bool synchronous)
    {
        var provider = new EthereumOASIS(null!, PrivateKey, ChainId, "0x" + new string('1', 40));
        foreach (var request in new GetWeb3WalletBalanceRequest[] { null!, new() { WalletAddress = "invalid" } })
        {
            var result = synchronous ? provider.GetBalance(request) : await provider.GetBalanceAsync(request);
            Assert.IsTrue(result.IsError);
            Assert.IsInstanceOfType<ArgumentException>(result.Exception);
            Assert.IsFalse(result.IsLoaded);
            Assert.IsFalse(provider.IsProviderActivated);
        }
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task WalletReadsPreserveRealUnavailableRpcCause(bool synchronous)
    {
        // The retained runner reserves this loopback port as a closed RPC endpoint.
        var provider = new EthereumOASIS("http://127.0.0.1:18546", PrivateKey, ChainId, "0x" + new string('1', 40));
        var address = "0x" + new string('2', 40);
        var balanceRequest = new GetWeb3WalletBalanceRequest { WalletAddress = address };
        var balance = synchronous ? provider.GetBalance(balanceRequest) : await provider.GetBalanceAsync(balanceRequest);
        Assert.IsTrue(balance.IsError);
        Assert.IsFalse(balance.IsLoaded);
        Assert.IsInstanceOfType<System.Net.Sockets.SocketException>(balance.Exception.GetBaseException());
        var historyRequest = new GetWeb3TransactionsRequest { WalletAddress = address, FromBlock = 0, ToBlock = 0 };
        var history = synchronous ? provider.GetTransactions(historyRequest) : await provider.GetTransactionsAsync(historyRequest);
        Assert.IsTrue(history.IsError);
        Assert.IsFalse(history.IsLoaded);
        Assert.IsNull(history.Result);
        Assert.IsInstanceOfType<System.Net.Sockets.SocketException>(history.Exception.GetBaseException());
        Assert.IsFalse(provider.IsProviderActivated);
        Assert.IsNull(provider.Web3Client);
    }
}
