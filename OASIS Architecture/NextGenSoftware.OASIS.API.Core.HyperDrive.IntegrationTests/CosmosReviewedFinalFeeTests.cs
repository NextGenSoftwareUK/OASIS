using System.Diagnostics;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class CosmosReviewedFinalFeeTests(Xunit.Abstractions.ITestOutputHelper output)
{
    private const string BinaryHash="FFDBE57FFE7CCA974AC9BF6A9E800B11B7C037FBC7650BFAF185A828D18217A4";
    private const string GenesisHash="56584C2F943A7FFE6FB8A135FE5F6BA17144484F75949B16AFEE3F096EF3D03C";
    internal static async Task<(CosmosBlockChainOASIS Provider,CosmosFeeModelReview Review,CosmosDeploymentObservation Observation)> CreateReviewedProviderAsync(Guid id,CancellationToken token,string maximumFeeUnits="50000")
    {
        var endpoint=new Uri("http://127.0.0.1:26657/");var observed=await InspectAsync(endpoint,token);
        Assert.Equal(BinaryHash,observed.BinarySha256);Assert.Equal(GenesisHash,observed.GenesisSha256);
        var now=DateTimeOffset.UtcNow;
        var review=CosmosFeeModelReview.ForWasmd0703("retained-local-"+id.ToString("N"),"oasis-cosmos-local",GenesisHash,BinaryHash,
            observed.CommittedHeight,observed.CommittedHeight+128,now.AddSeconds(-1),now.AddHours(1),TimeSpan.FromSeconds(30));
        var provider=await CosmosBlockChainOASIS.CreateWithReviewedFinalFeesAsync(
            CosmosNativeFeeLimits.ForReviewedStandardSdkChain(new Dictionary<string,string>{{"stake",maximumFeeUnits}},"oasis-cosmos-local"),review,InspectAsync,
            endpoint.AbsoluteUri,"oasis-cosmos-local",Environment.GetEnvironmentVariable("OASIS_COSMOS_TEST_KEY")!,Environment.GetEnvironmentVariable("OASIS_COSMOS_TEST_CONTRACT")!,
            storageEncryptionKeyResolver:()=>SHA256.HashData(Encoding.UTF8.GetBytes("oasis-cosmos-local-storage-fixture-v1")),token:token);
        return(provider,review,observed);
    }
    internal static async Task<CosmosDeploymentObservation> InspectAsync(Uri endpoint,CancellationToken token)
    {
        Assert.Equal("http://127.0.0.1:26657/",endpoint.AbsoluteUri);
        var start=new ProcessStartInfo("wsl.exe"){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
        foreach(var arg in new[]{"-d","Ubuntu","-u","root","--","bash","/mnt/c/Source/OASIS-hyperdrive-v2-gaps/Scripts/TestHosts/inspect-owned-cosmos-fee-deployment.sh"})start.ArgumentList.Add(arg);
        using var process=Process.Start(start)!;
        var stdout=process.StandardOutput.ReadToEndAsync(token);var stderr=process.StandardError.ReadToEndAsync(token);
        await process.WaitForExitAsync(token).WaitAsync(TimeSpan.FromSeconds(30),token);
        var text=await stdout;var error=await stderr;Assert.True(process.ExitCode==0,"Pinned running deployment inspection failed: "+error);
        Assert.InRange(text.Length,1,256*1024);
        string Field(string key)=>text.Split('\n').Single(line=>line.StartsWith(key+":",StringComparison.Ordinal))[(key.Length+1)..].Trim();
        using var http=new HttpClient{Timeout=TimeSpan.FromSeconds(3)};
        using var status=JsonDocument.Parse(await http.GetStringAsync(new Uri(endpoint,"status"),token));
        var result=status.RootElement.GetProperty("result");Assert.Equal("oasis-cosmos-local",result.GetProperty("node_info").GetProperty("network").GetString());
        Assert.False(result.GetProperty("sync_info").GetProperty("catching_up").GetBoolean());
        return new("oasis-cosmos-local",Field("genesis_sha256").ToUpperInvariant(),Field("binary_sha256").ToUpperInvariant(),
            Field("version"),Field("commit"),Field("cosmos_sdk_version"),long.Parse(result.GetProperty("sync_info").GetProperty("latest_block_height").GetString()!,System.Globalization.CultureInfo.InvariantCulture),DateTimeOffset.UtcNow)
            {RpcEndpointSha256=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(endpoint.AbsoluteUri)))};
    }

    [Fact]
    public async Task ActualReviewedDeploymentRecoversSuccessfulAndChargedFailedOriginalsWithoutReplay()
    {
        var root=Environment.GetEnvironmentVariable("OASIS_HYPERDRIVE_TEST_ROOT")!;
        var journal=Path.Combine(root,"reviewed-final-fee-pending.json");Assert.False(File.Exists(journal));
        string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        const string oldHash="9B5A15110B0F21FEF8FCA6D2CE215859EEBC8944E9F1F83733CACE90892DEA81";
        Assert.Equal(oldHash,Hash(Path.Combine(root,"replication-pending.json")));
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(240));var token=deadline.Token;
        var id=Guid.NewGuid();var deployment=await CreateReviewedProviderAsync(id,token);
        using var cosmos=deployment.Provider;var review=deployment.Review;var observed=deployment.Observation;
        Assert.False(cosmos.IsProviderActivated);
        var reader=new RegisteredNativeFeeReader(ProviderType.CosmosBlockChainOASIS,"oasis-cosmos-local",new IOASISProvider[]{cosmos});
        await new StorageAdmissionRecoveryReadiness(new[]{("CosmosBlockChainOASIS","oasis-cosmos-local")},new[]{reader}).StartAsync(token);
        var backend=typeof(CosmosBlockChainOASIS).GetField("_sdkBackend",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(cosmos)!;
        var invoke=backend.GetType().GetMethod("InvokeAsync",BindingFlags.NonPublic|BindingFlags.Instance)!;
        Task<JsonElement> Invoke(string operation,object args)=>(Task<JsonElement>)invoke.Invoke(backend,new[]{(object)operation,args,token})!;
        var identity=await Invoke("restoreKey",new{});var sender=identity.GetProperty("address").GetString()!;
        async Task<BigInteger> Balance()=>BigInteger.Parse((await Invoke("balance",new{walletAddress=sender,denom="stake"})).GetProperty("amount").GetString()!,System.Globalization.CultureInfo.InvariantCulture);
        var before=await Balance();
        var store=new MongoUsageOutboxStore(connection,"usage_cosmos_independent_routing");
        var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new CosmosIndependentRoutingTests.TestAuthority(user));
        var authority=new Web4SubscriptionAuthorizationClient(http,"https://offline-authority.invalid","WEB6","test-only-key");
        var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6"}).BeginAsync("test-only",user,id.ToString("D"),new string('f',64),
            "POST /reviewed-final-fee-fixture",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1,EstimatedCostUsd=.2m,MaximumNativeFeeCostUsd=.2m},token);
        var quoteAt=DateTime.UtcNow.AddSeconds(-1);
        execution=execution.WithNativePrices(new[]{new NativeFeePriceQuote(Guid.Parse(execution.OperationId),"CosmosBlockChainOASIS","oasis-cosmos-local","stake","local-test-stake",0,.000001m,
            "synthetic-local-price","fixture-v1","fixture-v1",quoteAt,quoteAt,quoteAt,quoteAt.AddMinutes(5))});
        using var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.Read);var complete=false;
        try{
            await JsonSerializer.SerializeAsync(file,new{holonId=id,operationId=execution.OperationId,database="usage_cosmos_independent_routing",review,
                recovery="Inspect original signed attempts and fees; never replay a failed reviewed-fee fixture."},cancellationToken:token);file.Flush(true);
            await cosmos.ExecutePreparedStorageAsync(async()=>{
                var saved=await cosmos.SaveHolonAsync(new Holon{Id=id,Name="owned-reviewed-fee-fixture"},saveChildren:false);
                Assert.False(saved.IsError,saved.Message);
                var deleted=await cosmos.DeleteHolonAsync(id);Assert.False(deleted.IsError,deleted.Message);return true;
            },execution,"reviewed-storage",sender,token);
            Assert.Null((await cosmos.LoadHolonAsync(id,loadChildren:false)).Result);
            var beforeFailure=await Balance();
            var failure=await Assert.ThrowsAsync<CosmosNativeMutationReconciliationException>(()=>cosmos.ExecutePreparedNativeAsync("execute",
                new{message=new{deliberately_invalid_owned_fee_probe=new{}},fee=new{amount=new[]{new{denom="stake",amount="10000"}},gas="400000"}},
                execution,"reviewed-charged-failure","oasis-cosmos-local",sender,token));
            Assert.Equal(false,failure.ExecutionSucceeded);Assert.NotNull(failure.OriginalTransactionId);
            Assert.Equal(new BigInteger(10000),beforeFailure-await Balance());
            var operation=await store.GetAsync(execution.OperationId,token);Assert.Equal(3,operation.ProviderAttempts.Count);
            var evidence=new List<PreparedNativeFeeEvidence>();
            foreach(var attempt in operation.ProviderAttempts){
                var fee=await reader.ReadPreparedNativeFeeEvidenceAsync(attempt,token);Assert.False(fee.IsError,fee.Message);
                Assert.Equal(NativeFeeCoverage.NetFinal,fee.Result.Coverage);Assert.Equal(review.EvidenceBasis,fee.Result.Basis);Assert.NotNull(fee.Result.Audit);evidence.Add(fee.Result);
            }
            Assert.Equal(2,evidence.Count(item=>item.NativeExecutionSucceeded));Assert.Equal(1,evidence.Count(item=>!item.NativeExecutionSucceeded));
            Assert.Contains(evidence,item=>item.ProviderRequestId==failure.OriginalTransactionId&&!item.NativeExecutionSucceeded);
            var net=evidence.SelectMany(item=>item.Components).Aggregate(BigInteger.Zero,(sum,coin)=>{Assert.Equal("stake",coin.Denomination);return sum+BigInteger.Parse(coin.NetUnits,System.Globalization.CultureInfo.InvariantCulture);});
            Assert.Equal(net,before-await Balance());
            var resolver=new PreparedNativeUsageRecoveryResolver(store,new[]{reader});Assert.True(resolver.CanResolve(operation));
            Assert.Null(await resolver.ResolveAsync(operation,token));
            var recovered=await store.GetAsync(execution.OperationId,token);Assert.Equal(3,recovered.Receipts.Count);Assert.Null(recovered.ObservedOutcome);Assert.Null(recovered.Settlement);
            var reloaded=await new MongoUsageOutboxStore(connection,"usage_cosmos_independent_routing").GetAsync(execution.OperationId,token);
            foreach(var receipt in reloaded.Receipts){
                Assert.NotNull(receipt.NativeFeeAudit);receipt.NativeFeeAudit.Validate(receipt.Provider,receipt.ProviderRequestId,"oasis-cosmos-local");
                using var audit=JsonDocument.Parse(receipt.NativeFeeAudit.PayloadJson);
                Assert.Equal(review.EvidenceBasis,audit.RootElement.GetProperty("Review").GetProperty("EvidenceBasis").GetString());
                Assert.Equal(BinaryHash,audit.RootElement.GetProperty("Deployment").GetProperty("BinarySha256").GetString());
                Assert.Equal(GenesisHash,audit.RootElement.GetProperty("Deployment").GetProperty("GenesisSha256").GetString());
                Assert.InRange(audit.RootElement.GetProperty("ReceiptHeight").GetInt64(),review.FirstHeight,review.LastHeight);
                var expectedSuccess=receipt.ProviderRequestId!=failure.OriginalTransactionId;
                Assert.Equal(expectedSuccess,audit.RootElement.GetProperty("NativeExecutionSucceeded").GetBoolean());
                var nativeCode=audit.RootElement.GetProperty("NativeExecutionCode").GetUInt32();
                if(expectedSuccess)Assert.Equal(0u,nativeCode);else Assert.NotEqual(0u,nativeCode);
            }
            Assert.False(resolver.CanResolve(reloaded));Assert.Null(await resolver.ResolveAsync(reloaded,token));
            Assert.Equal(3,(await store.GetAsync(execution.OperationId,token)).Receipts.Count);
            Assert.Equal(oldHash,Hash(Path.Combine(root,"replication-pending.json")));
            var report=JsonSerializer.Serialize(new{review,initialObservation=observed,operation=recovered,originalFees=evidence,
                observedWalletDebit=net.ToString(),chargedFailedOriginal=failure.OriginalTransactionId,chargedFailureDebit="10000",logicalOutcomeResolved=false,hostedAcceptance=false});
            await File.WriteAllTextAsync(Path.Combine(root,"reviewed-final-fee-current.json"),report,token);output.WriteLine(report);complete=true;
        }finally{
            if(complete){file.Dispose();File.Delete(journal);}else output.WriteLine("Preserved reviewed fee journal/outbox: "+journal);
        }
    }
}
