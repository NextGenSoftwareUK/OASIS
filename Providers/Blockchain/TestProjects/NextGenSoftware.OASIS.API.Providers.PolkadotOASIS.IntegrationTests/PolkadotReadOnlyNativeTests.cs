using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class PolkadotReadOnlyNativeTests
{
    private const string Genesis="0x91b171bb158e2d3848fa23a9f1c25182fb8e20313b2c1eb49219da7a70ce90c3";

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
