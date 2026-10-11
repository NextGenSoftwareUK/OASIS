using System.Numerics;
using System.Reflection;
using System.Text;
using Google.Protobuf;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronNftMintSafetyTests
{
    private static byte[] Encode(BigInteger id, string uri)
    {
        var address = new byte[21];
        address[0] = 0x41;
        address[20] = 1;
        return (byte[])typeof(TronNativeTransferClient).GetMethod("EncodeNftMint", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, new object[] { ByteString.CopyFrom(address), id, uri })!;
    }

    [Theory]
    [InlineData("0")]
    [InlineData("9007199254740993")]
    public void MintPreservesExplicitIdentityRecipientAndUnicodeUri(string token)
    {
        var id = BigInteger.Parse(token);
        var uri = "ipfs://test/🌳/metadata.json";
        var data = Encode(id, uri);
        Assert.Equal(new Nethereum.Util.Sha3Keccack().CalculateHash("mint(address,uint256,string)")[..8].ToUpperInvariant(),
            Convert.ToHexString(data.AsSpan(0, 4)));
        Assert.Equal(BigInteger.One, new BigInteger(data.AsSpan(4, 32), true, true));
        Assert.Equal(id, new BigInteger(data.AsSpan(36, 32), true, true));
        Assert.Equal(new BigInteger(96), new BigInteger(data.AsSpan(68, 32), true, true));
        var bytes = Encoding.UTF8.GetBytes(uri);
        Assert.Equal(new BigInteger(bytes.Length), new BigInteger(data.AsSpan(100, 32), true, true));
        Assert.Equal(bytes, data.AsSpan(132, bytes.Length).ToArray());
        Assert.All(data.Skip(132 + bytes.Length), b => Assert.Equal((byte)0, b));
    }

    [Fact]
    public void InvalidMintInputsCannotBeEncoded()
    {
        Assert.IsType<ArgumentOutOfRangeException>(Assert.Throws<TargetInvocationException>(() => Encode(-1, "ipfs://test")).InnerException);
        Assert.IsType<ArgumentOutOfRangeException>(Assert.Throws<TargetInvocationException>(() => Encode(BigInteger.One << 256, "ipfs://test")).InnerException);
        Assert.IsType<ArgumentException>(Assert.Throws<TargetInvocationException>(() => Encode(0, "")).InnerException);
        Assert.IsType<EncoderFallbackException>(Assert.Throws<TargetInvocationException>(() => Encode(0, "\uD800")).InnerException);
        Assert.IsType<ArgumentException>(Assert.Throws<TargetInvocationException>(() => Encode(0, new string('a', 8193))).InnerException);
    }

    [Fact]
    public async Task MissingMintRequestReturnsErrorBeforeActivation()
    {
        var provider = new TRONOASIS();
        var result = await provider.MintNFTAsync(null!);
        Assert.True(result.IsError);
        Assert.Null(result.Result);
        Assert.False(provider.IsProviderActivated);
    }
}
