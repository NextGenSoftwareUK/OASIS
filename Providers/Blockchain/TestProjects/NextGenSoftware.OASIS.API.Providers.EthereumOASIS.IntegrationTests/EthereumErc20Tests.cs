using System.Numerics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    private static async Task<Nethereum.Contracts.Contract> CreateTokenAsync(byte decimals, bool noOp = false)
    {
        Assert.AreEqual(new BigInteger(31337), ChainId, "Only retained local chain permitted.");
        var web3 = new Web3(new Account(PrivateKey, ChainId), Host);
        using var json = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(
            Path.GetDirectoryName(Environment.GetEnvironmentVariable("ETHEREUMOASIS_TEST_FIXTURE_JSON"))!, "OwnedErc20Fixture.json")));
        var abi = json.RootElement.GetProperty("abi").GetRawText();
        var receipt = await web3.Eth.DeployContract.SendRequestAndWaitForReceiptAsync(abi,
            json.RootElement.GetProperty("bytecode").GetString(), web3.TransactionManager.Account.Address,
            new HexBigInteger(4000000), null, decimals, noOp);
        Assert.AreEqual(BigInteger.One, receipt.Status.Value);
        return web3.Eth.GetContract(abi, receipt.ContractAddress);
    }

    private static BurnWeb3TokenRequest BurnRequest(string token, decimal amount) => new() {
        TokenAddress = token, Amount = amount, OwnerPrivateKey = PrivateKey, OwnerPublicKey = "", OwnerSeedPhrase = "" };
    private static SendWeb3TokenRequest TransferRequest(string token, decimal amount) => new() {
        FromTokenAddress = token, ToWalletAddress = "0x2222222222222222222222222222222222222222",
        Amount = amount, OwnerPrivateKey = PrivateKey };

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeErc20TransferAndBurnUseRequestedExactAmount(bool synchronous)
    {
        var (provider, _) = await CreateActivatedProviderAsync();
        try
        {
            foreach (var decimals in new byte[] { 6, 30 })
            {
                var token = await CreateTokenAsync(decimals);
                var owner = new Account(PrivateKey, ChainId).Address;
                var initial = await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner);
                var transfer = TransferRequest(token.Address, 0.234567m);
                var sent = synchronous ? provider.SendToken(transfer) : await provider.SendTokenAsync(transfer);
                Assert.IsFalse(sent.IsError, sent.Message + " " + sent.Exception);
                Assert.IsTrue(sent.IsSaved); Assert.IsNotNull(sent.Result);
                var unit = BigInteger.Pow(10, decimals - 6);
                Assert.AreEqual(234567 * unit, await token.GetFunction("balanceOf").CallAsync<BigInteger>(transfer.ToWalletAddress));
                var request = BurnRequest(token.Address, 1.234567m);
                var burnt = synchronous ? provider.BurnToken(request) : await provider.BurnTokenAsync(request);
                Assert.IsFalse(burnt.IsError, burnt.Message + " " + burnt.Exception);
                Assert.IsTrue(burnt.IsSaved); Assert.AreEqual(1, burnt.SavedCount);
                Assert.AreEqual(initial - 1469134 * unit, await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner));
                Assert.AreEqual(initial - 1234567 * unit, await token.GetFunction("totalSupply").CallAsync<BigInteger>());
                var web3 = new Web3(Host);
                var receipt = await web3.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(burnt.Result.TransactionResult);
                Assert.AreEqual(BigInteger.One, receipt.Status.Value);
                if (decimals == 6)
                {
                    var nonce = await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owner);
                    var invalid = await provider.BurnTokenAsync(BurnRequest(token.Address, 0.0000001m));
                    Assert.IsTrue(invalid.IsError); Assert.IsFalse(invalid.IsSaved);
                    Assert.IsInstanceOfType<ArgumentException>(invalid.Exception);
                    Assert.AreEqual(nonce.Value, (await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(owner)).Value);
                }
            }
        }
        finally { await provider.DeActivateProviderAsync(); }
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeNoOpErc20IsNotSuccessfulAndRetainsNativeHash(bool synchronous)
    {
        var (provider, _) = await CreateActivatedProviderAsync();
        try
        {
            var token = await CreateTokenAsync(6, true);
            var owner = new Account(PrivateKey, ChainId).Address;
            var initial = await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner);
            foreach (var burn in new[] { false, true })
            {
                var result = burn
                    ? synchronous ? provider.BurnToken(BurnRequest(token.Address, 2)) : await provider.BurnTokenAsync(BurnRequest(token.Address, 2))
                    : synchronous ? provider.SendToken(TransferRequest(token.Address, 2)) : await provider.SendTokenAsync(TransferRequest(token.Address, 2));
                Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsSaved); Assert.IsNull(result.Result);
                Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));
                var evidence = (EthereumTokenMutationReconciliationException)result.Exception;
                var receipt = await new Web3(Host).Eth.Transactions.GetTransactionReceipt.SendRequestAsync(evidence.OriginalTransactionId);
                Assert.AreEqual(BigInteger.One, receipt.Status.Value);
                Assert.AreEqual(initial, await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner));
                Assert.AreEqual(initial, await token.GetFunction("totalSupply").CallAsync<BigInteger>());
            }
        }
        finally { await provider.DeActivateProviderAsync(); }
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task NativeTokenReceiptResponseLossRetainsCommittedIdentity(bool synchronous)
    {
        var (provider, address) = await CreateActivatedProviderAsync();
        var fault = new TokenReceiptResponseLossProvider(Host, PrivateKey, ChainId, address);
        try
        {
            var token = await CreateTokenAsync(6);
            var owner = new Account(PrivateKey, ChainId).Address;
            var initial = await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner);
            var request = BurnRequest(token.Address, 2.5m);
            var result = synchronous ? fault.BurnToken(request) : await fault.BurnTokenAsync(request);
            Assert.IsTrue(result.IsError); Assert.IsFalse(result.IsSaved);
            var evidence = (EthereumTokenMutationReconciliationException)result.Exception;
            Assert.AreEqual(fault.Receipt!.TransactionHash, evidence.OriginalTransactionId);
            Assert.AreSame(fault.Cause, evidence.InnerException);
            Assert.IsTrue(NativeMutationReconciliation.IsRequired(evidence));
            Assert.AreEqual(initial - 2500000, await token.GetFunction("balanceOf").CallAsync<BigInteger>(owner));
            Assert.AreEqual(initial - 2500000, await token.GetFunction("totalSupply").CallAsync<BigInteger>());
        }
        finally { await fault.DeActivateProviderAsync(); await provider.DeActivateProviderAsync(); }
    }

    private sealed class TokenReceiptResponseLossProvider(string host, string key, BigInteger chain, string address)
        : EthereumOASIS(host, key, chain, address)
    {
        public readonly IOException Cause = new("Owned token receipt response loss after real confirmation.");
        public TransactionReceipt? Receipt;
        protected override async Task<TransactionReceipt> PollTokenReceiptAsync(Web3 web3, string hash)
        { Receipt = await base.PollTokenReceiptAsync(web3, hash); throw Cause; }
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task InvalidTokenRequestsAreTerminalBeforeActivation(bool synchronous)
    {
        var provider = new EthereumOASIS(null!, PrivateKey, ChainId, "0x" + new string('1', 40));
        foreach (var burn in new[] { false, true })
        {
            var result = burn
                ? synchronous ? provider.BurnToken(null!) : await provider.BurnTokenAsync(null!)
                : synchronous ? provider.SendToken(null!) : await provider.SendTokenAsync(null!);
            Assert.IsTrue(result.IsError);
            Assert.IsFalse(result.IsSaved);
            Assert.IsNull(result.Result);
            Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID", result.ErrorCode);
            Assert.IsInstanceOfType<ArgumentException>(result.Exception);
            Assert.IsFalse(NativeMutationReconciliation.IsRequired(result.Exception));
            Assert.IsFalse(provider.IsProviderActivated);
        }
    }
}
