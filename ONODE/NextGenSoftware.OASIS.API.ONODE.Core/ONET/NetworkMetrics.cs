using System;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{
    /// <summary>
    /// Network metrics data structure, used by ONETRouting and ONETDiscovery. Previously defined inside
    /// NetworkMetricsService.cs alongside an entire NetworkMetricsService class that nothing in the codebase
    /// ever instantiated - that dead service class was removed, but this data type is genuinely used by both
    /// of the classes above, so it's kept here on its own.
    /// </summary>
    public class NetworkMetrics
    {
        public int ActiveConnections { get; set; }
        public int TotalConnections { get; set; }
        public double AverageLatency { get; set; }
        public double TotalThroughput { get; set; }
        public string NetworkId { get; set; }
        public DateTime Timestamp { get; set; }

        public double Latency { get; set; }
        public double Reliability { get; set; }
        public double Throughput { get; set; }
        public DateTime LastUpdated { get; set; }

        public double MaxLatency { get; set; }
        public double LatencyVariance { get; set; }

        public double Stability { get; set; }
        public double TrafficLoad { get; set; }
        public double Health { get; set; }
        public double Capacity { get; set; }
    }

    /// <summary>System metrics data structure, used by ONETRouting. See NetworkMetrics remarks above.</summary>
    public class SystemMetrics
    {
        public double CpuLoad { get; set; }
        public double MemoryLoad { get; set; }
        public double DiskLoad { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
