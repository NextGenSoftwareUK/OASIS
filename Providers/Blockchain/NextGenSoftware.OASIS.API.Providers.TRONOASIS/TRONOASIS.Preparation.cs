using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS;

public partial class TRONOASIS : IPreparedNativeStorageProvider
{
    /// <summary>Read-only recovery using independently verified permission authority for the original
    /// transaction. Does not fetch current permissions as a substitute for historical authority.</summary>
    public async Task<NextGenSoftware.OASIS.Common.OASISResult<System.Text.Json.JsonElement>> GetPreparedNativeFeeReceiptAsync(
        UsageProviderAttempt attempt,TronNet.Protocol.Account verifiedOriginalAccount,CancellationToken token=default)
    {
        var result=new NextGenSoftware.OASIS.Common.OASISResult<System.Text.Json.JsonElement>();
        await _runtimeLifecycle.WaitAsync(token);
        try{
            EnsureNativeRuntime();
            result.Result=await _nativeTransfers.GetPreparedNativeFeeReceiptAsync(attempt,verifiedOriginalAccount,token);
        }catch(Exception failure){NextGenSoftware.OASIS.Common.OASISErrorHandling.HandleError(ref result,failure.Message,failure);}
        finally{_runtimeLifecycle.Release();}
        return result;
    }

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
