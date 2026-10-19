using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass]
public class NearTokenSendValidationTests
{
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task IndependentSenderCannotSilentlyUseServiceKey(bool native)
    {
        var provider=new NEAROASIS("http://127.0.0.1:1","unused","unused","unused","service.near","unused");
        var result=await provider.SendTokenAsync(new SendWeb3TokenRequest{FromTokenAddress=native?"NEAR":"owned-custom-token",
            FromWalletAddress="independent.near",ToWalletAddress="receiver.near",Amount=1m});
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));Assert.IsFalse(provider.IsProviderActivated);
    }

    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task UnrepresentableAmountFailsBeforeActivation(bool native)
    {
        var provider=new NEAROASIS("http://127.0.0.1:1","unused","unused","unused","unused","unused");
        var request=new SendWeb3TokenRequest{FromTokenAddress=native?"NEAR":"owned-custom-token",ToWalletAddress="receiver.near",
            Amount=native?0.0000000000000000000000001m:0.000000001m};
        var result=await provider.SendTokenAsync(request);
        Assert.IsTrue(result.IsError);Assert.IsInstanceOfType(result.Exception,typeof(ArgumentException));Assert.IsFalse(provider.IsProviderActivated);
    }
}
