using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class SuiPreparedLedgerTests
{
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
    [TestMethod]
    public async Task PreparedLocalWriteRetainsOriginalSignedEvidenceAndConfirmsLedger()
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
        var committed=false;
        try{
            var store=new MongoUsageOutboxStore(connection,database);await store.InitializeAsync(deadline.Token);
            var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new TestAuthority(user));
            var authority=new Web4SubscriptionAuthorizationClient(http,"https://test-authority.invalid","WEB6","test-key");
            var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6",PersistenceTimeout=TimeSpan.FromSeconds(5)})
                .BeginAsync("test-only",user,key,new string('a',64),"POST /local-test",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1},deadline.Token);
            var response=await provider.ExecutePreparedNativeAsync("put",new{key,value="owned prepared live fixture"},execution,"write",
                settings.GetProperty("chainId").GetString()!,settings.GetProperty("address").GetString()!,deadline.Token);
            committed=true;
            var hash=response.GetProperty("transactionHash").GetString()!;
            var stored=await store.GetAsync(execution.OperationId,deadline.Token);Assert.AreEqual(1,stored.ProviderAttempts.Count);
            var attempt=stored.ProviderAttempts[0];Assert.AreEqual(hash,attempt.ProviderRequestId);
            Assert.IsFalse(string.IsNullOrWhiteSpace(attempt.NativePreparation.SignedTransactionBase64));
            Assert.IsFalse(string.IsNullOrWhiteSpace(attempt.NativePreparation.Signature));
            Assert.IsNull(attempt.AcknowledgedAtUtc,"Preparation alone must not claim ledger settlement.");
            var status=await provider.GetTransactionStatusAsync(hash,deadline.Token);Assert.IsFalse(status.IsError,status.Message);
            Assert.AreEqual(NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums.BridgeTransactionStatus.Completed,status.Result);
        }finally{
            // Only delete the exact owned key after verified successful submission, never retry an unknown mutation.
            if(committed){
                var backend=typeof(SuiOASIS).GetMethod("InvokeSdkAsync",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!;
                await (Task<JsonElement>)backend.Invoke(provider,new object[]{"delete",new{key},CancellationToken.None})!;
                await mongo.DropDatabaseAsync(database);
            }
            // On an unknown outcome preserve the durable outbox for original-transaction reconciliation.
        }
    }
}
