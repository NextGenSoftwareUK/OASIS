using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS;

public partial class TRONOASIS : IPreparedNativeStorageProvider
{
    /// <summary>Own every physical encrypted-storage mutation before broadcast, without replacing shared SDK state.</summary>
    public Task<T> ExecutePreparedStorageAsync<T>(Func<Task<T>> operation,UsageExecutionHandle execution,
        string clientRequestId,string expectedSender,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();ArgumentNullException.ThrowIfNull(operation);ArgumentNullException.ThrowIfNull(execution);
        if(string.IsNullOrWhiteSpace(clientRequestId)||clientRequestId.Length>110)
            throw new ArgumentException("Storage correlation must leave room for bounded write sequence suffix.",nameof(clientRequestId));
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedSender);
        var sequence=0;
        return ((TronProviderStorageBackend)Backend).WithPreparedStorageAsync(operation,
            client=>client.WithAccountPreparation(execution,
                ()=>clientRequestId+".storage."+Interlocked.Increment(ref sequence),expectedSender,token),token);
    }
}
