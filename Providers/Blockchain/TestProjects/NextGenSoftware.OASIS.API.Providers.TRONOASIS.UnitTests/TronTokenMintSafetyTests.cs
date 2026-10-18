using System.Numerics;
using System.Reflection;
using Google.Protobuf;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronTokenMintSafetyTests
{
    [Fact]
    public void MintAbiRetainsRecipientAndRequestedUnits()
    {
        var address = new byte[21];
        address[0] = 0x41;
        address[20] = 7;
        var method = typeof(TronNativeTransferClient).GetMethod("EncodeTrc20Call", BindingFlags.Static | BindingFlags.NonPublic)!;
        var data = (byte[])method.Invoke(null, new object[] { ByteString.CopyFrom(address), new BigInteger(125000000), true })!;
        Assert.Equal(68, data.Length);
        Assert.Equal("40C10F19", Convert.ToHexString(data.AsSpan(0, 4)));
        Assert.Equal(address.Skip(1), data.Skip(16).Take(20));
        Assert.Equal(new BigInteger(125000000), new BigInteger(data.AsSpan(36), isUnsigned: true, isBigEndian: true));
        Assert.IsType<ArgumentException>(Assert.Throws<TargetInvocationException>(() =>
            method.Invoke(null, new object?[] { null, BigInteger.One, true })).InnerException);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task InvalidMintAmountFailsBeforeWalletOrNetworkAccess(int amount)
    {
        var provider = new TRONOASIS(rpcEndpoint: "http://127.0.0.1:1");
        var result = await provider.MintTokenAsync(new MintWeb3TokenRequest { Amount = amount });
        Assert.True(result.IsError);
        Assert.False(provider.IsProviderActivated);
    }

    [Fact]
    public async Task MissingAvatarCannotBecomeContractAddressSigner()
    {
        var provider = new TRONOASIS(rpcEndpoint: "http://127.0.0.1:1", contractAddress: "contract");
        var result = await provider.MintTokenAsync(new MintWeb3TokenRequest { Amount = 1 });
        Assert.True(result.IsError);
        Assert.Contains("MintedByAvatarId", result.Message);
        Assert.False(provider.IsProviderActivated);
    }
}
