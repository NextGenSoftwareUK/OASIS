using System.Reflection;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass][DoNotParallelize]
public class NearPreparedActionTests
{
    private static UsageExecutionHandle Handle(IUsageOutboxStore store,UsageEndpointPolicy? policy=null)
        =>(UsageExecutionHandle)typeof(UsageExecutionHandle).GetConstructors(BindingFlags.Instance|BindingFlags.NonPublic)
            .Single(value=>value.GetParameters().Length==5).Invoke(new object?[]{new UsageOutboxOperation{Id=Guid.NewGuid().ToString("D"),
                ExecutionOwner="controlled-near-owner",Policy=policy??new UsageEndpointPolicy()},store,new Web4UsageLedgerOptions(),null,null});
    [TestMethod]
    public async Task SubmissionRequiresOwnedServerBeforeSdkProcess()
    {
        var type=typeof(NEAROASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.NEAROASIS.NearSdkService")!;
        var service=Activator.CreateInstance(type,new object[]{"http://127.0.0.1:1","fixture","contract.near","sender.near","unused","missing-node","missing-bridge"})!;
        var method=type.GetMethod("InvokeAsync",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var task=(Task<JsonDocument>)method.Invoke(service,new object?[]{"submitNativePreparation",null,CancellationToken.None})!;
        var error=await Assert.ThrowsExceptionAsync<InvalidOperationException>(async()=>await task);
        StringAssert.Contains(error.Message,"owned preparation server");
    }
    [TestMethod]
    public async Task PublicPreparedExecutionNeedsExistingAdmissionBeforeActivation()
    {
        var provider=new NEAROASIS("http://127.0.0.1:1","fixture","fixture","contract.near","sender.near","unused");
        var result=await provider.ExecutePreparedNativeActionAsync(new NearNativeAction("transfer","receiver.near","1"),null!,"owned-original","sender.near");
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentNullException));Assert.IsFalse(result.IsSaved);
    }
    [DataTestMethod][DataRow("sender")][DataRow("budget")][DataRow("inactive")][DataRow("action")]
    public async Task InvalidAuthorityBudgetAndIntentRejectBeforeSdkOrPersistence(string mode)
    {
        var provider=new NEAROASIS("http://127.0.0.1:1","fixture","fixture","contract.near","sender.near","unused");
        typeof(NEAROASIS).GetField("_isActivated",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,mode!="inactive");
        var store=DispatchProxy.Create<IUsageOutboxStore,Owner>();var owner=(Owner)(object)store;
        var policy=new UsageEndpointPolicy{MaximumNativeFeeCostUsd=mode=="budget"?0.01m:null};
        var result=await provider.ExecutePreparedNativeActionAsync(new NearNativeAction(mode=="action"?"deleteAccount":"transfer","receiver.near","1"),
            Handle(store,policy),"owned-original",mode=="sender"?"foreign.near":"sender.near");
        Assert.IsTrue(result.IsError);Assert.AreEqual(0,owner.Calls);Assert.IsFalse(result.IsSaved);
        Assert.IsInstanceOfType(result.Exception,mode=="budget"?typeof(UsageProtocolException):mode=="inactive"?typeof(InvalidOperationException):typeof(ArgumentException));
    }
    [DataTestMethod][DataRow("accepted")][DataRow("prepared-mismatch")][DataRow("native-failed")]
    [DataRow("outcome-mismatch")][DataRow("reply-mismatch")][DataRow("transport")][DataRow("owner-failure")]
    public async Task PhysicalPipeJoinsExistingOwnerAndRequiresExactFinalOutcome(string mode)
    {
        // Physical IPC + controlled SDK response, not real signature/native-chain/Mongo durability proof.
        var names=new[]{"OASIS_NEAR_SDK_BRIDGE","OASIS_NEAR_PREPARED_CASE"};var prior=names.Select(Environment.GetEnvironmentVariable).ToArray();
        try {
            Environment.SetEnvironmentVariable(names[0],Path.Combine(AppContext.BaseDirectory,"Fixtures","prepared-action.mjs"));
            Environment.SetEnvironmentVariable(names[1],mode);
            var provider=new NEAROASIS("http://127.0.0.1:1","fixture","fixture","contract.near","sender.near","controlled-key");
            typeof(NEAROASIS).GetField("_isActivated",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(provider,true);
            var store=DispatchProxy.Create<IUsageOutboxStore,Owner>();var owner=(Owner)(object)store;owner.Fail=mode=="owner-failure";
            var handle=Handle(store);
            var request=new NearNativeAction("functionCall","contract.near","0","put","100000000000000",Convert.ToBase64String(Encoding.UTF8.GetBytes("{\"value\":9223372036854775807}")));
            using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(15));
            var result=await provider.ExecutePreparedNativeActionAsync(request,handle,"owned-original","sender.near",timeout.Token);
            Assert.AreEqual(mode=="prepared-mismatch"?0:1,owner.Calls);
            Assert.AreEqual(mode!="accepted",result.IsError,result.Exception?.ToString());
            if(mode=="accepted"){Assert.IsTrue(result.IsSaved);Assert.AreEqual("controlled-near-original",result.Result.GetProperty("transactionHash").GetString());}
            else if(mode=="native-failed")Assert.IsInstanceOfType(result.Exception,typeof(NearMutationRejectedException));
            else if(mode!="prepared-mismatch"){
                Assert.IsTrue(NativeMutationReconciliation.IsRequired(result.Exception));
                Assert.AreEqual("controlled-near-original",result.Exception.GetType().GetProperty("OriginalTransactionId")!.GetValue(result.Exception));
            }
        } finally {for(var i=0;i<names.Length;i++)Environment.SetEnvironmentVariable(names[i],prior[i]);}
    }
    public class Owner:DispatchProxy
    {
        public int Calls;public bool Fail;
        protected override object? Invoke(MethodInfo? method,object?[]? args){
            if(method?.Name!="BeginProviderAttemptAsync")throw new InvalidOperationException("Unexpected controlled owner call.");
            var attempt=(UsageProviderAttempt)args![2]!;Calls++;
            Assert.AreEqual("NEAROASIS",attempt.Provider);Assert.AreEqual("owned-original",attempt.ClientRequestId);
            Assert.AreEqual("controlled-near-original",attempt.ProviderRequestId);Assert.AreEqual("AQID",attempt.NativePreparation.SignedTransactionBase64);
            Assert.AreEqual("BAUG",attempt.NativePreparation.SignDocBase64);Assert.IsNull(attempt.NativePreparation.Signature);
            return Fail?Task.FromException(new IOException("Controlled persistence did not acknowledge")):Task.CompletedTask;
        }
    }
}
