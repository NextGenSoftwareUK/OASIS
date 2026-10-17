using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using Google.Protobuf;
using Grpc.Core;
using TronNet.Protocol;
using Xunit;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronIdBoundEscrowTests
{
    [Theory]
    [InlineData("lock")] [InlineData("existing-allowance")] [InlineData("unlock")]
    [InlineData("runtime")] [InlineData("protocol")] [InlineData("used")]
    [InlineData("foreign")] [InlineData("malformed")]
    [InlineData("allowance")] [InlineData("approval-readback")] [InlineData("readback")]
    [InlineData("existing-readback")] [InlineData("unlock-readback")]
    [InlineData("broadcast-unknown")] [InlineData("runtime-identity")]
    public async Task ExactDepositSdkPreservesIdentityAmountsAndOriginalStages(string mode)
    {
        const string key="0000000000000000000000000000000000000000000000000000000000000001";
        string Address(int n)=>new TronNet.TronECKey(n.ToString("x64"),TronNet.TronNetwork.MainNet).GetPublicAddress();
        ByteString Bytes(string value)=>ByteString.CopyFrom(NBitcoin.DataEncoders.Encoders.Base58Check.DecodeData(value));
        var owner=Address(2);var escrow=Address(3);var token=Address(4);var signer=Address(1);
        var ownerBytes=Bytes(owner);var escrowBytes=Bytes(escrow);var tokenBytes=Bytes(token);
        var genesis=new string('a',64);var code=ByteString.CopyFromUtf8("synthetic reviewed runtime fixture");
        var codeHash=Convert.ToHexString(SHA256.HashData(code.ToByteArray()));
        var lockId=Guid.Parse("5f31afde-1229-4e99-8088-a0034d33a557");
        var nativeId=SHA256.HashData(Encoding.UTF8.GetBytes(lockId.ToString("N")));
        byte[] Word(BigInteger value){var result=new byte[32];var bytes=value.ToByteArray(true,true);bytes.CopyTo(result,32-bytes.Length);return result;}
        string Selector(string name)=>new Nethereum.Util.Sha3Keccack().CalculateHash(name)[..8].ToUpperInvariant();
        var unlocking=mode is "unlock" or "foreign" or "unlock-readback";
        var depositExists=unlocking||mode=="used";var released=false;
        BigInteger ownerBalance=unlocking?877:1000,escrowBalance=unlocking?123:0;
        BigInteger allowance=mode is "existing-allowance" or "existing-readback"?123:mode=="allowance"?1:0;
        var constructed=0;var broadcasts=0;var persisted=new Dictionary<string,string>();var originalIds=new List<string>();
        var lastId=ByteString.Empty;var approvalConfirmed=false;
        var authority=new Account{Address=ownerBytes,OwnerPermission=new Permission{Id=0,Threshold=1}};
        authority.OwnerPermission.Keys.Add(new Key{Address=Bytes(signer),Weight=1});
        var service=ServerServiceDefinition.CreateBuilder()
            .AddMethod(TronPermissionRpcTests.Rpc(Wallet.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),
                (NumberMessage _,ServerCallContext _)=>Task.FromResult(new BlockExtention{Blockid=ByteString.CopyFrom(Convert.FromHexString(genesis))}))
            .AddMethod(TronPermissionRpcTests.Rpc(WalletSolidity.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),
                (NumberMessage _,ServerCallContext _)=>Task.FromResult(new BlockExtention{Blockid=ByteString.CopyFrom(Convert.FromHexString(genesis))}))
            .AddMethod(TronPermissionRpcTests.Rpc(WalletSolidity.Descriptor.FullName,"GetAccount",Account.Parser,Account.Parser),
                (Account request,ServerCallContext _)=>{Assert.Equal(ownerBytes,request.Address);return Task.FromResult(authority);})
            .AddMethod(TronPermissionRpcTests.Rpc(Wallet.Descriptor.FullName,"GetContractInfo",BytesMessage.Parser,SmartContractDataWrapper.Parser),
                (BytesMessage request,ServerCallContext _)=>{
                    Assert.Equal(escrowBytes,request.Value);
                    return Task.FromResult(new SmartContractDataWrapper{SmartContract=new SmartContract{ContractAddress=mode=="runtime-identity"?tokenBytes:escrowBytes},
                        Runtimecode=mode=="runtime"?ByteString.CopyFromUtf8("different code"):code});
                })
            .AddMethod(TronPermissionRpcTests.Rpc(Wallet.Descriptor.FullName,"TriggerConstantContract",TriggerSmartContract.Parser,TransactionExtention.Parser),
                (TriggerSmartContract request,ServerCallContext _)=>{
                    Assert.Equal(ownerBytes,request.OwnerAddress);
                    var selector=Convert.ToHexString(request.Data.Span[..4]);byte[] data;
                    if(selector==Selector("escrowProtocolVersion()"))data=Word(mode=="protocol"?2:1);
                    else if(selector==Selector("deposits(bytes32)")){
                        Assert.Equal(escrowBytes,request.ContractAddress);Assert.Equal(nativeId,request.Data.ToByteArray()[4..]);
                        data=new byte[128];
                        if(depositExists){
                            (mode=="foreign"?Bytes(signer):ownerBytes).Span[1..].CopyTo(data.AsSpan(12,20));
                            tokenBytes.Span[1..].CopyTo(data.AsSpan(44,20));Word(mode.Contains("readback",StringComparison.Ordinal)&&broadcasts>0?124:123).CopyTo(data,64);
                            data[127]=(byte)(released?1:0);
                        }
                        if(mode=="malformed")data[0]=1;
                    }else{
                        Assert.Equal(tokenBytes,request.ContractAddress);
                        if(selector==Selector("decimals()"))data=Word(2);
                        else if(selector==Selector("totalSupply()"))data=Word(1000);
                        else if(selector==Selector("allowance(address,address)"))data=Word(allowance);
                        else if(selector==Selector("balanceOf(address)"))data=Word(request.Data.Span[16..36].SequenceEqual(ownerBytes.Span[1..])?ownerBalance:escrowBalance);
                        else throw new InvalidOperationException("Unexpected fixture query.");
                    }
                    var response=new TransactionExtention{Result=new Return{Result=true}};response.ConstantResult.Add(ByteString.CopyFrom(data));return Task.FromResult(response);
                })
            .AddMethod(TronPermissionRpcTests.Rpc(Wallet.Descriptor.FullName,"TriggerContract",TriggerSmartContract.Parser,TransactionExtention.Parser),
                (TriggerSmartContract request,ServerCallContext _)=>{
                    constructed++;Assert.Equal(ownerBytes,request.OwnerAddress);
                    var selector=Convert.ToHexString(request.Data.Span[..4]);
                    if(selector==Selector("approve(address,uint256)")){
                        Assert.Equal(tokenBytes,request.ContractAddress);Assert.Equal(escrowBytes.Span[1..].ToArray(),request.Data.ToByteArray()[16..36]);
                        Assert.Equal(Word(123),request.Data.ToByteArray()[36..68]);
                    }else{
                        Assert.Equal(escrowBytes,request.ContractAddress);Assert.Equal(nativeId,request.Data.ToByteArray()[4..36]);
                        if(unlocking)Assert.Equal(Selector("unlock(bytes32)"),selector);
                        else{
                            Assert.Equal(Selector("lock(bytes32,address,uint256)"),selector);
                            Assert.Equal(tokenBytes.Span[1..].ToArray(),request.Data.ToByteArray()[48..68]);Assert.Equal(Word(123),request.Data.ToByteArray()[68..100]);
                            Assert.True(mode is "existing-allowance" or "existing-readback"||approvalConfirmed);
                        }
                    }
                    var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var transaction=new Transaction{RawData=new Transaction.Types.raw{Timestamp=now,Expiration=now+60000}};
                    transaction.RawData.Contract.Add(new Transaction.Types.Contract{Type=Transaction.Types.Contract.Types.ContractType.TriggerSmartContract,
                        Parameter=Google.Protobuf.WellKnownTypes.Any.Pack(request)});
                    return Task.FromResult(new TransactionExtention{Transaction=transaction,Result=new Return{Result=true}});
                })
            .AddMethod(TronPermissionRpcTests.Rpc(Wallet.Descriptor.FullName,"BroadcastTransaction",Transaction.Parser,Return.Parser),
                (Transaction request,ServerCallContext _)=>{
                    broadcasts++;lastId=ByteString.CopyFrom(SHA256.HashData(request.RawData.ToByteArray()));
                    var hash=Convert.ToHexString(lastId.ToByteArray()).ToLowerInvariant();originalIds.Add(hash);
                    Assert.Equal(Convert.ToBase64String(request.ToByteArray()),persisted[hash]);
                    TronPreparationVerifier.VerifyWithAccountAuthority(persisted[hash],hash,ownerBytes.ToByteArray(),100000000,authority);
                    var call=request.RawData.Contract.Single().Parameter.Unpack<TriggerSmartContract>();
                    if(call.ContractAddress.Equals(tokenBytes))allowance=mode=="approval-readback"?0:123;
                    else {
                        depositExists=true;released=unlocking;ownerBalance+=unlocking?123:-123;escrowBalance+=unlocking?-123:123;
                        if(mode=="broadcast-unknown")throw new RpcException(new Status(StatusCode.Unavailable,"controlled lost acknowledgement"));
                    }
                    return Task.FromResult(new Return{Result=true});
                })
            .AddMethod(TronPermissionRpcTests.Rpc(WalletSolidity.Descriptor.FullName,"GetTransactionInfoById",BytesMessage.Parser,TransactionInfo.Parser),
                (BytesMessage request,ServerCallContext _)=>{
                    Assert.Equal(lastId,request.Value);approvalConfirmed=true;
                    var receipt=new TransactionInfo{Id=lastId,BlockNumber=1,Receipt=new ResourceReceipt{Result=Transaction.Types.Result.Types.contractResult.Success}};
                    receipt.ContractResult.Add(ByteString.CopyFrom(Word(1)));return Task.FromResult(receipt);
                }).Build();
        var server=new Server{Services={service},Ports={new ServerPort("127.0.0.1",0,ServerCredentials.Insecure)}};server.Start();
        try{
            var port=server.Ports.Single().BoundPort;
            await using var runtime=new TronSdkRuntime(new Uri($"http://127.0.0.1:{port}"),new Uri($"http://127.0.0.1:{port}"),genesis);
            var client=runtime.CreatePreparedNativeTransfers((hash,chain,bytes,_)=>{
                Assert.Equal(genesis,chain);Assert.True(persisted.TryAdd(hash,bytes));return Task.CompletedTask;
            });
            using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(15));
            async Task<TronNativeTransferClient.IdBoundEscrowResult> Run()=>await client.ExecuteIdBoundEscrowAsync(owner,escrow,token,lockId,unlocking?0:1.23m,key,codeHash,unlocking,deadline.Token);
            if(mode is "lock" or "existing-allowance" or "unlock"){
                var result=await Run();Assert.Equal(lockId,result.LockId);Assert.Equal(originalIds.Last(),result.TransactionId);
                Assert.Equal(mode=="lock"?originalIds.First():null,result.ApprovalTransactionId);
                Assert.Equal(mode=="lock"?2:1,broadcasts);Assert.Equal(unlocking?1000:877,ownerBalance);
            }else{
                var error=await Assert.ThrowsAnyAsync<Exception>(async()=>await Run());
                if(mode is "approval-readback" or "readback" or "broadcast-unknown"){
                    var reconciliation=Assert.IsType<TronEscrowReconciliationException>(error);
                    Assert.Equal(originalIds.First(),reconciliation.ApprovalTransactionId);
                    Assert.Equal(mode=="approval-readback"?null:originalIds.Last(),reconciliation.MutationTransactionId);
                    Assert.Equal(mode=="approval-readback"?1:2,broadcasts);
                }else if(mode is "existing-readback" or "unlock-readback"){
                    var reconciliation=Assert.IsType<TronCommittedReadbackException>(error);
                    Assert.Equal(originalIds.Single(),reconciliation.OriginalTransactionId);Assert.Equal(1,broadcasts);
                }else Assert.Equal(0,broadcasts);
            }
            Assert.Equal(broadcasts,constructed);Assert.Equal(broadcasts,persisted.Count);
        }finally{await server.ShutdownAsync();}
    }
}
