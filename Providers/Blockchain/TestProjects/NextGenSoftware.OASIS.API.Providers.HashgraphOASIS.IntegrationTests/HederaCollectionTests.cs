using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class HederaCollectionTests
{
    [TestMethod]
    public async Task MalformedUnicodeCannotBeSilentlyReplacedBeforeSubmission()
    {
        var provider=new HashgraphOASIS(network:"testnet");
        foreach(var malformed in new[] {new string((char)0xd800,1),new string((char)0xdfff,1)})
        {
            var created=await provider.CreateNftCollectionAsync("0.0.1001","",malformed,"OAK",10,100000000);
            Assert.IsTrue(created.IsError);Assert.IsNull(created.Result);
            Assert.IsInstanceOfType<System.Text.EncoderFallbackException>(created.Exception);
            var updated=await provider.UpdateNftMetadataAsync("0.0.1001","","0.0.2001",new long[] {1},malformed,100000000);
            Assert.IsTrue(updated.IsError);Assert.IsNull(updated.Result);
            Assert.IsInstanceOfType<System.Text.EncoderFallbackException>(updated.Exception);
        }
    }

    [DataTestMethod]
    [DataRow("0.0.2001",false)]
    [DataRow("0.0.2002",true)]
    [DataRow("0.0.2003",true)]
    public async Task MetadataUpdateProcessBoundaryPreservesExactSerialAndReceipt(string tokenId,bool error)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","hedera-collection.mjs"));
        try
        {
            var provider=new HashgraphOASIS(network:"testnet");
            var result=await provider.UpdateNftMetadataAsync("0.0.1001","test-only-collection-key",tokenId,new long[] {9007199254740993},"Oak 🌳",100000000);
            Assert.AreEqual(error,result.IsError,result.Message);Assert.IsNotNull(result.Result);
            Assert.AreEqual(tokenId,result.Result.TokenId);Assert.AreEqual(!error,result.Result.ConsensusVerified);
            StringAssert.StartsWith(result.Result.TransactionId,"0.0.1001@");
            Assert.IsFalse((result.Message??"").Contains("test-only-collection-key"));
        }
        finally {Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE",previous);}
    }

    [TestMethod]
    public async Task InvalidMetadataUpdateFailsBeforeSdkAccess()
    {
        var provider=new HashgraphOASIS(network:"testnet");
        foreach(var serials in new[] {Array.Empty<long>(),new long[] {0},new long[] {1,1},new long[11]})
        {
            var result=await provider.UpdateNftMetadataAsync("0.0.1001","","0.0.2001",serials,"Oak",100000000);
            Assert.IsTrue(result.IsError);Assert.IsNull(result.Result);StringAssert.Contains(result.Message,"serials");
        }
        var tooLarge=await provider.UpdateNftMetadataAsync("0.0.1001","","0.0.2001",new long[] {1},new string('x',101),100000000);
        Assert.IsTrue(tooLarge.IsError);Assert.IsNull(tooLarge.Result);StringAssert.Contains(tooLarge.Message,"UTF-8");
    }

    [DataTestMethod]
    [DataRow(0)]
    [DataRow(7)]
    [DataRow(100)]
    public async Task CollectionRoyaltyIsForwardedExactlyThroughProcessBoundary(int royalty)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE");
        Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures","hedera-collection.mjs"));
        try
        {
            var provider=new HashgraphOASIS(network:"testnet");
            var result=await provider.CreateNftCollectionAsync("0.0.1001","test-only-collection-key","royalty-"+royalty,"OAK",
                9007199254740993,100000000,royaltyPercentage:royalty);
            Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.Result.ConsensusVerified);
        }
        finally {Environment.SetEnvironmentVariable("OASIS_HEDERA_SDK_BRIDGE",previous);}
    }

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
