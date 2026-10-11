using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class HederaCollectionTests
{
    [DataTestMethod]
    [DataRow(-1)]
    [DataRow(101)]
    public async Task InvalidCollectionRoyaltyFailsBeforeSdkAccess(int royalty)
    {
        var provider=new HashgraphOASIS(network:"testnet");
        var result=await provider.CreateNftCollectionAsync("0.0.1001","", "Oak","OAK",10,100000000,royaltyPercentage:royalty);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);StringAssert.Contains(result.Message,"royalty percentage");
    }

    [DataTestMethod]
    [DataRow("0.0.2001",false)]
    [DataRow("0.0.2002",true)]
    [DataRow("0.0.2003",true)]
    public async Task DeletionProcessBoundaryRetainsTargetAndOriginalIdentity(string tokenId,bool error)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","hedera-collection.mjs"));
        try
        {
            var provider=new HashgraphOASIS(network:"testnet");
            var result=await provider.DeleteTokenCollectionAsync("0.0.1001","test-only-collection-key",tokenId,100000000);
            Assert.AreEqual(error,result.IsError,result.Message);Assert.IsNotNull(result.Result);
            Assert.AreEqual(tokenId,result.Result.TokenId);
            StringAssert.StartsWith(result.Result.TransactionId,"0.0.1001@");
            Assert.AreEqual(!error,result.Result.ConsensusVerified);
            Assert.IsFalse((result.Message??"").Contains("test-only-collection-key"));
        }
        finally {Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE",previous);}
    }

    [DataTestMethod]
    [DataRow("0.0.0")]
    [DataRow("0.0.2001/1")]
    [DataRow(null)]
    public async Task InvalidCollectionDeletionFailsBeforeSdkAccess(string? tokenId)
    {
        var provider=new HashgraphOASIS(network:"testnet");
        var result=await provider.DeleteTokenCollectionAsync("0.0.1001","",tokenId!,100000000);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
    }

    [DataTestMethod]
    [DataRow("success",false)]
    [DataRow("failure",true)]
    [DataRow("mismatch",true)]
    [DataRow("missing-token",true)]
    public async Task CollectionProcessBoundaryRequiresExactVerifiedReceipt(string mode,bool error)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","hedera-collection.mjs"));
        try
        {
            var provider=new HashgraphOASIS(network:"testnet");
            var result=await provider.CreateNftCollectionAsync("0.0.1001","test-only-collection-key",mode,"OAK",9007199254740993,100000000);
            Assert.AreEqual(error,result.IsError,result.Message);Assert.IsNotNull(result.Result);
            StringAssert.StartsWith(result.Result.TransactionId,"0.0.1001@");
            Assert.AreEqual(!error,result.Result.ConsensusVerified);
            if(error)Assert.IsNull(result.Result.TokenId);
            else Assert.AreEqual("0.0.2001",result.Result.TokenId);
            if(mode=="failure")StringAssert.Contains(result.Message,result.Result.TransactionId);
            Assert.IsFalse((result.Message??"").Contains("test-only-collection-key"));
        }
        finally {Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE",previous);}
    }

    [DataTestMethod]
    [DataRow("0.0.0", "Oak", "OAK", 10L, 100L)]
    [DataRow("0.0.1001", "", "OAK", 10L, 100L)]
    [DataRow("0.0.1001", "Oak", "", 10L, 100L)]
    [DataRow("0.0.1001", "Oak", "OAK", 0L, 100L)]
    [DataRow("0.0.1001", "Oak", "OAK", 10L, 0L)]
    public async Task InvalidCollectionInputFailsBeforeSdkAccess(string payer,string name,string symbol,long supply,long fee)
    {
        var provider=new HashgraphOASIS(network:"testnet");
        var result=await provider.CreateNftCollectionAsync(payer,"",name,symbol,supply,fee);
        Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);
    }
}
