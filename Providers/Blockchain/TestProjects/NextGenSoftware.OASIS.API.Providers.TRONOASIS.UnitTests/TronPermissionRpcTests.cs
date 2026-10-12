using Google.Protobuf;
using Grpc.Core;
using TronNet.Protocol;
using Xunit;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using System.Reflection;
using System.Security.Cryptography;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public class TronPermissionRpcTests
{
    private static Method<TRequest,TResponse> Rpc<TRequest,TResponse>(string service,string name,MessageParser<TRequest> request,MessageParser<TResponse> response)
        where TRequest:class,IMessage<TRequest> where TResponse:class,IMessage<TResponse>
        =>new(MethodType.Unary,service,name,Marshallers.Create<TRequest>(value=>value.ToByteArray(),bytes=>request.ParseFrom(bytes)),Marshallers.Create<TResponse>(value=>value.ToByteArray(),bytes=>response.ParseFrom(bytes)));
    private sealed class SolidNode(ByteString genesis,bool foreignAccount,bool emptyAccount=false,bool unavailable=false)
    {
        public int AccountQueries;
        public Task<BlockExtention> GetBlockByNum2(NumberMessage request,ServerCallContext context)
            =>Task.FromResult(new BlockExtention{Blockid=genesis});
        public Task<Account> GetAccount(Account request,ServerCallContext context)
        {
            AccountQueries++;
            if(unavailable)throw new RpcException(new Status(StatusCode.Unavailable,"controlled unavailable account query"));
            if(emptyAccount)return Task.FromResult(new Account());
            var address=request.Address.ToByteArray();if(foreignAccount)address[1]^=1;
            var account=new Account{Address=ByteString.CopyFrom(address),OwnerPermission=new Permission{Id=0,Threshold=1}};
            account.OwnerPermission.Keys.Add(new Key{Address=account.Address,Weight=1});return Task.FromResult(account);
        }
    }
    [Theory]
    [InlineData("valid")] [InlineData("foreign-account")] [InlineData("wrong-genesis")]
    [InlineData("wrong-solidity")] [InlineData("empty-account")] [InlineData("unavailable")]
    [InlineData("prepared")]
    public async Task LoaderUsesActualGrpcAndRejectsMismatchedAuthority(string mode)
    {
        var expected=new string('a',64);var served=ByteString.CopyFrom(Convert.FromHexString(mode=="wrong-genesis"||mode=="wrong-solidity"?new string('b',64):expected));
        var solid=new SolidNode(served,mode=="foreign-account",mode=="empty-account",mode=="unavailable");
        var service=ServerServiceDefinition.CreateBuilder()
            .AddMethod(Rpc(Wallet.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),(NumberMessage request,ServerCallContext context)=>Task.FromResult(new BlockExtention{Blockid=mode=="wrong-solidity"?ByteString.CopyFrom(Convert.FromHexString(expected)):served}))
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),solid.GetBlockByNum2)
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName,"GetAccount",Account.Parser,Account.Parser),solid.GetAccount).Build();
        var server=new Server{Services={service},Ports={new ServerPort("127.0.0.1",0,ServerCredentials.Insecure)}};
        server.Start();
        try{
            var port=server.Ports.Single().BoundPort;
            await using var runtime=new TronSdkRuntime(new Uri($"http://127.0.0.1:{port}"),new Uri($"http://127.0.0.1:{port}"),expected);
            using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var address=new TronNet.TronECKey(new string('0',63)+"1",TronNet.TronNetwork.MainNet).GetPublicAddress();
            if(mode=="valid"||mode=="prepared"){
                var account=await runtime.NativeTransfers.LoadPermissionAuthorityAsync(address,deadline.Token);
                Assert.Equal(21,account.Address.Length);Assert.NotNull(account.OwnerPermission);Assert.Equal(1,solid.AccountQueries);
                if(mode=="prepared"){
                    const string connection="mongodb://127.0.0.1:27017/?serverSelectionTimeoutMS=3000";
                    var database="usage_tron_rpc_test_"+Guid.NewGuid().ToString("N");var mongo=new MongoDB.Driver.MongoClient(connection);
                    try{
                        var store=new MongoUsageOutboxStore(connection,database);await store.InitializeAsync(deadline.Token);
                        var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new TronPreparedOutboxTests.OfflineAuthority(user));
                        var authority=new Web4SubscriptionAuthorizationClient(http,"https://offline-authority.test","WEB6","test-only-key");
                        var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions{Service="WEB6",PersistenceTimeout=TimeSpan.FromSeconds(5)})
                            .BeginAsync("test-only-bearer",user,"tron-rpc-signed-test",new string('a',64),"POST /offline-test",new UsageEndpointPolicy{MeterCategory="api.request",ReservedUnits=1},deadline.Token);
                        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                        var signer=(TronNet.ITransactionClient)typeof(TronNativeTransferClient).GetField("_transactions",flags)!.GetValue(runtime.NativeTransfers)!;
                        var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        var tx=new Transaction{RawData=new Transaction.Types.raw{Timestamp=now,Expiration=now+60000}};
                        tx.RawData.Contract.Add(new Transaction.Types.Contract{Type=Transaction.Types.Contract.Types.ContractType.TransferContract,
                            Parameter=Google.Protobuf.WellKnownTypes.Any.Pack(new TransferContract{OwnerAddress=account.Address,ToAddress=account.Address,Amount=1})});
                        var signed=signer.GetTransactionSign(tx,new string('0',63)+"1");var encoded=Convert.ToBase64String(signed.ToByteArray());
                        var id=Convert.ToHexString(SHA256.HashData(signed.RawData.ToByteArray())).ToLowerInvariant();
                        var view=runtime.CreatePreparedNativeTransfersWithAccountAuthority(execution,"prepared",address);
                        var callback=(Func<string,string,string,CancellationToken,Task>)typeof(TronNativeTransferClient).GetField("_persistPreparedSubmission",flags)!.GetValue(view)!;
                        await callback(id,expected,encoded,deadline.Token);
                        var stored=await store.GetAsync(execution.OperationId,deadline.Token);Assert.Single(stored.ProviderAttempts);
                        Assert.Equal(encoded,stored.ProviderAttempts[0].NativePreparation.SignedTransactionBase64);Assert.Equal(id,stored.ProviderAttempts[0].ProviderRequestId);
                        Assert.Null(stored.ProviderAttempts[0].AcknowledgedAtUtc);Assert.Equal(2,solid.AccountQueries);
                    }finally{await mongo.DropDatabaseAsync(database);}
                }
            }else{
                if(mode=="unavailable")await Assert.ThrowsAsync<RpcException>(()=>runtime.NativeTransfers.LoadPermissionAuthorityAsync(address,deadline.Token));
                else await Assert.ThrowsAsync<InvalidOperationException>(()=>runtime.NativeTransfers.LoadPermissionAuthorityAsync(address,deadline.Token));
                Assert.Equal(mode=="wrong-genesis"||mode=="wrong-solidity"?0:1,solid.AccountQueries);
            }
        }finally{await server.ShutdownAsync();}
    }
}
