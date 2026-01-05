namespace BuildingBlocks.Enums.Logging;


public enum LogEventId
{
    // Operation Lifecycle (1000-1999)
    OperationStarted = 1000,
    OperationCompleted = 1001,
    OperationCancelled = 1002,
    
    // Performance Monitoring (2000-2999)
    SlowOperationDetected = 2000,
    MemoryMetrics = 2001,
    CpuMetrics = 2002,
    
    // Error Handling & Exceptions (3000-3999)
    ValidationFailed = 3000,
    OperationFailed = 3001,
    OperationFailedWithException = 3002,
    RetryAttempt = 3003,
    
    // Security & Audit (4000-4999)
    AuthenticationEvent = 4000,
    AccessDenied = 4001,
    SensitiveDataAccessed = 4002,
    
    // External Dependencies (5000-5999)
    ExternalApiCall = 5000,
    DatabaseOperation = 5001,
    CacheOperation = 5002,
    
    // Helper Methods (6000-6999)
    MethodParameters = 6000
}