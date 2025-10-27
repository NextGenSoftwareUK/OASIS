using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
// Note: Avoid referencing Core enums here to prevent project cycles

namespace NextGenSoftware.OASIS.API.Core.Configuration
{
    public class OASISHyperDriveConfig
    {
        [Required]
        public bool IsEnabled { get; set; } = true;

        [Required]
        public string DefaultStrategy { get; set; } = "Auto";

        [Required]
        public bool AutoFailoverEnabled { get; set; } = true;

        [Required]
        public bool AutoReplicationEnabled { get; set; } = true;

        [Required]
        public bool AutoLoadBalancingEnabled { get; set; } = true;

        [Required]
        [Range(1, 100)]
        public int MaxRetryAttempts { get; set; } = 3;

        [Required]
        [Range(100, 30000)]
        public int RequestTimeoutMs { get; set; } = 5000;

        [Required]
        [Range(1, 1000)]
        public int HealthCheckIntervalMs { get; set; } = 30000;

        [Required]
        [Range(1, 100)]
        public int MaxConcurrentRequests { get; set; } = 100;

        [Required]
        [Range(0.1, 10.0)]
        public double PerformanceWeight { get; set; } = 0.4;

        [Required]
        [Range(0.1, 10.0)]
        public double CostWeight { get; set; } = 0.3;

        [Required]
        [Range(0.1, 10.0)]
        public double GeographicWeight { get; set; } = 0.2;

        [Required]
        [Range(0.1, 10.0)]
        public double AvailabilityWeight { get; set; } = 0.1;

        [Required]
        [Range(0.1, 10.0)]
        public double LatencyWeight { get; set; } = 0.5;

        [Required]
        [Range(0.1, 10.0)]
        public double ThroughputWeight { get; set; } = 0.3;

        [Required]
        [Range(0.1, 10.0)]
        public double ReliabilityWeight { get; set; } = 0.2;

        [Required]
        [Range(1, 1000)]
        public int MaxLatencyThresholdMs { get; set; } = 200;

        [Required]
        [Range(0.0, 1.0)]
        public double MaxErrorRateThreshold { get; set; } = 0.05;

        [Required]
        [Range(0.0, 100.0)]
        public double MinUptimeThreshold { get; set; } = 99.0;

        [Required]
        public List<string> EnabledProviders { get; set; } = new List<string>();

        [Required]
        public List<string> AutoFailoverProviders { get; set; } = new List<string>();

        [Required]
        public List<string> AutoReplicationProviders { get; set; } = new List<string>();

        [Required]
        public List<string> LoadBalancingProviders { get; set; } = new List<string>();

        public Dictionary<string, ProviderConfig> ProviderConfigs { get; set; } = new Dictionary<string, ProviderConfig>();

        public GeographicConfig GeographicConfig { get; set; } = new GeographicConfig();

        public CostConfig CostConfig { get; set; } = new CostConfig();

        public PerformanceConfig PerformanceConfig { get; set; } = new PerformanceConfig();

        public SecurityConfig SecurityConfig { get; set; } = new SecurityConfig();

        public MonitoringConfig MonitoringConfig { get; set; } = new MonitoringConfig();
    }

    public class ProviderConfig
    {
        [Required]
        public string ProviderType { get; set; }

        [Required]
        public bool IsEnabled { get; set; } = true;

        [Required]
        [Range(1, 100)]
        public int Weight { get; set; } = 50;

        [Required]
        [Range(100, 30000)]
        public int TimeoutMs { get; set; } = 5000;

        [Required]
        [Range(1, 1000)]
        public int MaxConnections { get; set; } = 100;

        [Required]
        [Range(0.0, 1.0)]
        public double ErrorThreshold { get; set; } = 0.05;

        [Required]
        [Range(0.0, 100.0)]
        public double MinUptime { get; set; } = 99.0;

        [Required]
        [Range(1, 1000)]
        public int MaxLatencyMs { get; set; } = 200;

        public string Endpoint { get; set; }
        public string ApiKey { get; set; }
        public string SecretKey { get; set; }
        public Dictionary<string, string> CustomSettings { get; set; } = new Dictionary<string, string>();
    }

    public class GeographicConfig
    {
        [Required]
        public bool IsEnabled { get; set; } = true;

        [Required]
        public string DefaultRegion { get; set; } = "Global";

        [Required]
        public string UserLocation { get; set; } = "Unknown";

        [Required]
        [Range(0.0, 1000.0)]
        public double MaxDistanceKm { get; set; } = 500.0;

        [Required]
        [Range(1, 100)]
        public int MaxNetworkHops { get; set; } = 10;

        [Required]
        [Range(1, 1000)]
        public int MaxLatencyMs { get; set; } = 200;

        public Dictionary<string, GeographicRegion> Regions { get; set; } = new Dictionary<string, GeographicRegion>();
    }

    public class GeographicRegion
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string Country { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public double Latitude { get; set; }

        [Required]
        public double Longitude { get; set; }

        [Required]
        public string TimeZone { get; set; }

        [Required]
        [Range(0.0, 1000.0)]
        public double MaxLatencyMs { get; set; } = 100.0;

        [Required]
        [Range(1, 100)]
        public int MaxNetworkHops { get; set; } = 5;
    }

