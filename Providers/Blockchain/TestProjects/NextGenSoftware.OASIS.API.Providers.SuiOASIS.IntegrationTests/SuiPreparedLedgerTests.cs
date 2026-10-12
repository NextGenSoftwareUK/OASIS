using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class SuiPreparedLedgerTests
{
    [TestMethod]
    public async Task ActivationVerifiesRealOwnerWithoutNetworkFee()
    {
        var rpc=Environment.GetEnvironmentVariable("OASIS_SUI_TEST_RPC")??throw new InvalidOperationException("Retained RPC required.");
        Assert.IsTrue(new Uri(rpc).IsLoopback);
        using var deployment=JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("OASIS_SUI_TEST_DEPLOYMENT")??throw new InvalidOperationException("Retained deployment required.")));
        var settings=deployment.RootElement;
        using var provider=new SuiOASIS(rpc,"localnet",settings.GetProperty("chainId").GetString()!,settings.GetProperty("packageAddress").GetString()!,
            settings.GetProperty("privateKey").GetString()!,settings.GetProperty("storageObjectId").GetString()!);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var address=settings.GetProperty("address").GetString()!;
        var before=await provider.GetAccountBalanceAsync(address,deadline.Token);Assert.IsFalse(before.IsError,before.Message);
        try{
            var activated=await provider.ActivateProviderAsync();Assert.IsFalse(activated.IsError,activated.Message);Assert.IsTrue(activated.Result);
            var after=await provider.GetAccountBalanceAsync(address,deadline.Token);Assert.IsFalse(after.IsError,after.Message);
            Assert.AreEqual(before.Result,after.Result,"Read-only activation must not charge a network mutation fee.");
        }finally{await provider.DeActivateProviderAsync();}
    }
    private sealed class TestAuthority(string user):HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var body=await request.Content!.ReadAsStringAsync(token);object response;
            if(request.RequestUri!.AbsolutePath.EndsWith("authorize")){
                var input=JsonSerializer.Deserialize<UsageAuthorizationRequest>(body)!;
                response=new SubscriptionAuthorizationResult{Allowed=true,StatusCode=200,UserId=user,OperationId=input.OperationId,Status="authorized",ExpiresAtUtc=DateTime.UtcNow.AddMinutes(2)};
            }else if(request.RequestUri.AbsolutePath.EndsWith("start")){
                var input=JsonSerializer.Deserialize<UsageStartRequest>(body)!;
                response=new UsageStartResult{Started=true,OperationId=input.OperationId,Status="executing"};
            }else throw new InvalidOperationException("Unexpected test authority call.");
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(response),System.Text.Encoding.UTF8,"application/json")};
        }
    }
    [DataTestMethod]
    [DataRow(false)] [DataRow(true)]
    public async Task PreparedLocalWriteRetainsOriginalSignedEvidenceAndConfirmsLedger(bool loseConfirmedReply)
    {
        var rpc=Environment.GetEnvironmentVariable("OASIS_SUI_TEST_RPC")??throw new InvalidOperationException("Retained RPC required.");
        Assert.IsTrue(new Uri(rpc).IsLoopback,"Never submit this fixture to a public chain.");
        using var deployment=JsonDocument.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("OASIS_SUI_TEST_DEPLOYMENT")??throw new InvalidOperationException("Retained deployment required.")));
        var settings=deployment.RootElement;
        using var provider=new SuiOASIS(rpc,"localnet",settings.GetProperty("chainId").GetString()!,settings.GetProperty("packageAddress").GetString()!,
            settings.GetProperty("privateKey").GetString()!,settings.GetProperty("storageObjectId").GetString()!);
        const string connection="mongodb://127.0.0.1:27017/?serverSelectionTimeoutMS=3000";
        var database="usage_sui_live_prepared_"+Guid.NewGuid().ToString("N");var mongo=new MongoDB.Driver.MongoClient(connection);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(90));
        var key="oasis/prepared-test/"+Guid.NewGuid().ToString("N");
        var directory=new DirectoryInfo(AppContext.BaseDirectory);
        while(directory!=null&&!File.Exists(Path.Combine(directory.FullName,"Scripts/TestHosts/start-sui-test-network.sh")))directory=directory.Parent;
        Assert.IsNotNull(directory,"Existing task repository required.");
        var journal=Path.Combine(directory!.FullName,"TestResults/SuiCurrent/prepared-live-pending.json");
        Directory.CreateDirectory(Path.GetDirectoryName(journal)!);
        // CreateNew is an atomic fence: never repeat a previous unknown submission.
        using var journalFile=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.Read);
        try{
            var store=new MongoUsageOutboxStore(connection,database);await store.InitializeAsync(deadline.Token);
            var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new TestAuthority(user));
            var authority=new Web4SubscriptionAuthorizationClient(http,"https://test-authority.invalid","WEB6","test-key");
            var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6",PersistenceTimeout=TimeSpan.FromSeconds(5)})
                .BeginAsync("test-only",user,key,new string('a',64),"POST /local-test",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1},deadline.Token);
            await JsonSerializer.SerializeAsync(journalFile,new{database,key,operationId=execution.OperationId,chainId=settings.GetProperty("chainId").GetString(),
                recovery="Inspect original write and cleanup attempts in this retained Mongo outbox; never resubmit on unknown outcome."},cancellationToken:deadline.Token);
            journalFile.Flush(true);
            string hash;
            if(loseConfirmedReply){
                var previous=Environment.GetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE");
                var previousReal=Environment.GetEnvironmentVariable("OASIS_SUI_TEST_REAL_BRIDGE");
                Assert.IsFalse(string.IsNullOrWhiteSpace(previous),"Retained source SDK path required.");
                try{
                    Environment.SetEnvironmentVariable("OASIS_SUI_TEST_REAL_BRIDGE",previous);
                    Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",Path.Combine(AppContext.BaseDirectory,"Fixtures/lose-confirmed-reply.mjs"));
                    var failure=await Assert.ThrowsExceptionAsync<SuiNativeMutationReconciliationException>(()=>provider.ExecutePreparedNativeAsync("put",new{key,value="owned prepared live fixture"},execution,"write",
                        settings.GetProperty("chainId").GetString()!,settings.GetProperty("address").GetString()!,deadline.Token));
                    hash=failure.OriginalTransactionId;Assert.IsFalse(string.IsNullOrWhiteSpace(hash));
                }finally{
                    Environment.SetEnvironmentVariable("OASIS_SUI_SDK_BRIDGE",previous);
                    Environment.SetEnvironmentVariable("OASIS_SUI_TEST_REAL_BRIDGE",previousReal);
                }
            }else{
                var response=await provider.ExecutePreparedNativeAsync("put",new{key,value="owned prepared live fixture"},execution,"write",
                    settings.GetProperty("chainId").GetString()!,settings.GetProperty("address").GetString()!,deadline.Token);
                hash=response.GetProperty("transactionHash").GetString()!;
            }
            var stored=await store.GetAsync(execution.OperationId,deadline.Token);Assert.AreEqual(1,stored.ProviderAttempts.Count);
            var attempt=stored.ProviderAttempts[0];Assert.AreEqual(hash,attempt.ProviderRequestId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(attempt.NativePreparation.SignedTransactionBase64));
            Assert.IsFalse(string.IsNullOrWhiteSpace(attempt.NativePreparation.Signature));
            Assert.IsNull(attempt.AcknowledgedAtUtc,"Preparation alone must not claim ledger settlement.");
            var status=await provider.GetTransactionStatusAsync(hash,deadline.Token);Assert.IsFalse(status.IsError,status.Message);
            Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed,status.Result);
            var sdk=typeof(SuiOASIS).GetMethod("InvokeSdkAsync",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
            var readback=await (Task<JsonElement>)sdk.Invoke(provider,new object[]{"get",new{key},deadline.Token})!;
            Assert.AreEqual("owned prepared live fixture",readback.GetString());
            var conflict=await Assert.ThrowsExceptionAsync<UsageProtocolException>(()=>provider.ExecutePreparedNativeAsync("put",new{key,value="must never be submitted"},execution,"write",
                settings.GetProperty("chainId").GetString()!,settings.GetProperty("address").GetString()!,deadline.Token));
            Assert.AreEqual("PROVIDER_ATTEMPT_ALREADY_STARTED",conflict.Code);
            var afterDuplicate=await store.GetAsync(execution.OperationId,deadline.Token);
            Assert.AreEqual(1,afterDuplicate.ProviderAttempts.Count);Assert.AreEqual(stored.NativePreparationBytes,afterDuplicate.NativePreparationBytes);
            var unchanged=await (Task<JsonElement>)sdk.Invoke(provider,new object[]{"get",new{key},deadline.Token})!;
            Assert.AreEqual("owned prepared live fixture",unchanged.GetString());
            var cleanup=await provider.ExecutePreparedNativeAsync("delete",new{key},execution,"cleanup",
                settings.GetProperty("chainId").GetString()!,settings.GetProperty("address").GetString()!,deadline.Token);
            var cleanupHash=cleanup.GetProperty("transactionHash").GetString()!;
            var cleaned=await store.GetAsync(execution.OperationId,deadline.Token);Assert.AreEqual(2,cleaned.ProviderAttempts.Count);
            Assert.AreEqual(cleanupHash,cleaned.ProviderAttempts[1].ProviderRequestId);
            var cleanupStatus=await provider.GetTransactionStatusAsync(cleanupHash,deadline.Token);Assert.IsFalse(cleanupStatus.IsError,cleanupStatus.Message);
            Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed,cleanupStatus.Result);
            var absent=await (Task<JsonElement>)sdk.Invoke(provider,new object[]{"get",new{key},deadline.Token})!;
            Assert.AreEqual(JsonValueKind.Null,absent.ValueKind);
            Console.WriteLine($"Confirmed original prepared write={hash}; cleanup={cleanupHash}; owned key={key}.");
            await mongo.DropDatabaseAsync(database);
            journalFile.Dispose();File.Delete(journal);
        }catch{
            Console.WriteLine($"Retained original prepared evidence for reconciliation: {journal}; database={database}; key={key}.");
            throw;
        }
    }
}
