using System.Numerics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow(false, "normal")] [DataRow(true, "normal")]
    [DataRow(false, "noop")] [DataRow(true, "noop")]
    [DataRow(false, "receipt-loss")] [DataRow(true, "receipt-loss")]
    public async Task NativeMintUsesExplicitContractRecipientAmountAndNativeEvidence(bool synchronous, string scenario)
    {
        var (storage, address) = await CreateActivatedProviderAsync();
        var provider = new MintFixtureProvider(Host, PrivateKey, ChainId, address) { LoseReceipt = scenario == "receipt-loss" };
        try
        {
            foreach (var decimals in scenario == "normal" ? new byte[] { 6, 30 } : new byte[] { 6 })
            {
                var token = await CreateTokenAsync(decimals, scenario == "noop");
                var owner = new Account(PrivateKey, ChainId).Address;
                foreach (var recipient in scenario == "normal" ? new[] { owner, "0x2222222222222222222222222222222222222222" }
                    : new[] { "0x2222222222222222222222222222222222222222" })
                {
                    var before = await token.GetFunction("balanceOf").CallAsync<BigInteger>(recipient);
                    var supply = await token.GetFunction("totalSupply").CallAsync<BigInteger>();
                    var request = new MintWeb3TokenRequest { MintedByAvatarId = Guid.NewGuid(), TokenAddress = token.Address,
                        ToWalletAddress = recipient, Amount = 2.345678m };
                    var minted = synchronous ? provider.MintToken(request) : await provider.MintTokenAsync(request);
                    Assert.AreEqual(request.MintedByAvatarId, provider.LastAvatar);
                    var units = 2345678 * BigInteger.Pow(10, decimals - 6);
                    if (scenario == "normal")
                    {
                        Assert.IsFalse(minted.IsError, minted.Message + " " + minted.Exception);
                        Assert.IsTrue(minted.IsSaved); Assert.AreEqual(1, minted.SavedCount);
                        Assert.AreEqual(before + units, await token.GetFunction("balanceOf").CallAsync<BigInteger>(recipient));
                        Assert.AreEqual(supply + units, await token.GetFunction("totalSupply").CallAsync<BigInteger>());
                        var receipt = await new Web3(Host).Eth.Transactions.GetTransactionReceipt.SendRequestAsync(minted.Result.TransactionResult);
                        Assert.AreEqual(BigInteger.One, receipt.Status.Value);
                    }
                    else
                    {
                        Assert.IsTrue(minted.IsError); Assert.IsFalse(minted.IsSaved); Assert.IsNull(minted.Result);
                        var evidence = (EthereumTokenMutationReconciliationException)minted.Exception;
                        Assert.IsTrue(NativeMutationReconciliation.IsRequired(evidence));
                        var receipt = await new Web3(Host).Eth.Transactions.GetTransactionReceipt.SendRequestAsync(evidence.OriginalTransactionId);
                        Assert.AreEqual(BigInteger.One, receipt.Status.Value);
                        Assert.AreEqual(before + (scenario == "noop" ? BigInteger.Zero : units), await token.GetFunction("balanceOf").CallAsync<BigInteger>(recipient));
                        Assert.AreEqual(supply + (scenario == "noop" ? BigInteger.Zero : units), await token.GetFunction("totalSupply").CallAsync<BigInteger>());
                        if (scenario == "receipt-loss") Assert.AreSame(provider.Cause, evidence.InnerException);
                    }
                }
            }
        }
        finally { await provider.DeActivateProviderAsync(); await storage.DeActivateProviderAsync(); }
    }

    [TestMethod]
    public async Task InvalidMintAndDeniedSignerDoNotActivateOrSelectDefaults()
    {
        var provider = new MintFixtureProvider(null!, PrivateKey, ChainId, "0x" + new string('1', 40));
        foreach (var request in new MintWeb3TokenRequest[] { null!, new(), new() { MintedByAvatarId=Guid.NewGuid(),Amount=2 },
            new() { MintedByAvatarId=Guid.NewGuid(),Amount=2,TokenAddress="0x"+new string('1',40),ToWalletAddress="0x"+new string('0',40) } })
        {
            var invalid = await provider.MintTokenAsync(request);
            Assert.IsTrue(invalid.IsError); Assert.IsNull(invalid.Result);
            Assert.AreEqual("HYPERDRIVE_PAYLOAD_INVALID", invalid.ErrorCode);
            Assert.AreEqual(0, provider.KeyCalls); Assert.IsFalse(provider.IsProviderActivated);
        }
        provider.DenySigner = true;
        var denied = await provider.MintTokenAsync(new MintWeb3TokenRequest {
            MintedByAvatarId=Guid.NewGuid(),Amount=2,TokenAddress="0x"+new string('1',40),ToWalletAddress="0x"+new string('2',40) });
        Assert.IsTrue(denied.IsError); Assert.IsNull(denied.Result);
        Assert.AreEqual("OASIS_PRIVATE_KEY_ACCESS_DENIED", denied.ErrorCode);
        Assert.AreSame(provider.Cause, denied.Exception); Assert.IsFalse(provider.IsProviderActivated);
    }

    private sealed class MintFixtureProvider(string host, string key, BigInteger chain, string address)
        : EthereumOASIS(host, key, chain, address)
    {
        public Guid LastAvatar;
        public int KeyCalls;
        public bool LoseReceipt, DenySigner;
        public readonly IOException Cause = new("Owned mint signer/receipt fault.");
        protected override OASISResult<List<string>> ResolveTokenSigningKeys(Guid avatarId)
        {
            LastAvatar=avatarId;KeyCalls++;
            return DenySigner ? new OASISResult<List<string>> {IsError=true,ErrorCode="OASIS_PRIVATE_KEY_ACCESS_DENIED",Exception=Cause,Message="Owned denied signer"}
                : new OASISResult<List<string>>(new List<string> {PrivateKey});
        }
        protected override async Task<TransactionReceipt> PollTokenReceiptAsync(Web3 web3, string hash)
        {
            var receipt = await base.PollTokenReceiptAsync(web3, hash);
            if (LoseReceipt) throw Cause;
            return receipt;
        }
    }
}
