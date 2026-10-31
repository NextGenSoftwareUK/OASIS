using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronPrivateStorageTests
{
    [Fact]
    public async Task CancellationAndDisposalDoNotResolveCredentialsOrAllocateAnotherRuntime()
    {
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),
            new Uri("http://127.0.0.1:50052"), new string('0', 64));
        var resolutions = 0;
        using var backend = new TronPrivateStorageBackend(runtime.NativeTransfers, "unused", "unused", () => "unused",
            () => { resolutions++; return new byte[32]; });
        var token = new CancellationToken(true);
        await Assert.ThrowsAsync<OperationCanceledException>(() => backend.GetAsync("key", token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => backend.PutAsync("key", "value", token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => backend.DeleteAsync("key", token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => backend.ListKeysAsync("", token));
        await Assert.ThrowsAsync<OperationCanceledException>(() => backend.VerifyAsync(token));
        backend.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(() => backend.GetAsync("key"));
        Assert.Equal(0, resolutions);
    }

    [Fact]
    public async Task InvalidEncryptionKeyFailsBeforeNetworkAndClearsOwnedBuffer()
    {
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),
            new Uri("http://127.0.0.1:50052"), new string('0', 64));
        var key = Enumerable.Repeat((byte)7, 31).ToArray();
        using var backend = new TronPrivateStorageBackend(runtime.NativeTransfers, "unused", "unused", () => "unused", () => key);
        await Assert.ThrowsAsync<InvalidOperationException>(() => backend.VerifyAsync());
        Assert.All(key, value => Assert.Equal((byte)0, value));
    }
}
