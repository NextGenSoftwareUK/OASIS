using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class PolkadotReadOnlyNativeTests
{
    private const string Genesis="0x91b171bb158e2d3848fa23a9f1c25182fb8e20313b2c1eb49219da7a70ce90c3";

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
