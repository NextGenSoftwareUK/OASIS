using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive
{
    /// <summary>
    /// Represents a performance data point for HyperDrive analytics and AI training
    /// </summary>
    public class PerformanceDataPoint
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public ProviderType ProviderType { get; set; }
        public string ProviderName { get; set; }
        public string Operation { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public TimeSpan Duration { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public long DataSize { get; set; }
        public int RetryCount { get; set; }
        public decimal Cost { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
        public string UserId { get; set; }
        public string SessionId { get; set; }
        public string RequestId { get; set; }
        public string AvatarId { get; set; }
        public string HolonId { get; set; }
        public string SourceIP { get; set; }
        public string UserAgent { get; set; }
        public GeographicLocation Location { get; set; }
        public Dictionary<string, string> Tags { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Aggregated performance metrics for analysis
    /// </summary>
    public class PerformanceMetrics
    {
        public double AverageLatency { get; set; }
        public double Throughput { get; set; }
        public double SuccessRate { get; set; }
        public double ErrorRate { get; set; }
        public decimal TotalCost { get; set; }
        public long TotalOperations { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public Dictionary<ProviderType, double> ProviderMetrics { get; set; } = new Dictionary<ProviderType, double>();
        public Dictionary<string, double> OperationMetrics { get; set; } = new Dictionary<string, double>();
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TimeWindow { get; set; }
    }

    /// <summary>
    /// Performance benchmark data for comparison
    /// </summary>
    public class PerformanceBenchmark
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Description { get; set; }
        public ProviderType ProviderType { get; set; }
        public string Operation { get; set; }
        public double BaselineLatency { get; set; }
        public double BaselineThroughput { get; set; }
        public decimal BaselineCost { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public Dictionary<string, object> Conditions { get; set; } = new Dictionary<string, object>();
    }
}
