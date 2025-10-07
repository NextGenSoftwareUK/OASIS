using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Configuration
{
    /// <summary>
    /// OASIS HyperDrive Configuration settings
    /// </summary>
    public class OASISHyperDriveConfig
    {
        [Required]
        public bool IsEnabled { get; set; } = true;

        [Required]
        public LoadBalancingStrategy DefaultStrategy { get; set; } = LoadBalancingStrategy.Auto;

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
        public double LatencyWeight { get; set; } = 0.5; // Primary criteria for lag/ping

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
        public List<ProviderType> EnabledProviders { get; set; } = new List<ProviderType>();

        [Required]
        public List<ProviderType> AutoFailoverProviders { get; set; } = new List<ProviderType>();

        [Required]
        public List<ProviderType> AutoReplicationProviders { get; set; } = new List<ProviderType>();

        [Required]
        public List<ProviderType> LoadBalancingProviders { get; set; } = new List<ProviderType>();

        public Dictionary<ProviderType, ProviderConfig> ProviderConfigs { get; set; } = new Dictionary<ProviderType, ProviderConfig>();

        public GeographicConfig GeographicConfig { get; set; } = new GeographicConfig();

        public CostConfig CostConfig { get; set; } = new CostConfig();

        public PerformanceConfig PerformanceConfig { get; set; } = new PerformanceConfig();

        public SecurityConfig SecurityConfig { get; set; } = new SecurityConfig();

        public MonitoringConfig MonitoringConfig { get; set; } = new MonitoringConfig();
    }

    /// <summary>
    /// Individual provider configuration
    /// </summary>
    public class ProviderConfig
    {
        [Required]
        public ProviderType ProviderType { get; set; }

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

    /// <summary>
    /// Geographic configuration for routing
    /// </summary>
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

    /// <summary>
    /// Geographic region information
    /// </summary>
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

    /// <summary>
    /// Cost configuration for provider selection
    /// </summary>
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

        public Dictionary<ProviderType, CostAnalysis> ProviderCosts { get; set; } = new Dictionary<ProviderType, CostAnalysis>();
    }

    /// <summary>
    /// Performance configuration
    /// </summary>
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

    /// <summary>
    /// Security configuration
    /// </summary>
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
        public int SessionTimeoutMs { get; set; } = 300000; // 5 minutes

        [Required]
        [Range(1, 100)]
        public int MaxConcurrentSessions { get; set; } = 10;

        public List<string> AllowedIPs { get; set; } = new List<string>();
        public List<string> BlockedIPs { get; set; } = new List<string>();
        public Dictionary<string, string> SecurityHeaders { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Monitoring configuration
    /// </summary>
    public class MonitoringConfig
    {
        [Required]
        public bool IsEnabled { get; set; } = true;

        [Required]
        [Range(1000, 3600000)]
        public int MetricsCollectionIntervalMs { get; set; } = 30000; // 30 seconds

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

    /// <summary>
    /// Load balancing strategy weights
    /// </summary>
    public class LoadBalancingWeights
    {
        [Required]
        [Range(0.0, 1.0)]
        public double LatencyWeight { get; set; } = 0.5; // Primary criteria

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
