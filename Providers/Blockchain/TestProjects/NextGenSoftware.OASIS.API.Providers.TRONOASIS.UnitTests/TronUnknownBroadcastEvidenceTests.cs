using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using TronNet;
using TronNet.Protocol;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public class TronUnknownBroadcastEvidenceTests
{
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
    }
    public class InterruptedTransactionProxy : DispatchProxy
    {
        public ITransactionClient Signer = null!;
        public Transaction Signed = null!;
        public int BroadcastCount;
        public int SignCount;
        public bool BroadcastAccepted;
        public Action? BeforeBroadcast;
        public Exception Interruption = new TimeoutException("controlled transport interruption");
        protected override object? Invoke(MethodInfo? method, object?[]? args)
        {
            if (method!.Name == nameof(ITransactionClient.GetTransactionSign))
            {
                SignCount++;
                return Signer.GetTransactionSign((Transaction)args![0]!, (string)args[1]!);
            }
            if (method.Name == nameof(ITransactionClient.BroadcastTransactionAsync))
            {
                Signed = ((Transaction)args![0]!).Clone();
                BeforeBroadcast?.Invoke();
                BroadcastCount++;
                return BroadcastAccepted ? Task.FromResult(new Return{Result=true}) : Task.FromException<Return>(Interruption);
            }
            throw new InvalidOperationException("Unexpected SDK operation in controlled interruption test: " + method.Name);
        }
    }

    public class InterruptedConfirmationWalletProxy:DispatchProxy
    {
        public IWalletClient Original=null!;
        public Exception Failure=null!;
        protected override object? Invoke(MethodInfo? method,object?[]? args)
        {
            if(method!.Name==nameof(IWalletClient.GetSolidityProtocol))throw Failure;
            return method.Invoke(Original,args);
        }
    }
    [Theory]
    [InlineData(false,false,false)] [InlineData(true,false,false)] [InlineData(false,true,false)] [InlineData(false,false,true)]
    public async Task ActualClientSignsOnceAndPreservesOriginalEvidenceWhenBroadcastOrConfirmationIsInterrupted(bool broadcastAccepted,bool persistenceRejected,bool prepared)
    {
        const string genesis = "0000000000000000c93baa76a4a508f798a96f59156d9eb17ecede8ec845df2f";
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"), new Uri("http://127.0.0.1:50052"), genesis);
        var flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var signer = (ITransactionClient)typeof(TronNativeTransferClient).GetField("_transactions", flags)!.GetValue(runtime.NativeTransfers)!;
        var wallet = (IWalletClient)typeof(TronNativeTransferClient).GetField("_wallet", flags)!.GetValue(runtime.NativeTransfers)!;
        var proxy = DispatchProxy.Create<ITransactionClient, InterruptedTransactionProxy>();
        var controlled = (InterruptedTransactionProxy)(object)proxy;
        controlled.Signer = signer;
        controlled.BroadcastAccepted=broadcastAccepted;
        if(broadcastAccepted){
            var confirmationWallet=DispatchProxy.Create<IWalletClient,InterruptedConfirmationWalletProxy>();
            var confirmationControl=(InterruptedConfirmationWalletProxy)(object)confirmationWallet;
            confirmationControl.Original=wallet;confirmationControl.Failure=controlled.Interruption;
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
        Assert.Same(controlled.Interruption, evidence.InnerException);
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
