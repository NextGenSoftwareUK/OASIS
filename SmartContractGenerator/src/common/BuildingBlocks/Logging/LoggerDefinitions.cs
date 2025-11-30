namespace BuildingBlocks.Logging;

#region Helper Methods and Scoped Operations

public static partial class LoggerDefinitions
{
    public static IDisposable BeginScopedOperation(
        this ILogger logger,
        string operationName,
        string? userId = LoggingConstants.SystemUser,
        string? correlationId = null,
        PerformanceThreshold performanceThreshold = PerformanceThreshold.Normal,
        bool trackMemory = false,
        [CallerMemberName] string callerMethod = "")
    {
        correlationId ??= LoggingHelpers.CreateCorrelationId();
        return new EnhancedOperationScope(logger, operationName, userId, correlationId, (double)performanceThreshold, trackMemory,
            callerMethod);
    }

    [LoggerMessage(
        EventId = (int)LogEventId.MethodParameters,
        Level = LogLevel.Trace,
        EventName = nameof(LogEventId.MethodParameters),
        Message = "🔧 Method parameters: {MethodName} | Parameters: {Parameters}")]
    public static partial void MethodParameters(
        this ILogger logger,
        string methodName,
        string parameters);

    private sealed class EnhancedOperationScope : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _operationName;
        private readonly string? _correlationId;
        private readonly double _performanceThresholdMs;
        private readonly bool _trackMemory;
        private readonly Stopwatch _stopwatch;
        private readonly long _startMemory;
        private bool _disposed;

        public EnhancedOperationScope(
            ILogger logger,
            string operationName,
            string? userId,
            string? correlationId,
            double performanceThresholdMs,
            bool trackMemory,
            string callerMethod)
        {
            _logger = logger;
            _operationName = operationName;
            _correlationId = correlationId;
            _performanceThresholdMs = performanceThresholdMs;
            _trackMemory = trackMemory;
            _stopwatch = Stopwatch.StartNew();
            _startMemory = trackMemory ? GC.GetTotalMemory(false) : 0;

            logger.OperationStarted(operationName, userId, correlationId, callerMethod);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _stopwatch.Stop();
            double elapsedMs = _stopwatch.Elapsed.TotalMilliseconds;

            try
            {
                _logger.OperationCompleted(_operationName, elapsedMs, _correlationId);

                if (elapsedMs > _performanceThresholdMs)
                {
                    PerformanceThreshold threshold = LoggingHelpers.GetPerformanceThreshold(elapsedMs);
                    _logger.SlowOperationDetected(_operationName, elapsedMs, threshold);
                }

                if (_trackMemory)
                {
                    long endMemory = GC.GetTotalMemory(false);
                    long allocatedBytes = Math.Max(0, endMemory - _startMemory);
                    double startMemoryMb = _startMemory / (1024.0 * 1024.0);
                    double endMemoryMb = endMemory / (1024.0 * 1024.0);
                    double allocatedMb = allocatedBytes / (1024.0 * 1024.0);

                    _logger.MemoryMetrics(_operationName, startMemoryMb, endMemoryMb, allocatedMb);
                }
            }
            finally
            {
                _disposed = true;
            }
        }
    }
}

#endregion

#region Operation Lifecycle

public static partial class LoggerDefinitions
{
    [LoggerMessage(
        EventId = (int)LogEventId.OperationStarted,
        Level = LogLevel.Information,
        EventName = nameof(LogEventId.OperationStarted),
        Message =
            "Operation started: {OperationName} | User: {UserId} | Correlation: {CorrelationId} | Source: {CallerMethod}")]
    public static partial void OperationStarted(
        this ILogger logger,
        string operationName,
        string? userId = null,
        string? correlationId = null,
        [CallerMemberName] string callerMethod = "");

    [LoggerMessage(
        EventId = (int)LogEventId.OperationCompleted,
        Level = LogLevel.Information,
        EventName = nameof(LogEventId.OperationCompleted),
        Message =
            "Operation completed: {OperationName} | Duration: {ElapsedMs}ms | Status: Success | Correlation: {CorrelationId}")]
    public static partial void OperationCompleted(
        this ILogger logger,
        string operationName,
        double elapsedMs,
        string? correlationId = null);

    [LoggerMessage(
        EventId = (int)LogEventId.OperationCancelled,
        Level = LogLevel.Warning,
        EventName = nameof(LogEventId.OperationCancelled),
        Message = "Operation cancelled: {OperationName} | Duration: {ElapsedMs}ms | Correlation: {CorrelationId}")]
    public static partial void OperationCancelled(
        this ILogger logger,
        string operationName,
        double elapsedMs,
        string? correlationId = null);
}

#endregion

#region Performance Monitoring

public static partial class LoggerDefinitions
{
    [LoggerMessage(
        EventId = (int)LogEventId.SlowOperationDetected,
        Level = LogLevel.Warning,
        EventName = nameof(LogEventId.SlowOperationDetected),
        Message =
            "Slow operation detected: {OperationName} | Duration: {ElapsedMs}ms | Threshold: {Threshold} | Performance impact detected")]
    public static partial void SlowOperationDetected(
        this ILogger logger,
        string operationName,
        double elapsedMs,
        PerformanceThreshold threshold);

    [LoggerMessage(
        EventId = (int)LogEventId.MemoryMetrics,
        Level = LogLevel.Debug,
        EventName = nameof(LogEventId.MemoryMetrics),
        Message =
            "Memory metrics: {OperationName} | Before: {MemoryBeforeMb}MB | After: {MemoryAfterMb}MB | Allocated: {AllocatedMb}MB")]
    public static partial void MemoryMetrics(
        this ILogger logger,
        string operationName,
        double memoryBeforeMb,
        double memoryAfterMb,
        double allocatedMb);

