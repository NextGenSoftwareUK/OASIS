using System;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public class TronRuntimeSafetyTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("0.0000001")]
    [InlineData("9223372036854.775808")]
    public async Task EmailNativeTransferRejectsInvalidAmountBeforeActivation(string amount)
    {
        var provider = new TRONOASIS();
        var result = await provider.SendTransactionByEmailAsync("sender@example.org", "recipient@example.org",
            decimal.Parse(amount, CultureInfo.InvariantCulture));
        Assert.True(result.IsError);
        Assert.Null(result.Result);
        Assert.False(provider.IsProviderActivated);
        Assert.Contains("SUN units", result.Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("0.0000001")]
    [InlineData("9223372036854.775808")]
    public async Task NativeTransferRejectsInvalidSunAmountsBeforeContactingNode(string amount)
    {
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),
            new Uri("http://127.0.0.1:50052"), new string('0', 64));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => runtime.NativeTransfers.SendAsync(
            "sender", "recipient", decimal.Parse(amount, CultureInfo.InvariantCulture), "", ""));
    }

    [Fact]
    public async Task NativeTransferRequiresSigningKeyBeforeContactingNode()
    {
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),
            new Uri("http://127.0.0.1:50052"), new string('0', 64));
        await Assert.ThrowsAsync<ArgumentException>(() => runtime.NativeTransfers.SendAsync(
            "sender", "recipient", 1m, "", ""));
    }

    [Theory]
    [InlineData("42", 20)]
    [InlineData("41", 19)]
    public async Task NativeTransferRejectsInvalidAddressBytesBeforeContactingNode(string prefix, int payloadBytes)
    {
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),
            new Uri("http://127.0.0.1:50052"), new string('0', 64));
        // Synthetic test-only signing key; no chain calls or funded account.
        var key = new TronNet.TronECKey(new string('1', 64), TronNet.TronNetwork.MainNet);
        await Assert.ThrowsAsync<ArgumentException>(() => runtime.NativeTransfers.SendAsync(
            key.GetPublicAddress(), prefix + new string('0', payloadBytes * 2), 1m, key.GetPrivateKey(), ""));
    }

    [Theory]
    [InlineData("http://example.org:50051")]
    [InlineData("https://user:password@example.org")]
    [InlineData("https://example.org/wallet")]
    [InlineData("https://example.org?key=secret")]
    [InlineData("https://example.org#fragment")]
    [InlineData("ftp://example.org")]
    public void RuntimeRejectsUnsafeEndpointsBeforeOpeningChannels(string endpoint)
    {
        Assert.Throws<ArgumentException>(() => new TronSdkRuntime(new Uri(endpoint),
            new Uri("https://example.org"), new string('0', 64)));
    }

    [Fact]
    public async Task UnconfiguredTransferMustNotReportUnsignedCreationAsSuccess()
    {
        var provider = new TRONOASIS();
        var result = await provider.SendTransactionAsync("sender", "recipient", 1m, "test");
        Assert.True(result.IsError);
        Assert.Null(result.Result);
    }

    [Fact]
    public async Task UnconfiguredActivationCanBeRetriedWithoutLeakingRuntimeState()
    {
        var provider = new TRONOASIS();
        Assert.True((await provider.ActivateProviderAsync()).IsError);
        Assert.True((await provider.ActivateProviderAsync()).IsError);
        var stopped = await provider.DeActivateProviderAsync();
        Assert.False(stopped.IsError);
        Assert.True(stopped.Result);
    }
}
