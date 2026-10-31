using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass, DoNotParallelize]
public class HederaNftSendValidationTests
{
    [TestMethod]
    public async Task ConfirmedDeliveryUsesOriginalRecipientAfterRequestMutation()
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","hedera-process-boundary.mjs"));
        try{
            var request=Valid();request.OwnerPrivateKey="test-only-delivery-key";
            var pending=new HashgraphOASIS().SendNFTAsync(request);Assert.IsFalse(pending.IsCompleted);
            request.ToWalletAddress="0.0.9999";request.FromNFTTokenAddress="0.0.9999/8";
            var result=await pending;Assert.IsFalse(result.IsError,result.Message);
            Assert.AreEqual("0.0.1002",result.Result.Web3NFT.SendToAddressAfterMinting);
            Assert.AreEqual("0.0.1234/7",result.Result.Web3NFT.NFTTokenAddress);
        }finally{Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE",previous);}
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ConfirmedStandaloneSendProjectsDeliveryHashAndRecipient(bool asynchronous)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","hedera-process-boundary.mjs"));
        try
        {
            var request=Valid(); request.OwnerPrivateKey="test-only-delivery-key";
            var provider=new HashgraphOASIS();
            var result=asynchronous?await provider.SendNFTAsync(request):provider.SendNFT(request);
            Assert.IsFalse(result.IsError,result.Message);
            Assert.IsFalse(string.IsNullOrWhiteSpace(result.Result.TransactionResult));
            Assert.AreEqual(result.Result.TransactionResult,result.Result.SendNFTTransactionResult);
            Assert.AreEqual(result.Result.SendNFTTransactionResult,result.Result.Web3NFT.SendNFTTransactionHash);
            Assert.AreEqual(request.ToWalletAddress,result.Result.Web3NFT.SendToAddressAfterMinting);
            Assert.AreEqual(request.FromNFTTokenAddress,result.Result.Web3NFT.NFTTokenAddress);
        }
        finally { Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE",previous); }
    }

    [TestMethod]
    public async Task BothEntryPointsRequireSigner()
    {
        var provider = new HashgraphOASIS();
        foreach (var result in new[] { provider.SendNFT(Valid()), await provider.SendNFTAsync(Valid()) })
        {
            Assert.IsTrue(result.IsError); Assert.IsNull(result.Result);
            StringAssert.Contains(result.Message, "signing key is required");
        }
    }

    [TestMethod]
    public async Task ContradictoryIdentityCannotBeIgnored()
    {
        var request = Valid(); request.TokenId = "8";
        var result = await new HashgraphOASIS().SendNFTAsync(request);
        Assert.IsTrue(result.IsError); StringAssert.Contains(result.Message, "identifiers must agree");
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(2)]
    public async Task SerialQuantityIsExactlyOne(int amount)
    {
        var request = Valid(); request.Amount = amount;
        var result = await new HashgraphOASIS().SendNFTAsync(request);
        Assert.IsTrue(result.IsError); StringAssert.Contains(result.Message, "Amount = 1");
    }

    [TestMethod]
    public async Task SeedPhraseCannotBeIgnored()
    {
        var request = Valid(); request.OwnerSeedPhrase = "synthetic seed";
        var result = await new HashgraphOASIS().SendNFTAsync(request);
        Assert.IsTrue(result.IsError); StringAssert.Contains(result.Message, "Recover an explicit");
    }

    private static SendWeb3NFTRequest Valid() => new()
    { FromNFTTokenAddress = "0.0.1234/7", FromWalletAddress = "0.0.1001", ToWalletAddress = "0.0.1002", Amount = 1 };
}
