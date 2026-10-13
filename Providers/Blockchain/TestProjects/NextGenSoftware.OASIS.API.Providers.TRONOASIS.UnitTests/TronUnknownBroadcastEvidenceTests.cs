using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using TronNet;
using TronNet.Protocol;
using Xunit;
using Grpc.Core;
using System.Linq;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public class TronUnknownBroadcastEvidenceTests
{
    [Theory]
    [InlineData("construction")] [InlineData("unknown")] [InlineData("readback")]
    public void EscrowFailureAlwaysRetainsConfirmedApprovalAndOnlyKnownMutationIdentity(string mode)
    {
        var approval=new string('a',64);var mutation=new string('b',64);var genesis=new string('c',64);
        var root=new InvalidOperationException("controlled escrow failure");
        Exception cause=mode=="unknown"?new TronBroadcastOutcomeUnknownException(mutation,genesis,1,2,root)
            :mode=="readback"?new TronCommittedReadbackException(mutation,genesis,root):root;
        var failure=new TronEscrowReconciliationException(approval,genesis,cause);
        Assert.Equal(approval,failure.ApprovalTransactionId);Assert.Same(cause,failure.InnerException);
        Assert.Equal(mode=="construction"?null:mutation,failure.MutationTransactionId);
        Assert.Equal(mode=="construction"?approval:mutation,failure.OriginalTransactionId);
        Assert.True(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(new AggregateException(failure)));
    }
    [Fact]
    public void ConfirmedReadbackFailureRetainsOriginalAndReconciliationMarker()
    {
        var cause=new InvalidOperationException("controlled account readback failure");
        var failure=new TronCommittedReadbackException(new string('a',64),new string('B',64),cause);
        Assert.IsAssignableFrom<NextGenSoftware.OASIS.API.Core.Interfaces.INativeMutationReconciliationRequired>(failure);
        Assert.Same(cause,failure.InnerException);Assert.Equal(new string('a',64),failure.OriginalTransactionId);
        Assert.Equal(new string('b',64),failure.ChainGenesisId);
        Assert.Throws<ArgumentException>(()=>new TronCommittedReadbackException("replacement",new string('b',64),cause));
    }
    [Theory]
    [InlineData("valid")] [InlineData("foreign-owner")] [InlineData("foreign-signature")]
    public async Task SignedPermissionUpdateBindsOriginalOwnerAndSignature(string mode)
    {
        await using var runtime=new TronSdkRuntime(new Uri("http://127.0.0.1:1"),new Uri("http://127.0.0.1:1"),new string('a',64));
        var signer=(ITransactionClient)typeof(TronNativeTransferClient).GetField("_transactions",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(runtime.NativeTransfers)!;
        var owner=new byte[21];owner[0]=0x41;new Nethereum.Signer.EthECKey(new string('0',63)+"1").GetPublicAddressAsBytes().CopyTo(owner,1);
        var foreign=new byte[21];foreign[0]=0x41;new Nethereum.Signer.EthECKey(new string('0',63)+"2").GetPublicAddressAsBytes().CopyTo(foreign,1);
        var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var transaction=new Transaction{RawData=new Transaction.Types.raw{Timestamp=now,Expiration=now+60000}};
        transaction.RawData.Contract.Add(new Transaction.Types.Contract{Type=Transaction.Types.Contract.Types.ContractType.AccountPermissionUpdateContract,
            Parameter=Google.Protobuf.WellKnownTypes.Any.Pack(new AccountPermissionUpdateContract{OwnerAddress=ByteString.CopyFrom(mode=="foreign-owner"?foreign:owner)})});
        var signed=signer.GetTransactionSign(transaction,new string('0',63)+(mode=="foreign-signature"?"2":"1"));
        var id=Convert.ToHexString(SHA256.HashData(signed.RawData.ToByteArray())).ToLowerInvariant();
        Action verify=()=>TronPreparationVerifier.VerifyOriginal(Convert.ToBase64String(signed.ToByteArray()),id,owner,100000000);
        if(mode=="valid")verify();else Assert.Throws<System.IO.InvalidDataException>(verify);
    }
    [Fact]
    public async Task PermissionLoaderPreCancellationCannotQueryNetwork()
    {
        await using var runtime=new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),new Uri("http://127.0.0.1:50052"),new string('a',64));
        using var cancellation=new CancellationTokenSource();cancellation.Cancel();
        var error=await Assert.ThrowsAsync<OperationCanceledException>(()=>runtime.NativeTransfers.LoadPermissionAuthorityAsync("invalid",cancellation.Token));
        Assert.Equal(cancellation.Token,error.CancellationToken);
    }
    [Theory]
    [InlineData("empty")] [InlineData("unsupported")] [InlineData("wrong-owner")]
    [InlineData("zero-timestamp")] [InlineData("invalid-expiry")] [InlineData("negative-fee")]
    [InlineData("excess-fee")]
    [InlineData("active-permission")]
    public async Task ActualSignedContractMustEstablishTrustedOperationOwner(string mode)
    {
        await using var runtime=new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),new Uri("http://127.0.0.1:50052"),new string('a',64));
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var signer=(ITransactionClient)typeof(TronNativeTransferClient).GetField("_transactions",flags)!.GetValue(runtime.NativeTransfers)!;
        var sender=new byte[21];sender[0]=0x41;
        new Nethereum.Signer.EthECKey(new string('0',63)+"1").GetPublicAddressAsBytes().CopyTo(sender,1);
        var now=DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var transaction=new Transaction{RawData=new Transaction.Types.raw{Timestamp=now,Expiration=now+60000}};
        if(mode=="zero-timestamp")transaction.RawData.Timestamp=0;
        if(mode=="invalid-expiry")transaction.RawData.Expiration=now;
        if(mode=="negative-fee")transaction.RawData.FeeLimit=-1;
        if(mode=="excess-fee")transaction.RawData.FeeLimit=101;
        if(mode!="empty"){
            var owner=(byte[])sender.Clone();if(mode=="wrong-owner")owner[1]^=1;
            transaction.RawData.Contract.Add(new Transaction.Types.Contract{
                PermissionId=mode=="active-permission"?1:0,
                Type=mode=="unsupported"?(Transaction.Types.Contract.Types.ContractType)999:Transaction.Types.Contract.Types.ContractType.TransferContract,
                Parameter=Google.Protobuf.WellKnownTypes.Any.Pack(new TransferContract{OwnerAddress=ByteString.CopyFrom(owner),ToAddress=ByteString.CopyFrom(sender),Amount=1})
            });
        }
        var signed=signer.GetTransactionSign(transaction,new string('0',63)+"1");
        var id=Convert.ToHexString(SHA256.HashData(signed.RawData.ToByteArray())).ToLowerInvariant();
        var error=Assert.Throws<System.IO.InvalidDataException>(()=>TronPreparationVerifier.VerifyOriginal(Convert.ToBase64String(signed.ToByteArray()),id,sender,100));
        Assert.Contains(mode=="empty"?"One original":mode=="unsupported"?"not supported":mode=="wrong-owner"?"owner differs":mode=="excess-fee"?"ceiling":mode=="active-permission"?"permission authority":"validity window",error.Message);
        if(mode=="active-permission"){
            transaction.RawData.Contract[0].PermissionId=2;
            signed=signer.GetTransactionSign(transaction,new string('0',63)+"1");
            id=Convert.ToHexString(SHA256.HashData(signed.RawData.ToByteArray())).ToLowerInvariant();
            var mask=new byte[32];mask[0]=2;
            var permission=new Permission{Id=2,Type=(Permission.Types.PermissionType)2,Threshold=1,Operations=ByteString.CopyFrom(mask)};
            permission.Keys.Add(new Key{Address=ByteString.CopyFrom(sender),Weight=1});
            var account=new Account{Address=ByteString.CopyFrom(sender)};account.ActivePermission.Add(permission);
            var encoded=Convert.ToBase64String(signed.ToByteArray());
            TronPreparationVerifier.VerifyWithAccountAuthority(encoded,id,sender,100,account);
            permission.Threshold=2;
            Assert.Throws<System.IO.InvalidDataException>(()=>TronPreparationVerifier.VerifyWithAccountAuthority(encoded,id,sender,100,account));
            var secondAddress=new byte[21];secondAddress[0]=0x41;
            new Nethereum.Signer.EthECKey(new string('0',63)+"2").GetPublicAddressAsBytes().CopyTo(secondAddress,1);
            permission.Keys.Add(new Key{Address=ByteString.CopyFrom(secondAddress),Weight=1});
            var twoSignatures=signer.GetTransactionSign(signed,new string('0',63)+"2");
            Assert.Equal(2,twoSignatures.Signature.Count);
            TronPreparationVerifier.VerifyWithAccountAuthority(Convert.ToBase64String(twoSignatures.ToByteArray()),id,sender,100,account);
            twoSignatures.Signature[1]=twoSignatures.Signature[0];
            Assert.Throws<System.IO.InvalidDataException>(()=>TronPreparationVerifier.VerifyWithAccountAuthority(Convert.ToBase64String(twoSignatures.ToByteArray()),id,sender,100,account));
        }
    }
    [Fact]
    public async Task PreparedRuntimeViewReusesExistingSdkServicesWithoutChangingOrdinaryClient()
    {
        await using var runtime=new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),new Uri("http://127.0.0.1:50052"),new string('a',64));
        Func<string,string,string,CancellationToken,Task> callback=(_,_,_,_)=>Task.CompletedTask;
        var prepared=runtime.CreatePreparedNativeTransfers(callback);
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        foreach(var field in new[]{"_wallet","_transactions"}){
            var member=typeof(TronNativeTransferClient).GetField(field,flags)!;
            Assert.Same(member.GetValue(runtime.NativeTransfers),member.GetValue(prepared));
        }
        var hook=typeof(TronNativeTransferClient).GetField("_persistPreparedSubmission",flags)!;
        Assert.Same(callback,hook.GetValue(prepared));Assert.Null(hook.GetValue(runtime.NativeTransfers));
        Assert.Throws<ArgumentNullException>(()=>runtime.CreatePreparedNativeTransfers(null!));
        Assert.Throws<ArgumentNullException>(()=>runtime.CreatePreparedNativeTransfers(null!,"attempt",new byte[21]));
        Assert.Throws<ArgumentNullException>(()=>runtime.CreatePreparedNativeTransfersWithAccountAuthority(null!,"attempt","owner"));
    }
    public class InterruptedTransactionProxy : DispatchProxy
    {
        public ITransactionClient Signer = null!;
        public Transaction Signed = null!;
        public int BroadcastCount;
        public int SignCount;
        public string? ReplacementSigningKey;
        public bool BroadcastAccepted;
        public bool NullAcknowledgement;
        public int? NegativeResponseCode;
        public Action? BeforeBroadcast;
        public Exception Interruption = new TimeoutException("controlled transport interruption");
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == nameof(ITransactionClient.GetTransactionSign))
            {
                SignCount++;
                return Signer.GetTransactionSign((Transaction)args![0]!, ReplacementSigningKey ?? (string)args[1]!);
            }
            if (method.Name == nameof(ITransactionClient.BroadcastTransactionAsync))
            {
                Signed = ((Transaction)args![0]!).Clone();
                BeforeBroadcast?.Invoke();
                BroadcastCount++;
                if(NegativeResponseCode.HasValue){
                    var response=new Return{Result=false};
                    var property=typeof(Return).GetProperty(nameof(Return.Code))!;
                    property.SetValue(response,Enum.ToObject(property.PropertyType,NegativeResponseCode.Value));
                    return Task.FromResult(response);
                }
                return NullAcknowledgement?Task.FromResult<Return>(null!):BroadcastAccepted ? Task.FromResult(new Return{Result=true}) : Task.FromException<Return>(Interruption);
            }
            throw new InvalidOperationException("Unexpected SDK operation in controlled interruption test: " + method.Name);
        }
    }

    public class InterruptedConfirmationWalletProxy:DispatchProxy
    {
        public IWalletClient Original=null!;
        public Exception Failure=null!;
        public Func<bool> HasBroadcast = null!;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            if(method!.Name==nameof(IWalletClient.GetSolidityProtocol)&&HasBroadcast())throw Failure;
            return method.Invoke(Original,args);
        }
    }
    [Theory]
    [InlineData(false,false,false,false,false,null)] [InlineData(true,false,false,false,false,null)] [InlineData(false,true,false,false,false,null)] [InlineData(false,false,true,false,false,null)]
    [InlineData(false,false,true,true,false,null)] [InlineData(false,false,true,false,true,null)]
    [InlineData(false,false,true,false,false,5)] [InlineData(false,false,true,false,false,11)]
    [InlineData(false,false,true,false,false,3)] [InlineData(false,false,true,false,false,20)] [InlineData(false,false,true,false,false,99)]
    [InlineData(false,false,true,false,false,0)]
    public async Task ActualClientSignsOnceAndPreservesOriginalEvidenceWhenBroadcastOrConfirmationIsInterrupted(bool broadcastAccepted,bool persistenceRejected,bool prepared,bool wrongSignature,bool nullAcknowledgement,int? negativeResponseCode)
    {
        const string genesis = "0000000000000000c93baa76a4a508f798a96f59156d9eb17ecede8ec845df2f";
        var service=ServerServiceDefinition.CreateBuilder()
            .AddMethod(TronPermissionRpcTests.Rpc(Wallet.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),(NumberMessage request,ServerCallContext context)=>Task.FromResult(new BlockExtention{Blockid=ByteString.CopyFrom(Convert.FromHexString(genesis))}))
            .AddMethod(TronPermissionRpcTests.Rpc(WalletSolidity.Descriptor.FullName,"GetBlockByNum2",NumberMessage.Parser,BlockExtention.Parser),(NumberMessage request,ServerCallContext context)=>Task.FromResult(new BlockExtention{Blockid=ByteString.CopyFrom(Convert.FromHexString(genesis))}))
            .AddMethod(TronPermissionRpcTests.Rpc(WalletSolidity.Descriptor.FullName,"GetAccount",Account.Parser,Account.Parser),(Account request,ServerCallContext context)=>Task.FromResult(new Account{Address=request.Address})).Build();
        var server=new Server{Services={service},Ports={new ServerPort("127.0.0.1",0,ServerCredentials.Insecure)}};server.Start();
        try {
        var port=server.Ports.Single().BoundPort;
        await using var runtime = new TronSdkRuntime(new Uri($"http://127.0.0.1:{port}"), new Uri($"http://127.0.0.1:{port}"), genesis);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var signer = (ITransactionClient)typeof(TronNativeTransferClient).GetField("_transactions", flags)!.GetValue(runtime.NativeTransfers)!;
        var wallet = (IWalletClient)typeof(TronNativeTransferClient).GetField("_wallet", flags)!.GetValue(runtime.NativeTransfers)!;
        var proxy = DispatchProxy.Create<ITransactionClient, InterruptedTransactionProxy>();
        var controlled = (InterruptedTransactionProxy)(object)proxy;
        controlled.Signer = signer;
        if(wrongSignature)controlled.ReplacementSigningKey=new string('0',63)+"2";
        controlled.BroadcastAccepted=broadcastAccepted;
        controlled.NullAcknowledgement=nullAcknowledgement;
        controlled.NegativeResponseCode=negativeResponseCode;
        if(broadcastAccepted){
            var confirmationWallet=DispatchProxy.Create<IWalletClient,InterruptedConfirmationWalletProxy>();
            var confirmationControl=(InterruptedConfirmationWalletProxy)(object)confirmationWallet;
            confirmationControl.Original=wallet;confirmationControl.Failure=controlled.Interruption;
            confirmationControl.HasBroadcast=()=>controlled.BroadcastCount>0;
            wallet=confirmationWallet;
        }
        var preparationCalls=0;var persistenceFailure=new InvalidOperationException("controlled persistence failure");
        if(prepared)controlled.BeforeBroadcast=()=>Assert.Equal(1,preparationCalls);
        string? persistedId=null;
        var client = new TronNativeTransferClient(wallet, proxy, genesis,100000000,persistenceRejected||prepared?async(id,chain,bytes,token)=>{
            Assert.Equal(genesis,chain);
            var prepared=Transaction.Parser.ParseFrom(Convert.FromBase64String(bytes));
            Assert.Equal(Convert.ToHexString(SHA256.HashData(prepared.RawData.ToByteArray())).ToLowerInvariant(),id);
            Assert.Single(prepared.Signature);await Task.Yield();preparationCalls++;persistedId=id;
            var expectedSender=new byte[21];expectedSender[0]=0x41;
            new Nethereum.Signer.EthECKey(new string('0',63)+"1").GetPublicAddressAsBytes().CopyTo(expectedSender,1);
            TronPreparationVerifier.VerifyOriginal(bytes,id,expectedSender);
            Assert.Throws<System.IO.InvalidDataException>(()=>TronPreparationVerifier.VerifyOriginal(bytes,new string('f',64),expectedSender));
            Assert.Throws<System.IO.InvalidDataException>(()=>TronPreparationVerifier.VerifyOriginal(bytes+" ",id,expectedSender));
            Assert.Throws<ArgumentException>(()=>TronPreparationVerifier.VerifyOriginal(bytes,id,new byte[20]));
            var altered=prepared.Clone();altered.Signature.Clear();
            Assert.Throws<System.IO.InvalidDataException>(()=>TronPreparationVerifier.VerifyOriginal(Convert.ToBase64String(altered.ToByteArray()),id,expectedSender));
            altered=prepared.Clone();var badParity=altered.Signature[0].ToByteArray();badParity[64]=2;altered.Signature[0]=ByteString.CopyFrom(badParity);
            Assert.Throws<System.IO.InvalidDataException>(()=>TronPreparationVerifier.VerifyOriginal(Convert.ToBase64String(altered.ToByteArray()),id,expectedSender));
            expectedSender[1]^=1;
            Assert.Throws<System.IO.InvalidDataException>(()=>TronPreparationVerifier.VerifyOriginal(bytes,id,expectedSender));
            if(persistenceRejected)throw persistenceFailure;
        }:null);
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var transaction = new Transaction { RawData = new Transaction.Types.raw { Timestamp = now, Expiration = now + 60000 } };
        var owner=wallet.ParseAddress(new TronECKey(new string('0',63)+"1",TronNetwork.MainNet).GetPublicAddress());
        transaction.RawData.Contract.Add(new Transaction.Types.Contract{Type=Transaction.Types.Contract.Types.ContractType.TransferContract,
            Parameter=Google.Protobuf.WellKnownTypes.Any.Pack(new TransferContract{OwnerAddress=owner,ToAddress=owner,Amount=1})});
        var submit = (Task<string>)typeof(TronNativeTransferClient).GetMethod("SignBroadcastConfirmAsync", flags)!.Invoke(client,
            new object?[] { transaction, new string('0', 63) + "1", "controlled public memo", CancellationToken.None, false, false, false, null })!;
        if(wrongSignature){
            await Assert.ThrowsAsync<System.IO.InvalidDataException>(()=>submit);
            Assert.Equal(1,controlled.SignCount);Assert.Equal(0,preparationCalls);Assert.Equal(0,controlled.BroadcastCount);return;
        }
        if(persistenceRejected){
            Assert.Same(persistenceFailure,await Assert.ThrowsAsync<InvalidOperationException>(()=>submit));
            Assert.Equal(1,preparationCalls);Assert.Equal(1,controlled.SignCount);Assert.Equal(0,controlled.BroadcastCount);return;
        }
        var evidence = await Assert.ThrowsAsync<TronBroadcastOutcomeUnknownException>(() => submit);
        if(prepared){Assert.Equal(1,preparationCalls);Assert.Equal(persistedId,evidence.TransactionId);}
        Assert.Equal(1, controlled.BroadcastCount);
        Assert.Equal(evidence.TransactionId, ((Core.Interfaces.INativeMutationReconciliationRequired)evidence).OriginalTransactionId);
        Assert.Equal(1, controlled.SignCount);
        Assert.Single(controlled.Signed.Signature);
        Assert.Equal(65, controlled.Signed.Signature[0].Length);
        Assert.Equal(Convert.ToHexString(SHA256.HashData(controlled.Signed.RawData.ToByteArray())).ToLowerInvariant(), evidence.TransactionId);
        Assert.Equal(now, evidence.TransactionTimestampMilliseconds);
        Assert.Equal(now + 60000, evidence.TransactionExpirationMilliseconds);
        Assert.Equal(genesis, evidence.ChainGenesisId);
        if(negativeResponseCode.HasValue)Assert.Contains("original reconciliation",evidence.InnerException!.Message);
        else if(nullAcknowledgement)Assert.Contains("no broadcast acknowledgement",evidence.InnerException!.Message);
        else Assert.Same(controlled.Interruption, evidence.InnerException);
        } finally { await server.ShutdownAsync(); }
    }

    [Fact]
    public void OriginalUnknownIdentityAndSignedWindowRemainStructuredWithoutGuessingOutcome()
    {
        var original = new TimeoutException("controlled broadcast interruption");
        var evidence = new TronBroadcastOutcomeUnknownException(new string('a', 64), new string('B', 64),
            1791812970000, 1791813030000, original);
        Assert.Equal(new string('a', 64), evidence.TransactionId);
        Assert.Equal(new string('b', 64), evidence.ChainGenesisId);
        Assert.Equal(1791812970000, evidence.TransactionTimestampMilliseconds);
        Assert.Equal(1791813030000, evidence.TransactionExpirationMilliseconds);
        Assert.Same(original, evidence.InnerException);
        Assert.True(evidence.HasOriginalValidityWindow);
        Assert.Contains("unknown", evidence.Message);
    }

    [Theory]
    [InlineData("invalid", 1, 2)]
    public void MalformedEvidenceCannotStandInForAnOriginalSubmission(string id, long timestamp, long expiry)
        => Assert.Throws<ArgumentException>(() => new TronBroadcastOutcomeUnknownException(id, new string('b', 64),
            timestamp, expiry, new TimeoutException()));

    [Theory]
    [InlineData(0, 2)]
    [InlineData(2, 2)]
    public void MissingValidityEvidenceNeverMasksOriginalUnknownBroadcast(long timestamp, long expiry)
    {
        var original = new TimeoutException();
        var evidence = new TronBroadcastOutcomeUnknownException(new string('a', 64), new string('b', 64), timestamp, expiry, original);
        Assert.False(evidence.HasOriginalValidityWindow);
        Assert.Same(original, evidence.InnerException);
        Assert.Equal(timestamp, evidence.TransactionTimestampMilliseconds);
    }
}
