using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Configuration;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Configuration
{
    /// <summary>
    /// Manages OASIS HyperDrive configuration with OASISDNA persistence
    /// </summary>
    public class OASISHyperDriveConfigManager
    {
        private static OASISHyperDriveConfigManager _instance;
        private OASISHyperDriveConfig _config;
        private readonly object _lockObject = new object();

        public static OASISHyperDriveConfigManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new OASISHyperDriveConfigManager();
                return _instance;
            }
        }

        private OASISHyperDriveConfigManager()
        {
            LoadConfiguration();
        }

        /// <summary>
        /// Gets the current HyperDrive configuration
        /// </summary>
        public OASISHyperDriveConfig GetConfiguration()
        {
            lock (_lockObject)
            {
                return _config ?? new OASISHyperDriveConfig();
            }
        }

        /// <summary>
        /// Updates the HyperDrive configuration
        /// </summary>
        public OASISResult<bool> UpdateConfiguration(OASISHyperDriveConfig config)
        {
            var result = new OASISResult<bool>();
            if (config == null)
            {
                result.IsError = true;
                result.Message = "The configuration is required. Please provide a valid OASISHyperDriveConfig.";
                return result;
            }
            try
            {
                lock (_lockObject)
                {
                    _config = config;
                    SaveConfiguration();
                    result.Result = true;
                    result.Message = "HyperDrive configuration updated successfully.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error updating HyperDrive configuration: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Loads configuration from OASISDNA
        /// </summary>
        private void LoadConfiguration()
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA;
                if (dna?.OASIS?.OASISHyperDriveConfig != null)
                {
                    _config = dna.OASIS.OASISHyperDriveConfig;
                }
                else
                {
                    _config = CreateDefaultConfiguration();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading HyperDrive configuration: {ex.Message}");
                _config = CreateDefaultConfiguration();
            }
        }

        /// <summary>
        /// Saves configuration to OASISDNA
        /// </summary>
        private void SaveConfiguration()
        {
            try
            {
                var dna = OASISDNAManager.OASISDNA;
                if (dna?.OASIS != null)
                {
                    dna.OASIS.OASISHyperDriveConfig = _config;
                    OASISDNAManager.SaveDNA();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving HyperDrive configuration: {ex.Message}");
            }
        }

        /// <summary>
        /// Creates default configuration
        /// </summary>
        private OASISHyperDriveConfig CreateDefaultConfiguration()
        {
            return new OASISHyperDriveConfig
            {
                IsEnabled = true,
                DefaultStrategy = "Auto",
                AutoFailoverEnabled = true,
                AutoReplicationEnabled = true,
                AutoLoadBalancingEnabled = true,
                MaxRetryAttempts = 3,
                RequestTimeoutMs = 5000,
                HealthCheckIntervalMs = 30000,
                MaxConcurrentRequests = 100,
                PerformanceWeight = 0.4,
                CostWeight = 0.3,
                GeographicWeight = 0.2,
                AvailabilityWeight = 0.1,
                LatencyWeight = 0.5, // Primary criteria
                ThroughputWeight = 0.3,
                ReliabilityWeight = 0.2,
                MaxLatencyThresholdMs = 200,
                MaxErrorRateThreshold = 0.05,
                MinUptimeThreshold = 99.0,
                EnabledProviders = new List<string>
                {
                    nameof(ProviderType.MongoDBOASIS),
                    nameof(ProviderType.SQLLiteDBOASIS),
                    nameof(ProviderType.EthereumOASIS),
                    nameof(ProviderType.IPFSOASIS)
                },
                AutoFailoverProviders = new List<string>
                {
                    nameof(ProviderType.MongoDBOASIS),
                    nameof(ProviderType.SQLLiteDBOASIS)
                },
                AutoReplicationProviders = new List<string>
                {
                    nameof(ProviderType.EthereumOASIS),
                    nameof(ProviderType.IPFSOASIS)
                },
                LoadBalancingProviders = new List<string>
                {
                    nameof(ProviderType.MongoDBOASIS),
                    nameof(ProviderType.SQLLiteDBOASIS),
                    nameof(ProviderType.EthereumOASIS),
                    nameof(ProviderType.IPFSOASIS)
                },
                ProviderConfigs = CreateDefaultProviderConfigs(),
                GeographicConfig = CreateDefaultGeographicConfig(),
                CostConfig = CreateDefaultCostConfig(),
                PerformanceConfig = CreateDefaultPerformanceConfig(),
                SecurityConfig = CreateDefaultSecurityConfig(),
                MonitoringConfig = CreateDefaultMonitoringConfig()
            };
        }

        /// <summary>
        /// Creates default provider configurations
        /// </summary>
        private Dictionary<string, ProviderConfig> CreateDefaultProviderConfigs()
        {
            var configs = new Dictionary<string, ProviderConfig>();

            // MongoDB Configuration
            configs[nameof(ProviderType.MongoDBOASIS)] = new ProviderConfig
            {
                ProviderType = nameof(ProviderType.MongoDBOASIS),
                IsEnabled = true,
                Weight = 80,
                TimeoutMs = 5000,
                MaxConnections = 100,
                ErrorThreshold = 0.05,
                MinUptime = 99.0,
                MaxLatencyMs = 200
            };

            // SQLite Configuration
            configs[nameof(ProviderType.SQLLiteDBOASIS)] = new ProviderConfig
            {
                ProviderType = nameof(ProviderType.SQLLiteDBOASIS),
                IsEnabled = true,
                Weight = 90,
                TimeoutMs = 3000,
                MaxConnections = 50,
                ErrorThreshold = 0.02,
                MinUptime = 99.5,
                MaxLatencyMs = 100
            };

            // Ethereum Configuration
            configs[nameof(ProviderType.EthereumOASIS)] = new ProviderConfig
            {
                ProviderType = nameof(ProviderType.EthereumOASIS),
                IsEnabled = true,
                Weight = 70,
                TimeoutMs = 10000,
                MaxConnections = 20,
                ErrorThreshold = 0.10,
                MinUptime = 95.0,
                MaxLatencyMs = 1000
            };

            // IPFS Configuration
            configs[nameof(ProviderType.IPFSOASIS)] = new ProviderConfig
            {
                ProviderType = nameof(ProviderType.IPFSOASIS),
                IsEnabled = true,
                Weight = 75,
                TimeoutMs = 8000,
                MaxConnections = 30,
                ErrorThreshold = 0.08,
                MinUptime = 97.0,
                MaxLatencyMs = 500
            };

            return configs;
        }

        /// <summary>
        /// Creates default geographic configuration
        /// </summary>
        private GeographicConfig CreateDefaultGeographicConfig()
        {
            return new GeographicConfig
            {
                IsEnabled = true,
                DefaultRegion = "Global",
                UserLocation = "Unknown",
                MaxDistanceKm = 500.0,
                MaxNetworkHops = 10,
                MaxLatencyMs = 200,
                Regions = new Dictionary<string, GeographicRegion>
                {
                    ["US-East"] = new GeographicRegion
                    {
                        Name = "US-East",
                        Country = "United States",
                        City = "New York",
                        Latitude = 40.7128,
                        Longitude = -74.0060,
                        TimeZone = "EST",
                        MaxLatencyMs = 100.0,
                        MaxNetworkHops = 5
                    },
                    ["US-West"] = new GeographicRegion
                    {
                        Name = "US-West",
                        Country = "United States",
                        City = "San Francisco",
                        Latitude = 37.7749,
                        Longitude = -122.4194,
                        TimeZone = "PST",
                        MaxLatencyMs = 100.0,
                        MaxNetworkHops = 5
                    },
                    ["Europe"] = new GeographicRegion
                    {
                        Name = "Europe",
                        Country = "United Kingdom",
                        City = "London",
                        Latitude = 51.5074,
                        Longitude = -0.1278,
                        TimeZone = "GMT",
                        MaxLatencyMs = 150.0,
                        MaxNetworkHops = 7
                    },
                    ["Asia"] = new GeographicRegion
                    {
                        Name = "Asia",
                        Country = "Japan",
                        City = "Tokyo",
                        Latitude = 35.6762,
                        Longitude = 139.6503,
                        TimeZone = "JST",
                        MaxLatencyMs = 200.0,
                        MaxNetworkHops = 10
                    }
                }
            };
        }

        /// <summary>
        /// Creates default cost configuration
        /// </summary>
        private CostConfig CreateDefaultCostConfig()
        {
            return new CostConfig
            {
                IsEnabled = true,
                Currency = "USD",
                MaxCostPerOperation = 1.0,
                MaxStorageCostPerGB = 0.1,
                MaxComputeCostPerHour = 0.5,
                MaxNetworkCostPerGB = 0.05,
                ProviderCosts = new Dictionary<string, CostAnalysisDNA>
                {
                    [nameof(ProviderType.MongoDBOASIS)] = new CostAnalysisDNA
                    {
                        ProviderType = nameof(ProviderType.MongoDBOASIS),
                        StorageCostPerGB = 0.05,
                        ComputeCostPerHour = 0.10,
                        NetworkCostPerGB = 0.02,
                        TransactionCost = 0.001,
                        ApiCallCost = 0.0001,
                        TotalCost = 0.15,
                        Currency = "USD",
                        LastUpdated = DateTime.UtcNow,
                        CostEfficiencyScore = 85
                    },
                    [nameof(ProviderType.SQLLiteDBOASIS)] = new CostAnalysisDNA
                    {
                        ProviderType = nameof(ProviderType.SQLLiteDBOASIS),
                        StorageCostPerGB = 0.01,
                        ComputeCostPerHour = 0.05,
                        NetworkCostPerGB = 0.00,
                        TransactionCost = 0.0001,
                        ApiCallCost = 0.00001,
                        TotalCost = 0.06,
                        Currency = "USD",
                        LastUpdated = DateTime.UtcNow,
                        CostEfficiencyScore = 95
                    },
                    [nameof(ProviderType.EthereumOASIS)] = new CostAnalysisDNA
                    {
                        ProviderType = nameof(ProviderType.EthereumOASIS),
                        StorageCostPerGB = 0.20,
                        ComputeCostPerHour = 0.50,
                        NetworkCostPerGB = 0.10,
                        TransactionCost = 0.01,
                        ApiCallCost = 0.001,
                        TotalCost = 0.81,
                        Currency = "USD",
                        LastUpdated = DateTime.UtcNow,
                        CostEfficiencyScore = 60
                    },
                    [nameof(ProviderType.IPFSOASIS)] = new CostAnalysisDNA
                    {
                        ProviderType = nameof(ProviderType.IPFSOASIS),
                        StorageCostPerGB = 0.03,
                        ComputeCostPerHour = 0.15,
                        NetworkCostPerGB = 0.01,
                        TransactionCost = 0.0005,
                        ApiCallCost = 0.0001,
                        TotalCost = 0.19,
                        Currency = "USD",
                        LastUpdated = DateTime.UtcNow,
                        CostEfficiencyScore = 80
                    }
                }
            };
        }

        /// <summary>
        /// Creates default performance configuration
        /// </summary>
        private PerformanceConfig CreateDefaultPerformanceConfig()
        {
            return new PerformanceConfig
            {
                IsEnabled = true,
                MaxResponseTimeMs = 1000,
                MaxErrorRate = 0.05,
                MinUptime = 99.0,
                MinThroughputMbps = 10,
                MaxConcurrentConnections = 100,
                QueueDepthThreshold = 50,
                MaxCpuUsage = 80.0,
                MaxMemoryUsage = 80.0
            };
        }

        /// <summary>
        /// Creates default security configuration
        /// </summary>
        private SecurityConfig CreateDefaultSecurityConfig()
        {
            return new SecurityConfig
            {
                IsEnabled = true,
                RequireEncryption = true,
                RequireAuthentication = true,
                RequireAuthorization = true,
                MaxRetryAttempts = 3,
                SessionTimeoutMs = 300000, // 5 minutes
                MaxConcurrentSessions = 10,
                AllowedIPs = new List<string>(),
                BlockedIPs = new List<string>(),
                SecurityHeaders = new Dictionary<string, string>
                {
                    ["X-Content-Type-Options"] = "nosniff",
                    ["X-Frame-Options"] = "DENY",
                    ["X-XSS-Protection"] = "1; mode=block"
                }
            };
        }

        /// <summary>
        /// Creates default monitoring configuration
        /// </summary>
        private MonitoringConfig CreateDefaultMonitoringConfig()
        {
            return new MonitoringConfig
            {
                IsEnabled = true,
                MetricsCollectionIntervalMs = 30000, // 30 seconds
                MaxMetricsHistory = 100,
                AlertThreshold = 5,
                EnableRealTimeMonitoring = true,
                EnablePerformanceProfiling = true,
                EnableCostTracking = true,
                EnableGeographicTracking = true,
                MonitoringEndpoints = new List<string>(),
                CustomMetrics = new Dictionary<string, string>()
            };
        }

        /// <summary>
        /// Resets configuration to defaults
        /// </summary>
        public OASISResult<bool> ResetToDefaults()
        {
            var result = new OASISResult<bool>();

            try
            {
                lock (_lockObject)
                {
                    _config = CreateDefaultConfiguration();
                    SaveConfiguration();
                    result.Result = true;
                    result.Message = "HyperDrive configuration reset to defaults successfully.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error resetting HyperDrive configuration: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Validates the current configuration
        /// </summary>
        public OASISResult<bool> ValidateConfiguration()
        {
            var result = new OASISResult<bool>();

            try
            {
                var config = GetConfiguration();
                
                // Validate weights sum to 1.0
                var totalWeight = config.PerformanceWeight + config.CostWeight + 
                                config.GeographicWeight + config.AvailabilityWeight;
                
                if (Math.Abs(totalWeight - 1.0) > 0.01)
                {
                    result.IsError = true;
                    result.Message = "Configuration weights must sum to 1.0";
                    return result;
                }

                // Validate thresholds
                if (config.MaxLatencyThresholdMs <= 0)
                {
                    result.IsError = true;
                    result.Message = "Max latency threshold must be greater than 0";
                    return result;
                }

                if (config.MaxErrorRateThreshold < 0 || config.MaxErrorRateThreshold > 1)
                {
                    result.IsError = true;
                    result.Message = "Max error rate threshold must be between 0 and 1";
                    return result;
                }

                if (config.MinUptimeThreshold < 0 || config.MinUptimeThreshold > 100)
                {
                    result.IsError = true;
                    result.Message = "Min uptime threshold must be between 0 and 100";
                    return result;
                }

                result.Result = true;
                result.Message = "Configuration is valid.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error validating configuration: {ex.Message}", ex);
            }

            return result;
        }
    }
}
