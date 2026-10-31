using System.Reflection;
using System.Runtime.ExceptionServices;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

// Lose the receipt statement acknowledgement after the real native mutation,
// before COMMIT dispatch. The existing transaction owner must roll everything back.
public class Neo4jReceiptFaultProxy : DispatchProxy
{
    public sealed class Probe { public int Receipts; public readonly IOException Failure=new("owned delta receipt acknowledgement failure"); }
    public object Target=null!; public Probe State=null!;
    public static T Wrap<T>(T target,Probe state) where T:class
    {var result=Create<T,Neo4jReceiptFaultProxy>();var proxy=(Neo4jReceiptFaultProxy)(object)result;proxy.Target=target;proxy.State=state;return result;}
    protected override object? Invoke(MethodInfo? method,object?[]? args)
    {
        object? result;
        try {result=method!.Invoke(Target,args);}
        catch(TargetInvocationException cause) when(cause.InnerException!=null)
        {ExceptionDispatchInfo.Capture(cause.InnerException).Throw();throw;}
        if(result is IAsyncSession session)return Wrap(session,State);
        if(result is Task<IAsyncTransaction> transaction)return WrapTransaction(transaction);
        if(Target is IAsyncTransaction&&method!.Name=="RunAsync"&&args![0] is string query&&query.Contains("SET r.Fingerprint=$fingerprint,r.ResultJson=$json",StringComparison.Ordinal))
            return LoseReceipt((Task<IResultCursor>)result!);
        return result;
    }
    private async Task<IAsyncTransaction> WrapTransaction(Task<IAsyncTransaction> task)=>Wrap(await task,State);
    private async Task<IResultCursor> LoseReceipt(Task<IResultCursor> task)
    {var row=await(await task).SingleAsync();if(row["count"].As<long>()!=1)throw new InvalidDataException("Owned receipt write was not acknowledged.");State.Receipts++;throw State.Failure;}
}
