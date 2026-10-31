using System.Reflection;
using System.Runtime.ExceptionServices;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

// Pause before dispatching each client's first real receipt-lock statement.
// The SDK, native transaction and database locks remain real.
public class Neo4jPreparationGateProxy:DispatchProxy
{
    public sealed class Gate
    {
        internal readonly TaskCompletionSource Arrived=new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly TaskCompletionSource Release=new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int Used;
    }
    public object Target=null!;public Gate State=null!;
    public static T Wrap<T>(T target,Gate state) where T:class
    {
        var result=Create<T,Neo4jPreparationGateProxy>();
        var proxy=(Neo4jPreparationGateProxy)(object)result;proxy.Target=target;proxy.State=state;return result;
    }
    protected override object? Invoke(MethodInfo? method,object?[]? args)
    {
        if(Target is IAsyncTransaction&&method!.Name=="RunAsync"&&args![0] is string query
            &&query.StartsWith("MERGE (r:CosmicWriteReceipt",StringComparison.Ordinal)
            &&Interlocked.CompareExchange(ref State.Used,1,0)==0)
            return DispatchAfterRelease(method,args);
        var value=Call(method!,args);
        if(value is IAsyncSession session)return Wrap(session,State);
        if(value is Task<IAsyncTransaction> transaction)return WrapTransaction(transaction);
        return value;
    }
    private object? Call(MethodInfo method,object?[]? args)
    {
        try{return method.Invoke(Target,args);}
        catch(TargetInvocationException cause) when(cause.InnerException!=null)
        {ExceptionDispatchInfo.Capture(cause.InnerException).Throw();throw;}
    }
    private async Task<IAsyncTransaction> WrapTransaction(Task<IAsyncTransaction> task)=>Wrap(await task,State);
    private async Task<IResultCursor> DispatchAfterRelease(MethodInfo method,object?[]? args)
    {
        State.Arrived.TrySetResult();await State.Release.Task;
        return await (Task<IResultCursor>)Call(method,args)!;
    }
}
