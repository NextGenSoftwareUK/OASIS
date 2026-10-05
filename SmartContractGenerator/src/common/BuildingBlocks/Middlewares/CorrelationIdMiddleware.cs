namespace BuildingBlocks.Middlewares;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId = string.Empty;
        if (!context.Request.Headers.TryGetValue(CorrelationIdHeader, out _))
        {
            correlationId = LoggingHelpers.CreateCorrelationId();
            context.Request.Headers[CorrelationIdHeader] = correlationId;
        }

        context.Items[CorrelationIdHeader] = correlationId;

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
            {
                context.Response.Headers.Add(CorrelationIdHeader, correlationId);
            }

            return Task.CompletedTask;
        });

        await next(context);
    }
}