using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware;

/// <summary>Bounded, private-data-free timing for authentication and history requests.</summary>
public sealed class RequestTimingMiddleware
{
    private static readonly object StateKey = new();
    private readonly RequestDelegate next;
    private readonly ILogger<RequestTimingMiddleware> logger;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    { this.next = next; this.logger = logger; }

    public enum Phase
    {
        Pipeline, AvatarLookup, Endpoint, EngineConfiguration, Authentication,
        RefreshSession, HolonLookup, PermissionCheck, HistoryVersions, ActorNames, SealVerification
    }

    public static void Mark(HttpContext context, Phase phase)
    {
        if (context.Items.TryGetValue(StateKey, out var value) && value is RequestState state)
            Volatile.Write(ref state.PhaseValue, (int)phase);
    }

    public async Task Invoke(HttpContext context)
    {
        var route = Classify(context.Request.Path);
        if (route.Length == 0) { await next(context); return; }
        var state = new RequestState();
        var operation = Guid.NewGuid().ToString("N");
        context.Items[StateKey] = state;
        var watch = Stopwatch.StartNew();
        var failed = false;
        // Only fixed route/phase labels and timings are emitted: no URL, query,
        // headers, body, avatar/holon IDs, credentials or exception text.
        logger.LogInformation("OASIS request started operation={Operation} route={Route}", operation, route);
        using var timer = new Timer(_ =>
        {
            lock (state)
            {
                if (state.Completed) return;
                logger.LogWarning("OASIS request pending operation={Operation} route={Route} phase={Phase} elapsedMs={ElapsedMs}",
                    operation, route, (Phase)Volatile.Read(ref state.PhaseValue), watch.ElapsedMilliseconds);
            }
        }, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
        try { await next(context); }
        catch { failed = true; throw; }
        finally
        {
            lock (state)
            {
                state.Completed = true;
                logger.Log(watch.ElapsedMilliseconds >= 1000 || failed ? LogLevel.Warning : LogLevel.Information,
                    "OASIS request finished operation={Operation} route={Route} phase={Phase} elapsedMs={ElapsedMs} status={Status} failed={Failed} aborted={Aborted}",
                    operation, route, (Phase)Volatile.Read(ref state.PhaseValue), watch.ElapsedMilliseconds,
                    context.Response.StatusCode, failed, context.RequestAborted.IsCancellationRequested);
            }
            context.Items.Remove(StateKey);
        }
    }

    private static string Classify(PathString path)
    {
        if (path.Equals("/api/avatar/authenticate", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/api/avatar/authenticate", StringComparison.OrdinalIgnoreCase)) return "authenticate";
        if (path.Equals("/api/avatar/refresh-token", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/api/avatar/refresh-token", StringComparison.OrdinalIgnoreCase)) return "refresh-token";
        if (path.StartsWithSegments("/api/data/holon-audit-trail", StringComparison.OrdinalIgnoreCase)) return "holon-audit-trail";
        if (path.StartsWithSegments("/api/data/holon-access-log", StringComparison.OrdinalIgnoreCase)) return "holon-access-log";
        return string.Empty;
    }

    private sealed class RequestState
    {
        public int PhaseValue;
        public bool Completed;
    }
}
