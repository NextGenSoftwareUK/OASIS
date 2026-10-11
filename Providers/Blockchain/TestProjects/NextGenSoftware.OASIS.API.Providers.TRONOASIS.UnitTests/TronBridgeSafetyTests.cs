using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronBridgeSafetyTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("0.0000001")]
    public async Task InvalidAmountCannotActivateOrReportBridgeSuccess(string value)
    {
        var provider = new TRONOASIS();
        var amount = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        var withdrawal = await provider.WithdrawAsync(amount, "unused", "unused");
        var deposit = await provider.DepositAsync(amount, "unused");
        Assert.True(withdrawal.IsError);
        Assert.True(deposit.IsError);
        Assert.Null(withdrawal.Result);
        Assert.Null(deposit.Result);
        Assert.False(provider.IsProviderActivated);
    }

    [Fact]
    public async Task MissingPoolCannotInventBridgeAddress()
    {
        var provider = new TRONOASIS();
        Assert.True((await provider.WithdrawAsync(1, "unused", "unused")).IsError);
        Assert.True((await provider.DepositAsync(1, "unused")).IsError);
        Assert.False(provider.IsProviderActivated);
    }

    [Fact]
    public async Task TokenContractCannotBeUsedAsNativeBridgePool()
    {
        var provider = new TRONOASIS(contractAddress: "configured-token-contract");
        var result = await provider.DepositAsync(1, "unused");
        Assert.True(result.IsError);
        Assert.Contains("BridgePoolWalletAddress", result.Message);
        Assert.False(provider.IsProviderActivated);
    }
}
