using System.Reflection;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.CosmosBlockChainOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class CosmosPreparedNativeLedgerTests
{
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task IndependentPreparedKeyUsesSignedAggregateBudgetAndOriginalNativeReceipts(bool storageScope)
    {
        string Required(string name)=>Environment.GetEnvironmentVariable(name)??throw new InvalidOperationException(name+" required.");
        var rpc=Required("OASIS_COSMOS_TEST_RPC");Assert.IsTrue(new Uri(rpc).IsLoopback);
        var chain=Required("OASIS_COSMOS_TEST_CHAIN");Assert.AreEqual("oasis-cosmos-local",chain);
        var contract=Required("OASIS_COSMOS_TEST_CONTRACT");
        var root=Required("OASIS_HYPERDRIVE_TEST_ROOT");
        var oldJournal=Path.Combine(root,"replication-pending.json");
        const string oldHash="9B5A15110B0F21FEF8FCA6D2CE215859EEBC8944E9F1F83733CACE90892DEA81";
        string Hash(string path)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)));
        Assert.AreEqual(oldHash,Hash(oldJournal),"Preserve the reviewed uncertain operation; this test never replays it.");
        var journal=Path.Combine(root,"prepared-native-pending.json");
        Assert.IsFalse(File.Exists(journal),"Prior independent intent requires original-attempt reconciliation.");
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection,Required("SUBSCRIPTION_TEST_MONGODB_URI"));
        const string database="usage_cosmos_live_prepared";
        var mongo=new MongoClient(connection);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(90));var token=deadline.Token;
        using(var names=await mongo.ListDatabaseNamesAsync(cancellationToken:token))
            Assert.IsFalse((await names.ToListAsync(token)).Contains(database),"Do not overwrite an earlier outbox.");
        using var provider=CosmosBlockChainOASIS.CreateWithNativeFeeLimits(
            CosmosNativeFeeLimits.ForReviewedStandardSdkChain(new Dictionary<string,string>{{"stake","10000"}},chain),
            rpcEndpoint:rpc,chainId:chain,privateKey:Required("OASIS_COSMOS_TEST_KEY"),contractAddress:contract,
            addressPrefix:"wasm",gasPrice:"0.025stake",nativeDenom:"stake",nativeDecimals:0);
        var backend=typeof(CosmosBlockChainOASIS).GetField("_sdkBackend",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!;
        var method=backend.GetType().GetMethod("InvokeAsync",BindingFlags.Instance|BindingFlags.NonPublic)!;
        Task<JsonElement> Invoke(string op,object args)=>(Task<JsonElement>)method.Invoke(backend,new object[]{op,args,token})!;
        var sender=(await Invoke("restoreKey",new{})).GetProperty("address").GetString()!;
        var key="oasis/independent-prepared-budget/"+Guid.NewGuid().ToString("N");
        Assert.AreEqual(JsonValueKind.Null,(await Invoke("get",new{key})).ValueKind);
        using var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.Read);
        var complete=false;
        try{
            var store=new MongoUsageOutboxStore(connection,database);await store.InitializeAsync(token);
            var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new TestAuthority(user));
            var authority=new Web4SubscriptionAuthorizationClient(http,"https://offline-authority.invalid","WEB6","test-only-key");
            var executor=new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6"});
            async Task<UsageExecutionHandle> Begin(string suffix,decimal maximum){
                var handle=await executor.BeginAsync("test-only",user,key+suffix,new string('a',64),"POST /independent-local-test",
                    new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1,EstimatedCostUsd=0.02m,MaximumNativeFeeCostUsd=maximum},token);
                var time=DateTime.UtcNow.AddSeconds(-1);
                return handle.WithNativePrices(new[]{new NativeFeePriceQuote(Guid.Parse(handle.OperationId),"CosmosBlockChainOASIS",chain,"stake","local-test-stake",0,
                    0.000001m,"synthetic-local-price","fixture-v1","fixture-v1",time,time,time,time.AddMinutes(5))});
            }
            var execution=await Begin("-accepted",0.02m);var rejected=await Begin("-zero-budget",0m);
            await JsonSerializer.SerializeAsync(file,new{database,key,chain,contract,sender,storageScope,operationId=execution.OperationId,rejectedOperationId=rejected.OperationId,
                preservedOriginalJournalSha256=oldHash,recovery="Reconcile only these original outbox attempts; never replay unknown submission or alter replication-pending.json."},cancellationToken:token);
            file.Flush(true);
            Task<JsonElement> Mutate(string op,object args,string correlation,UsageExecutionHandle handle)=>storageScope
                ?provider.ExecutePreparedStorageAsync(async()=>{
                    var storage=(NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.IKeyValueBackend)backend;
                    var input=JsonSerializer.SerializeToElement(args);
                    if(op=="put")await storage.PutAsync(input.GetProperty("key").GetString()!,input.GetProperty("value").GetString()!,token);
                    else if(op=="delete")await storage.DeleteAsync(input.GetProperty("key").GetString()!,token);
                    else throw new InvalidOperationException("Unexpected fixture mutation.");
                    return JsonSerializer.SerializeToElement(new{});
                },handle,correlation,sender,token)
                :provider.ExecutePreparedNativeAsync(op,args,handle,correlation,chain,sender,token);
            var denied=await Assert.ThrowsExceptionAsync<UsageProtocolException>(()=>Mutate("put",new{key,value="must-not-submit"},"zero-budget",rejected));
            Assert.AreEqual("NATIVE_FEE_BUDGET_EXCEEDED",denied.Code);
            var noAttempt=await store.GetAsync(rejected.OperationId,token);Assert.AreEqual(0,noAttempt.ProviderAttempts.Count);Assert.AreEqual(0m,noAttempt.NativeFeeExposureUsd);
            Assert.AreEqual(JsonValueKind.Null,(await Invoke("get",new{key})).ValueKind);
            await Mutate("put",new{key,value="isolated prepared fixture"},"write",execution);
            Assert.AreEqual("isolated prepared fixture",(await Invoke("get",new{key})).GetString());
            await Mutate("delete",new{key},"cleanup",execution);
            Assert.AreEqual(JsonValueKind.Null,(await Invoke("get",new{key})).ValueKind);
            var stored=await new MongoUsageOutboxStore(connection,database).GetAsync(execution.OperationId,token);
            Assert.AreEqual(2,stored.ProviderAttempts.Count);
            Assert.AreEqual(stored.ProviderAttempts.Sum(attempt=>NativeFeeMaximumAccounting.ValueUsd(attempt.NativePreparation.FeeMaximums,attempt.NativePreparation.PriceQuotes)),stored.NativeFeeExposureUsd);
            Assert.IsTrue(stored.NativeFeeExposureUsd>0m&&stored.NativeFeeExposureUsd<=0.02m);
            foreach(var attempt in stored.ProviderAttempts){
                var maximum=attempt.NativePreparation.FeeMaximums.Single();Assert.AreEqual("stake",maximum.Denomination);
                Assert.AreEqual(NativeFeeMaximumCoverage.AllFees,maximum.Coverage);Assert.IsNull(attempt.AcknowledgedAtUtc);
                var receipt=await provider.GetPreparedNativeFeeReceiptAsync(attempt,token);Assert.IsFalse(receipt.IsError,receipt.Message);
                Assert.AreEqual(attempt.ProviderRequestId,receipt.Result.GetProperty("transactionHash").GetString());
                Assert.AreEqual(sender,receipt.Result.GetProperty("payer").GetString());Assert.AreEqual(chain,receipt.Result.GetProperty("chainId").GetString());
                var fee=receipt.Result.GetProperty("fees").EnumerateArray().Single();Assert.AreEqual("stake",fee.GetProperty("denom").GetString());
                Assert.IsTrue(System.Numerics.BigInteger.Parse(fee.GetProperty("amount").GetString()!)<=System.Numerics.BigInteger.Parse(maximum.NativeMaximumUnits));
                Console.WriteLine(JsonSerializer.Serialize(new{attempt.ProviderRequestId,maximum.NativeMaximumUnits,observedAnteFee=fee.GetProperty("amount").GetString()}));
            }
            Assert.AreEqual(0,stored.Receipts.Count,"Ante-only lookup must not invent complete net-cost settlement.");Assert.IsNull(stored.Settlement);
            Assert.AreEqual(oldHash,Hash(oldJournal));complete=true;
        }finally{
            if(complete){await mongo.DropDatabaseAsync(database,token);file.Dispose();File.Delete(journal);}
            else Console.WriteLine("Preserved independent original evidence: "+journal+"; database="+database);
        }
    }

    [TestMethod]
    public async Task ReconcileReviewedIndependentOriginalWithoutReplay()
    {
        var root=Environment.GetEnvironmentVariable("OASIS_HYPERDRIVE_TEST_ROOT")!;
        var journal=Path.Combine(root,"prepared-native-pending.json");
        var inspection=Path.Combine(root,"cosmos-independent-original-current.json");
        string Hash(string path)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)));
        Assert.AreEqual("EF0CDE533070555A505BE561FDF287DEB5D10CE73BFD055952B27278DDAAEF5C",Hash(journal));
        Assert.AreEqual("6615FD80158531AF671B07462AAF5A619700B03F9490A1C11207876564B726A6",Hash(inspection));
        using var intent=JsonDocument.Parse(File.ReadAllText(journal));using var original=JsonDocument.Parse(File.ReadAllText(inspection));
        var match=original.RootElement.GetProperty("matches").EnumerateArray().Single();
        const string originalHash="E94CA3A8B056C309E062832A011788C0CF5D9B80C2EBC9CF73CD42023CCB267B";
        Assert.AreEqual(originalHash,match.GetProperty("hash").GetString());Assert.AreEqual(0,match.GetProperty("code").GetInt32());
        var key=intent.RootElement.GetProperty("key").GetString()!;
        Assert.AreEqual("oasis/independent-prepared-budget/141b67730e1c4e96ba1af66d90aa04fc",key);
        const string chain="oasis-cosmos-local";const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.AreEqual(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        Assert.AreEqual("http://127.0.0.1:26657",Environment.GetEnvironmentVariable("OASIS_COSMOS_TEST_RPC"));
        const string database="usage_cosmos_live_prepared";
        using var tokenSource=new CancellationTokenSource(TimeSpan.FromSeconds(60));var token=tokenSource.Token;
        var store=new MongoUsageOutboxStore(connection,database);
        foreach(var name in new[]{"operationId","rejectedOperationId"})
            Assert.AreEqual(0,(await store.GetAsync(intent.RootElement.GetProperty(name).GetString()!,token)).ProviderAttempts.Count);
        using var provider=CosmosBlockChainOASIS.CreateWithNativeFeeLimits(CosmosNativeFeeLimits.ForReviewedStandardSdkChain(new Dictionary<string,string>{{"stake","10000"}},chain),
            rpcEndpoint:"http://127.0.0.1:26657",chainId:chain,privateKey:Environment.GetEnvironmentVariable("OASIS_COSMOS_TEST_KEY")!,contractAddress:intent.RootElement.GetProperty("contract").GetString()!,
            addressPrefix:"wasm",gasPrice:"0.025stake",nativeDenom:"stake",nativeDecimals:0);
        var backend=typeof(CosmosBlockChainOASIS).GetField("_sdkBackend",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(provider)!;
        var invoke=backend.GetType().GetMethod("InvokeAsync",BindingFlags.Instance|BindingFlags.NonPublic)!;
        Task<JsonElement> Read()=>(Task<JsonElement>)invoke.Invoke(backend,new object[]{"get",new{key},token})!;
        Assert.AreEqual("must-not-submit",(await Read()).GetString());
        var originalReceipt=await provider.GetNativeFeeReceiptAsync(originalHash,token);Assert.IsFalse(originalReceipt.IsError,originalReceipt.Message);
        var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new TestAuthority(user));
        var authority=new Web4SubscriptionAuthorizationClient(http,"https://offline-authority.invalid","WEB6","test-only-key");
        var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6"}).BeginAsync("test-only",user,"reviewed-cleanup-"+originalHash,new string('c',64),
            "POST /reviewed-isolated-cleanup",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1,EstimatedCostUsd=0.01m,MaximumNativeFeeCostUsd=0.01m},token);
        var time=DateTime.UtcNow.AddSeconds(-1);
        execution=execution.WithNativePrices(new[]{new NativeFeePriceQuote(Guid.Parse(execution.OperationId),"CosmosBlockChainOASIS",chain,"stake","local-test-stake",0,0.000001m,
            "synthetic-local-price","fixture-v1","fixture-v1",time,time,time,time.AddMinutes(5))});
        var cleanupJournal=Path.Combine(root,"prepared-native-cleanup-pending.json");
        using var file=new FileStream(cleanupJournal,FileMode.CreateNew,FileAccess.Write,FileShare.Read);
        await JsonSerializer.SerializeAsync(file,new{key,originalHash,cleanupOperationId=execution.OperationId,database},cancellationToken:token);file.Flush(true);
        var cleanup=await provider.ExecutePreparedNativeAsync("delete",new{key},execution,"reviewed-cleanup",chain,intent.RootElement.GetProperty("sender").GetString()!,token);
        Assert.AreEqual(JsonValueKind.Null,(await Read()).ValueKind);
        var operation=await store.GetAsync(execution.OperationId,token);var attempt=operation.ProviderAttempts.Single();
        var receipt=await provider.GetPreparedNativeFeeReceiptAsync(attempt,token);Assert.IsFalse(receipt.IsError,receipt.Message);
        var evidence=JsonSerializer.SerializeToUtf8Bytes(new{originalIntent=intent.RootElement,originalInspection=original.RootElement,originalReceipt=originalReceipt.Result,
            cleanupResult=cleanup,cleanupAttempt=attempt,cleanupReceipt=receipt.Result,cleanupVerified=true});
        var archive=Path.Combine(root,"cosmos-independent-reviewed-reconciliation.json");Assert.IsFalse(File.Exists(archive));
        await File.WriteAllBytesAsync(archive,evidence,token);
        Assert.AreEqual(Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(evidence)),Hash(archive));
        Assert.AreEqual("9B5A15110B0F21FEF8FCA6D2CE215859EEBC8944E9F1F83733CACE90892DEA81",Hash(Path.Combine(root,"replication-pending.json")));
        await new MongoClient(connection).DropDatabaseAsync(database,token);
        file.Dispose();File.Delete(cleanupJournal);File.Delete(journal);
        Console.WriteLine("Reconciled exact original="+originalHash+"; admitted cleanup="+attempt.ProviderRequestId+"; archive SHA256="+Hash(archive));
    }

    private sealed class TestAuthority(string user):HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token){
            var body=await request.Content!.ReadAsStringAsync(token);object response;
            if(request.RequestUri!.AbsolutePath.EndsWith("authorize")){
                var input=JsonSerializer.Deserialize<UsageAuthorizationRequest>(body)!;
                response=new SubscriptionAuthorizationResult{Allowed=true,StatusCode=200,UserId=user,OperationId=input.OperationId,Status="authorized",ExpiresAtUtc=DateTime.UtcNow.AddMinutes(2)};
            }else if(request.RequestUri.AbsolutePath.EndsWith("start")){
                var input=JsonSerializer.Deserialize<UsageStartRequest>(body)!;response=new UsageStartResult{Started=true,OperationId=input.OperationId,Status="executing"};
            }else throw new InvalidOperationException("Unexpected test-authority call.");
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(response))};
        }
    }
}
