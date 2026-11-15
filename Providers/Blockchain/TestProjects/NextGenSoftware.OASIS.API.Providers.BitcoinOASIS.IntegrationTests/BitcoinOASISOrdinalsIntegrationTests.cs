using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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
}
