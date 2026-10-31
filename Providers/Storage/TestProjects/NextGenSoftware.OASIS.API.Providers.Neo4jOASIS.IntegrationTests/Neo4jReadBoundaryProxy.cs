using System.Reflection;
using System.Runtime.ExceptionServices;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

// Observe completed real SDK cursor reads without changing, replaying or buffering their rows.
public class Neo4jReadBoundaryProxy:DispatchProxy
{
    public sealed class State{public Action? AfterRead;public int Observations;}
    public object Target=null!;public State Boundary=null!;
    public static T Wrap<T>(T target,State boundary) where T:class{var value=Create<T,Neo4jReadBoundaryProxy>();var proxy=(Neo4jReadBoundaryProxy)(object)value;proxy.Target=target;proxy.Boundary=boundary;return value;}
    protected override object? Invoke(MethodInfo? method,object?[]? args)
    {
        object? result;try{result=method!.Invoke(Target,args);}catch(TargetInvocationException cause)when(cause.InnerException!=null){ExceptionDispatchInfo.Capture(cause.InnerException).Throw();throw;}
        if(result is IAsyncSession session)return Wrap(session,Boundary);
        if(result is Task<IResultCursor> cursor)return WrapCursor(cursor);
        if(method!.Name=="FetchAsync"&&result is Task<bool> fetched)return ObserveCompletion(fetched);
        return result;
    }
    private async Task<IResultCursor> WrapCursor(Task<IResultCursor> task)=>Wrap(await task,Boundary);
    private async Task<bool> ObserveCompletion(Task<bool> task){var more=await task;if(!more&&Boundary.AfterRead!=null){Boundary.Observations++;Boundary.AfterRead();}return more;}
}
