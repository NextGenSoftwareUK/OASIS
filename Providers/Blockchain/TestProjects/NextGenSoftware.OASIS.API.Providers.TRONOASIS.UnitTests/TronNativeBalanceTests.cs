using System.Reflection;
using Google.Protobuf;
using TronNet.Protocol;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronNativeBalanceTests
{
    private static ByteString Address()
    {
        var bytes = new byte[21];
        bytes[0] = 0x41;
        return ByteString.CopyFrom(bytes);
    }

    private static decimal Balance(Account account, ByteString address)
        => (decimal)typeof(TronNativeTransferClient).GetMethod("NativeBalance", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { account, address })!;

    [Theory]
    [InlineData(0L, "0")]
    [InlineData(1L, "0.000001")]
    [InlineData(9007199254740993L, "9007199254.740993")]
    public void ConfirmedAccountBalanceRetainsExactSunPrecision(long units, string expected)
        => Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture),
            Balance(new Account { Address = Address(), Balance = units }, Address()));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void MissingAccountOrNegativeBalanceCannotBeSuccessfulZero(bool negative)
    {
        var account = new Account();
        if (negative) { account.Address = Address(); account.Balance = -1; }
        Assert.IsType<InvalidOperationException>(Assert.Throws<TargetInvocationException>(() => Balance(account, Address())).InnerException);
    }

    [Fact]
    public async Task MissingAddressFailsBeforeProviderActivation()
    {
        var provider = new TRONOASIS();
        var result = await provider.GetBalanceAsync(new Core.Objects.Wallet.Requests.GetWeb3WalletBalanceRequest());
        Assert.True(result.IsError);
        Assert.False(provider.IsProviderActivated);
    }

    [Fact]
    public async Task AccountBalanceMissingAddressIsErrorBeforeActivation()
    {
        var provider = new TRONOASIS();
        var result = await provider.GetAccountBalanceAsync("");
        Assert.True(result.IsError);
        Assert.False(provider.IsProviderActivated);
    }

    [Fact]
    public async Task CancelledAccountBalanceDoesNotActivateProvider()
    {
        var provider = new TRONOASIS();
        var result = await provider.GetAccountBalanceAsync("unused", new CancellationToken(true));
        Assert.True(result.IsError);
        Assert.False(provider.IsProviderActivated);
    }
}
