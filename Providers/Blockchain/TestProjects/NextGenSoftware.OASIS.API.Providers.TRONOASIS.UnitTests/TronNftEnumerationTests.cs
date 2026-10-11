using System.Numerics;
using System.Reflection;
using Google.Protobuf;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronNftEnumerationTests
{
    [Theory]
    [InlineData("", "unused")]
    [InlineData("unused", "")]
    public async Task CollectionQueryRequiresExplicitScopeBeforeActivation(string collection, string owner)
    {
        var provider = new TRONOASIS();
        var result = await provider.LoadNFTsForCollectionOwnerAsync(collection, owner);
        Assert.True(result.IsError);
        Assert.Null(result.Result);
        Assert.False(provider.IsProviderActivated);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("9007199254740993")]
    public void EnumerationAbiPreservesOwnerAndExactIndex(string value)
    {
        var address = new byte[21]; address[0] = 0x41; address[20] = 7;
        var index = BigInteger.Parse(value);
        var method = typeof(TronNativeTransferClient).GetMethod("EncodeNftOwnerIndex", BindingFlags.Static | BindingFlags.NonPublic)!;
        var bytes = (byte[])method.Invoke(null, new object[] { ByteString.CopyFrom(address), index })!;
        Assert.Equal(68, bytes.Length);
        Assert.Equal(new Nethereum.Util.Sha3Keccack().CalculateHash("tokenOfOwnerByIndex(address,uint256)")[..8].ToUpperInvariant(), Convert.ToHexString(bytes.AsSpan(0, 4)));
        Assert.Equal(new BigInteger(7), new BigInteger(bytes.AsSpan(4, 32), true, true));
        Assert.Equal(index, new BigInteger(bytes.AsSpan(36), true, true));
    }

    [Fact]
    public async Task PrecancelledEnumerationDoesNotAccessNetwork()
    {
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"), new Uri("http://127.0.0.1:50052"), new string('0', 64));
        await Assert.ThrowsAsync<OperationCanceledException>(() => runtime.NativeTransfers.GetNftIdsForOwnerAsync("unused", "unused", new CancellationToken(true)));
    }
}
