using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaNftDeliveryResponseTests
{
    [TestMethod]
    public void BatchResponseRetainsEverySerialWithIndependentMetadataAndStableIds()
    {
        const string mintId = "0.0.1001@1700000000.000000001";
        var template = new Web3NFT { CollectionPublicKey = "0.0.1234", Title = "HTS batch",
            MetaData = new Dictionary<string,string> { ["park"] = "oak" } };
        var method = typeof(HashgraphOASIS).GetMethod("CreateHederaMintResponse", BindingFlags.NonPublic | BindingFlags.Static)!;
        Web3NFTTransactionResponse Build() => (Web3NFTTransactionResponse)method.Invoke(null, new object[] { template, new[] { "7", "8", "9" }, mintId })!;
        var response = Build(); var repeated = Build();
        Assert.AreEqual(3, response.Web3NFTs.Count);
        Assert.AreSame(response.Web3NFTs[0], response.Web3NFT);
        CollectionAssert.AreEqual(new[] { "0.0.1234/7", "0.0.1234/8", "0.0.1234/9" }, response.Web3NFTs.Select(nft => nft.NFTTokenAddress).ToArray());
        Assert.AreEqual(3, response.Web3NFTs.Select(nft => nft.Id).Distinct().Count());
        CollectionAssert.AreEqual(response.Web3NFTs.Select(nft => nft.Id).ToArray(), repeated.Web3NFTs.Select(nft => nft.Id).ToArray());
        response.Web3NFTs[0].MetaData["park"] = "changed";
        Assert.AreEqual("oak", response.Web3NFTs[1].MetaData["park"]);
        Assert.AreEqual("oak", template.MetaData["park"]);
        Record(response, "0.0.1001@1700000001.000000001");
        foreach (var nft in response.Web3NFTs)
        {
            Assert.AreNotEqual(Guid.Empty, nft.Id);
            Assert.AreEqual(mintId, nft.MintTransactionHash);
            Assert.AreEqual(response.SendNFTTransactionResult, nft.SendNFTTransactionHash);
            Assert.AreEqual("0.0.1002", nft.SendToAddressAfterMinting);
        }
    }
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(true,true)]
    public void PostMintFailureRetainsBothOriginalIdentitiesWithoutInventingDelivery(bool minted,bool deliveryRequested)
    {
        const string mint="0.0.1001@1700000000.000000001";const string delivery="0.0.1001@1700000001.000000001";
        var cause=new IOException("controlled post-mint failure");
        var method=typeof(HashgraphOASIS).GetMethod("RetainHederaPostMintEvidence",BindingFlags.Static|BindingFlags.NonPublic)!;
        var error=(Exception)method.Invoke(null,new object?[]{cause,minted?mint:null,deliveryRequested?delivery:null})!;
        if(!minted){Assert.AreSame(cause,error);Assert.IsFalse(Core.Helpers.NativeMutationReconciliation.IsRequired(error));return;}
        Assert.IsInstanceOfType<HederaNftPostMintReconciliationException>(error);
        var evidence=(HederaNftPostMintReconciliationException)error;
        Assert.AreEqual(mint,evidence.ConfirmedMintTransactionId);Assert.AreEqual(deliveryRequested?delivery:null,evidence.RequestedDeliveryTransactionId);
        Assert.AreEqual(deliveryRequested?delivery:mint,evidence.OriginalTransactionId);Assert.AreSame(cause,evidence.InnerException);
        Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(evidence));
    }
    [TestMethod]
    public void ConfirmedSendSurvivesOnodeResponseProjection()
    {
        const string mintId = "0.0.1001@1700000000.000000001";
        const string sendId = "0.0.1001@1700000001.000000001";
        var response = new Web3NFTTransactionResponse
        {
            TransactionResult = mintId,
            Web3NFT = new Web3NFT { MintTransactionHash = mintId, NFTTokenAddress = "0.0.1234/7" }
        };
        Record(response, sendId);
        Assert.AreEqual(sendId, response.SendNFTTransactionResult);
        // NFTManager copies this field over the provider's NFT send hash.
        response.Web3NFT.SendNFTTransactionHash = response.SendNFTTransactionResult;
        Assert.AreEqual(sendId, response.Web3NFT.SendNFTTransactionHash);
        Assert.AreEqual("0.0.1002", response.Web3NFT.SendToAddressAfterMinting);
        Assert.AreEqual(mintId, response.TransactionResult);
        Assert.AreEqual(mintId, response.Web3NFT.MintTransactionHash);
    }

    [TestMethod]
    public void MissingReceiptIdCannotProduceDeliveryMarker()
    {
        var response = new Web3NFTTransactionResponse { Web3NFT = new Web3NFT() };
        var error = Assert.ThrowsException<TargetInvocationException>(() => Record(response, ""));
        Assert.IsInstanceOfType(error.InnerException, typeof(InvalidOperationException));
        Assert.IsTrue(string.IsNullOrEmpty(response.SendNFTTransactionResult));
        Assert.IsTrue(string.IsNullOrEmpty(response.Web3NFT.SendNFTTransactionHash));
    }

    private static void Record(Web3NFTTransactionResponse response, string id)
        => typeof(HashgraphOASIS).GetMethod("RecordHederaNftDelivery", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { response, id, "0.0.1002" });
}
