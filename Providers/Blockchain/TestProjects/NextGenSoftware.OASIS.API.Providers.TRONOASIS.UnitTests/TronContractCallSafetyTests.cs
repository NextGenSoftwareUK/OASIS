using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronContractCallSafetyTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(1024 * 1024 + 4)]
    public async Task MalformedAbiIsRejectedBeforeNetworkUse(int length)
    {
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),
            new Uri("http://127.0.0.1:50052"), new string('0', 64));
        var input = new byte[length];
        await Assert.ThrowsAsync<ArgumentException>(() => runtime.NativeTransfers.QueryContractAsync("unused", "unused", input));
        await Assert.ThrowsAsync<ArgumentException>(() => runtime.NativeTransfers.ExecuteSignedContractAsync("unused", "unused", input, "unused"));
    }

    [Fact]
    public async Task MissingSigningKeyIsRejectedBeforeAddressLookupOrNetwork()
    {
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),
            new Uri("http://127.0.0.1:50052"), new string('0', 64));
        await Assert.ThrowsAsync<ArgumentException>(() => runtime.NativeTransfers.ExecuteSignedContractAsync(
            "unused", "unused", new byte[4], ""));
    }
}
