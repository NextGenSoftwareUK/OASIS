using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{    public partial class ONETRouting
    {
        public async Task<OASISResult<bool>> StartRoutingAsync()
        {
            var result = new OASISResult<bool>();
            
            try
            {
                _isRoutingActive = true;
                
                // Initialize routing algorithms
                await InitializeRoutingAlgorithmsAsync();
                
                // Start routing optimization loop
                _ = Task.Run(RoutingOptimizationLoopAsync);
                
                result.Result = true;
                result.IsError = false;
                result.Message = "ONET routing system started successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error starting routing: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<bool>> StopRoutingAsync()
        {
            var result = new OASISResult<bool>();
            
            try
            {
                _isRoutingActive = false;
                
                result.Result = true;
                result.IsError = false;
                result.Message = "ONET routing system stopped successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error stopping routing: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Add a node to the routing table
        /// </summary>
        public async Task<OASISResult<bool>> AddNodeAsync(ONETNode node)
        {
            var result = new OASISResult<bool>();
            
            try
            {
                lock (_routingLock)
                {
                    var routingNode = new RoutingNode
                    {
                        NodeId = node.Id,
                        Address = node.Address,
                        Capabilities = node.Capabilities,
                        Latency = node.Latency,
                        Reliability = node.Reliability,
                        LastSeen = DateTime.UtcNow,
                        IsActive = true
                    };

                    _routingTable[node.Id] = routingNode;
                    _nodeMetrics[node.Id] = new NetworkMetrics();

                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Node {node.Id} added to routing table";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding node to routing: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Remove a node from the routing table
        /// </summary>
        public async Task<OASISResult<bool>> RemoveNodeAsync(string nodeId)
        {
            var result = new OASISResult<bool>();
            
            try
            {
                lock (_routingLock)
                {
                    if (_routingTable.ContainsKey(nodeId))
                    {
                        _routingTable.Remove(nodeId);
                        _nodeMetrics.Remove(nodeId);
                        
                        // Clear cached paths involving this node
                        var keysToRemove = _pathCache.Keys.Where(k => k.Contains(nodeId)).ToList();
                        foreach (var key in keysToRemove)
                        {
                            _pathCache.Remove(key);
                        }

                        result.Result = true;
                        result.IsError = false;
                        result.Message = $"Node {nodeId} removed from routing table";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"Node {nodeId} not found in routing table");
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error removing node from routing: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Find optimal route to target node
        /// </summary>
        public async Task<OASISResult<List<string>>> FindOptimalRouteAsync(string targetNodeId, int priority = 1)
        {
            var result = new OASISResult<List<string>>();
            
            try
            {
                if (!_routingTable.ContainsKey(targetNodeId))
                {
                    OASISErrorHandling.HandleError(ref result, $"Target node {targetNodeId} not found in routing table");
                    return result;
                }

                var cacheKey = $"route_{targetNodeId}_{priority}";
                
                // Check cache first
                if (_pathCache.ContainsKey(cacheKey))
                {
                    var cachedPath = _pathCache[cacheKey].FirstOrDefault();
                    if (cachedPath != null && cachedPath.IsValid)
                    {
                        result.Result = cachedPath.Nodes;
                        result.IsError = false;
                        result.Message = "Route found in cache";
                        return result;
                    }
                }

                // Calculate optimal route based on algorithm
                List<string> route;
                switch (_algorithm)
                {
                    case RoutingAlgorithm.Dijkstra:
                        route = await CalculateDijkstraRouteAsync(targetNodeId, priority);
                        break;
                    case RoutingAlgorithm.AStar:
                        route = await CalculateAStarRouteAsync(targetNodeId, priority);
                        break;
                    case RoutingAlgorithm.Intelligent:
                        route = await CalculateIntelligentRouteAsync(targetNodeId, priority);
                        break;
                    default:
                        route = await CalculateShortestPathRouteAsync(targetNodeId);
                        break;
                }

                // Cache the route
                var routingPath = new RoutingPath
                {
                    Nodes = route,
                    CalculatedAt = DateTime.UtcNow,
                    IsValid = true,
                    Priority = priority
                };

                if (!_pathCache.ContainsKey(cacheKey))
                {
                    _pathCache[cacheKey] = new List<RoutingPath>();
                }
                _pathCache[cacheKey].Add(routingPath);

                result.Result = route;
                result.IsError = false;
                result.Message = "Optimal route calculated successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error finding optimal route: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Update node metrics for routing optimization
        /// </summary>
        public async Task<OASISResult<bool>> UpdateNodeMetricsAsync(string nodeId, double latency, int reliability, int throughput)
        {
            var result = new OASISResult<bool>();
            
            try
            {
                lock (_routingLock)
                {
                    if (_routingTable.ContainsKey(nodeId))
                    {
                        _routingTable[nodeId].Latency = latency;
                        _routingTable[nodeId].Reliability = reliability;
                        _routingTable[nodeId].LastSeen = DateTime.UtcNow;

                        if (_nodeMetrics.ContainsKey(nodeId))
                        {
                            _nodeMetrics[nodeId].Latency = latency;
                            _nodeMetrics[nodeId].Reliability = reliability;
                            _nodeMetrics[nodeId].Throughput = throughput;
                            _nodeMetrics[nodeId].LastUpdated = DateTime.UtcNow;
                        }

                        // Invalidate cached paths involving this node
                        var keysToInvalidate = _pathCache.Keys.Where(k => k.Contains(nodeId)).ToList();
                        foreach (var key in keysToInvalidate)
                        {
                            foreach (var path in _pathCache[key])
                            {
                                path.IsValid = false;
                            }
                        }

                        result.Result = true;
                        result.IsError = false;
                        result.Message = $"Metrics updated for node {nodeId}";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"Node {nodeId} not found in routing table");
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error updating node metrics: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Get routing statistics
        /// </summary>
        public async Task<OASISResult<RoutingStats>> GetRoutingStatsAsync()
        {
            var result = new OASISResult<RoutingStats>();
            
            try
            {
                var stats = new RoutingStats
                {
                    TotalNodes = _routingTable.Count,
                    ActiveNodes = _routingTable.Values.Count(n => n.IsActive),
                    CachedPaths = _pathCache.Values.Sum(paths => paths.Count),
                    AverageLatency = _routingTable.Values.Average(n => n.Latency),
                    AverageReliability = _routingTable.Values.Average(n => n.Reliability),
                    RoutingAlgorithm = _algorithm.ToString(),
                    LastOptimization = DateTime.UtcNow
                };

                result.Result = stats;
                result.IsError = false;
                result.Message = "Routing statistics retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting routing statistics: {ex.Message}", ex);
            }

            return result;
        }

    }
}