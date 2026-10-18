using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.PolkadotOASIS.IntegrationTests;

[TestClass][DoNotParallelize]
public class PolkadotReadOnlyNativeTests
{
    private const string Genesis="0x91b171bb158e2d3848fa23a9f1c25182fb8e20313b2c1eb49219da7a70ce90c3";

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
