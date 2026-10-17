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
    [InlineData(false)] [InlineData(true)]
    public async Task PrivateStorageVerificationUsesOnlyConstantCallsAndRejectsForeignOwner(bool foreign)
    {
        var owner=new TronNet.TronECKey(new string('0',63)+"1",TronNet.TronNetwork.MainNet).GetPublicAddress();
        var address=NBitcoin.DataEncoders.Encoders.Base58Check.DecodeData(owner);
        var calls=0;var signingResolutions=0;
        var service=ServerServiceDefinition.CreateBuilder()
            .AddMethod(Rpc(Wallet.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),(NumberMessage request,ServerCallContext context)=>Task.FromResult(new BlockExtention{Blockid=ByteString.CopyFrom(new byte[32])}))
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),(NumberMessage request,ServerCallContext context)=>Task.FromResult(new BlockExtention{Blockid=ByteString.CopyFrom(new byte[32])}))
            .AddMethod(Rpc(Wallet.Descriptor.FullName,"TriggerConstantContract",TriggerSmartContract.Parser,TransactionExtention.Parser),(TriggerSmartContract request,ServerCallContext context)=>{
                calls++;
                var selector=Convert.ToHexString(request.Data.ToByteArray()[..4]);
                string Selector(string signature)=>new Nethereum.Util.Sha3Keccack().CalculateHash(signature)[..8].ToUpperInvariant();
                byte[] payload;
                if(selector==Selector("owner()")){
                    payload=new byte[32];address.AsSpan(1).CopyTo(payload.AsSpan(12));if(foreign)payload[31]^=1;
                }else{
                    var signature=selector==Selector("revision()")?"result(uint256)":"result(string[],uint256,uint256)";
                    Assert.True(selector==Selector("revision()") || selector==Selector("keyPage(uint256,uint256,uint256)"));
                    var types=signature=="result(uint256)"?new[]{"uint256"}:new[]{"string[]","uint256","uint256"};
                    var values=types.Length==1?new object[]{System.Numerics.BigInteger.Zero}:new object[]{Array.Empty<string>(),System.Numerics.BigInteger.Zero,System.Numerics.BigInteger.Zero};
                    var encoded=(byte[])typeof(TronStorageBackend).GetMethod("Encode",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,new object[]{signature,types,values})!;
                    payload=encoded[4..];
                }
                var response=new TransactionExtention{Result=new Return{Result=true}};response.ConstantResult.Add(ByteString.CopyFrom(payload));return Task.FromResult(response);
            }).Build();
        var server=new Server{Services={service},Ports={new ServerPort("127.0.0.1",0,ServerCredentials.Insecure)}};server.Start();
        try{
            var port=server.Ports.Single().BoundPort;
            await using var runtime=new TronSdkRuntime(new Uri($"http://127.0.0.1:{port}"),new Uri($"http://127.0.0.1:{port}"),new string('0',64));
            using var backend=new TronPrivateStorageBackend(runtime.NativeTransfers,owner,owner,()=>{signingResolutions++;throw new InvalidOperationException("Read-only verification must not resolve signing keys.");},()=>new byte[32]);
            if(foreign)await Assert.ThrowsAsync<InvalidOperationException>(()=>backend.VerifyAsync());
            else await backend.VerifyAsync();
            Assert.Equal(0,signingResolutions);Assert.True(calls>0);
        }finally{await server.ShutdownAsync();}
    }
    [Theory]
    [InlineData("call")] [InlineData("deploy")] [InlineData("missing-address")] [InlineData("readback-unavailable")]
    [InlineData("nft-mint")] [InlineData("nft-send")] [InlineData("token-send")] [InlineData("approve")]
    [InlineData("escrow-lock")] [InlineData("escrow-unlock")]
    [InlineData("nft-send-provider")]
    [InlineData("contradictory-broadcast")]
    public async Task RotatedContractSignerAndConfirmedDeploymentReadbackPreserveOneSubmission(string mode)
    {
        var genesis=new string('a',64);var block=ByteString.CopyFrom(Convert.FromHexString(genesis));
        var key=new string('0',63)+"1";var ownerKey=new string('0',63)+"2";
        var owner=new TronNet.TronECKey(ownerKey,TronNet.TronNetwork.MainNet).GetPublicAddress();
        var expectedOwner=new byte[21];expectedOwner[0]=0x41;new Nethereum.Signer.EthECKey(ownerKey).GetPublicAddressAsBytes().CopyTo(expectedOwner,1);
        var signerBytes=new byte[21];signerBytes[0]=0x41;new Nethereum.Signer.EthECKey(key).GetPublicAddressAsBytes().CopyTo(signerBytes,1);
        var contractBytes=new byte[21];contractBytes[0]=0x41;new Nethereum.Signer.EthECKey(new string('0',63)+"3").GetPublicAddressAsBytes().CopyTo(contractBytes,1);
        var contract=NBitcoin.DataEncoders.Encoders.Base58Check.EncodeData(contractBytes);
        var constructions=0;var broadcasts=0;var receiptReads=0;var id=ByteString.Empty;Account authority=null!;
        var submitted=new System.Collections.Generic.List<ByteString>();
        TransactionExtention Construct(IMessage request,Transaction.Types.Contract.Types.ContractType type){
            constructions++;var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();var tx=new Transaction{RawData=new Transaction.Types.raw{Timestamp=now,Expiration=now+60000}};
            if(mode=="escrow-lock"&&constructions==2){Assert.Equal(1,broadcasts);Assert.Equal(1,receiptReads);}
            tx.RawData.Contract.Add(new Transaction.Types.Contract{Type=type,Parameter=Google.Protobuf.WellKnownTypes.Any.Pack(request)});
            return new TransactionExtention{Transaction=tx,Result=new Return{Result=true}};
        }
        var service=ServerServiceDefinition.CreateBuilder()
            .AddMethod(Rpc(Wallet.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),(NumberMessage request,ServerCallContext context)=>Task.FromResult(new BlockExtention{Blockid=block}))
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),(NumberMessage request,ServerCallContext context)=>Task.FromResult(new BlockExtention{Blockid=block}))
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName,"GetAccount",Account.Parser,Account.Parser),(Account request,ServerCallContext context)=>{
                Assert.Equal(ByteString.CopyFrom(expectedOwner),request.Address);
                authority=new Account{Address=request.Address,OwnerPermission=new Permission{Id=0,Threshold=1}};authority.OwnerPermission.Keys.Add(new Key{Address=ByteString.CopyFrom(signerBytes),Weight=1});return Task.FromResult(authority);
            })
            .AddMethod(Rpc(Wallet.Descriptor.FullName,"TriggerConstantContract",TriggerSmartContract.Parser,TransactionExtention.Parser),(TriggerSmartContract request,ServerCallContext context)=>{
                Assert.Equal(authority.Address,request.OwnerAddress);var word=new byte[32];
                var selector=Convert.ToHexString(request.Data.ToByteArray()[..4]);
                if(selector=="6352211E")authority.Address.ToByteArray()[1..].CopyTo(word,12);
                else if(selector=="313CE567")word[31]=6;
                else if(selector=="70A08231")word[31]=5;
                else if(selector=="DD62ED3E")word[31]=0;
                else if(selector==new Nethereum.Util.Sha3Keccack().CalculateHash("locked(address,address)")[..8].ToUpperInvariant())word[31]=5;
                else throw new InvalidOperationException("Unexpected controlled token/NFT query.");
                var result=new TransactionExtention{Result=new Return{Result=true}};result.ConstantResult.Add(ByteString.CopyFrom(word));return Task.FromResult(result);
            })
            .AddMethod(Rpc(Wallet.Descriptor.FullName,"TriggerContract",TriggerSmartContract.Parser,TransactionExtention.Parser),(TriggerSmartContract request,ServerCallContext context)=>Task.FromResult(Construct(request,Transaction.Types.Contract.Types.ContractType.TriggerSmartContract)))
            .AddMethod(Rpc(Wallet.Descriptor.FullName,"DeployContract",CreateSmartContract.Parser,TransactionExtention.Parser),(CreateSmartContract request,ServerCallContext context)=>Task.FromResult(Construct(request,Transaction.Types.Contract.Types.ContractType.CreateSmartContract)))
            .AddMethod(Rpc(Wallet.Descriptor.FullName,"BroadcastTransaction",Transaction.Parser,Return.Parser),(Transaction request,ServerCallContext context)=>{
                broadcasts++;id=ByteString.CopyFrom(SHA256.HashData(request.RawData.ToByteArray()));
                submitted.Add(id);
                TronPreparationVerifier.VerifyWithAccountAuthority(Convert.ToBase64String(request.ToByteArray()),Convert.ToHexString(id.ToByteArray()).ToLowerInvariant(),authority.Address.ToByteArray(),100000000,authority);
                var acknowledgement=new Return{Result=true};
                if(mode=="contradictory-broadcast"){
                    var code=typeof(Return).GetProperty(nameof(Return.Code))!;
                    code.SetValue(acknowledgement,System.Enum.ToObject(code.PropertyType,5));
                }
                return Task.FromResult(acknowledgement);
            })
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName,"GetTransactionInfoById",BytesMessage.Parser,TransactionInfo.Parser),(BytesMessage request,ServerCallContext context)=>{
                receiptReads++;Assert.Equal(id,request.Value);
                if(mode=="readback-unavailable"&&receiptReads>1)throw new RpcException(new Status(StatusCode.Unavailable,"controlled confirmed deployment readback failure"));
                var receipt=new TransactionInfo{Id=id,BlockNumber=1,Receipt=new ResourceReceipt{Result=Transaction.Types.Result.Types.contractResult.Success},ContractAddress=mode=="missing-address"?ByteString.Empty:ByteString.CopyFrom(contractBytes)};
                var word=new byte[32];word[31]=(byte)(mode=="escrow-unlock"||(mode=="escrow-lock"&&broadcasts==2)?5:1);receipt.ContractResult.Add(ByteString.CopyFrom(word));return Task.FromResult(receipt);
            }).Build();
        var server=new Server{Services={service},Ports={new ServerPort("127.0.0.1",0,ServerCredentials.Insecure)}};server.Start();
        try{
            var port=server.Ports.Single().BoundPort;await using var runtime=new TronSdkRuntime(new Uri($"http://127.0.0.1:{port}"),new Uri($"http://127.0.0.1:{port}"),genesis);
            using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));
            if(mode=="contradictory-broadcast"){
                var failure=await Assert.ThrowsAsync<TronBroadcastOutcomeUnknownException>(()=>runtime.NativeTransfers.ExecuteSignedContractAsync(owner,contract,new byte[]{1,2,3,4},key,deadline.Token));
                Assert.Equal(Convert.ToHexString(id.ToByteArray()).ToLowerInvariant(),failure.OriginalTransactionId);
                Assert.True(Core.Helpers.NativeMutationReconciliation.IsRequired(failure));
                Assert.Equal(0,receiptReads);
            }else if(mode == "nft-send-provider"){
                var provider = new TRONOASIS(nativeTransfers:runtime.NativeTransfers);
                // Controlled SDK/RPC operation fixture, not real chain activation evidence.
                typeof(TRONOASIS).GetProperty("IsProviderActivated")!.SetValue(provider,true);
                var response = await provider.SendNFTAsync(new Core.Objects.NFT.Requests.SendWeb3NFTRequest {
                    TokenAddress=contract,TokenId="1",FromNFTTokenAddress=contract+"/1",FromWalletAddress=owner,ToWalletAddress=contract,
                    OwnerPrivateKey=key,Amount=1 });
                Assert.False(response.IsError,response.Message);
                Assert.Equal(Convert.ToHexString(id.ToByteArray()).ToLowerInvariant(),response.Result.TransactionResult);
                Assert.Equal(response.Result.TransactionResult,response.Result.SendNFTTransactionResult);
            }
            else if(mode is "call" or "nft-mint" or "nft-send" or "token-send" or "approve" or "escrow-lock" or "escrow-unlock"){
                var result=await (mode switch {
                    "nft-mint"=>runtime.NativeTransfers.MintNftAsync(owner,owner,contract,System.Numerics.BigInteger.One,"https://example.invalid/metadata",key,"",deadline.Token),
                    "nft-send"=>runtime.NativeTransfers.SendNftAsync(owner,contract,contract,System.Numerics.BigInteger.One,key,"",deadline.Token),
                    "token-send"=>runtime.NativeTransfers.SendTokenAsync(owner,contract,contract,0.000001m,key,"",cancellationToken:deadline.Token),
                    "approve"=>runtime.NativeTransfers.ApproveTokenAsync(owner,contract,contract,System.Numerics.BigInteger.One,key,deadline.Token),
                    "escrow-lock"=>runtime.NativeTransfers.LockEscrowAsync(owner,contract,contract,key,deadline.Token),
                    "escrow-unlock"=>runtime.NativeTransfers.UnlockEscrowAsync(owner,contract,contract,key,deadline.Token),
                    _=>runtime.NativeTransfers.ExecuteSignedContractAsync(owner,contract,new byte[]{1,2,3,4},key,deadline.Token)
                });
                Assert.Equal(Convert.ToHexString(id.ToByteArray()).ToLowerInvariant(),result);
            }
            else if(mode=="deploy"){
                var result=await runtime.NativeTransfers.DeployBytecodeAsync(owner,new byte[]{0x60,0x00},key,deadline.Token);
                Assert.Equal(contract,result.ContractAddress);Assert.Equal(Convert.ToHexString(id.ToByteArray()).ToLowerInvariant(),result.TransactionId);
            }else{
                var error=await Assert.ThrowsAsync<TronCommittedReadbackException>(()=>runtime.NativeTransfers.DeployBytecodeAsync(owner,new byte[]{0x60,0x00},key,deadline.Token));
                Assert.Equal(Convert.ToHexString(id.ToByteArray()).ToLowerInvariant(),error.OriginalTransactionId);Assert.True(Core.Helpers.NativeMutationReconciliation.IsRequired(error));
                if(mode=="readback-unavailable")Assert.IsType<RpcException>(error.InnerException);else Assert.IsType<InvalidOperationException>(error.InnerException);
            }
            Assert.Equal(mode=="escrow-lock"?2:1,constructions);Assert.Equal(mode=="escrow-lock"?2:1,broadcasts);
            Assert.Equal(submitted.Count,submitted.Distinct().Count());
        }finally{await server.ShutdownAsync();}
    }
    internal static async Task WithDefaultOwnerAuthority(Func<TronSdkRuntime,Task> test)
    {
        var genesis=new string('a',64);var block=ByteString.CopyFrom(Convert.FromHexString(genesis));
        var service=ServerServiceDefinition.CreateBuilder()
            .AddMethod(Rpc(Wallet.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),(NumberMessage request,ServerCallContext context)=>Task.FromResult(new BlockExtention{Blockid=block}))
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),(NumberMessage request,ServerCallContext context)=>Task.FromResult(new BlockExtention{Blockid=block}))
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName,"GetAccount",Account.Parser,Account.Parser),(Account request,ServerCallContext context)=>Task.FromResult(new Account{Address=request.Address})).Build();
        var server=new Server{Services={service},Ports={new ServerPort("127.0.0.1",0,ServerCredentials.Insecure)}};server.Start();
        try{var port=server.Ports.Single().BoundPort;await using var runtime=new TronSdkRuntime(new Uri($"http://127.0.0.1:{port}"),new Uri($"http://127.0.0.1:{port}"),genesis);await test(runtime);}
        finally{await server.ShutdownAsync();}
    }
    [Theory]
    [InlineData("rotated")] [InlineData("foreign")] [InlineData("threshold")] [InlineData("revoked")]
    public async Task NativeSendUsesSolidifiedOwnerPermissionBeforeConstruction(string mode)
    {
        var genesis=new string('a',64);var block=ByteString.CopyFrom(Convert.FromHexString(genesis));
        var key=new string('0',63)+"1";var ownerKey=new string('0',63)+"2";
        var owner=new TronNet.TronECKey(ownerKey,TronNet.TronNetwork.MainNet).GetPublicAddress();
        var signer=new TronNet.TronECKey(key,TronNet.TronNetwork.MainNet).GetPublicAddress();
        var address=ByteString.CopyFrom(Convert.FromHexString(new Nethereum.Signer.EthECKey(mode=="foreign"?ownerKey:key).GetPublicAddress()[2..]));
        var signerBytes=ByteString.CopyFrom(new byte[]{0x41}.Concat(address.ToByteArray()).ToArray());
        var constructions=0;var broadcasts=0;var queries=0;var id=ByteString.Empty;
        var service=ServerServiceDefinition.CreateBuilder()
            .AddMethod(Rpc(Wallet.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),(NumberMessage request,ServerCallContext context)=>Task.FromResult(new BlockExtention{Blockid=block}))
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),(NumberMessage request,ServerCallContext context)=>Task.FromResult(new BlockExtention{Blockid=block}))
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName,"GetAccount",Account.Parser,Account.Parser),(Account request,ServerCallContext context)=>{
                queries++;
                var account=new Account{Address=request.Address,OwnerPermission=new Permission{Id=0,Threshold=mode=="threshold"?2:1}};
                account.OwnerPermission.Keys.Add(new Key{Address=mode=="revoked"&&queries>1?request.Address:signerBytes,Weight=1});return Task.FromResult(account);
            })
            .AddMethod(Rpc(Wallet.Descriptor.FullName,"CreateTransaction2",TransferContract.Parser,TransactionExtention.Parser),(TransferContract request,ServerCallContext context)=>{
                constructions++;var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();var tx=new Transaction{RawData=new Transaction.Types.raw{Timestamp=now,Expiration=now+60000}};
                tx.RawData.Contract.Add(new Transaction.Types.Contract{Type=Transaction.Types.Contract.Types.ContractType.TransferContract,Parameter=Google.Protobuf.WellKnownTypes.Any.Pack(request)});
                return Task.FromResult(new TransactionExtention{Transaction=tx,Result=new Return{Result=true}});
            })
            .AddMethod(Rpc(Wallet.Descriptor.FullName,"BroadcastTransaction",Transaction.Parser,Return.Parser),(Transaction request,ServerCallContext context)=>{
                broadcasts++;Assert.Single(request.Signature);id=ByteString.CopyFrom(SHA256.HashData(request.RawData.ToByteArray()));return Task.FromResult(new Return{Result=true});
            })
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName,"GetTransactionInfoById",BytesMessage.Parser,TransactionInfo.Parser),(BytesMessage request,ServerCallContext context)=>Task.FromResult(new TransactionInfo{Id=id,BlockNumber=1})).Build();
        var server=new Server{Services={service},Ports={new ServerPort("127.0.0.1",0,ServerCredentials.Insecure)}};server.Start();
        try{
            var port=server.Ports.Single().BoundPort;await using var runtime=new TronSdkRuntime(new Uri($"http://127.0.0.1:{port}"),new Uri($"http://127.0.0.1:{port}"),genesis);
            using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));
            if(mode=="rotated"){
                var result=await runtime.NativeTransfers.SendAsync(owner,signer,0.000001m,key,"",deadline.Token);
                Assert.Equal(Convert.ToHexString(id.ToByteArray()).ToLowerInvariant(),result);Assert.Equal(1,constructions);Assert.Equal(1,broadcasts);
            }else{
                await Assert.ThrowsAsync<System.IO.InvalidDataException>(()=>runtime.NativeTransfers.SendAsync(owner,signer,0.000001m,key,"",deadline.Token));
                Assert.Equal(mode=="revoked"?1:0,constructions);Assert.Equal(0,broadcasts);
            }
            Assert.Equal(mode=="rotated"||mode=="revoked"?2:1,queries);
        }finally{await server.ShutdownAsync();}
    }
    [Theory]
    [InlineData("rejected")] [InlineData("changed")] [InlineData("unavailable")] [InlineData("permission")] [InlineData("authority")]
    public async Task EscrowConstructionFailureAfterSdkApprovalRequiresOriginalReconciliation(string mode)
    {
        var genesis=new string('a',64);var genesisBytes=ByteString.CopyFrom(Convert.FromHexString(genesis));
        var approvalId=ByteString.Empty;var broadcasts=0;var constructions=0;var authorityQueries=0;
        var service=ServerServiceDefinition.CreateBuilder()
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName,"GetAccount",Account.Parser,Account.Parser),(Account request,ServerCallContext context)=>{
                authorityQueries++;var account=new Account{Address=request.Address};
                if(mode=="authority"&&authorityQueries>3){account.OwnerPermission=new Permission{Id=0,Threshold=2};account.OwnerPermission.Keys.Add(new Key{Address=request.Address,Weight=1});}
                return Task.FromResult(account);
            })
            .AddMethod(Rpc(Wallet.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),(NumberMessage request,ServerCallContext context)=>Task.FromResult(new BlockExtention{Blockid=genesisBytes}))
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),(NumberMessage request,ServerCallContext context)=>Task.FromResult(new BlockExtention{Blockid=genesisBytes}))
            .AddMethod(Rpc(Wallet.Descriptor.FullName,"TriggerConstantContract",TriggerSmartContract.Parser,TransactionExtention.Parser),(TriggerSmartContract request,ServerCallContext context)=>{
                var value=new byte[32];if(Convert.ToHexString(request.Data.ToByteArray()).StartsWith("70A08231"))value[31]=5;
                var result=new TransactionExtention{Result=new Return{Result=true}};result.ConstantResult.Add(ByteString.CopyFrom(value));return Task.FromResult(result);
            })
            .AddMethod(Rpc(Wallet.Descriptor.FullName,"TriggerContract",TriggerSmartContract.Parser,TransactionExtention.Parser),(TriggerSmartContract request,ServerCallContext context)=>{
                constructions++;var approval=Convert.ToHexString(request.Data.ToByteArray()).StartsWith("095EA7B3");
                if(!approval&&mode=="unavailable")throw new RpcException(new Status(StatusCode.Unavailable,"controlled escrow construction failure"));
                if(!approval&&mode=="rejected")return Task.FromResult(new TransactionExtention{Result=new Return{Result=false}});
                var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();var tx=new Transaction{RawData=new Transaction.Types.raw{Timestamp=now,Expiration=now+60000}};
                var supplied=request.Clone();if(!approval&&mode=="changed")supplied.Data=ByteString.CopyFromUtf8("changed");
                tx.RawData.Contract.Add(new Transaction.Types.Contract{Type=Transaction.Types.Contract.Types.ContractType.TriggerSmartContract,Parameter=Google.Protobuf.WellKnownTypes.Any.Pack(supplied)});
                if(!approval&&mode=="permission")tx.RawData.Contract[0].PermissionId=2;
                return Task.FromResult(new TransactionExtention{Transaction=tx,Result=new Return{Result=true}});
            })
            .AddMethod(Rpc(Wallet.Descriptor.FullName,"BroadcastTransaction",Transaction.Parser,Return.Parser),(Transaction request,ServerCallContext context)=>{
                broadcasts++;Assert.Single(request.Signature);approvalId=ByteString.CopyFrom(SHA256.HashData(request.RawData.ToByteArray()));return Task.FromResult(new Return{Result=true});
            })
            .AddMethod(Rpc(WalletSolidity.Descriptor.FullName,"GetTransactionInfoById",BytesMessage.Parser,TransactionInfo.Parser),(BytesMessage request,ServerCallContext context)=>{
                Assert.Equal(approvalId,request.Value);var word=new byte[32];word[31]=1;
                var receipt=new TransactionInfo{Id=approvalId,BlockNumber=1,Receipt=new ResourceReceipt{Result=Transaction.Types.Result.Types.contractResult.Success}};
                receipt.ContractResult.Add(ByteString.CopyFrom(word));return Task.FromResult(receipt);
            }).Build();
        var server=new Server{Services={service},Ports={new ServerPort("127.0.0.1",0,ServerCredentials.Insecure)}};server.Start();
        try{
            var port=server.Ports.Single().BoundPort;await using var runtime=new TronSdkRuntime(new Uri($"http://127.0.0.1:{port}"),new Uri($"http://127.0.0.1:{port}"),genesis);
            var key=new string('0',63)+"1";var owner=new TronNet.TronECKey(key,TronNet.TronNetwork.MainNet).GetPublicAddress();
            var contract=new TronNet.TronECKey(new string('0',63)+"2",TronNet.TronNetwork.MainNet).GetPublicAddress();
            using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var failure=await Assert.ThrowsAsync<TronEscrowReconciliationException>(()=>runtime.NativeTransfers.LockEscrowAsync(owner,contract,contract,key,deadline.Token));
            Assert.Equal(Convert.ToHexString(approvalId.ToByteArray()).ToLowerInvariant(),failure.ApprovalTransactionId);
            Assert.Null(failure.MutationTransactionId);Assert.Equal(failure.ApprovalTransactionId,failure.OriginalTransactionId);
            Assert.True(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(failure));
            if(mode=="unavailable")Assert.IsType<RpcException>(failure.InnerException);
            else if(mode=="authority")Assert.IsType<System.IO.InvalidDataException>(failure.InnerException);
            else Assert.IsType<InvalidOperationException>(failure.InnerException);
            Assert.Equal(1,broadcasts);Assert.Equal(2,constructions);
        }finally{await server.ShutdownAsync();}
    }
    [Theory]
    [InlineData("valid")] [InlineData("empty")] [InlineData("foreign")] [InlineData("unavailable")] [InlineData("rotated")]
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
                var ownerBytes=new byte[21];ownerBytes[0]=0x41;
                new Nethereum.Signer.EthECKey(new string('0',63)+(mode=="rotated"?"2":"1")).GetPublicAddressAsBytes().CopyTo(ownerBytes,1);
                if(request.Address.Equals(ByteString.CopyFrom(ownerBytes))){
                    var account=new Account{Address=request.Address};
                    if(mode=="rotated"){
                        var keyAddress=new byte[21];keyAddress[0]=0x41;new Nethereum.Signer.EthECKey(new string('0',63)+"1").GetPublicAddressAsBytes().CopyTo(keyAddress,1);
                        account.OwnerPermission=new Permission{Id=0,Threshold=1};account.OwnerPermission.Keys.Add(new Key{Address=ByteString.CopyFrom(keyAddress),Weight=1});
                    }
                    return Task.FromResult(account);
                }
                reads++;if(mode=="unavailable")throw new RpcException(new Status(StatusCode.Unavailable,"controlled post-confirmation readback failure"));
                var address=request.Address.ToByteArray();if(mode=="foreign")address[1]^=1;
                return Task.FromResult(mode=="empty"?new Account():new Account{Address=ByteString.CopyFrom(address)});
            }).Build();
        var server=new Server{Services={service},Ports={new ServerPort("127.0.0.1",0,ServerCredentials.Insecure)}};server.Start();
        try{
            var port=server.Ports.Single().BoundPort;
            await using var runtime=new TronSdkRuntime(new Uri($"http://127.0.0.1:{port}"),new Uri($"http://127.0.0.1:{port}"),genesis);
            var key=new string('0',63)+"1";var owner=new TronNet.TronECKey(mode=="rotated"?new string('0',63)+"2":key,TronNet.TronNetwork.MainNet).GetPublicAddress();
            var created=new TronNet.TronECKey(new string('0',63)+"3",TronNet.TronNetwork.MainNet).GetPublicAddress();
            using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var operation=runtime.NativeTransfers.CreateLedgerAccountAsync(owner,created,key,deadline.Token);
            if(mode=="valid"||mode=="rotated"){
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
    internal static Method<TRequest,TResponse> Rpc<TRequest,TResponse>(string service,string name,MessageParser<TRequest> request,MessageParser<TResponse> response)
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
                    const string connection="mongodb://127.0.0.1:27029/?replicaSet=oasisRecoveryRs&serverSelectionTimeoutMS=3000";
                    Assert.Equal(connection,Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI"));
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
