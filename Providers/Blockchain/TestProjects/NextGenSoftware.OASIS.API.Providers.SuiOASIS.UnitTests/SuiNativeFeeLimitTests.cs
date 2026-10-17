using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests;

[TestClass]
public class SuiNativeFeeLimitTests
{
    [TestMethod]
    public void ExplicitLimitsRejectMissingOrZeroBeforeStorageSecrets()
    {
        var secrets=0;
        Assert.ThrowsException<ArgumentNullException>(()=>SuiOASIS.CreateWithNativeFeeLimits(null!,rpcEndpoint:"http://127.0.0.1:1",storageEncryptionKeyResolver:()=>{secrets++;return new byte[32];}));
        Assert.ThrowsException<ArgumentOutOfRangeException>(()=>SuiOASIS.CreateWithNativeFeeLimits(new SuiNativeFeeLimits(0),rpcEndpoint:"http://127.0.0.1:1",storageEncryptionKeyResolver:()=>{secrets++;return new byte[32];}));
        Assert.ThrowsException<ArgumentException>(()=>new SuiOASIS(null!));
        Assert.AreEqual(0,secrets);
    }
    [TestMethod]
    public void ExplicitLimitsAreCapturedWithoutChangingTheExistingConstructor()
    {
        var original=new SuiNativeFeeLimits(1234);
        using var configured=SuiOASIS.CreateWithNativeFeeLimits(original,rpcEndpoint:"http://127.0.0.1:1");
        using var ordinary=new SuiOASIS("http://127.0.0.1:1");
        original=original with{MaximumGasBudgetMist=9999};
        var flags=BindingFlags.NonPublic|BindingFlags.Instance;
        object Client(SuiOASIS provider)=>typeof(SuiOASIS).GetField("_sdkClient",flags)!.GetValue(provider)!;
        var selected=Client(configured);var unconfigured=Client(ordinary);
        Assert.AreEqual("1234",selected.GetType().GetField("_maximumGasBudgetMist",flags)!.GetValue(selected));
        Assert.IsNull(unconfigured.GetType().GetField("_maximumGasBudgetMist",flags)!.GetValue(unconfigured));
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task InvocationArgumentsCannotSupplyOrReplaceTheProviderCeiling(bool configured)
    {
        using var provider=configured?SuiOASIS.CreateWithNativeFeeLimits(new SuiNativeFeeLimits(1000),rpcEndpoint:"http://127.0.0.1:1"):new SuiOASIS("http://127.0.0.1:1");
        var method=typeof(SuiOASIS).GetMethod("InvokeSdkAsync",BindingFlags.NonPublic|BindingFlags.Instance)!;
        var pending=(Task<JsonElement>)method.Invoke(provider,new object[]{"verifyNativePreparation",new{maximumGasBudgetMist="18446744073709551615"},CancellationToken.None})!;
        var failure=await Assert.ThrowsExceptionAsync<ArgumentException>(()=>pending);
        StringAssert.Contains(failure.Message,"configured gas budget ceiling");
    }
}
