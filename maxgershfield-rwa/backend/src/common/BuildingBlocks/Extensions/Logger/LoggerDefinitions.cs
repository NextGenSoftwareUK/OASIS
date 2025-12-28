namespace BuildingBlocks.Extensions.Logger;

/// <summary>
/// A collection of strongly-typed logging definitions leveraging the `LoggerMessageAttribute` source generator
/// for high-performance structured logging.
/// 
/// This pattern eliminates boxing allocations and improves logging performance in hot paths,
/// especially under high-throughput services. It also promotes consistency in log messages and event IDs.
/// 
/// Recommended usage:
/// - Use `OperationStarted` and `OperationCompleted` for timing business operations.
/// - Use `OperationException` to log exceptions in a human-readable way.
/// </summary>
public static partial class LoggerDefinitions
{
    /// <summary>
    /// Logs when an operation has started.
    /// Includes the name of the operation and the timestamp at which it began.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="startTime">The time the operation started.</param>
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Operation started: {OperationName} at {StartTime}")]
    public static partial void OperationStarted(
        this ILogger logger,
        string operationName,
        DateTimeOffset startTime);

    /// <summary>
    /// Logs when an operation has successfully completed.
    /// Includes operation name, end time, and elapsed duration.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="endTime">The time the operation finished.</param>
    /// <param name="elapsed">The total duration of the operation.</param>
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Operation completed: {OperationName} at {EndTime}. Duration: {Elapsed}")]
    public static partial void OperationCompleted(
        this ILogger logger,
        string operationName,
        DateTimeOffset endTime,
        TimeSpan elapsed);

    /// <summary>
    /// Logs an error that occurred during the specified operation.
    /// Intended for catching and recording exception messages without logging the full stack trace.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="operationName">The operation in which the exception occurred.</param>
    /// <param name="message">The exception message or custom error message.</param>
    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Exception in {OperationName}.\nMessages:{Message}")]
    public static partial void OperationException(this ILogger logger, string operationName, string message);

    /// <summary>
    /// Logs an error that occurred during the specified operation.
    /// Intended for catching and recording exception with full stack trace.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="ex">Throwed exception to be logged.</param>
    /// <param name="operationName">The operation in which the exception occurred.</param>
    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "Exception in {OperationName}.")]
    public static partial void OperationException(this ILogger logger, Exception ex, string operationName);
}