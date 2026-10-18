using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.UnitTests;

[TestClass,DoNotParallelize]
public sealed class CosmosReviewedFeeCompositionTests
{
    private sealed class Clock:TimeProvider
    {
        public DateTimeOffset Now=new(2026,10,18,4,0,0,TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow()=>Now;
    }
    private static CosmosFeeModelReview Review(Clock clock)=>CosmosFeeModelReview.ForWasmd0703("composition-test","controlled-chain",new string('A',64),new string('B',64),
        10,100,clock.Now.AddMinutes(-1),clock.Now.AddMinutes(1),TimeSpan.FromSeconds(15),clock);
    private static CosmosDeploymentObservation Observe(Uri uri,Clock clock)=>new("controlled-chain",new string('A',64),new string('B',64),
        CosmosFeeModelReview.WasmdVersion,CosmosFeeModelReview.WasmdCommit,CosmosFeeModelReview.SdkVersion,50,clock.Now)
        {RpcEndpointSha256=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(uri.AbsoluteUri)))};
    private static CosmosNativeFeeLimits Limits()=>CosmosNativeFeeLimits.ForReviewedStandardSdkChain(new Dictionary<string,string>{{"stake","50000"}},"controlled-chain");

    [TestMethod]
    public async Task ExplicitFactoryValidatesInspectorBeforeNonActivatingRegistrationAndExpiresClosed()
    {
        var clock=new Clock();var review=Review(clock);var inspections=0;
        using var provider=await CosmosBlockChainOASIS.CreateWithReviewedFinalFeesAsync(Limits(),review,(uri,token)=>{inspections++;return Task.FromResult(Observe(uri,clock));},
            "http://127.0.0.1:1","controlled-chain");
        Assert.AreEqual(1,inspections);Assert.IsFalse(provider.IsProviderActivated);
        var reader=(IPreparedNativeFeeEvidenceReader)provider;Assert.AreEqual(NativeFeeCoverage.NetFinal,reader.DeclaredFeeCoverage);
        var readiness=new StorageAdmissionRecoveryReadiness(new[]{("CosmosBlockChainOASIS","controlled-chain")},new[]{reader});
        await readiness.StartAsync(CancellationToken.None);Assert.AreEqual(1,inspections);
        Assert.IsTrue(review.EvidenceBasis.Length<=128);StringAssert.StartsWith(review.EvidenceBasis,"wasmd0703-fixed:");
        clock.Now=review.ExpiresAtUtc;
        Assert.ThrowsException<InvalidDataException>(()=>{_=reader.DeclaredFeeCoverage;});
    }

    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task FactoryRejectsMissingOrSubstitutedEndpointBinding(bool missing)
    {
        var clock=new Clock();
        await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>CosmosBlockChainOASIS.CreateWithReviewedFinalFeesAsync(Limits(),Review(clock),
            (uri,token)=>Task.FromResult(Observe(uri,clock) with{RpcEndpointSha256=missing?null:new string('C',64)}),"http://127.0.0.1:1","controlled-chain"));
    }

    [TestMethod]
    public async Task FactoryChecksExpiryAfterInspectorReturns()
    {
        var clock=new Clock();var review=Review(clock);
        await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>CosmosBlockChainOASIS.CreateWithReviewedFinalFeesAsync(Limits(),review,(uri,token)=>{
            var observed=Observe(uri,clock);clock.Now=review.ExpiresAtUtc;return Task.FromResult(observed);
        },"http://127.0.0.1:1","controlled-chain"));
    }

    [DataTestMethod]
    [DataRow("put")] [DataRow("delete")] [DataRow("execute")] [DataRow("transfer")] [DataRow("upload")] [DataRow("instantiate")]
    public async Task EveryDeclaredMutationReinspectsBeforeSdkChildOrSigning(string operation)
    {
        var clock=new Clock();var inspectCount=0;
        using var provider=await CosmosBlockChainOASIS.CreateWithReviewedFinalFeesAsync(Limits(),Review(clock),(uri,token)=>{
            inspectCount++;var observed=Observe(uri,clock);return Task.FromResult(inspectCount==1?observed:observed with{BinarySha256=new string('C',64)});
        },"http://127.0.0.1:1","controlled-chain");
        var backend=typeof(CosmosBlockChainOASIS).GetField("_sdkBackend",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(provider)!;
        var pending=(Task<JsonElement>)backend.GetType().GetMethod("InvokeAsync",BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(backend,new object[]{operation,new{},CancellationToken.None})!;
        var failure=await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>pending);
        StringAssert.Contains(failure.Message,"deployment/height evidence");Assert.AreEqual(2,inspectCount);Assert.IsFalse(provider.IsProviderActivated);
    }
}
