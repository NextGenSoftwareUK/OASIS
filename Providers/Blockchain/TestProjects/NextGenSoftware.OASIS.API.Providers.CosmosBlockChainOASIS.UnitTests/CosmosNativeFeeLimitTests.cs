using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class CosmosNativeFeeLimitTests
{
    [TestMethod]
    public void StandardSdkFeeReviewIsExplicitImmutableAndBoundToExactChain()
    {
        var map=new Dictionary<string,string>{{"stake","100"}};
        Assert.IsNull(new CosmosNativeFeeLimits(map).ReviewedStandardSdkChainId);
        foreach(var chain in new[]{""," ","bad chain",new string('a',129)})
            Assert.ThrowsException<ArgumentException>(()=>CosmosNativeFeeLimits.ForReviewedStandardSdkChain(map,chain));
        var reviewed=CosmosNativeFeeLimits.ForReviewedStandardSdkChain(map,"reviewed-chain");
        map["stake"]="999";
        Assert.AreEqual("100",reviewed.MaximumFeeUnitsByDenomination["stake"]);
        Assert.AreEqual("reviewed-chain",reviewed.ReviewedStandardSdkChainId);
        Assert.ThrowsException<ArgumentException>(()=>CosmosBlockChainOASIS.CreateWithNativeFeeLimits(reviewed,rpcEndpoint:"http://127.0.0.1:1",chainId:"foreign-chain"));
        using var provider=CosmosBlockChainOASIS.CreateWithNativeFeeLimits(reviewed,rpcEndpoint:"http://127.0.0.1:1",chainId:"reviewed-chain");
        Assert.IsFalse(provider.IsProviderActivated);
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task ConfiguredCeilingReachesRealSdkChildBeforePreparationAndBroadcast(bool reject)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE");
        var previousReal=Environment.GetEnvironmentVariable("OASIS_COSMOS_TEST_FEE_LIMIT_REAL_BRIDGE");
        var real=previous??Path.Combine(AppContext.BaseDirectory,"CosmosSdkBridge","bridge.mjs");
        Assert.IsTrue(File.Exists(real),"Retained production SDK bridge required.");
        try{
            Environment.SetEnvironmentVariable("OASIS_COSMOS_TEST_FEE_LIMIT_REAL_BRIDGE",real);
            Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures/fee-limit-sdk.mjs"));
            using var provider=CosmosBlockChainOASIS.CreateWithNativeFeeLimits(new(new Dictionary<string,string>{{"stake",reject?"5":"6"}}),rpcEndpoint:"http://127.0.0.1:1");
            var flags=BindingFlags.NonPublic|BindingFlags.Instance;
            var backend=typeof(CosmosBlockChainOASIS).GetField("_sdkBackend",flags)!.GetValue(provider)!;
            var pending=(Task<JsonElement>)backend.GetType().GetMethod("InvokeAsync",flags)!.Invoke(backend,new object[]{"put",new{key="fee-limit-fixture"},CancellationToken.None})!;
            if(reject){
                var error=await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>pending);
                StringAssert.Contains(error.Message,"fee and tip exceed");
                StringAssert.Contains(error.Message,"preparations=0;broadcasts=0;ceiling=5");
                Assert.IsFalse(error is CosmosNativeMutationReconciliationException);
            }else{
                var result=await pending;Assert.AreEqual(1,result.GetProperty("preparations").GetInt32());
                Assert.AreEqual(1,result.GetProperty("broadcasts").GetInt32());Assert.AreEqual("6",result.GetProperty("ceiling").GetString());
            }
        }finally{
            Environment.SetEnvironmentVariable("OASIS_COSMOS_SDK_BRIDGE",previous);
            Environment.SetEnvironmentVariable("OASIS_COSMOS_TEST_FEE_LIMIT_REAL_BRIDGE",previousReal);
        }
    }
    [DataTestMethod]
    [DataRow("")] [DataRow("0")] [DataRow("01")] [DataRow("-1")] [DataRow("1.5")] [DataRow("1e3")]
    public void ExplicitAmountsMustBeCanonicalPositiveIntegerText(string value)
        =>Assert.ThrowsException<ArgumentException>(()=>new CosmosNativeFeeLimits(new Dictionary<string,string>{{"stake",value}}));

    [TestMethod]
    public void ExplicitMapsAreBoundedAndRequired()
    {
        Assert.ThrowsException<ArgumentNullException>(()=>new CosmosNativeFeeLimits(null!));
        Assert.ThrowsException<ArgumentException>(()=>new CosmosNativeFeeLimits(new Dictionary<string,string>()));
        Assert.ThrowsException<ArgumentException>(()=>new CosmosNativeFeeLimits(Enumerable.Range(0,33).ToDictionary(i=>"coin"+i,_=>"1")));
        Assert.ThrowsException<ArgumentException>(()=>new CosmosNativeFeeLimits(new Dictionary<string,string>{{"bad denom","1"}}));
        Assert.ThrowsException<ArgumentException>(()=>new CosmosNativeFeeLimits(new Dictionary<string,string>{{"stake",new string('1',129)}}));
        Assert.ThrowsException<ArgumentNullException>(()=>CosmosBlockChainOASIS.CreateWithNativeFeeLimits(null!,rpcEndpoint:"http://127.0.0.1:1"));
        Assert.ThrowsException<ArgumentException>(()=>new CosmosBlockChainOASIS(null!));
    }
    [TestMethod]
    public void ProviderCapturesTheImmutableExplicitMapAndLeavesExistingConstructorUnconfigured()
    {
        var original=new Dictionary<string,string>{{"stake","9007199254740993"}};
        var limits=new CosmosNativeFeeLimits(original);original["stake"]="1";
        Assert.ThrowsException<NotSupportedException>(()=>((IDictionary<string,string>)limits.MaximumFeeUnitsByDenomination).Add("other","1"));
        using var configured=CosmosBlockChainOASIS.CreateWithNativeFeeLimits(limits,rpcEndpoint:"http://127.0.0.1:1");
        using var ordinary=new CosmosBlockChainOASIS("http://127.0.0.1:1");
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        object Client(CosmosBlockChainOASIS provider)=>typeof(CosmosBlockChainOASIS).GetField("_sdkBackend",flags)!.GetValue(provider)!;
        var client=Client(configured);
        var captured=(IReadOnlyDictionary<string,string>)client.GetType().GetField("_maximumFeeUnitsByDenomination",flags)!.GetValue(client)!;
        Assert.AreEqual("9007199254740993",captured["stake"]);
        Assert.IsNull(client.GetType().GetField("_maximumFeeUnitsByDenomination",flags)!.GetValue(Client(ordinary)));
        Assert.IsFalse(configured.IsProviderActivated);
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task OperationArgumentsCannotIntroduceOrReplaceConfiguredCeilings(bool configured)
    {
        using var provider=configured?CosmosBlockChainOASIS.CreateWithNativeFeeLimits(new(new Dictionary<string,string>{{"stake","1"}}),rpcEndpoint:"http://127.0.0.1:1")
            :new CosmosBlockChainOASIS("http://127.0.0.1:1");
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        var client=typeof(CosmosBlockChainOASIS).GetField("_sdkBackend",flags)!.GetValue(provider)!;
        var method=client.GetType().GetMethod("InvokeAsync",flags);
        Assert.IsNotNull(method,"The production internal SDK invocation boundary must exist.");
        var pending=(Task<JsonElement>)method.Invoke(client,new object[]{"verifyNativePreparation",new{maximumFeeUnitsByDenomination=new{stake="999"}},CancellationToken.None})!;
        var error=await Assert.ThrowsExceptionAsync<ArgumentException>(()=>pending);
        StringAssert.Contains(error.Message,"configured Cosmos fee ceilings");
    }
}
