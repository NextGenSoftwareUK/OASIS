using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests;

[TestClass]
public class CosmosNativeFeeLimitTests
{
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
