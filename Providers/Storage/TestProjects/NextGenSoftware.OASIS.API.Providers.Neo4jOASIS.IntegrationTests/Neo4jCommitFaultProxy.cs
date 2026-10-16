using System.Reflection;
using System.Runtime.ExceptionServices;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

// Fault injection delegates every database action to the real SDK. It loses only the
// acknowledgement AFTER CommitAsync has returned successfully from the real server.
public class Neo4jCommitFaultProxy : DispatchProxy
{
    public object Target = null!;
    public Exception Failure = null!;
    public Action Committed = null!;

    public static T Wrap<T>(T target, Exception failure, Action committed) where T : class
    {
        var proxy = Create<T, Neo4jCommitFaultProxy>();
        var state = (Neo4jCommitFaultProxy)(object)proxy;
        state.Target = target; state.Failure = failure; state.Committed = committed;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        object? result;
        try { result = method!.Invoke(Target, args); }
        catch (TargetInvocationException cause) when (cause.InnerException != null)
        { ExceptionDispatchInfo.Capture(cause.InnerException).Throw(); throw; }
        if (result is IAsyncSession session) return Wrap(session, Failure, Committed);
        if (result is Task<IAsyncTransaction> transaction) return WrapTransactionAsync(transaction);
        if (Target is IAsyncTransaction && method!.Name == nameof(IAsyncTransaction.CommitAsync))
            return LoseCommitAcknowledgementAsync((Task)result!);
        return result;
    }

    private async Task<IAsyncTransaction> WrapTransactionAsync(Task<IAsyncTransaction> transaction)
        => Wrap(await transaction, Failure, Committed);

    private async Task LoseCommitAcknowledgementAsync(Task commit)
    {
        await commit;
        Committed();
        throw Failure;
    }
}
