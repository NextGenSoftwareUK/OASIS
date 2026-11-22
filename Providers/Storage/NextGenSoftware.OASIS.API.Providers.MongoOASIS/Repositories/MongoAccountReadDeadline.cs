using System;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories;

internal sealed class MongoAccountReadTimeoutException(Exception cause)
    : TimeoutException("The Mongo account read exceeded its configured provider deadline.", cause) { }

internal static class MongoAccountReadDeadline
{
    internal static async Task<T> RunAsync<T>(TimeSpan timeout, Func<CancellationToken, Task<T>> read)
    {
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        using var deadline = new CancellationTokenSource(timeout);
        try { return await read(deadline.Token).ConfigureAwait(false); }
        catch (OperationCanceledException cause) when (deadline.IsCancellationRequested)
        {
            // The SDK operation has ended before control returns to HyperDrive.
            // This is provider-owned cancellation, never a detached task or caller cancellation.
            throw new MongoAccountReadTimeoutException(cause);
        }
    }
}
