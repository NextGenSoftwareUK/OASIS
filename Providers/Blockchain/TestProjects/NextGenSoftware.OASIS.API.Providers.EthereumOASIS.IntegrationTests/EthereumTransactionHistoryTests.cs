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
}
