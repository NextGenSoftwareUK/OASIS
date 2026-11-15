using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NBitcoin;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS.IntegrationTests;

[TestClass]
public class BitcoinOASISOrdinalsIntegrationTests
{
    [TestMethod]
    public async Task OrdRegtestReadsIndexedInscription()
    {
        var server = Environment.GetEnvironmentVariable("OASIS_BITCOIN_ORD_API");
        var inscription = Environment.GetEnvironmentVariable("OASIS_BITCOIN_ORD_INSCRIPTION");
        var owner = Environment.GetEnvironmentVariable("OASIS_BITCOIN_ORD_OWNER");
        Assert.IsTrue(Uri.TryCreate(server, UriKind.Absolute, out var endpoint) && endpoint.IsLoopback);
        Assert.IsFalse(string.IsNullOrWhiteSpace(inscription));
        Assert.IsFalse(string.IsNullOrWhiteSpace(owner));

        using var provider = new BitcoinOASIS(network: "regtest");
        var read = await provider.LoadOnChainNFTDataAsync(inscription);
        Assert.IsFalse(read.IsError, read.Message);
        Assert.AreEqual(inscription, read.Result.TokenId);
        Assert.AreEqual(inscription[..64], read.Result.MintTransactionHash);
        Assert.AreEqual(owner, read.Result.MetaData["CurrentOwnerAddress"]);
        StringAssert.StartsWith(read.Result.MetaData["ContentType"], "text/plain");

        var missing = await provider.LoadOnChainNFTDataAsync(new string('0', 64) + "i0");
        Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound, missing.ErrorCode);

        using var wrongNetwork = new BitcoinOASIS(network: "mainnet");
        var mismatch = await wrongNetwork.LoadOnChainNFTDataAsync(inscription);
        Assert.IsTrue(mismatch.IsError);
        StringAssert.Contains(mismatch.Message, "different Bitcoin network");
    }

    [TestMethod]
    public async Task OrdRegtestTransfersIndexedInscription()
    {
        var api = Environment.GetEnvironmentVariable("OASIS_BITCOIN_REGTEST_API");
        var inscription = Environment.GetEnvironmentVariable("OASIS_BITCOIN_ORD_INSCRIPTION");
        var owner = Environment.GetEnvironmentVariable("OASIS_BITCOIN_ORD_OWNER");
        var recipient = Environment.GetEnvironmentVariable("OASIS_BITCOIN_ORD_RECIPIENT");
        Assert.IsTrue(Uri.TryCreate(api, UriKind.Absolute, out var endpoint) && endpoint.IsLoopback);
        Assert.IsFalse(string.IsNullOrWhiteSpace(inscription));
        Assert.IsFalse(string.IsNullOrWhiteSpace(owner));
        Assert.IsFalse(string.IsNullOrWhiteSpace(recipient));

        // Public test vector used on the disposable regtest chain only.
        var privateScalar = new byte[32];
        privateScalar[31] = 1;
        using var signer = new Key(privateScalar);
        Assert.AreEqual(owner, signer.PubKey.GetAddress(ScriptPubKeyType.TaprootBIP86, Network.RegTest).ToString());
        using var unrelatedSigner = new Key();
        var request = new SendWeb3NFTRequest
        {
            FromNFTTokenAddress = inscription,
            FromWalletAddress = owner,
            ToWalletAddress = recipient,
            OwnerPrivateKey = unrelatedSigner.GetWif(Network.RegTest).ToString()
        };

        using var provider = new BitcoinOASIS(api, "regtest", "", null, 2);
        var denied = await provider.SendNFTAsync(request);
        Assert.IsTrue(denied.IsError);
        StringAssert.Contains(denied.Message, "must belong to the signing key");

        request.OwnerPrivateKey = signer.GetWif(Network.RegTest).ToString();
        var transferred = await provider.SendNFTAsync(request);
        Assert.IsFalse(transferred.IsError, transferred.Message);
        Assert.IsTrue(uint256.TryParse(transferred.Result.TransactionResult, out _));
        Assert.AreEqual(inscription, transferred.Result.Web3NFT.TokenId);
        Assert.AreEqual(transferred.Result.TransactionResult, transferred.Result.Web3NFT.SendNFTTransactionHash);
    }
}
