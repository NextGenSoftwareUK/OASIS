using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NBitcoin;
using NBitcoin.RPC;
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

    [TestMethod]
    public async Task OrdRegtestBurnsOnlyInscribedSatoshi()
    {
        var api = Environment.GetEnvironmentVariable("OASIS_BITCOIN_REGTEST_API");
        var inscription = Environment.GetEnvironmentVariable("OASIS_BITCOIN_ORD_INSCRIPTION");
        var miner = Environment.GetEnvironmentVariable("OASIS_BITCOIN_ORD_MINER");
        var endpoint = new Uri(Environment.GetEnvironmentVariable("OASIS_BITCOIN_CORE_RPC_URL"));
        Assert.IsTrue(endpoint.IsLoopback);
        var credentials = Environment.GetEnvironmentVariable("OASIS_BITCOIN_CORE_RPC_AUTH");
        var core = new RPCClient(credentials, endpoint, Network.RegTest);

        // Public test vector used on the disposable regtest chain only.
        var privateScalar = new byte[32];
        privateScalar[31] = 2;
        using var signer = new Key(privateScalar);
        using var provider = new BitcoinOASIS(api, "regtest", "", null, 2);
        var request = new BurnWeb3NFTRequest
        {
            TokenId = inscription,
            OwnerPublicKey = "",
            OwnerPrivateKey = signer.GetWif(Network.RegTest).ToString(),
            OwnerSeedPhrase = "",
            WaitTillNFTBurnt = true,
            WaitForNFTToBurnInSeconds = 40,
            AttemptToBurnEveryXSeconds = 1
        };
        var burn = provider.BurnNFTAsync(request);
        uint256[] pool = Array.Empty<uint256>();
        for (var attempt = 0; attempt < 30 && !burn.IsCompleted; attempt++)
        {
            pool = await core.GetRawMempoolAsync();
            if (pool.Length != 0)
                break;
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        if (pool.Length == 0 && burn.IsCompleted)
        {
            var failure = await burn;
            Assert.Fail(failure.Message);
        }

        Assert.AreEqual(1, pool.Length);
        var transaction = await core.GetRawTransactionAsync(pool[0]);
        var output = transaction.Outputs.Single(output => output.ScriptPubKey.IsUnspendable);
        Assert.AreEqual(Money.Satoshis(1), output.Value);
        await core.SendCommandAsync("generatetoaddress", 1, miner);
        var burned = await burn;
        Assert.IsFalse(burned.IsError, burned.Message);
        Assert.AreEqual(pool[0].ToString(), burned.Result.TransactionResult);
        Assert.AreEqual("true", burned.Result.Web3NFT.MetaData["Burned"]);
        Assert.IsFalse(burned.Result.Web3NFT.MetaData.ContainsKey("CurrentOwnerAddress"));
    }

    [TestMethod]
    public async Task OrdRegtestMintsCommitRevealAndReadback()
    {
        var api = Environment.GetEnvironmentVariable("OASIS_BITCOIN_REGTEST_API");
        var recipient = Environment.GetEnvironmentVariable("OASIS_BITCOIN_ORD_RECIPIENT");
        var miner = Environment.GetEnvironmentVariable("OASIS_BITCOIN_ORD_MINER");
        var endpoint = new Uri(Environment.GetEnvironmentVariable("OASIS_BITCOIN_CORE_RPC_URL"));
        Assert.IsTrue(endpoint.IsLoopback);
        var credentials = Environment.GetEnvironmentVariable("OASIS_BITCOIN_CORE_RPC_AUTH");
        var core = new RPCClient(credentials, endpoint, Network.RegTest);

        var privateScalar = new byte[32];
        privateScalar[31] = 1;
        using var signer = new Key(privateScalar);
        using var provider = new BitcoinOASIS(api, "regtest", signer.GetWif(Network.RegTest).ToString(), null, 2);
        var request = new MintWeb3NFTRequest
        {
            Title = "OASIS Ordinals mint acceptance",
            Description = "Commit and reveal on one Bitcoin Core chain",
            SendToAddressAfterMinting = recipient,
            WaitTillNFTMinted = true,
            WaitForNFTToMintInSeconds = 40,
            AttemptToMintEveryXSeconds = 1
        };
        var mint = provider.MintNFTAsync(request);
        uint256[] pool = Array.Empty<uint256>();
        for (var attempt = 0; attempt < 30 && !mint.IsCompleted; attempt++)
        {
            pool = await core.GetRawMempoolAsync();
            if (pool.Length == 2)
                break;
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        if (pool.Length != 2 && mint.IsCompleted)
        {
            var failure = await mint;
            Assert.Fail(failure.Message);
        }

        Assert.AreEqual(2, pool.Length);
        var first = await core.GetRawTransactionAsync(pool[0]);
        var second = await core.GetRawTransactionAsync(pool[1]);
        var reveal = first.Inputs[0].PrevOut.Hash == second.GetHash() ? first : second;
        var commit = ReferenceEquals(reveal, first) ? second : first;
        Assert.AreEqual(commit.GetHash(), reveal.Inputs[0].PrevOut.Hash);
        Assert.AreEqual(Money.Satoshis(10_000), reveal.Outputs.Single().Value);
        Assert.AreEqual(BitcoinAddress.Create(recipient, Network.RegTest).ScriptPubKey, reveal.Outputs[0].ScriptPubKey);
        await core.SendCommandAsync("generatetoaddress", 1, miner);
        var minted = await mint;
        Assert.IsFalse(minted.IsError, minted.Message);
        Assert.AreEqual(reveal.GetHash().ToString(), minted.Result.TransactionResult);
        Assert.AreEqual(reveal.GetHash() + "i0", minted.Result.Web3NFT.TokenId);
        Assert.AreEqual(commit.GetHash().ToString(), minted.Result.Web3NFT.MetaData["CommitTransactionId"]);
        Assert.AreEqual(recipient, minted.Result.Web3NFT.MetaData["CurrentOwnerAddress"]);

        using var reader = new HttpClient();
        using var body = JsonDocument.Parse(await reader.GetStringAsync(minted.Result.Web3NFT.MetaData["ContentUrl"]));
        Assert.AreEqual(request.Title, body.RootElement.GetProperty("title").GetString());
        Assert.AreEqual(request.Description, body.RootElement.GetProperty("description").GetString());

        var tip = (await core.GetBestBlockHashAsync()).ToString();
        for (var attempt = 0; attempt < 30; attempt++)
        {
            if ((await reader.GetStringAsync(api + "/blocks/tip/hash")).Trim() == tip)
                break;
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        var poolAddress = signer.GetAddress(ScriptPubKeyType.TaprootBIP86, Network.RegTest).ToString();
        Assert.AreEqual(poolAddress, Environment.GetEnvironmentVariable("OASIS_BITCOIN_BRIDGE_POOL_ADDRESS"));
        var identifier = minted.Result.Web3NFT.TokenId;
        var denied = await provider.WithdrawNFTAsync(identifier, identifier, recipient, signer.GetWif(Network.RegTest).ToString());
        Assert.IsTrue(denied.IsError);
        StringAssert.Contains(denied.Message, "must belong to the signing key");

        privateScalar[31] = 2;
        using var ownerSigner = new Key(privateScalar);
        var custody = await provider.WithdrawNFTAsync(identifier, identifier, recipient, ownerSigner.GetWif(Network.RegTest).ToString());
        Assert.IsFalse(custody.IsError, custody.Message);
        var locked = await core.GetRawTransactionAsync(uint256.Parse(custody.Result.TransactionId));
        Assert.AreEqual(reveal.GetHash(), locked.Inputs[0].PrevOut.Hash);
        Assert.AreEqual(BitcoinAddress.Create(poolAddress, Network.RegTest).ScriptPubKey, locked.Outputs[0].ScriptPubKey);
        Assert.AreEqual(reveal.Outputs[0].Value, locked.Outputs[0].Value);
        await core.SendCommandAsync("generatetoaddress", 1, miner);

        for (var attempt = 0; attempt < 30; attempt++)
        {
            var lookup = await provider.LoadOnChainNFTDataAsync(identifier);
            if (!lookup.IsError && lookup.Result.MetaData.TryGetValue("CurrentOwnerAddress", out var owner) && owner == poolAddress)
            {
                Assert.AreEqual(custody.Result.TransactionId + ":0:0", lookup.Result.MetaData["Satpoint"]);
                return;
            }
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        Assert.Fail("The ord index did not confirm the inscription under bridge custody.");
    }
}
