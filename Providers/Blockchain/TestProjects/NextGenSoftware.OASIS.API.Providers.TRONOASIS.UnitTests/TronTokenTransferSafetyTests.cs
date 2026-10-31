using System.Numerics;
using System.Reflection;
using Google.Protobuf;
using TronNet;
using TronNet.Crypto;
using TronNet.Protocol;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronTokenTransferSafetyTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("9007199254740993")]
    public void ApprovalAbiSupportsExactAllowanceAndExplicitRevocation(string amount)
    {
        var address = new byte[21]; address[0] = 0x41; address[20] = 7;
        var units = BigInteger.Parse(amount);
        var data = (byte[])typeof(TronNativeTransferClient).GetMethod("EncodeApproval", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { ByteString.CopyFrom(address), units })!;
        Assert.Equal("095EA7B3", Convert.ToHexString(data.AsSpan(0, 4)));
        Assert.Equal(address.Skip(1), data.Skip(16).Take(20));
        Assert.Equal(units, new BigInteger(data.AsSpan(36), isUnsigned: true, isBigEndian: true));
    }

    [Fact]
    public async Task ApprovalCannotUseAnotherHoldersSigningKey()
    {
        await TronPermissionRpcTests.WithDefaultOwnerAuthority(async runtime => {
        var other = new TronECKey(new string('2', 64), TronNetwork.MainNet).GetPublicAddress();
        await Assert.ThrowsAsync<System.IO.InvalidDataException>(() => runtime.NativeTransfers.ApproveTokenAsync(
            other, other, other, BigInteger.One, new string('1', 64)));
        });
    }

    [Theory]
    [InlineData(false, 0, "token")]
    [InlineData(true, -1, "token")]
    [InlineData(false, 1, "")]
    [InlineData(true, 1, "")]
    public async Task AvatarTokenRoutesRejectInvalidRequestBeforeWalletOrNodeAccess(bool byEmail, int amount, string token)
    {
        var provider = new TRONOASIS();
        var result = byEmail
            ? await provider.SendTransactionByEmailAsync("sender@example.org", "recipient@example.org", amount, token)
            : await provider.SendTransactionByUsernameAsync("sender", "recipient", amount, token);
        Assert.True(result.IsError);
        Assert.Null(result.Result);
        Assert.False(provider.IsProviderActivated);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void OnlyCanonicalTrueProvesTransferSuccess(int returnedValue, bool expected)
    {
        var receipt = new TransactionInfo();
        var encoded = new byte[32];
        encoded[31] = (byte)returnedValue;
        receipt.ContractResult.Add(ByteString.CopyFrom(encoded));
        var method = typeof(TronNativeTransferClient).GetMethod("ValidateTrc20TransferReturn", BindingFlags.Static | BindingFlags.NonPublic)!;
        if (expected)
            method.Invoke(null, new object[] { receipt });
        else
            Assert.IsType<InvalidOperationException>(Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { receipt })).InnerException);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void MissingMalformedOrMultipleReturnValuesAreNotSuccess(int mode)
    {
        var receipt = new TransactionInfo();
        if (mode == 1) receipt.ContractResult.Add(ByteString.CopyFrom(new byte[] { 1 }));
        if (mode == 2)
        {
            receipt.ContractResult.Add(ByteString.CopyFrom(new byte[32]));
            receipt.ContractResult.Add(ByteString.CopyFrom(new byte[32]));
        }
        var method = typeof(TronNativeTransferClient).GetMethod("ValidateTrc20TransferReturn", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.IsType<InvalidOperationException>(Assert.Throws<TargetInvocationException>(() => method.Invoke(null, new object[] { receipt })).InnerException);
    }

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
    public async Task UnauthorisedSenderIsRejectedBeforeConstruction()
    {
        await TronPermissionRpcTests.WithDefaultOwnerAuthority(async runtime => {
        var key = new string('1', 64);
        var other = new TronECKey(new string('2', 64), TronNetwork.MainNet).GetPublicAddress();
        await Assert.ThrowsAsync<System.IO.InvalidDataException>(() => runtime.NativeTransfers.SendTokenAsync(
            other, other, other, 1m, key));
        });
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
            .Invoke(null, new object?[] { recipient, units, false })!;
}
