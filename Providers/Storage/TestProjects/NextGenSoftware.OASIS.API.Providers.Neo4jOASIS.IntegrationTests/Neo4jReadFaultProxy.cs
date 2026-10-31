using System.Collections;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Neo4j.Driver;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public class Neo4jReadFaultProxy : DispatchProxy
{
    public object Target = null!;
    public string ParentId = null!;
    public Exception Failure = null!;
    public Action Observed = null!;

    public static T Wrap<T>(T target, string parentId, Exception failure, Action observed) where T : class
    {
        var proxy = Create<T, Neo4jReadFaultProxy>();
        var state = (Neo4jReadFaultProxy)(object)proxy;
        state.Target = target; state.ParentId = parentId; state.Failure = failure; state.Observed = observed;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        object? result;
        try { result = method!.Invoke(Target, args); }
        catch (TargetInvocationException cause) when (cause.InnerException != null)
        { ExceptionDispatchInfo.Capture(cause.InnerException).Throw(); throw; }
        if (result is IAsyncSession session) return Wrap(session, ParentId, Failure, Observed);
        if (result is Task<IResultCursor> cursor && args?.Length >= 2 && args[0] is string query && query.Contains("HAS_CHILD", StringComparison.Ordinal))
        {
            var parameters = args[1];
            var value = parameters is IDictionary dictionary ? dictionary["id"] : parameters?.GetType().GetProperty("id")?.GetValue(parameters);
            if ((string?)value == ParentId) return ReadThenFailAsync(cursor);
        }
        return result;
    }

    private async Task<IResultCursor> ReadThenFailAsync(Task<IResultCursor> read)
    {
        await (await read).ConsumeAsync(); // Complete an actual server read; inject only its boundary failure.
        Observed();
        throw Failure;
    }
}
