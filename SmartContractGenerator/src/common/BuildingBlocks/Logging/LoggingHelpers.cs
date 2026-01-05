namespace BuildingBlocks.Logging;

public static class LoggingHelpers
{
    public static string CreateCorrelationId()
        => $"{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";

    public static PerformanceThreshold GetPerformanceThreshold(double elapsedMs)
        => elapsedMs switch
        {
            < 100 => PerformanceThreshold.Fast,
            < 500 => PerformanceThreshold.Normal,
            < 1000 => PerformanceThreshold.Slow,
            < 5000 => PerformanceThreshold.VerySlow,
            _ => PerformanceThreshold.Critical
        };
}