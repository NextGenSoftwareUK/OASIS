using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class CosmosNftSignerTests
{
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
