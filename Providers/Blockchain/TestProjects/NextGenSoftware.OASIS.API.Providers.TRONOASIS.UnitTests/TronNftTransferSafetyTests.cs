using System.Numerics;
using System.Reflection;
using Google.Protobuf;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronNftTransferSafetyTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("9007199254740993")]
    public void SafeTransferRetainsExplicitTokenIdentityAndBothAddresses(string token)
    {
        var from = new byte[21]; from[0] = 0x41; from[20] = 1;
        var to = new byte[21]; to[0] = 0x41; to[20] = 2;
        var id = BigInteger.Parse(token);
        var bytes = (byte[])typeof(TronNativeTransferClient).GetMethod("EncodeNftTransfer", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { ByteString.CopyFrom(from), ByteString.CopyFrom(to), id })!;
        Assert.Equal(100, bytes.Length);
        Assert.Equal("42842E0E", Convert.ToHexString(bytes.AsSpan(0, 4)));
        Assert.Equal(from.Skip(1), bytes.Skip(16).Take(20));
        Assert.Equal(to.Skip(1), bytes.Skip(48).Take(20));
        Assert.Equal(id, new BigInteger(bytes.AsSpan(68), isUnsigned: true, isBigEndian: true));
    }

    [Fact]
    public async Task MissingNativeIdentityCannotGenerateRandomNftId()
    {
        var provider = new TRONOASIS();
        var result = await provider.SendNFTAsync(new Core.Objects.NFT.Requests.SendWeb3NFTRequest { Amount = 1 });
        Assert.True(result.IsError);
        Assert.Null(result.Result);
        Assert.False(provider.IsProviderActivated);
    }

    [Fact]
    public async Task UnauthorisedSenderSigningKeyFailsBeforeConstruction()
    {
        await TronPermissionRpcTests.WithDefaultOwnerAuthority(async runtime => {
        var other = new TronNet.TronECKey(new string('2', 64), TronNet.TronNetwork.MainNet).GetPublicAddress();
        await Assert.ThrowsAsync<System.IO.InvalidDataException>(() => runtime.NativeTransfers.SendNftAsync(
            other, other, other, BigInteger.Zero, new string('1', 64)));
        });
    }
}
