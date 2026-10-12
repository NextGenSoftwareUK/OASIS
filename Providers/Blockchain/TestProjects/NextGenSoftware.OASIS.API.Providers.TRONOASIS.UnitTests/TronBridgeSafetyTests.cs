using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronBridgeSafetyTests
{
    [Fact]
    public async Task PoolSelfTransferCannotActivateResolveKeyOrReportMovement()
    {
        var key = new string('0',63)+"1";
        var pool = new TronNet.TronECKey(key,TronNet.TronNetwork.MainNet).GetPublicAddress();
        var keyReads = 0;
        var provider = new TRONOASIS(bridgePoolWalletAddress:pool,nativeSigningKeyResolver:address => { keyReads++; return Task.FromResult(key); });
        var withdrawal = await provider.WithdrawAsync(1,pool,key);
        var deposit = await provider.DepositAsync(1,pool);
        Assert.True(withdrawal.IsError); Assert.Null(withdrawal.Result);
        Assert.True(deposit.IsError); Assert.Null(deposit.Result);
        Assert.Contains("differ",withdrawal.Message); Assert.Contains("differ",deposit.Message);
        Assert.False(provider.IsProviderActivated); Assert.Equal(0,keyReads);
    }
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
