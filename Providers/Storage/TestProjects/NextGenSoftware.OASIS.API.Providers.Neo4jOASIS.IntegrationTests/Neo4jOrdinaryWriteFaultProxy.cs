using System.Reflection;
using System.Runtime.ExceptionServices;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

// All database calls use the real SDK. Faults are injected after the selected SDK acknowledgement.
public class Neo4jOrdinaryWriteFaultProxy : DispatchProxy
{
    public sealed class Faults(string mode)
    {
        public string Mode=mode;
        public int Commits,Rollbacks,TransactionDisposals,SessionDisposals;
        public Exception Query=new IOException("owned ordinary query acknowledgement failure");
        public Exception Rollback=new IOException("owned ordinary rollback acknowledgement failure");
        public Exception Commit=new IOException("owned ordinary commit acknowledgement failure");
        public Exception TransactionCleanup=new IOException("owned ordinary transaction cleanup failure");
        public Exception SessionCleanup=new IOException("owned ordinary session cleanup failure");
    }
    public object Target=null!;
    public Faults State=null!;
    public static T Wrap<T>(T target,Faults state) where T:class
    {
        var proxy=Create<T,Neo4jOrdinaryWriteFaultProxy>();
        var probe=(Neo4jOrdinaryWriteFaultProxy)(object)proxy;probe.Target=target;probe.State=state;return proxy;
    }
    protected override object? Invoke(MethodInfo? method,object?[]? args)
    {
        object? result;
        try{result=method!.Invoke(Target,args);}
        catch(TargetInvocationException cause) when(cause.InnerException!=null)
        {ExceptionDispatchInfo.Capture(cause.InnerException).Throw();throw;}
        if(result is IAsyncSession session)return Wrap(session,State);
        if(result is Task<IAsyncTransaction> transaction)return WrapTransaction(transaction);
        if(Target is IAsyncTransaction)
        {
            if(method!.Name=="RunAsync" && State.Mode is "rollback" or "rollbackLost")
                return LoseQuery((Task<IResultCursor>)result!);
            if(method.Name=="RollbackAsync")
            {State.Rollbacks++;return LoseAcknowledgement((Task)result!,State.Mode=="rollbackLost" ? State.Rollback:null);}
            if(method.Name=="CommitAsync")
            {State.Commits++;return LoseAcknowledgement((Task)result!,State.Mode=="commitLost" ? State.Commit:null);}
            if(method.Name=="Dispose")
            {State.TransactionDisposals++;if(State.Mode!="commitLost")throw State.TransactionCleanup;}
        }
        if(Target is IAsyncSession && method!.Name=="Dispose")
        {State.SessionDisposals++;if(State.Mode!="commitLost")throw State.SessionCleanup;}
        return result;
    }
    private async Task<IAsyncTransaction> WrapTransaction(Task<IAsyncTransaction> transaction)=>Wrap(await transaction,State);
    private async Task<IResultCursor> LoseQuery(Task<IResultCursor> query){await query;throw State.Query;}
    private static async Task LoseAcknowledgement(Task action,Exception? failure){await action;if(failure!=null)throw failure;}
}
