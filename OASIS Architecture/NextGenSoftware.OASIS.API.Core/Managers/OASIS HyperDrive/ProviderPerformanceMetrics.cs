using System;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public class ProviderPerformanceMetrics
    {
        public ProviderType ProviderType { get; set; }
        public double ResponseTimeMs { get; set; }
        public double ThroughputMbps { get; set; }
        public double ErrorRate { get; set; }
        public double UptimePercentage { get; set; }
        public int ActiveConnections { get; set; }
        public double CostPerOperation { get; set; }
        public string GeographicRegion { get; set; }
        public DateTime LastUpdated { get; set; }
        public int TotalRequests { get; set; }
        public int SuccessfulRequests { get; set; }
        public int FailedRequests { get; set; }
        public double AverageLatency { get; set; }
        public double PeakLatency { get; set; }
        public double MinLatency { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double NetworkLatency { get; set; }
        public double BandwidthUtilization { get; set; }
        public double QueueDepth { get; set; }
        public double ProcessingTime { get; set; }
        public double AvailabilityScore { get; set; }
        public double PerformanceScore { get; set; }
        public double CostScore { get; set; }
        public double GeographicScore { get; set; }
        public double OverallScore { get; set; }
        public double AverageResponseTime { get; set; }
        public double TotalResponseTime { get; set; }
        public DateTime LastActivity { get; set; }
    }
}
