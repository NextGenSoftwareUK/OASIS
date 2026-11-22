using System.Diagnostics;
using Microsoft.Extensions.Logging;

internal sealed class SanitizedHttpExceptionLogger : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new ErrorLogger();
    public void Dispose() { }
    private sealed class ErrorLogger : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => level >= LogLevel.Error;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(level) || exception == null) return;
            Console.WriteLine("MFA_HTTP_SERVER_EXCEPTION: type=" + exception.GetType().FullName +
                " base=" + exception.GetBaseException().GetType().FullName);
            Console.WriteLine("MFA_HTTP_SERVER_METHODS: " + string.Join(" | ",
                new StackTrace(exception.GetBaseException(), false).GetFrames()?.Take(8).Select(frame =>
                    frame.GetMethod()?.DeclaringType?.FullName + "." + frame.GetMethod()?.Name) ?? Array.Empty<string>()));
        }
    }
}
