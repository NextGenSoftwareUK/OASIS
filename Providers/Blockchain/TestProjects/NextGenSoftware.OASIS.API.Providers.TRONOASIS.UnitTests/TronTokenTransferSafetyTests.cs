using System.Numerics;
using System.Reflection;
using Google.Protobuf;
using TronNet;
using TronNet.Crypto;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronTokenTransferSafetyTests
{
    [Fact]
    public void TransferAbiUsesTwentyByteAddressAndExactUint256()
    {
        var address = new byte[21];
        address[0] = 0x41;
        for (var i = 1; i < address.Length; i++) address[i] = (byte)i;
        var units = BigInteger.Parse("9007199254740993");
        var data = Encode(ByteString.CopyFrom(address), units);
        Assert.Equal(68, data.Length);
        Assert.Equal("A9059CBB", Convert.ToHexString(data.AsSpan(0, 4)));
        Assert.All(data.Skip(4).Take(12), value => Assert.Equal(0, value));
        Assert.Equal(address.Skip(1), data.Skip(16).Take(20));
        Assert.Equal(units, new BigInteger(data.AsSpan(36), isUnsigned: true, isBigEndian: true));
        var burn = Encode(null!, units);
        Assert.Equal(36, burn.Length);
        Assert.Equal("42966C68", Convert.ToHexString(burn.AsSpan(0, 4)));
        Assert.Equal(units, new BigInteger(burn.AsSpan(4), isUnsigned: true, isBigEndian: true));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task InvalidTransferAmountCannotContactNode(int amount)
    {
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),
            new Uri("http://127.0.0.1:50052"), new string('0', 64));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => runtime.NativeTransfers.SendTokenAsync(
            "sender", "recipient", "contract", amount, ""));
    }

    [Fact]
    public async Task WrongSenderIsRejectedBeforeContactingNode()
    {
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),
            new Uri("http://127.0.0.1:50052"), new string('0', 64));
        var key = new string('1', 64);
        var other = new TronECKey(new string('2', 64), TronNetwork.MainNet).GetPublicAddress();
        var error = await Assert.ThrowsAsync<ArgumentException>(() => runtime.NativeTransfers.SendTokenAsync(
            other, other, other, 1m, key));
        Assert.Contains("sender", error.Message);
    }

    [Fact]
    public async Task MissingSenderCannotSelectBurnInsteadOfTransfer()
    {
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),
            new Uri("http://127.0.0.1:50052"), new string('0', 64));
        await Assert.ThrowsAsync<ArgumentException>(() => runtime.NativeTransfers.SendTokenAsync(null!, "recipient", "contract", 1m, ""));
        await Assert.ThrowsAsync<ArgumentException>(() => runtime.NativeTransfers.SendTokenAsync("sender", null!, "contract", 1m, ""));
    }

    private static byte[] Encode(ByteString recipient, BigInteger units) => (byte[])
        typeof(TronNativeTransferClient).GetMethod("EncodeTrc20Call", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object?[] { recipient, units })!;
}
