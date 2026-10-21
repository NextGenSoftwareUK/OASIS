using System;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace NextGenSoftware.OASIS.API.Providers.MongoOASIS.IntegrationTests;

// Forward all work to the actual driver. Inject only a lost application cleanup
// acknowledgement after the native read-only transaction has really been aborted.
public class MongoReceiptCleanupFault : DispatchProxy
{
    private object target = null!;
    private bool failDispose;
    public static IMongoClient Wrap(IMongoClient client, bool failDispose) => WrapTarget<IMongoClient>(client, failDispose);
    private static T WrapTarget<T>(T target, bool failDispose) where T : class
    {
        var proxy = Create<T, MongoReceiptCleanupFault>();
        ((MongoReceiptCleanupFault)(object)proxy).target = target;
        ((MongoReceiptCleanupFault)(object)proxy).failDispose = failDispose;
        return proxy;
    }
    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        object? returned;
        try { returned = method!.Invoke(target, args); }
        catch (TargetInvocationException error) when (error.InnerException != null)
        { ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
        if (method!.Name == nameof(IMongoClient.StartSessionAsync))
            return WrapSession((Task<IClientSessionHandle>)returned!, failDispose);
        if (method.Name == nameof(IClientSessionHandle.AbortTransactionAsync))
            return LoseAbortAcknowledgement((Task)returned!);
        if (method.Name == nameof(IDisposable.Dispose) && failDispose && target is IClientSessionHandle)
            throw new IOException("Owned application fault after native session disposal.");
        return returned;
    }
    private static async Task<IClientSessionHandle> WrapSession(Task<IClientSessionHandle> pending, bool failDispose)
        => WrapTarget(await pending.ConfigureAwait(false), failDispose);
    private static async Task LoseAbortAcknowledgement(Task pending)
    {
        await pending.ConfigureAwait(false);
        throw new IOException("Owned application fault after native read-only transaction abort.");
    }
}
