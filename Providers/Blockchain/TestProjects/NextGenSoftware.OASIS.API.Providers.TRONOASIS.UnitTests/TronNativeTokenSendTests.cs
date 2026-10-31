using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public class TronNativeTokenSendTests
{
    [Fact]
    public async Task NativeTrxTokenRequestRejectsSubSunPrecisionBeforeActivation()
    {
        var provider=new TRONOASIS();
        var result=await provider.SendTokenAsync(new SendWeb3TokenRequest {FromTokenAddress="TRX",Amount=0.0000001m});
        Assert.True(result.IsError);Assert.False(provider.IsProviderActivated);
        Assert.Contains("SUN",result.Message);
    }

    [Fact]
    public async Task NativeSignerPublicKeyMismatchRejectsBeforeNodeAccess()
    {
        await using var runtime=new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),new Uri("http://127.0.0.1:50052"),new string('0',64));
        var error=await Assert.ThrowsAsync<ArgumentException>(()=>runtime.NativeTransfers.SendAsync("sender","recipient",1m,new string('0',63)+"1",null,publicKey:"00"));
        Assert.Contains("native signing key",error.Message);
    }
}
