using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class CosmosNftSignerTests
{
    [TestMethod]
    public async Task MintPreservesEconomicAndSaleMetadataInSerializedRequest()
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","nft-signer.mjs"));
        try
        {
            using var provider=new CosmosBlockChainOASIS("http://127.0.0.1:1","fixture-chain","test-only-explicit","fixture-contract");
            var result=await provider.MintNFTAsync(new MintWeb3NFTRequest { Title="Economic metadata",Price=12.5m,Discount=2m,
                RoyaltyPercentage=7,IsForSale=true,SaleStartDate=DateTime.UnixEpoch,SaleEndDate=DateTime.UnixEpoch.AddDays(1),MemoText="mint memo" });
            Assert.IsFalse(result.IsError,result.Message);Assert.IsNotNull(result.Result.Web3NFT);
            Assert.AreEqual(12.5m,result.Result.Web3NFT.Price);
            Assert.AreEqual(7,result.Result.Web3NFT.RoyaltyPercentage);
            Assert.AreEqual(700u,result.Result.Web3NFT.SellerFeeBasisPoints);
            Assert.AreEqual(DateTime.UnixEpoch,result.Result.Web3NFT.SaleStartDate);
        }
        finally {Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE",previous);}
    }

    [DataTestMethod]
    [DataRow(-1, 0)]
    [DataRow(0, 101)]
    [DataRow(0, -1)]
    public async Task InvalidMintEconomicsAreRejectedBeforeSdkAccess(int price, int royalty)
    {
        using var provider=new CosmosBlockChainOASIS();
        var result=await provider.MintNFTAsync(new MintWeb3NFTRequest { Price=price,RoyaltyPercentage=royalty });
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
        StringAssert.Contains(result.Message,"royalty percentage");
    }

    [TestMethod]
    public async Task MintRejectsForeignOnChainProviderBeforeSdkAccess()
    {
        using var provider=new CosmosBlockChainOASIS();
        var result=await provider.MintNFTAsync(new MintWeb3NFTRequest { OnChainProvider=Core.Enums.ProviderType.SuiOASIS });
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
        StringAssert.Contains(result.Message,"OnChainProvider");
    }

    [DataTestMethod]
    [DataRow(null)]
    [DataRow("fixture-public")]
    [DataRow("wrong-public")]
    public async Task ExplicitSignerOverridesConfiguredKeyAndPublicMismatchFailsBeforeTransfer(string? publicKey)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","nft-signer.mjs"));
        try
        {
            using var provider=new CosmosBlockChainOASIS("http://127.0.0.1:1","fixture-chain","different-configured-key","fixture-contract");
            var result=await provider.SendNFTAsync(new SendWeb3NFTRequest { FromWalletAddress="fixture-owner",ToWalletAddress="fixture-recipient",
                FromNFTTokenAddress="fixture-contract#fixture-token",OwnerPrivateKey="test-only-explicit",OwnerPublicKey=publicKey,Amount=1 });
            Assert.IsTrue(result.IsError);
            if(publicKey=="wrong-public")
            {
                Assert.IsNull(result.Result);StringAssert.Contains(result.Message,"public key does not match");
            }
            else
            {
                Assert.IsNotNull(result.Result);Assert.AreEqual("fixture-committed-cosmos-nft",result.Result.TransactionResult);
                Assert.AreEqual("fixture-committed-cosmos-nft",result.Result.SendNFTTransactionResult);
                StringAssert.Contains(result.Message,"fixture-committed-cosmos-nft");
            }
            Assert.IsFalse(result.Message.Contains("test-only-explicit"));
        }
        finally {Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE",previous);}
    }
}
