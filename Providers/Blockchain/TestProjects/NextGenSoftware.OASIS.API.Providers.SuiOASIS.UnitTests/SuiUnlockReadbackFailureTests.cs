using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class SuiUnlockReadbackFailureTests
{
    [DataTestMethod]
    [DataRow(null,null,null,"object ID is required")]
    [DataRow(" ","",null,"object ID is required")]
    [DataRow("first","second",null,"different objects")]
    [DataRow(null,"first","second","different objects")]
    [DataRow("first",null,"second","different objects")]
    public async Task InvalidNftSelectorsFailBeforeSignerAccess(string? tokenId,string? fromAddress,string? address,string error)
    {
        using var provider=new SuiOASIS("http://127.0.0.1:1","localnet");
        var result=await provider.SendNFTAsync(new SendWeb3NFTRequest { TokenId=tokenId,FromNFTTokenAddress=fromAddress,TokenAddress=address,Amount=1 });
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);StringAssert.Contains(result.Message,error);
    }

    [TestMethod]
    public async Task CommittedMintDigestSurvivesSeparateReadbackFailure()
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","unlock-readback-failure.mjs"));
        try
        {
            using var provider=new SuiOASIS("http://127.0.0.1:1","localnet",privateKey:"test-only-secret");
            var result=await provider.MintNFTAsync(new MintWeb3NFTRequest { Title="Receipt test",NumberToMint=1 });
            Assert.IsTrue(result.IsError);Assert.IsNotNull(result.Result);
            Assert.AreEqual("fixture-committed-mint-digest",result.Result.TransactionResult);
            Assert.AreEqual("fixture-committed-mint-digest",result.Result.SendNFTTransactionResult);
            StringAssert.Contains(result.Message,"reconcile before retrying");
            Assert.IsFalse(result.Message.Contains("test-only-secret"));
        }
        finally {Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",previous);}
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task ExplicitNftSignerAndCommittedSendDigestSurviveReadbackFailure(bool agreeingSelectors)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","unlock-readback-failure.mjs"));
        try
        {
            using var provider=new SuiOASIS("http://127.0.0.1:1","localnet",privateKey:"different-configured-secret");
            var result=await provider.SendNFTAsync(new SendWeb3NFTRequest { FromNFTTokenAddress="fixture-nft",
                TokenId=agreeingSelectors ? "fixture-nft" : null,TokenAddress=agreeingSelectors ? "fixture-nft" : null,
                FromWalletAddress="fixture-owner",ToWalletAddress="fixture-recipient",OwnerPrivateKey="test-only-explicit",Amount=1 });
            Assert.IsTrue(result.IsError);Assert.IsNotNull(result.Result);
            Assert.AreEqual("fixture-committed-send-digest",result.Result.TransactionResult);
            Assert.AreEqual("fixture-committed-send-digest",result.Result.SendNFTTransactionResult);
            StringAssert.Contains(result.Message,"fixture-committed-send-digest");
            Assert.IsFalse(result.Message.Contains("test-only-explicit"));
        }
        finally {Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",previous);}
    }

    [TestMethod]
    public async Task CommittedUnlockDigestSurvivesSeparateReadbackFailure()
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","unlock-readback-failure.mjs"));
        try
        {
            using var provider=new SuiOASIS("http://127.0.0.1:1","localnet",privateKey:"test-only-secret");
            var result=await provider.UnlockNFTAsync(new UnlockWeb3NFTRequest { NFTTokenAddress="fixture-nft" });
            Assert.IsTrue(result.IsError);Assert.IsNotNull(result.Result);
            Assert.AreEqual("fixture-committed-unlock-digest",result.Result.TransactionResult);
            StringAssert.Contains(result.Message,"fixture-committed-unlock-digest");
            StringAssert.Contains(result.Message,"reconcile before retrying");
            Assert.IsFalse(result.Message.Contains("test-only-secret"));
        }
        finally {Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",previous);}
    }
}
