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
    [Theory]
    [InlineData("valid")] [InlineData("empty")] [InlineData("foreign")] [InlineData("unavailable")]
    public async Task AccountCreationReadbackFailurePreservesOneActualSdkSubmission(string mode)
    {
        var genesis=new string('a',64);var genesisBytes=ByteString.CopyFrom(Convert.FromHexString(genesis));
        var originalId=ByteString.Empty;var broadcasts=0;var reads=0;
        var service=ServerServiceDefinition.CreateBuilder()
            .AddMethod(Rpc(Wallet.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),(NumberMessage request,ServerCallContext context)=>Task.FromResult(new BlockExtention{Blockid=genesisBytes}))
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),(NumberMessage request,ServerCallContext context)=>Task.FromResult(new BlockExtention{Blockid=genesisBytes}))
            .AddMethod(Rpc(Wallet.Descriptor.FullName,"CreateAccount2",AccountCreateContract.Parser,TransactionExtention.Parser),(AccountCreateContract request,ServerCallContext context)=>{
                var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();var tx=new Transaction{RawData=new Transaction.Types.raw{Timestamp=now,Expiration=now+60000}};
                tx.RawData.Contract.Add(new Transaction.Types.Contract{Type=Transaction.Types.Contract.Types.ContractType.AccountCreateContract,Parameter=Google.Protobuf.WellKnownTypes.Any.Pack(request)});
                return Task.FromResult(new TransactionExtention{Transaction=tx,Result=new Return{Result=true}});
            })
            .AddMethod(Rpc(Wallet.Descriptor.FullName,"BroadcastTransaction",Transaction.Parser,Return.Parser),(Transaction request,ServerCallContext context)=>{
                broadcasts++;Assert.Single(request.Signature);originalId=ByteString.CopyFrom(SHA256.HashData(request.RawData.ToByteArray()));return Task.FromResult(new Return{Result=true});
            })
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName,"GetTransactionInfoById",BytesMessage.Parser,TransactionInfo.Parser),(BytesMessage request,ServerCallContext context)=>{
                Assert.Equal(originalId,request.Value);return Task.FromResult(new TransactionInfo{Id=originalId,BlockNumber=1});
            })
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName,"GetAccount",Account.Parser,Account.Parser),(Account request,ServerCallContext context)=>{
                reads++;if(mode=="unavailable")throw new RpcException(new Status(StatusCode.Unavailable,"controlled post-confirmation readback failure"));
                var address=request.Address.ToByteArray();if(mode=="foreign")address[1]^=1;
                return Task.FromResult(mode=="empty"?new Account():new Account{Address=ByteString.CopyFrom(address)});
            }).Build();
        var server=new Server{Services={service},Ports={new ServerPort("127.0.0.1",0,ServerCredentials.Insecure)}};server.Start();
        try{
            var port=server.Ports.Single().BoundPort;
            await using var runtime=new TronSdkRuntime(new Uri($"http://127.0.0.1:{port}"),new Uri($"http://127.0.0.1:{port}"),genesis);
            var key=new string('0',63)+"1";var owner=new TronNet.TronECKey(key,TronNet.TronNetwork.MainNet).GetPublicAddress();
            var created=new TronNet.TronECKey(new string('0',63)+"2",TronNet.TronNetwork.MainNet).GetPublicAddress();
            using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var operation=runtime.NativeTransfers.CreateLedgerAccountAsync(owner,created,key,deadline.Token);
            if(mode=="valid"){
                var returned=await operation;Assert.Equal(Convert.ToHexString(originalId.ToByteArray()).ToLowerInvariant(),returned);
            }
            else{
                var failure=await Assert.ThrowsAsync<TronCommittedReadbackException>(()=>operation);
                Assert.Equal(Convert.ToHexString(originalId.ToByteArray()).ToLowerInvariant(),failure.OriginalTransactionId);
                Assert.Equal(genesis,failure.ChainGenesisId);Assert.IsAssignableFrom<NextGenSoftware.OASIS.API.Core.Interfaces.INativeMutationReconciliationRequired>(failure);
                if(mode=="unavailable")Assert.IsType<RpcException>(failure.InnerException);else Assert.IsType<InvalidOperationException>(failure.InnerException);
            }
            Assert.Equal(1,broadcasts);Assert.Equal(1,reads);
        }finally{await server.ShutdownAsync();}
    }
    private static Method<TRequest,TResponse> Rpc<TRequest,TResponse>(string service,string name,MessageParser<TRequest> request,MessageParser<TResponse> response)
        where TRequest:class,IMessage<TRequest> where TResponse:class,IMessage<TResponse>
        =>new(MethodType.Unary,service,name,Marshallers.Create<TRequest>(value=>value.ToByteArray(),bytes=>request.ParseFrom(bytes)),Marshallers.Create<TResponse>(value=>value.ToByteArray(),bytes=>response.ParseFrom(bytes)));
    private sealed class SolidNode(ByteString genesis,bool foreignAccount,bool emptyAccount=false,bool unavailable=false,bool denied=false)
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
            var account=new Account{Address=ByteString.CopyFrom(address),OwnerPermission=new Permission{Id=0,Threshold=denied?2:1}};
            account.OwnerPermission.Keys.Add(new Key{Address=account.Address,Weight=1});return Task.FromResult(account);
        }
    }
    [Theory]
    [InlineData("valid")] [InlineData("foreign-account")] [InlineData("wrong-genesis")]
    [InlineData("wrong-solidity")] [InlineData("empty-account")] [InlineData("unavailable")]
    [InlineData("prepared")]
    [InlineData("prepared-denied")]
    public async Task LoaderUsesActualGrpcAndRejectsMismatchedAuthority(string mode)
    {
        var expected=new string('a',64);var served=ByteString.CopyFrom(Convert.FromHexString(mode=="wrong-genesis"||mode=="wrong-solidity"?new string('b',64):expected));
        var solid=new SolidNode(served,mode=="foreign-account",mode=="empty-account",mode=="unavailable",mode=="prepared-denied");
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
            if(mode=="valid"||mode=="prepared"||mode=="prepared-denied"){
                var account=await runtime.NativeTransfers.LoadPermissionAuthorityAsync(address,deadline.Token);
                Assert.Equal(21,account.Address.Length);Assert.NotNull(account.OwnerPermission);Assert.Equal(1,solid.AccountQueries);
                if(mode=="prepared"||mode=="prepared-denied"){
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
                        if(mode=="prepared-denied"){
                            await Assert.ThrowsAsync<System.IO.InvalidDataException>(()=>callback(id,expected,encoded,deadline.Token));
                            var rejected=await store.GetAsync(execution.OperationId,deadline.Token);
                            Assert.Empty(rejected.ProviderAttempts);Assert.Equal(0,rejected.NativePreparationBytes);Assert.Equal(2,solid.AccountQueries);return;
                        }
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
