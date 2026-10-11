using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronNftCustodyTests
{
    [Theory]
    [InlineData("")]
    [InlineData("-1")]
    [InlineData("1.5")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("115792089237316195423570985008687907853269984665640564039457584007913129639936")]
    public async Task WithdrawalRejectsNonNativeIdentityBeforeActivation(string id)
    {
        var provider = new TRONOASIS(bridgePoolWalletAddress: "unused");
        var result = await provider.WithdrawNFTAsync("unused", id, "unused", "unused");
        Assert.True(result.IsError);
        Assert.Contains("uint256", result.Message);
        Assert.Null(result.Result);
        Assert.False(provider.IsProviderActivated);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("9007199254740993")]
    public async Task WithdrawalDoesNotReuseTokenContractAsCustodyWallet(string id)
    {
        var provider = new TRONOASIS(contractAddress: "unused token contract");
        var result = await provider.WithdrawNFTAsync("unused", id, "unused", "unused");
        Assert.True(result.IsError);
        Assert.Contains("BridgePoolWalletAddress", result.Message);
        Assert.Null(result.Result);
        Assert.False(provider.IsProviderActivated);
    }
}
