using System.Reflection;
using System.Security.Cryptography;
using Google.Protobuf;
using Grpc.Core;
using TronNet.Protocol;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public class TronPreparedStorageTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    [InlineData(false,true)]
    [InlineData(false,false,true,false)] [InlineData(true,false,true,false)]
    [InlineData(false,false,true,true)] [InlineData(true,false,true,true)]
    public async Task EncryptedPhysicalWritesPersistBeforeSdkBroadcast(bool rejectPersistence,bool concurrent=false,bool routed=false,bool synchronous=false)
    {
        const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
        Assert.Equal(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
        const string database="usage_tron_prepared_storage_owned";
        var mongo=new MongoDB.Driver.MongoClient(connection);
        using(var names=await mongo.ListDatabaseNamesAsync())
            Assert.DoesNotContain(database,await MongoDB.Driver.IAsyncCursorExtensions.ToListAsync(names));
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var store=new MongoUsageOutboxStore(connection,database);
        await store.InitializeAsync(deadline.Token);
        var user=Guid.NewGuid().ToString("D");using var http=new HttpClient(new TronPreparedOutboxTests.OfflineAuthority(user));
        var authority=new Web4SubscriptionAuthorizationClient(http,"https://offline-authority.test","WEB6","test-only-key");
        var execution=await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions {Service="WEB6"})
            .BeginAsync("test-only",user,"prepared-storage",new string('a',64),"POST /offline-storage",
                new UsageEndpointPolicy {MeterCategory="api.request",ReservedUnits=1},deadline.Token);
        var other=concurrent ? await new Web4UsageExecution(authority,store,new Web4UsageLedgerOptions {Service="WEB6"})
            .BeginAsync("test-only",user,"prepared-storage-other",new string('a',64),"POST /offline-storage",
                new UsageEndpointPolicy {MeterCategory="api.request",ReservedUnits=1},deadline.Token):null;
        var key=new string('0',63)+"1";
        var owner=new TronNet.TronECKey(key,TronNet.TronNetwork.MainNet).GetPublicAddress();
        var address=NBitcoin.DataEncoders.Encoders.Base58Check.DecodeData(owner);
        var genesis=new string('a',64);var block=ByteString.CopyFrom(Convert.FromHexString(genesis));
        var broadcasts=0;var originalId=ByteString.Empty;
        var feeLookups=0;
        var submitted=new System.Collections.Concurrent.ConcurrentDictionary<ByteString,Transaction>();
        string Selector(string signature)=>new Nethereum.Util.Sha3Keccack().CalculateHash(signature)[..8].ToUpperInvariant();
        byte[] Encode(string[] types,params object[] values)=>(byte[])typeof(TronStorageBackend)
            .GetMethod("Encode",BindingFlags.Static|BindingFlags.NonPublic)!
            .Invoke(null,new object[] {"result("+string.Join(",",types)+")",types,values})!;
        var service=ServerServiceDefinition.CreateBuilder()
            .AddMethod(TronPermissionRpcTests.Rpc(Wallet.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),
                (NumberMessage _,ServerCallContext _)=>Task.FromResult(new BlockExtention {Blockid=block}))
            .AddMethod(TronPermissionRpcTests.Rpc(WalletSolidity.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),
                (NumberMessage _,ServerCallContext _)=>Task.FromResult(new BlockExtention {Blockid=block}))
            .AddMethod(TronPermissionRpcTests.Rpc(WalletSolidity.Descriptor.FullName,"GetAccount",Account.Parser,Account.Parser),
                (Account request,ServerCallContext _)=>Task.FromResult(new Account {Address=request.Address}))
            .AddMethod(TronPermissionRpcTests.Rpc(Wallet.Descriptor.FullName,"TriggerConstantContract",TriggerSmartContract.Parser,TransactionExtention.Parser),
                (TriggerSmartContract request,ServerCallContext _)=>{
                    var selector=Convert.ToHexString(request.Data.ToByteArray()[..4]);byte[] payload;
                    if(selector==Selector("owner()")){payload=new byte[32];address.AsSpan(1).CopyTo(payload.AsSpan(12));}
                    else if(selector==Selector("revision()"))payload=Encode(new[]{"uint256"},System.Numerics.BigInteger.Zero)[4..];
                    else if(selector==Selector("keyPage(uint256,uint256,uint256)"))
                        payload=Encode(new[]{"string[]","uint256","uint256"},Array.Empty<string>(),System.Numerics.BigInteger.Zero,System.Numerics.BigInteger.Zero)[4..];
                    else {Assert.Equal(Selector("get(string)"),selector);payload=Encode(new[]{"bool","string"},false,"")[4..];}
                    var response=new TransactionExtention {Result=new Return {Result=true}};
                    response.ConstantResult.Add(ByteString.CopyFrom(payload));return Task.FromResult(response);
                })
            .AddMethod(TronPermissionRpcTests.Rpc(Wallet.Descriptor.FullName,"TriggerContract",TriggerSmartContract.Parser,TransactionExtention.Parser),
                (TriggerSmartContract request,ServerCallContext _)=>{
                    var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var transaction=new Transaction {RawData=new Transaction.Types.raw {Timestamp=now,Expiration=now+60000}};
                    transaction.RawData.Contract.Add(new Transaction.Types.Contract {
                        Type=Transaction.Types.Contract.Types.ContractType.TriggerSmartContract,
                        Parameter=Google.Protobuf.WellKnownTypes.Any.Pack(request)});
                    return Task.FromResult(new TransactionExtention {Transaction=transaction,Result=new Return {Result=true}});
                })
            .AddMethod(TronPermissionRpcTests.Rpc(Wallet.Descriptor.FullName,"BroadcastTransaction",Transaction.Parser,Return.Parser),
                async(Transaction request,ServerCallContext _)=>{
                    broadcasts++;Assert.Single(request.Signature);
                    originalId=ByteString.CopyFrom(SHA256.HashData(request.RawData.ToByteArray()));
                    Assert.True(submitted.TryAdd(originalId,request.Clone()));
                    var durable=await store.GetAsync(execution.OperationId,deadline.Token);
                    var attempts=durable.ProviderAttempts.AsEnumerable();
                    if(other!=null)attempts=attempts.Concat((await store.GetAsync(other.OperationId,deadline.Token)).ProviderAttempts);
                    var attempt=Assert.Single(attempts.Where(x=>x.ProviderRequestId==Convert.ToHexString(originalId.ToByteArray()).ToLowerInvariant()));
                    Assert.Equal(Convert.ToBase64String(request.ToByteArray()),attempt.NativePreparation.SignedTransactionBase64);
                    Assert.Null(attempt.AcknowledgedAtUtc);
                    if(!concurrent)Assert.Equal(routed?"storage.route."+broadcasts+".storage.1":"storage.storage."+broadcasts,attempt.ClientRequestId);
                    Assert.DoesNotContain("owned secret holon",System.Text.Encoding.UTF8.GetString(request.ToByteArray()));
                    return new Return {Result=true};
                })
            .AddMethod(TronPermissionRpcTests.Rpc(WalletSolidity.Descriptor.FullName,"GetTransactionInfoById",BytesMessage.Parser,TransactionInfo.Parser),
                (BytesMessage request,ServerCallContext _)=>{
                    Interlocked.Increment(ref feeLookups);
                    return Task.FromResult(new TransactionInfo {Id=request.Value,BlockNumber=1,
                        BlockTimeStamp=1700000001000,Fee=17,Receipt=new ResourceReceipt {Result=Transaction.Types.Result.Types.contractResult.Success}});
                })
            .AddMethod(TronPermissionRpcTests.Rpc(WalletSolidity.Descriptor.FullName,"GetTransactionById",BytesMessage.Parser,Transaction.Parser),
                (BytesMessage request,ServerCallContext _)=>Task.FromResult(submitted.TryGetValue(request.Value,out var tx)?tx.Clone():new Transaction())).Build();
        var server=new Server {Services={service},Ports={new ServerPort("127.0.0.1",0,ServerCredentials.Insecure)}};
        server.Start();
        try {
            var port=server.Ports.Single().BoundPort;
            await using var runtime=new TronSdkRuntime(new Uri($"http://127.0.0.1:{port}"),new Uri($"http://127.0.0.1:{port}"),genesis);
            using var storage=new TronPrivateStorageBackend(runtime.NativeTransfers,owner,owner,()=>key,()=>new byte[32]);
            var provider=new TRONOASIS(nativeTransfers:runtime.NativeTransfers,storageBackend:storage);
            Assert.False((await provider.ActivateProviderAsync()).IsError);
            var invalidEntered=false;
            foreach(var length in new[]{110,128})
                await Assert.ThrowsAsync<ArgumentException>(()=>provider.ExecutePreparedStorageAsync(
                    ()=>{invalidEntered=true;return Task.FromResult(true);},execution,new string('x',length),owner,deadline.Token));
            Assert.False(invalidEntered);
            Assert.Empty((await store.GetAsync(execution.OperationId,deadline.Token)).ProviderAttempts);
            Assert.Equal(0,broadcasts);
            Assert.True(await provider.ExecutePreparedStorageAsync(()=>Task.FromResult(true),execution,new string('x',109),owner,deadline.Token));
            var manager=new ProviderManager(null,new OASISDNA {OASIS=new() {HyperDriveMode=HyperDriveModes.V2,
                StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}}) {IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=false};
            manager.RegisterProvider(provider);var router=new OASISHyperDrive(manager);
            var admitted=new PreparedStorageExecution(execution,"storage",new Dictionary<ProviderType,string> {{ProviderType.TRONOASIS,owner}},Array.Empty<ProviderType>());
            Task<NextGenSoftware.OASIS.Common.OASISResult<IHolon>> Save(Holon holon){
                if(!routed)return provider.SaveHolonAsync(holon,saveChildren:false);
                var request=new StorageOperationRequest {Operation="SaveHolon",Payload=holon,SaveChildren=false,
                    PreferredProvider=ProviderType.TRONOASIS,PreparedExecution=admitted};
                return synchronous?Task.FromResult(router.RouteRequest<IHolon>(request)):router.RouteRequestAsync<IHolon>(request);
            }
            if(rejectPersistence)await execution.BeginProviderAttemptAsync("TRONOASIS",null,routed?"storage.route.1.storage.1":"storage.storage.1",deadline.Token);
            async Task<NextGenSoftware.OASIS.Common.OASISResult<NextGenSoftware.OASIS.API.Core.Interfaces.IHolon>> SavePair(){
                var one=await Save(new Holon {Id=Guid.NewGuid(),Name="owned secret holon one"});
                if(one.IsError)return one;
                await Task.Yield();
                return await Save(new Holon {Id=Guid.NewGuid(),Name="owned secret holon two"});
            }
            if(concurrent){
                var bothEntered=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);var entered=0;
                async Task Run(UsageExecutionHandle handle,string prefix){
                    var result=await provider.ExecutePreparedStorageAsync(async()=>{
                        if(Interlocked.Increment(ref entered)==2)bothEntered.SetResult();
                        await bothEntered.Task.WaitAsync(deadline.Token);
                        return await SavePair();
                    },handle,prefix,owner,deadline.Token);
                    Assert.False(result.IsError,result.Message);
                }
                await Task.WhenAll(Run(execution,"left"),Run(other!,"right"));
                var left=await store.GetAsync(execution.OperationId,deadline.Token);var right=await store.GetAsync(other!.OperationId,deadline.Token);
                Assert.Equal(new[]{"left.storage.1","left.storage.2"},left.ProviderAttempts.Select(x=>x.ClientRequestId));
                Assert.Equal(new[]{"right.storage.1","right.storage.2"},right.ProviderAttempts.Select(x=>x.ClientRequestId));
                Assert.Equal(4,left.ProviderAttempts.Concat(right.ProviderAttempts).Select(x=>x.ProviderRequestId).Distinct().Count());
            }else{
                var result=routed?await SavePair():await provider.ExecutePreparedStorageAsync(SavePair,execution,"storage",owner,deadline.Token);
                Assert.Equal(rejectPersistence,result.IsError);
                if(routed&&rejectPersistence)Assert.Equal(PreparedStorageExecution.RejectedCode,result.ErrorCode);
            }
            var expectedBroadcasts=rejectPersistence?0:concurrent?4:2;
            Assert.Equal(expectedBroadcasts,broadcasts);
            var retained=await store.GetAsync(execution.OperationId,deadline.Token);
            Assert.Equal(rejectPersistence?1:2,retained.ProviderAttempts.Count);
            if(!rejectPersistence){
                var prepared=retained.ProviderAttempts[0];
                var fee=await provider.GetPreparedNativeFeeReceiptAsync(prepared,new Account {Address=ByteString.CopyFrom(address)},deadline.Token);
                Assert.False(fee.IsError,fee.Message);
                Assert.Equal(prepared.ProviderRequestId,fee.Result.GetProperty("transactionHash").GetString());
                Assert.Equal("17",fee.Result.GetProperty("netUnits").GetString());
                Assert.Equal(owner,fee.Result.GetProperty("payer").GetString());
                var beforeRejected=feeLookups;
                var wrongAccount=await provider.GetPreparedNativeFeeReceiptAsync(prepared,new Account(),deadline.Token);
                Assert.True(wrongAccount.IsError);Assert.IsType<System.IO.InvalidDataException>(wrongAccount.Exception);
                var substituted=new UsageProviderAttempt {Provider="TRONOASIS",ProviderRequestId=new string('0',64),NativePreparation=prepared.NativePreparation};
                var wrongHash=await provider.GetPreparedNativeFeeReceiptAsync(substituted,new Account {Address=ByteString.CopyFrom(address)},deadline.Token);
                Assert.True(wrongHash.IsError);Assert.IsType<System.IO.InvalidDataException>(wrongHash.Exception);
                Assert.Equal(beforeRejected,feeLookups);
                Assert.Equal(expectedBroadcasts,broadcasts);
            }
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            Assert.Null(typeof(TronNativeTransferClient).GetField("_persistPreparedSubmission",flags)!.GetValue(runtime.NativeTransfers));
            await Assert.ThrowsAsync<InvalidOperationException>(()=>provider.ExecutePreparedStorageAsync<int>(
                ()=>throw new InvalidOperationException("owned operation failed"),execution,"failure",owner,deadline.Token));
            await Assert.ThrowsAsync<InvalidOperationException>(()=>provider.ExecutePreparedStorageAsync(
                ()=>provider.ExecutePreparedStorageAsync(()=>Task.FromResult(1),execution,"nested",owner,deadline.Token),
                execution,"outer",owner,deadline.Token));
            Assert.Equal(42,await provider.ExecutePreparedStorageAsync(()=>Task.FromResult(42),execution,"restored",owner,deadline.Token));
            using var cancelled=new CancellationTokenSource();cancelled.Cancel();var called=false;
            await Assert.ThrowsAnyAsync<OperationCanceledException>(async()=>await provider.ExecutePreparedStorageAsync(
                ()=>{called=true;return Task.FromResult(1);},execution,"cancelled",owner,cancelled.Token));
            Assert.False(called);
            using var cancelledInside=new CancellationTokenSource();
            var cancelledRead=await provider.ExecutePreparedStorageAsync(()=>{
                cancelledInside.Cancel();
                return provider.LoadHolonAsync(Guid.NewGuid(),loadChildren:false);
            },execution,"cancelled-read",owner,cancelledInside.Token);
            Assert.True(cancelledRead.IsError);Assert.IsAssignableFrom<OperationCanceledException>(cancelledRead.Exception);
            Assert.Equal(expectedBroadcasts,broadcasts);
            await provider.DeActivateProviderAsync();
        } finally {await server.ShutdownAsync();await mongo.DropDatabaseAsync(database);}
    }
}
