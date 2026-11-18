using System.Collections.Concurrent;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware;
using Xunit;

public sealed class RequestTimingTests
{
    [Fact]
    public async Task SlowRequestReportsItsLiveStageWithoutPrivateRequestData()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/data/holon-audit-trail/private-holon-id";
        context.Request.QueryString = new QueryString("?secret=private-query");
        context.Request.Headers.Authorization = "Bearer private-token";
        var logger = new RecordedLogger();
        var pipeline = new RequestTimingMiddleware(async http =>
        {
            RequestTimingMiddleware.Mark(http, RequestTimingMiddleware.Phase.HistoryVersions);
            await Task.Delay(5500);
            http.Response.StatusCode = 206;
        }, logger);
        await pipeline.Invoke(context);
        Assert.Contains(logger.Messages, text => text.Contains("pending") && text.Contains("HistoryVersions"));
        Assert.Contains(logger.Messages, text => text.Contains("finished") && text.Contains("206"));
        var output = string.Join("\n", logger.Messages);
        Assert.DoesNotContain("private-", output);
        Assert.DoesNotContain("Bearer", output);
        Assert.DoesNotContain("secret", output);
        Assert.Empty(context.Items);
    }

    [Fact]
    public async Task FailurePropagatesUnchangedAndDoesNotLogExceptionContents()
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/avatar/authenticate";
        var expected = new InvalidOperationException("private-password");
        var logger = new RecordedLogger();
        var pipeline = new RequestTimingMiddleware(_ => Task.FromException(expected), logger);
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.Invoke(context));
        Assert.Same(expected, actual);
        Assert.DoesNotContain("private-password", string.Join("\n", logger.Messages));
        Assert.Contains(logger.Messages, text => text.Contains("failed=True"));
        Assert.Empty(context.Items);
    }

    [Theory]
    [InlineData("/api/health")]
    [InlineData("/api/avatar/authenticate-token/private-token")]
    [InlineData("/api/data/holon-audit-trail-unrelated")]
    public async Task OtherRoutesHaveNoTimerOrDiagnosticState(string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        var logger = new RecordedLogger();
        var invoked = false;
        var pipeline = new RequestTimingMiddleware(http =>
        {
            Assert.Empty(http.Items);
            invoked = true;
            return Task.CompletedTask;
        }, logger);
        await pipeline.Invoke(context);
        Assert.True(invoked);
        Assert.Empty(logger.Messages);
    }

    private sealed class RecordedLogger : ILogger<RequestTimingMiddleware>
    {
        public ConcurrentQueue<string> Messages { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Enqueue(formatter(state, exception));
    }
}
