using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Common;
using Provider = NextGenSoftware.OASIS.API.Providers.MoralisOASIS.MoralisOASIS;

namespace NextGenSoftware.OASIS.API.Providers.MoralisOASIS.UnitTests;

[TestClass]
public sealed class MoralisBurnCapabilityTests
{
    [TestMethod]
    public async Task AsyncBurnReportsUnsupportedWithoutActivating()
    {
        var provider = new ActivationProbe();

        var result = await provider.BurnNFTAsync(null!);

        Assert.IsTrue(result.IsError);
        Assert.AreEqual(1, result.ErrorCount);
        Assert.AreEqual("MORALIS_NFT_BURN_UNSUPPORTED", result.ErrorCode);
        Assert.IsNull(result.Result);
        Assert.AreEqual(0, provider.ActivationAttempts);
        Assert.IsFalse(provider.IsProviderActivated);
    }

    [TestMethod]
    public void SyncBurnReportsUnsupportedWithoutActivating()
    {
        var provider = new ActivationProbe();

        var result = provider.BurnNFT(null!);

        Assert.IsTrue(result.IsError);
        Assert.AreEqual("MORALIS_NFT_BURN_UNSUPPORTED", result.ErrorCode);
        Assert.IsNull(result.Result);
        Assert.AreEqual(0, provider.ActivationAttempts);
    }

    private sealed class ActivationProbe : Provider
    {
        public ActivationProbe() : base("offline-capability-test") { }

        public int ActivationAttempts { get; private set; }

        public override Task<OASISResult<bool>> ActivateProviderAsync()
        {
            ActivationAttempts++;
            throw new InvalidOperationException("Unsupported burns must not activate a provider.");
        }
    }
}
