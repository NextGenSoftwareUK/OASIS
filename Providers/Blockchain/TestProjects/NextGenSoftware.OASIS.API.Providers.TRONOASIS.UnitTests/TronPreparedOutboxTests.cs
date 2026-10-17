using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using Google.Protobuf;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using TronNet;
using TronNet.Protocol;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public class TronPreparedOutboxTests
{
    internal sealed class OfflineAuthority(string user):HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken token)
        {
            var body=await request.Content!.ReadAsStringAsync(token);object response;
            if(request.RequestUri!.AbsolutePath.EndsWith("authorize")){
                var input=JsonSerializer.Deserialize<UsageAuthorizationRequest>(body)!;
                response=new SubscriptionAuthorizationResult{Allowed=true,StatusCode=200,UserId=user,OperationId=input.OperationId,Status="authorized",ExpiresAtUtc=DateTime.UtcNow.AddMinutes(1)};
            }else if(request.RequestUri.AbsolutePath.EndsWith("start")){
                var input=JsonSerializer.Deserialize<UsageStartRequest>(body)!;response=new UsageStartResult{Started=true,OperationId=input.OperationId,Status="executing"};
            }else throw new InvalidOperationException("Unexpected offline authority request.");
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK){Content=new StringContent(JsonSerializer.Serialize(response),System.Text.Encoding.UTF8,"application/json")};
        }
    }
    [Fact]
    public async Task RealSignedRuntimeAdapterPersistsOriginalAndRejectsWrongSender()
    {
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        var database="usage_tron_signed_test_"+Guid.NewGuid().ToString("N");var mongo=new MongoDB.Driver.MongoClient(connection);
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(15));
        try{
            var store=new MongoUsageOutboxStore(connection,database);await store.InitializeAsync(deadline.Token);
            var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new OfflineAuthority(user));
            var authority=new Web4SubscriptionAuthorizationClient(http,"https://offline-authority.test","WEB6","test-only-key");
            var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6",PersistenceTimeout=TimeSpan.FromSeconds(5)})
                .BeginAsync("test-only-bearer",user,"tron-signed-test",new string('a',64),"POST /offline-test",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1},deadline.Token);
            var genesis=new string('a',64);
            await using var runtime=new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),new Uri("http://127.0.0.1:50052"),genesis);
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            var signer=(ITransactionClient)typeof(TronNativeTransferClient).GetField("_transactions",flags)!.GetValue(runtime.NativeTransfers)!;
            var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var transaction=new Transaction{RawData=new Transaction.Types.raw{Timestamp=now,Expiration=now+60000}};
            var operationOwner=new byte[21];operationOwner[0]=0x41;new Nethereum.Signer.EthECKey(new string('0',63)+"1").GetPublicAddressAsBytes().CopyTo(operationOwner,1);
            transaction.RawData.Contract.Add(new Transaction.Types.Contract{Type=Transaction.Types.Contract.Types.ContractType.TransferContract,
                Parameter=Google.Protobuf.WellKnownTypes.Any.Pack(new TransferContract{OwnerAddress=ByteString.CopyFrom(operationOwner),ToAddress=ByteString.CopyFrom(operationOwner),Amount=1})});
            var signed=signer.GetTransactionSign(transaction,new string('0',63)+"1");
            var id=Convert.ToHexString(SHA256.HashData(signed.RawData.ToByteArray())).ToLowerInvariant();var bytes=Convert.ToBase64String(signed.ToByteArray());
            var sender=new byte[21];sender[0]=0x41;new Nethereum.Signer.EthECKey(new string('0',63)+"1").GetPublicAddressAsBytes().CopyTo(sender,1);
            var view=runtime.CreatePreparedNativeTransfers(execution,"signed-attempt",sender);
            sender[1]^=1; // Caller mutation must not alter the trusted sender snapshot held by the view.
            var callback=(Func<string,string,string,CancellationToken,Task>)typeof(TronNativeTransferClient).GetField("_persistPreparedSubmission",flags)!.GetValue(view)!;
            await callback(id,genesis,bytes,deadline.Token);
            var stored=await store.GetAsync(execution.OperationId,deadline.Token);
            Assert.Single(stored.ProviderAttempts);Assert.Equal(id,stored.ProviderAttempts[0].ProviderRequestId);
            Assert.Equal(bytes,stored.ProviderAttempts[0].NativePreparation.SignedTransactionBase64);Assert.Null(stored.ProviderAttempts[0].AcknowledgedAtUtc);
            var duplicate=await Assert.ThrowsAsync<UsageProtocolException>(()=>callback(id,genesis,bytes,deadline.Token));
            Assert.Equal("PROVIDER_ATTEMPT_ALREADY_STARTED",duplicate.Code);
            await Assert.ThrowsAsync<InvalidOperationException>(()=>callback(id,new string('b',64),bytes,deadline.Token));
            var wrong=runtime.CreatePreparedNativeTransfers(execution,"wrong-sender",sender);
            var wrongCallback=(Func<string,string,string,CancellationToken,Task>)typeof(TronNativeTransferClient).GetField("_persistPreparedSubmission",flags)!.GetValue(wrong)!;
            await Assert.ThrowsAsync<System.IO.InvalidDataException>(()=>wrongCallback(id,genesis,bytes,deadline.Token));
            var after=await store.GetAsync(execution.OperationId,deadline.Token);Assert.Single(after.ProviderAttempts);Assert.Equal(stored.NativePreparationBytes,after.NativePreparationBytes);
        }finally{await mongo.DropDatabaseAsync(database);}
    }
}
