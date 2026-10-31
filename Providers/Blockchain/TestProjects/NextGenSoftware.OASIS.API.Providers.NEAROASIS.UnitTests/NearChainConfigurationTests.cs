using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass]
public class NearChainConfigurationTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task ConflictingChainConfigurationFailsBeforeSdkProbe(bool synchronous)
    {
        var provider=new NEAROASIS("http://127.0.0.1:1","configured-network","different-chain","unused","","");
        var result=synchronous ? provider.ActivateProvider() : await provider.ActivateProviderAsync();
        Assert.IsTrue(result.IsError);
        StringAssert.Contains(result.Message,"networkId and chainId");
        Assert.IsFalse(provider.IsProviderActivated);
    }
}
