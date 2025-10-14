using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.Logging;

namespace NextGenSoftware.OASIS.API.Core.Managers
{

    /// <summary>
    /// Geographic information for routing decisions
    /// </summary>
    public class GeographicInfo
    {
        public string Region { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string TimeZone { get; set; }
        public double DistanceKm { get; set; }
        public int NetworkHops { get; set; }
        public double NetworkLatency { get; set; }
    }

    /// <summary>
    /// Cost analysis for provider selection
    /// </summary>
    public class CostAnalysis
    {
        public ProviderType ProviderType { get; set; }
        public double StorageCostPerGB { get; set; }
        public double ComputeCostPerHour { get; set; }
        public double NetworkCostPerGB { get; set; }
        public double TransactionCost { get; set; }
        public double ApiCallCost { get; set; }
        public double TotalCost { get; set; }
        public string Currency { get; set; }
        public DateTime LastUpdated { get; set; }
        public double CostEfficiencyScore { get; set; }
    }

    /// <summary>
    /// Provider switch tracking for analytics
    /// </summary>
    public class ProviderSwitch
    {
        public ProviderType FromProvider { get; set; }
        public ProviderType ToProvider { get; set; }
        public DateTime Timestamp { get; set; }
        public string Reason { get; set; }
        public double PerformanceGain { get; set; }
    }

    /// <summary>
    /// Performance monitoring and metrics collection
    /// </summary>
    public class PerformanceMonitor
    {
        private static PerformanceMonitor _instance;
        private readonly Dictionary<ProviderType, ProviderPerformanceMetrics> _metrics;
        private readonly Dictionary<ProviderType, GeographicInfo> _geographicInfo;
        private readonly Dictionary<ProviderType, CostAnalysis> _costAnalysis;
        private readonly Dictionary<ProviderType, int> _activeConnections;
        private readonly Dictionary<ProviderType, Queue<DateTime>> _requestHistory;
        private readonly List<ProviderSwitch> _providerSwitches;
        private readonly AIOptimizationEngine _aiEngine;
        private readonly object _lockObject = new object();

        public static PerformanceMonitor Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new PerformanceMonitor();
                return _instance;
            }
        }

        private PerformanceMonitor()
        {
            _metrics = new Dictionary<ProviderType, ProviderPerformanceMetrics>();
            _geographicInfo = new Dictionary<ProviderType, GeographicInfo>();
            _costAnalysis = new Dictionary<ProviderType, CostAnalysis>();
            _activeConnections = new Dictionary<ProviderType, int>();
            _requestHistory = new Dictionary<ProviderType, Queue<DateTime>>();
            _providerSwitches = new List<ProviderSwitch>();
            _aiEngine = new AIOptimizationEngine();
        }

        /// <summary>
        /// Records a request to a provider for performance tracking
        /// </summary>
        public void RecordRequest(ProviderType providerType, bool success, double responseTimeMs, double cost = 0)
        {
            lock (_lockObject)
            {
                if (!_metrics.ContainsKey(providerType))
                {
                    _metrics[providerType] = new ProviderPerformanceMetrics
                    {
                        ProviderType = providerType,
                        LastUpdated = DateTime.UtcNow
                    };
                }

                var metrics = _metrics[providerType];
                metrics.TotalRequests++;
                metrics.LastUpdated = DateTime.UtcNow;

                if (success)
                {
                    metrics.SuccessfulRequests++;
                    metrics.ResponseTimeMs = CalculateMovingAverage(metrics.ResponseTimeMs, responseTimeMs, metrics.SuccessfulRequests);
                    metrics.AverageLatency = metrics.ResponseTimeMs;
                    
                    if (responseTimeMs > metrics.PeakLatency)
                        metrics.PeakLatency = responseTimeMs;
                    
                    if (metrics.MinLatency == 0 || responseTimeMs < metrics.MinLatency)
                        metrics.MinLatency = responseTimeMs;
                }
                else
                {
                    metrics.FailedRequests++;
                }

                metrics.ErrorRate = (double)metrics.FailedRequests / metrics.TotalRequests;
                metrics.UptimePercentage = (double)metrics.SuccessfulRequests / metrics.TotalRequests * 100;
                metrics.CostPerOperation = cost;
                metrics.OverallScore = CalculateOverallScore(metrics);
            }
        }

        /// <summary>
        /// Records connection activity for least connections load balancing
        /// </summary>
        public void RecordConnection(ProviderType providerType, bool isConnecting)
        {
            lock (_lockObject)
            {
                if (!_activeConnections.ContainsKey(providerType))
                    _activeConnections[providerType] = 0;

                if (isConnecting)
                    _activeConnections[providerType]++;
                else
                    _activeConnections[providerType] = Math.Max(0, _activeConnections[providerType] - 1);

                if (_metrics.ContainsKey(providerType))
                    _metrics[providerType].ActiveConnections = _activeConnections[providerType];
            }
        }

