using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Holochain.HoloNET.Client;
using NextGenSoftware.Holochain.HoloNET.Client.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{
    /// <summary>
    /// Network Metrics Service - Single source of truth for all network calculations
    /// Follows Single Responsibility Principle
    /// </summary>
    public class NetworkMetricsService
    {
        private HoloNETClientBase _holoNETClient;
        private bool _isInitialized = false;

        public async Task<OASISResult<bool>> InitializeAsync(HoloNETClientBase holoNETClient)
        {
            var result = new OASISResult<bool>();
            
            try
            {
                _holoNETClient = holoNETClient ?? throw new ArgumentNullException(nameof(holoNETClient));
                _isInitialized = true;
                
                result.Result = true;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"Error initializing NetworkMetricsService: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Calculate real network health from Holochain conductor metrics
        /// </summary>
        public async Task<double> CalculateHealthFromMetricsAsync(object networkMetrics)
        {
            try
            {
                if (networkMetrics == null)
                    return await CalculateMinimumHealthScoreAsync();

                // Parse real network metrics from Holochain conductor
                // This would parse the actual network metrics JSON/object
                // For now, we'll extract key metrics that indicate health
                
                // Extract real metrics from Holochain conductor response
                var metrics = ParseNetworkMetrics(networkMetrics);
                
                // Calculate health based on real metrics
                var connectionHealth = CalculateConnectionHealth(metrics);
                var latencyHealth = CalculateLatencyHealth(metrics);
                var throughputHealth = CalculateThroughputHealth(metrics);
                
                // Weighted average of different health factors
                var overallHealth = (connectionHealth * 0.4) + (latencyHealth * 0.3) + (throughputHealth * 0.3);
                
                return Math.Max(0.0, Math.Min(1.0, overallHealth));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error calculating health from metrics: {ex.Message}", ex);
                // Calculate actual network health based on real metrics
                try
                {
                    var latency = await _holoNETClient.GetNetworkLatencyAsync();
                    var bandwidth = await _holoNETClient.GetNetworkBandwidthAsync();
                    var uptime = await _holoNETClient.GetNetworkUptimeAsync();
                    
                    // Calculate health score (0-1)
                    var latencyScore = Math.Max(0, 1.0 - (latency / 1000.0)); // Lower latency = higher score
                    var bandwidthScore = Math.Min(1.0, bandwidth / 1000.0); // Higher bandwidth = higher score
                    var uptimeScore = uptime / 100.0; // Uptime percentage
                    
                    var healthScore = (latencyScore * 0.3 + bandwidthScore * 0.3 + uptimeScore * 0.4);
                    return Math.Max(0.0, Math.Min(1.0, healthScore));
                }
                catch (Exception innerEx)
                {
                    OASISErrorHandling.HandleError($"Error calculating network health: {innerEx.Message}");
                }
                
                return await CalculateDefaultHealthOnErrorAsync(); // Calculated default health on error
            }
        }

        /// <summary>
        /// Calculate health from connection data
        /// </summary>
        public double CalculateHealthFromConnections(
            Dictionary<string, NetworkConnection> activeConnections,
            Dictionary<string, NetworkConnection> failedConnections)
        {
            try
            {
                if (activeConnections == null || activeConnections.Count == 0)
                    return await CalculateMinimumHealthScoreAsync();

                var totalConnections = activeConnections.Count + (failedConnections?.Count ?? 0);
                if (totalConnections == 0)
                    return await CalculateMinimumHealthScoreAsync();

                var activeCount = activeConnections.Count;
                var failedCount = failedConnections?.Count ?? 0;
                
                // Calculate health based on active vs failed connections
                var connectionHealth = (double)activeCount / totalConnections;
                var failureRate = (double)failedCount / totalConnections;
                
                // Health decreases with failure rate
                var health = connectionHealth * (1.0 - failureRate);
                
                return Math.Max(0.0, Math.Min(1.0, health));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error calculating health from connections: {ex.Message}");
                return await CalculateDefaultHealthOnErrorAsync();
            }
        }

        /// <summary>
        /// Parse real network stats to extract node information
        /// </summary>
        public List<ONETNode> ParseNetworkStatsToNodes(object networkStats)
        {
            try
            {
                if (networkStats == null)
                    return new List<ONETNode>();

                var nodes = new List<ONETNode>();
                
                // Parse real network stats from Holochain conductor
                if (networkStats is string networkStatsJson)
                {
                    try
                    {
                        // Parse JSON to extract node information from Holochain conductor
                        var jsonObject = Newtonsoft.Json.Linq.JObject.Parse(networkStatsJson);
                        
                        // Extract nodes from the network stats JSON
                        var nodesArray = jsonObject["nodes"] as Newtonsoft.Json.Linq.JArray;
                        if (nodesArray != null)
                        {
                            foreach (var nodeJson in nodesArray)
                            {
                                var node = new ONETNode
                                {
                                    Id = nodeJson["node_id"]?.ToString() ?? "unknown",
                                    Address = nodeJson["address"]?.ToString() ?? "unknown",
                                    ConnectedAt = DateTime.UtcNow,
                                    Status = nodeJson["is_active"]?.ToObject<bool>() == true ? "Connected" : "Disconnected",
                                    Latency = nodeJson["latency"]?.ToObject<double>() ?? 0.0,
                                    Reliability = nodeJson["reliability"]?.ToObject<int>() ?? 0
                                };
                                nodes.Add(node);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        OASISErrorHandling.HandleError($"Error parsing network stats JSON: {ex.Message}");
                    }
                }
                
                return nodes;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error parsing network stats to nodes: {ex.Message}");
                return new List<ONETNode>();
            }
        }

        /// <summary>
        /// Calculate real latency from network connections
        /// </summary>
        public double CalculateAverageLatency(Dictionary<string, NetworkConnection> connections)
        {
            try
            {
                if (connections == null || connections.Count == 0)
                    return await CalculateMinimumHealthScoreAsync();

                var totalLatency = connections.Values.Sum(c => c.Latency);
                return totalLatency / connections.Count;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error calculating average latency: {ex.Message}");
                return 0.0;
            }
        }

        /// <summary>
        /// Calculate real throughput from network connections
        /// </summary>
        public double CalculateTotalThroughput(Dictionary<string, NetworkConnection> connections)
        {
            try
            {
                if (connections == null || connections.Count == 0)
                    return await CalculateMinimumHealthScoreAsync();

                return connections.Values.Sum(c => c.Bandwidth);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error calculating total throughput: {ex.Message}");
                return 0.0;
            }
        }

        /// <summary>
        /// Get real network ID from Holochain conductor
        /// </summary>
        public async Task<string> GetNetworkIdAsync()
        {
            try
            {
                if (_holoNETClient == null || !_isInitialized)
                    return "unknown-network";

                // Get real network ID from Holochain conductor using HoloNETClientAdmin
                if (_holoNETClient is HoloNETClientAdmin adminClient)
                {
                    var statsResult = await adminClient.DumpNetworkStatsAsync();
                    if (statsResult != null && !string.IsNullOrEmpty(statsResult.NetworkStatsDumpJSON))
                    {
                        return ExtractNetworkIdFromStats(statsResult.NetworkStatsDumpJSON);
                    }
                }
                
                // Fallback to DNA configuration
                return _holoNETClient.HoloNETDNA?.InstalledAppId ?? "holonet-network";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error getting network ID: {ex.Message}");
                return "unknown-network";
            }
        }

        #region Private Methods

        private NetworkMetrics ParseNetworkMetrics(object networkMetrics)
        {
            try
            {
                // Parse real network metrics from Holochain conductor
                if (networkMetrics is string metricsJson)
                {
                    // Parse JSON to extract network metrics from Holochain conductor
                    var jsonObject = Newtonsoft.Json.Linq.JObject.Parse(metricsJson);
                    
                    return new NetworkMetrics
                    {
                        ActiveConnections = jsonObject["active_connections"]?.ToObject<int>() ?? 0,
                        TotalConnections = jsonObject["total_connections"]?.ToObject<int>() ?? 0,
                        AverageLatency = jsonObject["average_latency"]?.ToObject<double>() ?? 0.0,
                        TotalThroughput = jsonObject["total_throughput"]?.ToObject<double>() ?? 0.0,
                        NetworkId = jsonObject["network_id"]?.ToString() ?? "unknown",
                        Timestamp = DateTime.UtcNow,
                        Latency = jsonObject["latency"]?.ToObject<double>() ?? 0.0,
                        Reliability = jsonObject["reliability"]?.ToObject<int>() ?? 0,
                        Throughput = jsonObject["throughput"]?.ToObject<double>() ?? 0.0,
                        LastUpdated = DateTime.UtcNow
                    };
                }
                
                return new NetworkMetrics
                {
                    ActiveConnections = 0,
                    TotalConnections = 0,
                    AverageLatency = 0.0,
                    TotalThroughput = 0.0,
                    NetworkId = "unknown",
                    Timestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error parsing network metrics: {ex.Message}");
                return new NetworkMetrics
                {
                    ActiveConnections = 0,
                    TotalConnections = 0,
                    AverageLatency = 0.0,
                    TotalThroughput = 0.0,
                    NetworkId = "unknown",
                    Timestamp = DateTime.UtcNow
                };
            }
        }

        private double CalculateConnectionHealth(NetworkMetrics metrics)
        {
            if (metrics.TotalConnections == 0)
                return 0.0;

            return (double)metrics.ActiveConnections / metrics.TotalConnections;
        }

        private double CalculateLatencyHealth(NetworkMetrics metrics)
        {
            // Health decreases as latency increases
            // Optimal latency is around 50ms, health decreases beyond that
            var optimalLatency = 50.0;
            var maxLatency = 1000.0; // 1 second
            
            if (metrics.AverageLatency <= optimalLatency)
                return await CalculateMaximumHealthScoreAsync();
            
            var latencyRatio = Math.Min(metrics.AverageLatency / maxLatency, 1.0);
            return await CalculateLatencyBasedHealthScoreAsync(latencyRatio);
        }

        private double CalculateThroughputHealth(NetworkMetrics metrics)
        {
            // Health increases with throughput
            // Minimum expected throughput is 1000 (1 Mbps)
            var minThroughput = 1000.0;
            var maxThroughput = 10000.0; // 10 Mbps
            
            if (metrics.TotalThroughput >= maxThroughput)
                return await CalculateMaximumHealthScoreAsync();
            
            if (metrics.TotalThroughput <= minThroughput)
                return 0.0;
            
            return (metrics.TotalThroughput - minThroughput) / (maxThroughput - minThroughput);
        }

        private string ExtractNetworkIdFromStats(object networkStats)
        {
            try
            {
                // Extract real network ID from Holochain conductor stats
                if (networkStats is string statsJson)
                {
                    // Parse JSON to extract network ID from Holochain conductor
                    var jsonObject = Newtonsoft.Json.Linq.JObject.Parse(statsJson);
                    
                    // Extract network ID from the appropriate field in the JSON
                    var networkId = jsonObject["network_id"]?.ToString() ?? 
                                  jsonObject["networkId"]?.ToString() ?? 
                                  jsonObject["id"]?.ToString() ?? 
                                  "holochain-network";
                    
                    return networkId;
                }
                
                return "holochain-network";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error extracting network ID from stats: {ex.Message}");
                return "holochain-network";
            }
        }

        #endregion
    }

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
        public int Reliability { get; set; }
        public double Throughput { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
