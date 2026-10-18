using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Text.Json;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class PolkadotReadOnlyNativeTests
{
    private const string Genesis="0x91b171bb158e2d3848fa23a9f1c25182fb8e20313b2c1eb49219da7a70ce90c3";

    [DataTestMethod][DataRow(false,false,false,false)][DataRow(true,false,false,false)][DataRow(false,true,false,false)][DataRow(false,false,true,false)][DataRow(false,false,false,true)][TestCategory("ReadOnlyNative")]
    public async Task NativePreparationPipeAcknowledgesOnlyAfterVerifiedPersistence(bool rejectPersistence,bool rejectOriginalIdentity,bool cancelPersistence,bool failAfterFlush)
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_POLKADOT_READONLY_NATIVE"));
        var journal=Environment.GetEnvironmentVariable("OASIS_POLKADOT_NATIVE_PREPARATION_JOURNAL")!;
        Assert.IsFalse(string.IsNullOrWhiteSpace(journal),"Explicit fixed fixture journal path required.");
        Assert.IsFalse(File.Exists(journal),"Preserve original pending preparation fixture; never overwrite/replay it.");
        using var provider=new PolkadotOASIS("https://rpc.polkadot.io",Genesis);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var prepared=await provider.PrepareNativeTokenTransferAsync(new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests.SendWeb3TokenRequest
        {
            FromWalletAddress="0x189dac29296d31814dc8c56cf3d36a0543372bba7538fa322a4aebfebc39e056",
            ToWalletAddress="0xd43593c715fdd31c61141abd04a99fd6822c8558854ccde39a5684e7a56da27d",Amount=1,
            OwnerPrivateKey="0x"+string.Concat(Enumerable.Repeat("01",32))
        });
        Assert.IsFalse(prepared.IsError,prepared.Exception?.ToString());
        var calls=0;var flushed=false;var denied=new IOException("Owned persistence fixture rejected before acknowledgement.");
        var cancelled=new OperationCanceledException("Owned persistence fixture cancelled before acknowledgement.");
        using var server=new PreparedSubmissionServer(async(evidence,token)=>{
            calls++;
            var verified=await provider.VerifyNativeTokenTransferPreparationAsync(evidence);
            Assert.IsFalse(verified.IsError,verified.Exception?.ToString());
            if(cancelPersistence)throw cancelled;
            if(rejectPersistence)throw denied;
            using var file=new FileStream(journal,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough);
            await file.WriteAsync(JsonSerializer.SerializeToUtf8Bytes(evidence),token);file.Flush(true);flushed=true;
            if(failAfterFlush)throw denied;
        },rejectOriginalIdentity?"0x"+new string('0',64):prepared.Result.GetProperty("transactionHash").GetString());
        var method=typeof(PolkadotOASIS).GetMethod("InvokePolkadotSdkAsync",BindingFlags.Instance|BindingFlags.NonPublic)!;
        var pending=(Task<JsonElement>)method.Invoke(provider,new object[]{new{operation="persistNativePreparation",endpoint="https://rpc.polkadot.io",
            genesisHash=Genesis,preparation=prepared.Result},CancellationToken.None,server})!;
        if(failAfterFlush){
            var observed=await Assert.ThrowsExceptionAsync<IOException>(async()=>await pending);
            Assert.AreSame(denied,observed);Assert.IsTrue(flushed);Assert.IsNull(server.PreparedTransactionId);
            using var retained=JsonDocument.Parse(await File.ReadAllTextAsync(journal));
            Assert.AreEqual(prepared.Result.GetProperty("transactionHash").GetString(),retained.RootElement.GetProperty("transactionHash").GetString());
            // Explicitly reconcile this exact owned, unsubmitted fixture after failure; never replay it.
            File.Delete(journal);
        }else if(cancelPersistence){
            try{await pending;Assert.Fail("Cancelled persistence must not be acknowledged.");}
            catch(OperationCanceledException observed){Assert.AreSame(cancelled,observed);}
            Assert.IsFalse(flushed);Assert.IsNull(server.PreparedTransactionId);Assert.IsFalse(File.Exists(journal));
        }else if(rejectOriginalIdentity){
            await Assert.ThrowsExceptionAsync<InvalidDataException>(async()=>await pending);
            Assert.AreEqual(0,calls);Assert.IsFalse(flushed);Assert.IsNull(server.PreparedTransactionId);Assert.IsFalse(File.Exists(journal));
        }else if(rejectPersistence){
            var observed=await Assert.ThrowsExceptionAsync<IOException>(async()=>await pending);
            Assert.AreSame(denied,observed);Assert.IsFalse(flushed);Assert.IsNull(server.PreparedTransactionId);Assert.IsFalse(File.Exists(journal));
        }else{
            var acknowledged=await pending;
            Assert.IsTrue(flushed);Assert.IsTrue(acknowledged.GetProperty("persisted").GetBoolean());Assert.IsFalse(acknowledged.GetProperty("submitted").GetBoolean());
            Assert.AreEqual(prepared.Result.GetProperty("transactionHash").GetString(),server.PreparedTransactionId);
            using var stored=JsonDocument.Parse(await File.ReadAllTextAsync(journal));
            Assert.AreEqual(server.PreparedTransactionId,stored.RootElement.GetProperty("transactionHash").GetString());
            File.Delete(journal); // Only this confirmed, unsubmitted, owned fixture record; failures preserve it.
        }
        Assert.AreEqual(rejectOriginalIdentity?0:1,calls);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)][TestCategory("ReadOnlyNative")]
    public async Task PreparationRejectsSignerOrPublicKeyMismatchWithoutBroadcast(bool mismatchPublicKey)
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_POLKADOT_READONLY_NATIVE"));
        using var provider=new PolkadotOASIS("https://rpc.polkadot.io",Genesis);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        const string sender="0x189dac29296d31814dc8c56cf3d36a0543372bba7538fa322a4aebfebc39e056";
        const string other="0xd43593c715fdd31c61141abd04a99fd6822c8558854ccde39a5684e7a56da27d";
        var request=new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests.SendWeb3TokenRequest
        {
            FromWalletAddress=mismatchPublicKey?sender:other, ToWalletAddress=other, Amount=1,
            OwnerPublicKey=mismatchPublicKey?other:null, OwnerPrivateKey="0x"+string.Concat(Enumerable.Repeat("01",32))
        };
        var result=await provider.PrepareNativeTokenTransferAsync(request);
        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message,"Explicit signer does not match");
        Assert.IsFalse(result.Message.Contains(request.OwnerPrivateKey));
    }

    [DataTestMethod][DataRow(false)][DataRow(true)][TestCategory("ReadOnlyNative")]
    public async Task NativeTransferPreparationSignsAndEstimatesButCannotBroadcast(bool withMemo)
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_POLKADOT_READONLY_NATIVE"));
        using var provider=new PolkadotOASIS("https://rpc.polkadot.io",Genesis);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        // Public deterministic TEST-ONLY seed. Never fund this identity; every transport operation is read-only.
        var request=new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests.SendWeb3TokenRequest
        {
            FromWalletAddress="0x189dac29296d31814dc8c56cf3d36a0543372bba7538fa322a4aebfebc39e056",
            ToWalletAddress="0xd43593c715fdd31c61141abd04a99fd6822c8558854ccde39a5684e7a56da27d",
            Amount=1.0000000001m,OwnerPrivateKey="0x"+string.Concat(Enumerable.Repeat("01",32)),
            MemoText=withMemo?"OASIS preparation only 🌳":null
        };
        var result=await provider.PrepareNativeTokenTransferAsync(request);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());
        var prepared=result.Result;
        Assert.IsFalse(prepared.GetProperty("submitted").GetBoolean());
        Assert.AreEqual(Genesis,prepared.GetProperty("genesisHash").GetString());
        Assert.AreEqual("10000000001",prepared.GetProperty("units").GetString());
        Assert.AreEqual(10,prepared.GetProperty("decimals").GetInt32());
        Assert.AreEqual(request.FromWalletAddress,prepared.GetProperty("sender").GetString());
        Assert.AreEqual(request.ToWalletAddress,prepared.GetProperty("recipient").GetString());
        Assert.IsTrue(System.Numerics.BigInteger.Parse(prepared.GetProperty("partialFeeUnits").GetString()!)>0);
        Assert.AreEqual("estimate-only-not-final-or-capped",prepared.GetProperty("feeCoverage").GetString());
        Assert.IsTrue(Convert.FromBase64String(prepared.GetProperty("signedTransactionBase64").GetString()!).Length>64);
        Assert.IsTrue(Convert.FromBase64String(prepared.GetProperty("signDocBase64").GetString()!).Length>64);
        Assert.AreEqual(66,prepared.GetProperty("transactionHash").GetString()!.Length);
        Assert.IsFalse(prepared.GetRawText().Contains(request.OwnerPrivateKey));
        StringAssert.Contains(result.Message,"not submitted");
        var verified=await provider.VerifyNativeTokenTransferPreparationAsync(prepared);
        Assert.IsFalse(verified.IsError,verified.Exception?.ToString());
        Assert.IsTrue(verified.Result.GetProperty("verified").GetBoolean());
        Assert.AreEqual(prepared.GetProperty("transactionHash").GetString(),verified.Result.GetProperty("transactionHash").GetString());
        Assert.AreEqual(request.MemoText,verified.Result.GetProperty("memo").GetString());
        using var signing=JsonDocument.Parse(Convert.FromBase64String(prepared.GetProperty("signDocBase64").GetString()!));
        var signingBlock=signing.RootElement.GetProperty("signingPayload").GetProperty("blockNumber").GetString()!;
        var start=Convert.ToUInt64(signingBlock[2..],16).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var outcome=await provider.ReadOriginalNativeOutcomeAsync(prepared,start,1);
        Assert.IsFalse(outcome.IsError,outcome.Exception?.ToString());
        Assert.IsFalse(outcome.Result.GetProperty("found").GetBoolean());
        Assert.IsFalse(outcome.Result.GetProperty("finalized").GetBoolean());
        Assert.IsTrue(outcome.Result.GetProperty("reconciliationRequired").GetBoolean());
        Assert.AreEqual(prepared.GetProperty("transactionHash").GetString(),outcome.Result.GetProperty("transactionHash").GetString());
        Assert.IsTrue(outcome.Result.GetProperty("inspectedBlocks").GetInt32()<=1);
    }

    [DataTestMethod]
    [DataRow(false, false)][DataRow(true, false)][DataRow(false, true)][DataRow(true, true)]
    [TestCategory("ReadOnlyNative")]
    public async Task PublicBalanceUsesPinnedSdkAndFinalizedRuntimeStorage(bool synchronous, bool fundedPublicAccount)
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_POLKADOT_READONLY_NATIVE"));
        using var provider=new PolkadotOASIS("https://rpc.polkadot.io",Genesis);
        Assert.IsFalse((await provider.ActivateProviderAsync()).IsError);
        var request=new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests.GetWeb3WalletBalanceRequest
        // Public read-only fixture observed through finalized System.Account enumeration; no signer is held.
        {WalletAddress=fundedPublicAccount
            ? "0xee080855f606cce66bdfffb8a73c54a440fa4a4ea1f9a487b7e2dadedaac205b"
            : "0xd43593c715fdd31c61141abd04a99fd6822c8558854ccde39a5684e7a56da27d"};
        var result=synchronous?provider.GetBalance(request):await provider.GetBalanceAsync(request);
        Assert.IsFalse(result.IsError,result.Exception?.ToString());
        Assert.IsTrue(double.IsFinite(result.Result) && result.Result>=0);
        if(fundedPublicAccount)Assert.IsTrue(result.Result>0,"Public fixture balance changed; inspect finalized state before replacing the fixture.");
        StringAssert.Contains(result.Message,"finalized");
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)][TestCategory("ReadOnlyNative")]
    public async Task PublicRpcVerifiesPolkadotIdentityWithoutSigningOrSubmitting(bool pinnedGenesis)
    {
        Assert.AreEqual("1",Environment.GetEnvironmentVariable("OASIS_POLKADOT_READONLY_NATIVE"),"Explicit read-only public RPC opt-in required.");
        using var provider=new PolkadotOASIS("https://rpc.polkadot.io",pinnedGenesis?Genesis:"polkadot");
        var result=pinnedGenesis?provider.ActivateProvider():await provider.ActivateProviderAsync();
        Assert.IsFalse(result.IsError,result.Exception?.ToString());Assert.IsTrue(result.Result);Assert.IsTrue(provider.IsProviderActivated);
        Assert.AreEqual("Polkadot",provider.ReportedChainName);Assert.AreEqual(Genesis,provider.ReportedGenesisHash);
        Assert.IsFalse((await provider.DeActivateProviderAsync()).IsError);Assert.IsFalse(provider.IsProviderActivated);
    }
}
