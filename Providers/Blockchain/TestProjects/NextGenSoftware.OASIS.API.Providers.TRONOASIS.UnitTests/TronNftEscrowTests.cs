using System.Numerics;
using System.Reflection;
using Google.Protobuf;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronNftEscrowTests
{
    [Theory]
    [InlineData("lockNFT(address,uint256)", "0")]
    [InlineData("unlockNFT(address,uint256)", "9007199254740993")]
    [InlineData("lockNFT(address,uint256)", "115792089237316195423570985008687907853269984665640564039457584007913129639935")]
    public void EscrowAbiPreservesNativeContractAndExactTokenId(string signature, string value)
    {
        var address = new byte[21]; address[0] = 0x41; address[20] = 7;
        var id = BigInteger.Parse(value);
        var method = typeof(TronNativeTransferClient).GetMethod("EncodeNftEscrow", BindingFlags.Static | BindingFlags.NonPublic)!;
        var bytes = (byte[])method.Invoke(null, new object[] { signature, ByteString.CopyFrom(address), id })!;
        Assert.Equal(68, bytes.Length);
        Assert.Equal(new Nethereum.Util.Sha3Keccack().CalculateHash(signature)[..8].ToUpperInvariant(), Convert.ToHexString(bytes.AsSpan(0, 4)));
        Assert.Equal(new BigInteger(7), new BigInteger(bytes.AsSpan(4, 32), true, true));
        Assert.Equal(id, new BigInteger(bytes.AsSpan(36), true, true));
    }

    [Fact]
    public async Task NullRequestsCannotReachWalletOrNetwork()
    {
        var provider = new TRONOASIS();
        Assert.True((await provider.LockNFTAsync(null!)).IsError);
        Assert.True((await provider.UnlockNFTAsync(null!)).IsError);
        Assert.False(provider.IsProviderActivated);
    }

    [Fact]
    public async Task PrecancelledEscrowDoesNotAccessNetwork()
    {
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"), new Uri("http://127.0.0.1:50052"), new string('0', 64));
        await Assert.ThrowsAsync<OperationCanceledException>(() => runtime.NativeTransfers.ExecuteNftEscrowAsync(
            "unused", "unused", "unused", "unused", false, new CancellationToken(true)));
    }
}
