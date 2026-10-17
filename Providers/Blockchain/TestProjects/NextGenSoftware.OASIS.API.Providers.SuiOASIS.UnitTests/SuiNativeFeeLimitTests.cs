using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests;

[TestClass,DoNotParallelize]
public class SuiNativeFeeLimitTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task ConfiguredCeilingFlowsThroughActualChildAndProductionSdkExecutor(bool reject)
    {
        var previous=Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE");
        var previousReal=Environment.GetEnvironmentVariable("OASIS_SUI_TEST_GAS_LIMIT_REAL_BRIDGE");
        Assert.IsFalse(string.IsNullOrWhiteSpace(previous));Assert.IsTrue(File.Exists(previous));
        try{
            Environment.SetEnvironmentVariable("OASIS_SUI_TEST_GAS_LIMIT_REAL_BRIDGE",previous);
            Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures/gas-limit-sdk.mjs"));
            using var provider=SuiOASIS.CreateWithNativeFeeLimits(new SuiNativeFeeLimits(reject?999UL:1000UL),rpcEndpoint:"http://127.0.0.1:1",network:"localnet");
            var method=typeof(SuiOASIS).GetMethod("InvokeSdkAsync",BindingFlags.NonPublic|BindingFlags.Instance)!;
            var pending=(Task<JsonElement>)method.Invoke(provider,new object[]{"put",new{key="fee-limit-fixture",value="not-sent-to-a-chain"},CancellationToken.None})!;
            if(reject){
                var error=await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>pending);
                StringAssert.Contains(error.Message,"gas budget exceeds");StringAssert.Contains(error.Message,"signatures=0;transportCalls=0;ceiling=999");
                Assert.IsFalse(error is SuiNativeMutationReconciliationException);
            }else{
                var result=await pending;Assert.AreEqual(1,result.GetProperty("signatures").GetInt32());
                Assert.AreEqual(1,result.GetProperty("transportCalls").GetInt32());Assert.AreEqual("1000",result.GetProperty("ceiling").GetString());
            }
        }finally{
            Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",previous);
            Environment.SetEnvironmentVariable("OASIS_SUI_TEST_GAS_LIMIT_REAL_BRIDGE",previousReal);
        }
    }
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