    public class CostConfig
    {
        [Required]
        public bool IsEnabled { get; set; } = true;

        [Required]
        public string Currency { get; set; } = "USD";

        [Required]
        [Range(0.0, 1000.0)]
        public double MaxCostPerOperation { get; set; } = 1.0;

        [Required]
        [Range(0.0, 1000.0)]
        public double MaxStorageCostPerGB { get; set; } = 0.1;

        [Required]
        [Range(0.0, 1000.0)]
        public double MaxComputeCostPerHour { get; set; } = 0.5;

        [Required]
        [Range(0.0, 1000.0)]
        public double MaxNetworkCostPerGB { get; set; } = 0.05;

        public Dictionary<string, CostAnalysisDNA> ProviderCosts { get; set; } = new Dictionary<string, CostAnalysisDNA>();
    }

    public class CostAnalysisDNA
    {
        public string ProviderType { get; set; }
        public double StorageCostPerGB { get; set; }
        public double ComputeCostPerHour { get; set; }
        public double NetworkCostPerGB { get; set; }
        public double TransactionCost { get; set; }
        public double ApiCallCost { get; set; }
        public double TotalCost { get; set; }
        public string Currency { get; set; }
        public DateTime LastUpdated { get; set; }
        public int CostEfficiencyScore { get; set; }
    }

    public class PerformanceConfig
    {
        [Required]
        public bool IsEnabled { get; set; } = true;

        [Required]
        [Range(1, 10000)]
        public int MaxResponseTimeMs { get; set; } = 1000;

        [Required]
        [Range(0.0, 1.0)]
        public double MaxErrorRate { get; set; } = 0.05;

        [Required]
        [Range(0.0, 100.0)]
        public double MinUptime { get; set; } = 99.0;

        [Required]
        [Range(1, 1000)]
        public int MinThroughputMbps { get; set; } = 10;

        [Required]
        [Range(1, 100)]
        public int MaxConcurrentConnections { get; set; } = 100;

        [Required]
        [Range(1, 1000)]
        public int QueueDepthThreshold { get; set; } = 50;

        [Required]
        [Range(0.0, 100.0)]
        public double MaxCpuUsage { get; set; } = 80.0;

        [Required]
        [Range(0.0, 100.0)]
        public double MaxMemoryUsage { get; set; } = 80.0;
    }

    public class SecurityConfig
    {
        [Required]
        public bool IsEnabled { get; set; } = true;

        [Required]
        public bool RequireEncryption { get; set; } = true;

        [Required]
        public bool RequireAuthentication { get; set; } = true;

        [Required]
        public bool RequireAuthorization { get; set; } = true;

        [Required]
        [Range(1, 1000)]
        public int MaxRetryAttempts { get; set; } = 3;

        [Required]
        [Range(1000, 3600000)]
        public int SessionTimeoutMs { get; set; } = 300000;

        [Required]
        [Range(1, 100)]
        public int MaxConcurrentSessions { get; set; } = 10;

        public List<string> AllowedIPs { get; set; } = new List<string>();
        public List<string> BlockedIPs { get; set; } = new List<string>();
        public Dictionary<string, string> SecurityHeaders { get; set; } = new Dictionary<string, string>();
    }

    public class MonitoringConfig
    {
        [Required]
        public bool IsEnabled { get; set; } = true;

        [Required]
        [Range(1000, 3600000)]
        public int MetricsCollectionIntervalMs { get; set; } = 30000;

        [Required]
        [Range(1, 1000)]
        public int MaxMetricsHistory { get; set; } = 100;

        [Required]
        [Range(1, 100)]
        public int AlertThreshold { get; set; } = 5;

        [Required]
        public bool EnableRealTimeMonitoring { get; set; } = true;

        [Required]
        public bool EnablePerformanceProfiling { get; set; } = true;

        [Required]
        public bool EnableCostTracking { get; set; } = true;

        [Required]
        public bool EnableGeographicTracking { get; set; } = true;

        public List<string> MonitoringEndpoints { get; set; } = new List<string>();
        public Dictionary<string, string> CustomMetrics { get; set; } = new Dictionary<string, string>();
    }

    public class LoadBalancingWeights
    {
        [Required]
        [Range(0.0, 1.0)]
        public double LatencyWeight { get; set; } = 0.5;

        [Required]
        [Range(0.0, 1.0)]
        public double PerformanceWeight { get; set; } = 0.3;

        [Required]
        [Range(0.0, 1.0)]
        public double CostWeight { get; set; } = 0.1;

        [Required]
        [Range(0.0, 1.0)]
        public double GeographicWeight { get; set; } = 0.1;

        [Required]
        [Range(0.0, 1.0)]
        public double AvailabilityWeight { get; set; } = 0.0;

        [Required]
        [Range(0.0, 1.0)]
        public double ReliabilityWeight { get; set; } = 0.0;

        [Required]
        [Range(0.0, 1.0)]
        public double ThroughputWeight { get; set; } = 0.0;

        [Required]
        [Range(0.0, 1.0)]
        public double ConnectionWeight { get; set; } = 0.0;
    }
}


