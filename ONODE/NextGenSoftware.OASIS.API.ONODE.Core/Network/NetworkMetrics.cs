using System;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{
    /// <summary>
    /// Network metrics data structure
    /// </summary>
    public class NetworkMetrics
    {
        public int ActiveConnections { get; set; }
        public int TotalConnections { get; set; }
        public double AverageLatency { get; set; }
        public double TotalThroughput { get; set; }
        public string NetworkId { get; set; }
        public DateTime Timestamp { get; set; }
        
        // Additional properties for node-specific metrics
        public double Latency { get; set; }
        public double Reliability { get; set; }
        public double Throughput { get; set; }
        public DateTime LastUpdated { get; set; }
        
        // Additional properties for latency analysis
        public double MaxLatency { get; set; }
        public double LatencyVariance { get; set; }
        
        // Additional properties for network analysis
        public double Stability { get; set; }
        public double TrafficLoad { get; set; }
        public double Health { get; set; }
        public double Capacity { get; set; }
    }

    /// <summary>
    /// System metrics data structure
    /// </summary>
    public class SystemMetrics
    {
        public double CpuLoad { get; set; }
        public double MemoryLoad { get; set; }
        public double DiskLoad { get; set; }
        public DateTime Timestamp { get; set; }
    }
}