        /// <summary>
        /// Updates geographic information for a provider
        /// </summary>
        public void UpdateGeographicInfo(ProviderType providerType, GeographicInfo geoInfo)
        {
            lock (_lockObject)
            {
                _geographicInfo[providerType] = geoInfo;
                
                if (_metrics.ContainsKey(providerType))
                {
                    _metrics[providerType].GeographicRegion = geoInfo.Region;
                    _metrics[providerType].NetworkLatency = geoInfo.NetworkLatency;
                    _metrics[providerType].GeographicScore = CalculateGeographicScore(geoInfo);
                    _metrics[providerType].OverallScore = CalculateOverallScore(_metrics[providerType]);
                }
            }
        }

        /// <summary>
        /// Updates cost analysis for a provider
        /// </summary>
        public void UpdateCostAnalysis(ProviderType providerType, CostAnalysis costAnalysis)
        {
            lock (_lockObject)
            {
                _costAnalysis[providerType] = costAnalysis;
                
                if (_metrics.ContainsKey(providerType))
                {
                    _metrics[providerType].CostPerOperation = costAnalysis.TotalCost;
                    _metrics[providerType].CostScore = costAnalysis.CostEfficiencyScore;
                    _metrics[providerType].OverallScore = CalculateOverallScore(_metrics[providerType]);
                }
            }
        }

        /// <summary>
        /// Gets current performance metrics for a provider
        /// </summary>
        public ProviderPerformanceMetrics GetMetrics(ProviderType providerType)
        {
            lock (_lockObject)
            {
                return _metrics.ContainsKey(providerType) ? _metrics[providerType] : null;
            }
        }

        /// <summary>
        /// Gets provider metrics for AI optimization
        /// </summary>
        public ProviderPerformanceMetrics GetProviderMetrics(ProviderType providerType)
        {
            return GetMetrics(providerType);
        }

        /// <summary>
        /// Gets all performance metrics
        /// </summary>
        public Dictionary<ProviderType, ProviderPerformanceMetrics> GetAllMetrics()
        {
            lock (_lockObject)
            {
                return new Dictionary<ProviderType, ProviderPerformanceMetrics>(_metrics);
            }
        }

        /// <summary>
        /// Gets active connection count for a provider
        /// </summary>
        public int GetActiveConnections(ProviderType providerType)
        {
            lock (_lockObject)
            {
                return _activeConnections.ContainsKey(providerType) ? _activeConnections[providerType] : 0;
            }
        }

        /// <summary>
        /// Gets geographic information for a provider
        /// </summary>
        public GeographicInfo GetGeographicInfo(ProviderType providerType)
        {
            lock (_lockObject)
            {
                return _geographicInfo.ContainsKey(providerType) ? _geographicInfo[providerType] : null;
            }
        }

        /// <summary>
        /// Gets cost analysis for a provider
        /// </summary>
        public CostAnalysis GetCostAnalysis(ProviderType providerType)
        {
            lock (_lockObject)
            {
                return _costAnalysis.ContainsKey(providerType) ? _costAnalysis[providerType] : null;
            }
        }

        /// <summary>
        /// Calculates weighted score for provider selection
        /// </summary>
        public double CalculateWeightedScore(ProviderType providerType, double performanceWeight = 0.4, double costWeight = 0.3, double geographicWeight = 0.2, double availabilityWeight = 0.1)
        {
            lock (_lockObject)
            {
                if (!_metrics.ContainsKey(providerType))
                    return 0;

                var metrics = _metrics[providerType];
                var performanceScore = CalculatePerformanceScore(metrics);
                var costScore = CalculateCostScore(metrics);
                var geographicScore = CalculateGeographicScore(_geographicInfo.ContainsKey(providerType) ? _geographicInfo[providerType] : null);
                var availabilityScore = CalculateAvailabilityScore(metrics);

                return (performanceScore * performanceWeight) + 
                       (costScore * costWeight) + 
                       (geographicScore * geographicWeight) + 
                       (availabilityScore * availabilityWeight);
            }
        }

        /// <summary>
        /// Gets the best performing provider based on multiple criteria
        /// </summary>
        public ProviderType GetBestProvider(List<ProviderType> availableProviders, LoadBalancingStrategy strategy = LoadBalancingStrategy.Auto)
        {
            if (availableProviders == null || !availableProviders.Any())
                return ProviderType.Default;

            lock (_lockObject)
            {
                var providerScores = new Dictionary<ProviderType, double>();

                foreach (var provider in availableProviders)
                {
                    double score = strategy switch
                    {
                        LoadBalancingStrategy.Performance => CalculatePerformanceScore(_metrics.ContainsKey(provider) ? _metrics[provider] : null),
                        LoadBalancingStrategy.CostBased => CalculateCostScore(_metrics.ContainsKey(provider) ? _metrics[provider] : null),
                        LoadBalancingStrategy.Geographic => CalculateGeographicScore(_geographicInfo.ContainsKey(provider) ? _geographicInfo[provider] : null),
                        LoadBalancingStrategy.LeastConnections => CalculateLeastConnectionsScore(provider),
                        _ => CalculateWeightedScore(provider)
                    };

                    providerScores[provider] = score;
                }

                return providerScores.OrderByDescending(x => x.Value).First().Key;
            }
        }

