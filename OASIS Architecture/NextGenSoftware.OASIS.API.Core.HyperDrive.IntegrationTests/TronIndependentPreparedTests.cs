using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.TRONOASIS;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using TronNet;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed class TronIndependentPreparedTests(Xunit.Abstractions.ITestOutputHelper output)
{
    [Fact]
    public async Task IndependentPrivateKeyRetainsOriginalPermissionAndSolidifiedFees()
    {
        Assert.Equal("ef2843babd111bde2709159e6eaacf2350977cccee738d62d1bcbd8833c46a12",Environment.GetEnvironmentVariable("OASIS_TRON_TEST_OWNED_CONTAINER"));
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory!=null&&!File.Exists(Path.Combine(directory.FullName,"Scripts/TestHosts/Use-ReusableTronRuntime.ps1")))directory=directory.Parent;
        Assert.NotNull(directory);var root=Path.Combine(directory!.FullName,"TestResults/HyperDriveTron");
        using var lease=new FileStream(Path.Combine(root,"store.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        string Hash(string path)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
        const string oldHash="9A6B6E580DB63E5F20A91D9A345430FA72670CC041B278E3B44E00BD0FDEBAEE";
        var oldFence=Path.Combine(root,"pending-native-outcome.json");Assert.Equal(oldHash,Hash(oldFence));
        var journal=Path.Combine(root,"independent-prepared-native-pending.json");Assert.False(File.Exists(journal));
        const string genesis="0000000000000000c93baa76a4a508f798a96f59156d9eb17ecede8ec845df2f";
        var contract=File.ReadAllText(Path.Combine(directory.FullName,"TestResults/TronRuntime/local-store-address.txt")).Trim();
        var signingKey=new string('0',63)+"1"; // Existing public local development identity; never use on public networks.
        var owner=new TronECKey(signingKey,TronNetwork.MainNet).GetPublicAddress();
        await using var runtime=new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),new Uri("http://127.0.0.1:50052"),genesis,contractFeeLimitSun:100000000);
        using var privateStorage=new TronPrivateStorageBackend(runtime.NativeTransfers,owner,contract,()=>signingKey,
            ()=>SHA256.HashData(Encoding.UTF8.GetBytes("public reusable local TRON encryption fixture")));
        var provider=new TRONOASIS(rpcEndpoint:"http://127.0.0.1:9090",nativeTransfers:runtime.NativeTransfers,storageBackend:privateStorage);
        var activated=await provider.ActivateProviderAsync();Assert.False(activated.IsError,activated.Message);
        var backend=(IKeyValueBackend)typeof(TRONOASIS).GetProperty("Backend",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!;
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        const string database="usage_tron_independent_prepared";
        var mongo=new MongoClient(connection);using var deadline=new CancellationTokenSource(TimeSpan.FromMinutes(7));var token=deadline.Token;
        using(var names=await mongo.ListDatabaseNamesAsync(cancellationToken:token))Assert.DoesNotContain(database,await names.ToListAsync(token));
        var key="oasis/independent-prepared-native/"+Guid.NewGuid().ToString("N");Assert.Null(await backend.GetAsync(key,token));
        using var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.Read);var complete=false;
        var store=new MongoUsageOutboxStore(connection,database);string? operationId=null;
        try{
            await store.InitializeAsync(token);var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new TestAuthority(user));
            var authority=new Web4SubscriptionAuthorizationClient(http,"https://offline-authority.invalid","WEB6","test-only-key");
            var executor=new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6"});
            async Task<UsageExecutionHandle> Begin(string suffix,bool reject){
                var handle=await executor.BeginAsync("test-only",user,key+suffix,new string('a',64),"POST /isolated-tron-native",
                    new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1,EstimatedCostUsd=1000m,MaximumProviderAttempts=2,MaximumNativeFeeCostUsd=reject?0m:null},token);
                var time=DateTime.UtcNow.AddSeconds(-1);
                return handle.WithNativePrices(new[]{new NativeFeePriceQuote(Guid.Parse(handle.OperationId),"TRONOASIS",genesis,"SUN","tron",6,1m,
                    "synthetic-local-price","fixture-v1","fixture-v1",time,time,time,time.AddMinutes(10))});
            }
            var execution=await Begin("-own-local-funds",false);var deniedExecution=await Begin("-unsupported-total-budget",true);operationId=execution.OperationId;
            await JsonSerializer.SerializeAsync(file,new{database,key,genesis,contract,owner,operationId,rejectedOperationId=deniedExecution.OperationId,
                encryptionKeyLabel="public reusable local TRON encryption fixture",preservedOriginalFenceSha256=oldHash,
                totalNativeBudgetConfigured=false,recovery="Read exact retained signed attempts/receipts; do not replay unknown transactions or alter pending-native-outcome.json."},cancellationToken:token);file.Flush(true);
            var rejected=await Assert.ThrowsAsync<UsageProtocolException>(()=>provider.ExecutePreparedStorageAsync(async()=>{
                await backend.PutAsync(key,"must-not-submit",token);return true;
            },deniedExecution,"unsupported-budget",owner,token));
            Assert.Equal("NATIVE_FEE_MAXIMUM_REQUIRED",rejected.Code);
            Assert.Empty((await store.GetAsync(deniedExecution.OperationId,token)).ProviderAttempts);Assert.Null(await backend.GetAsync(key,token));
            await provider.ExecutePreparedStorageAsync(async()=>{await backend.PutAsync(key,"isolated prepared TRON fixture",token);return true;},execution,"write",owner,token);
            Assert.Equal("isolated prepared TRON fixture",await backend.GetAsync(key,token));
            await provider.ExecutePreparedStorageAsync(async()=>{await backend.DeleteAsync(key,token);return true;},execution,"cleanup",owner,token);
            Assert.Null(await backend.GetAsync(key,token));
            var operation=await new MongoUsageOutboxStore(connection,database).GetAsync(operationId,token);Assert.Equal(2,operation.ProviderAttempts.Count);
            var originals=new List<object>();
            foreach(var attempt in operation.ProviderAttempts){
                Assert.Equal("tron-account-permissions-v1",attempt.NativePreparation.SigningAuthority.Scheme);Assert.Null(attempt.AcknowledgedAtUtc);
                var fee=await provider.GetPreparedNativeFeeReceiptAsync(attempt,token);Assert.False(fee.IsError,fee.Message);
                Assert.Equal(attempt.ProviderRequestId,fee.Result.GetProperty("transactionHash").GetString());Assert.Equal(owner,fee.Result.GetProperty("payer").GetString());
                Assert.Equal(genesis,fee.Result.GetProperty("chainGenesisId").GetString());Assert.True(fee.Result.GetProperty("success").GetBoolean());
                originals.Add(new{attempt,solidifiedFee=fee.Result});
            }
            var resolver=new PreparedNativeUsageRecoveryResolver(store,new[]{(IPreparedNativeFeeEvidenceReader)provider});
            Assert.Null(await resolver.ResolveAsync(operation,token));var measured=await store.GetAsync(operationId,token);Assert.Equal(2,measured.Receipts.Count);
            Assert.Null(measured.ObservedOutcome);Assert.Null(measured.Settlement);
            output.WriteLine(JsonSerializer.Serialize(new{key,originals,measuredReceipts=measured.Receipts,totalNativeBudgetConfigured=false,hostedSettlementVerified=false}));
            Assert.Equal(oldHash,Hash(oldFence));complete=true;
        }catch(Exception failure){
            output.WriteLine("Original independent failure: "+failure.Message);
            if(operationId!=null)try{using var evidenceDeadline=new CancellationTokenSource(TimeSpan.FromSeconds(5));
                output.WriteLine(JsonSerializer.Serialize(new{retainedOriginal=await store.GetAsync(operationId,evidenceDeadline.Token)}));
            }catch(Exception readFailure){output.WriteLine("Retained outbox read failed; inspect original journal: "+readFailure.Message);}
            throw;
        }finally{
            if(complete){await mongo.DropDatabaseAsync(database,token);file.Dispose();File.Delete(journal);}
            await provider.DeActivateProviderAsync();
        }
    }

    private sealed class TestAuthority(string user):HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){
            var body=await request.Content!.ReadAsStringAsync(token);object result;
            if(request.RequestUri!.AbsolutePath.EndsWith("authorize")){var input=JsonSerializer.Deserialize<UsageAuthorizationRequest>(body)!;
                result=new SubscriptionAuthorizationResult{Allowed=true,StatusCode=200,UserId=user,OperationId=input.OperationId,Status="authorized",ExpiresAtUtc=DateTime.UtcNow.AddMinutes(10)};
            }else if(request.RequestUri.AbsolutePath.EndsWith("start")){var input=JsonSerializer.Deserialize<UsageStartRequest>(body)!;result=new UsageStartResult{Started=true,OperationId=input.OperationId,Status="executing"};}
            else throw new InvalidOperationException("Unexpected test authority call.");
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(result))};
        }
    }
}
