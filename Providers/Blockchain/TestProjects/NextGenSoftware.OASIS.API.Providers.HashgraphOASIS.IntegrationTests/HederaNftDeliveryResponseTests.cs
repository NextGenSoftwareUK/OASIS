using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass]
public class HederaNftDeliveryResponseTests
{
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
