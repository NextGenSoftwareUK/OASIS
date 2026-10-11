using System.Reflection;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS.UnitTests;

public sealed class TronTokenLifecycleTests
{
    private static Task<string> Execute(TRONOASIS provider, Func<TronNativeTransferClient, Task<string>> operation)
        => (Task<string>)typeof(TRONOASIS).GetMethod("ExecuteRuntimeAsync", BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(typeof(string)).Invoke(provider, new object[] { operation })!;

    private static void SetActive(TRONOASIS provider)
        => typeof(TRONOASIS).GetProperty("IsProviderActivated")!.SetValue(provider, true);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeactivationWaitsForInFlightTokenOperationAndLockIsReleasedOnFailure(bool fail)
    {
        await using var runtime = new TronSdkRuntime(new Uri("http://127.0.0.1:50051"),
            new Uri("http://127.0.0.1:50052"), new string('0', 64));
        var provider = new TRONOASIS(nativeTransfers: runtime.NativeTransfers);
        SetActive(provider); // Offline lifecycle fixture; no chain activation or transaction is claimed.
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var operation = Execute(provider, async client =>
        {
            Assert.Same(runtime.NativeTransfers, client);
            entered.SetResult();
            await release.Task;
            if (fail) throw new InvalidOperationException("execution failed");
            return "confirmed-id";
        });
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var stop = provider.DeActivateProviderAsync();
        Assert.False(stop.IsCompleted);
        Assert.True(provider.IsProviderActivated);
        release.SetResult();
        if (fail) await Assert.ThrowsAsync<InvalidOperationException>(() => operation);
        else Assert.Equal("confirmed-id", await operation);
        Assert.False((await stop.WaitAsync(TimeSpan.FromSeconds(5))).IsError);
        Assert.False(provider.IsProviderActivated);
        var called = false;
        await Assert.ThrowsAsync<InvalidOperationException>(() => Execute(provider, client =>
        {
            called = true;
            return Task.FromResult("unexpected");
        }));
        Assert.False(called);
        Assert.False((await provider.DeActivateProviderAsync().WaitAsync(TimeSpan.FromSeconds(5))).IsError);
    }
}
