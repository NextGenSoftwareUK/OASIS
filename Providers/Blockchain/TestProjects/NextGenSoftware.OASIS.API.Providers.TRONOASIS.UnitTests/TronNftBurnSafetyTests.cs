using System.Numerics;
using System.Reflection;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronNftBurnSafetyTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("9007199254740993")]
    public void BurnPreservesNativeTokenId(string token)
    {
        var id = BigInteger.Parse(token);
        var bytes = (byte[])typeof(TronNativeTransferClient).GetMethod("EncodeNftBurn", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { id })!;
        Assert.Equal(36, bytes.Length);
        Assert.Equal("42966C68", Convert.ToHexString(bytes.AsSpan(0, 4)));
        Assert.Equal(id, new BigInteger(bytes.AsSpan(4), isUnsigned: true, isBigEndian: true));
    }

    [Theory]
    [InlineData("contract")]
    [InlineData("contract/-1")]
    [InlineData("contract/1/2")]
    [InlineData("contract/00000000-0000-0000-0000-000000000000")]
    public void RejectsMissingOrNonNativeIdentity(string address)
    {
        var method = typeof(TronNativeTransferClient).GetMethod("ParseNftAddress", BindingFlags.NonPublic | BindingFlags.Static)!;
        var error = Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { address }));
        Assert.IsType<ArgumentException>(error.InnerException);
    }

    [Fact]
    public async Task MissingTransferRecipientCannotBecomeBurn()
    {
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),
            new Uri("http://127.0.0.1:50052"), new string('0', 64));
        await Assert.ThrowsAsync<ArgumentException>(() => runtime.NativeTransfers.SendNftAsync(
            "unused", null!, "unused", BigInteger.Zero, new string('1', 64)));
    }
}
