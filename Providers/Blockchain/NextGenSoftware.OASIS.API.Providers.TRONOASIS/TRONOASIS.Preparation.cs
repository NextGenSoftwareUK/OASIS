using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS;

public partial class TRONOASIS : IPreparedNativeStorageProvider
{
    /// <summary>Own every physical encrypted-storage mutation before broadcast, without replacing shared SDK state.</summary>
    public Task<T> ExecutePreparedStorageAsync<T>(Func<Task<T>> operation,UsageExecutionHandle execution,
        string clientRequestId,string expectedSender,CancellationToken token=default)
    {
        token.ThrowIfCancellationRequested();ArgumentNullException.ThrowIfNull(operation);ArgumentNullException.ThrowIfNull(execution);
        var correlation=new NativeStorageCorrelation(clientRequestId);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedSender);
        return ((TronProviderStorageBackend)Backend).WithPreparedStorageAsync(operation,
            client=>client.WithAccountPreparation(execution,
                correlation.Next,expectedSender,token),token);
    }
}