        private double CalculateMovingAverage(double current, double newValue, int count)
        {
            return (current * (count - 1) + newValue) / count;
        }

        private double CalculateOverallScore(ProviderPerformanceMetrics metrics)
        {
            if (metrics == null) return 0;

            var performanceScore = CalculatePerformanceScore(metrics);
            var costScore = CalculateCostScore(metrics);
            var availabilityScore = CalculateAvailabilityScore(metrics);

            return (performanceScore * 0.4) + (costScore * 0.3) + (availabilityScore * 0.3);
        }

        private double CalculatePerformanceScore(ProviderPerformanceMetrics metrics)
        {
            if (metrics == null) return 0;

            // Normalize response time (lower is better)
            var responseTimeScore = Math.Max(0, 100 - (metrics.ResponseTimeMs / 10));
            
            // Normalize throughput (higher is better)
            var throughputScore = Math.Min(100, metrics.ThroughputMbps * 10);
            
            // Normalize error rate (lower is better)
            var errorRateScore = Math.Max(0, 100 - (metrics.ErrorRate * 100));

            return (responseTimeScore * 0.4) + (throughputScore * 0.3) + (errorRateScore * 0.3);
        }

        private double CalculateCostScore(ProviderPerformanceMetrics metrics)
        {
            if (metrics == null) return 0;

            // Lower cost is better, so invert the score
            var costScore = Math.Max(0, 100 - (metrics.CostPerOperation * 100));
            return costScore;
        }

        private double CalculateGeographicScore(GeographicInfo geoInfo)
        {
            if (geoInfo == null) return 50; // Default neutral score

            // Lower latency is better
            var latencyScore = Math.Max(0, 100 - (geoInfo.NetworkLatency * 10));
            
            // Fewer hops is better
            var hopsScore = Math.Max(0, 100 - (geoInfo.NetworkHops * 5));

            return (latencyScore * 0.7) + (hopsScore * 0.3);
        }

        private double CalculateAvailabilityScore(ProviderPerformanceMetrics metrics)
        {
            if (metrics == null) return 0;

            return metrics.UptimePercentage;
        }

        private double CalculateLeastConnectionsScore(ProviderType providerType)
        {
            var connections = GetActiveConnections(providerType);
            // Lower connections is better
            return Math.Max(0, 100 - connections);
        }

        /// <summary>
        /// Resets all metrics for a provider
        /// </summary>
        public void ResetMetrics(ProviderType providerType)
        {
            lock (_lockObject)
            {
                if (_metrics.ContainsKey(providerType))
                {
                    _metrics[providerType] = new ProviderPerformanceMetrics
                    {
                        ProviderType = providerType,
                        LastUpdated = DateTime.UtcNow
                    };
                }

                if (_activeConnections.ContainsKey(providerType))
                    _activeConnections[providerType] = 0;
            }
        }

        /// <summary>
        /// Resets all metrics for all providers
        /// </summary>
        public void ResetAllMetrics()
        {
            lock (_lockObject)
            {
                _metrics.Clear();
                _activeConnections.Clear();
                _geographicInfo.Clear();
                _costAnalysis.Clear();
                _requestHistory.Clear();
            }
        }

        /// <summary>
        /// Records a provider switch asynchronously
        /// </summary>
        public async Task RecordProviderSwitchAsync(EnumValue<ProviderType> fromProvider, EnumValue<ProviderType> toProvider)
        {
            try
            {
                var switchRecord = new ProviderSwitch
                {
                    FromProvider = fromProvider.Value,
                    ToProvider = toProvider.Value,
                    Timestamp = DateTime.UtcNow,
                    Reason = "Performance Optimization"
                };

                lock (_lockObject)
                {
                    _providerSwitches.Add(switchRecord);

                    // Keep only recent switches (last 24 hours)
                    var cutoffTime = DateTime.UtcNow.AddHours(-24);
                    _providerSwitches.RemoveAll(s => s.Timestamp < cutoffTime);
                }

                // Log the switch for monitoring
                LoggingManager.Logger.Log($"Provider switched from {fromProvider.Name} to {toProvider.Name} at {switchRecord.Timestamp}", LogType.Info);
            }
            catch (Exception ex)
            {
                LoggingManager.Logger.Log($"Error recording provider switch: {ex.Message}", LogType.Error);
            }
        }

        /// <summary>
        /// Updates metrics asynchronously
        /// </summary>
        public async Task UpdateMetricsAsync(EnumValue<ProviderType> providerType, OASISResult<object> result, long responseTimeMs)
        {
            try
            {
                // Update performance metrics
                RecordRequest(providerType.Value, !result.IsError, responseTimeMs);

                // Log performance data for AI learning
                await _aiEngine.RecordPerformanceDataAsync(providerType.Value, new StorageOperationRequest(), result, responseTimeMs);
            }
            catch (Exception ex)
            {
                LoggingManager.Logger.Log($"Error updating metrics for {providerType.Name}: {ex.Message}", LogType.Error);
            }
        }
    }
}