    [LoggerMessage(
        EventId = (int)LogEventId.CpuMetrics,
        Level = LogLevel.Debug,
        EventName = nameof(LogEventId.CpuMetrics),
        Message = "CPU metrics: {OperationName} | CPU Time: {CpuTimeMs}ms | Thread Time: {ThreadTimeMs}ms")]
    public static partial void CpuMetrics(
        this ILogger logger,
        string operationName,
        double cpuTimeMs,
        double threadTimeMs);
}

#endregion

#region Error Handling & Exceptions

public static partial class LoggerDefinitions
{
    [LoggerMessage(
        EventId = (int)LogEventId.ValidationFailed,
        Level = LogLevel.Warning,
        EventName = nameof(LogEventId.ValidationFailed),
        Message = "Validation failed: {OperationName} | Errors: {ValidationErrors} | User: {UserId}")]
    public static partial void ValidationFailed(
        this ILogger logger,
        string operationName,
        string validationErrors,
        string? userId = null);

    [LoggerMessage(
        EventId = (int)LogEventId.OperationFailed,
        Level = LogLevel.Error,
        EventName = nameof(LogEventId.OperationFailed),
        Message = "Operation failed: {OperationName} | Error: {ErrorMessage} | User: {UserId} | Correlation: {CorrelationId}")]
    public static partial void OperationFailed(
        this ILogger logger,
        string operationName,
        string errorMessage,
        string? userId = null,
        string? correlationId = null);

    [LoggerMessage(
        EventId = (int)LogEventId.OperationFailedWithException,
        Level = LogLevel.Error,
        EventName = nameof(LogEventId.OperationFailedWithException),
        Message =
            "Operation failed with exception: {OperationName} | Type: {ExceptionMessage} | User: {UserId} | Correlation: {CorrelationId}")]
    public static partial void OperationFailedWithException(
        this ILogger logger,
        string operationName,
        string exceptionMessage,
        string? userId = null,
        string? correlationId = null);

    [LoggerMessage(
        EventId = (int)LogEventId.RetryAttempt,
        Level = LogLevel.Warning,
        EventName = nameof(LogEventId.RetryAttempt),
        Message =
            "Retry attempt: {OperationName} | Attempt: {AttemptNumber}/{MaxAttempts} | Delay: {DelayMs}ms | Last error: {LastError}")]
    public static partial void RetryAttempt(
        this ILogger logger,
        string operationName,
        int attemptNumber,
        int maxAttempts,
        double delayMs,
        string lastError);
}

#endregion

#region Security & Audit

public static partial class LoggerDefinitions
{
    [LoggerMessage(
        EventId = (int)LogEventId.AuthenticationEvent,
        Level = LogLevel.Information,
        EventName = nameof(LogEventId.AuthenticationEvent),
        Message = "Authentication: {EventType} | User: {UserId} | IP: {IpAddress} | UserAgent: {UserAgent}")]
    public static partial void AuthenticationEvent(
        this ILogger logger,
        AuthenticationEventType eventType,
        string userId,
        string? ipAddress = null,
        string? userAgent = null);

    [LoggerMessage(
        EventId = (int)LogEventId.AccessDenied,
        Level = LogLevel.Warning,
        EventName = nameof(LogEventId.AccessDenied),
        Message =
            "Access denied: {Resource} | User: {UserId} | Required permissions: {RequiredPermissions} | IP: {IpAddress}")]
    public static partial void AccessDenied(
        this ILogger logger,
        string resource,
        string userId,
        string requiredPermissions,
        string? ipAddress = null);

    [LoggerMessage(
        EventId = (int)LogEventId.SensitiveDataAccessed,
        Level = LogLevel.Information,
        EventName = nameof(LogEventId.SensitiveDataAccessed),
        Message = "Sensitive data accessed: {DataType} | User: {UserId} | Reason: {AccessReason} | IP: {IpAddress}")]
    public static partial void SensitiveDataAccessed(
        this ILogger logger,
        SensitiveDataType dataType,
        string userId,
        DataAccessReason accessReason,
        string? ipAddress = null);
}

#endregion

#region External Dependencies

public static partial class LoggerDefinitions
{
    [LoggerMessage(
        EventId = (int)LogEventId.ExternalApiCall,
        Level = LogLevel.Debug,
        EventName = nameof(LogEventId.ExternalApiCall),
        Message =
            "External API call: {ServiceName} | Method: {HttpMethod} | URL: {Url} | Duration: {ElapsedMs}ms | Status: {StatusCode}")]
    public static partial void ExternalApiCall(
        this ILogger logger,
        ExternalService serviceName,
        HttpMethodType httpMethod,
        string url,
        double elapsedMs,
        int statusCode);

    [LoggerMessage(
        EventId = (int)LogEventId.DatabaseOperation,
        Level = LogLevel.Debug,
        EventName = nameof(LogEventId.DatabaseOperation),
        Message =
            "Database operation: {OperationName} | Table: {TableName} | Duration: {ElapsedMs}ms | Rows affected: {RowsAffected}")]
    public static partial void DatabaseOperation(
        this ILogger logger,
        DatabaseOperationType operationName,
        string tableName,
        double elapsedMs,
        int rowsAffected);

    [LoggerMessage(
        EventId = (int)LogEventId.CacheOperation,
        Level = LogLevel.Debug,
        EventName = nameof(LogEventId.CacheOperation),
        Message =
            "Cache operation: {OperationName} | Key: {CacheKey} | Hit: {IsHit} | Duration: {ElapsedMs}ms | Size: {SizeBytes}B")]
    public static partial void CacheOperation(
        this ILogger logger,
        CacheOperationType operationName,
        string cacheKey,
        bool isHit,
        double elapsedMs,
        long sizeBytes);
}

#endregion