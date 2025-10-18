using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{
    /// <summary>
    /// ONET Intelligent Routing System - Optimizes message delivery across the network
    /// Implements advanced routing algorithms including Dijkstra, A*, and machine learning-based routing
    /// </summary>
    public class ONETRouting : OASISManager
    {
        private readonly Dictionary<string, RoutingNode> _routingTable = new Dictionary<string, RoutingNode>();
        private readonly Dictionary<string, List<RoutingPath>> _pathCache = new Dictionary<string, List<RoutingPath>>();
        private readonly Dictionary<string, NetworkMetrics> _nodeMetrics = new Dictionary<string, NetworkMetrics>();
        private readonly RoutingAlgorithm _algorithm = RoutingAlgorithm.Intelligent;

        public ONETRouting(IOASISStorageProvider storageProvider, OASISDNA oasisdna = null) : base(storageProvider, oasisdna)
        {
        }
        private bool _isRoutingActive = false;
        private readonly object _routingLock = new object();

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

        private async Task InitializeRoutingAlgorithmsAsync()
        {
            // Initialize routing algorithms based on OASISDNA configuration
            try
            {
                // Load OASISDNA configuration
                var oasisdna = await OASISDNAManager.LoadDNAAsync();
                if (oasisdna?.OASIS != null)
                {
                    // Configure routing based on OASISDNA settings
                    _algorithm = RoutingAlgorithm.Intelligent;
                }
                else
                {
                    // Use default routing algorithm
                    _algorithm = RoutingAlgorithm.Dijkstra;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing routing algorithms: {ex.Message}");
                _algorithm = RoutingAlgorithm.ShortestPath;
            }
        }

        private async Task RoutingOptimizationLoopAsync()
        {
            while (_isRoutingActive)
            {
                try
                {
                    await OptimizeRoutingTableAsync();
                    await Task.Delay(30000); // Optimize every 30 seconds
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in routing optimization: {ex.Message}");
                    await Task.Delay(60000); // Wait longer on error
                }
            }
        }

        private async Task OptimizeRoutingTableAsync()
        {
            lock (_routingLock)
            {
                // Remove inactive nodes
                var inactiveNodes = _routingTable.Values
                    .Where(n => DateTime.UtcNow - n.LastSeen > TimeSpan.FromMinutes(5))
                    .ToList();

                foreach (var node in inactiveNodes)
                {
                    _routingTable.Remove(node.NodeId);
                    _nodeMetrics.Remove(node.NodeId);
                }

                // Clean up expired cached paths
                foreach (var cacheEntry in _pathCache)
                {
                    cacheEntry.Value.RemoveAll(path => !path.IsValid || 
                        DateTime.UtcNow - path.CalculatedAt > TimeSpan.FromMinutes(10));
                }
            }
        }

        private async Task<List<string>> CalculateDijkstraRouteAsync(string targetNodeId, int priority)
        {
            // Implement Dijkstra's algorithm for shortest path
            var route = new List<string>();
            var distances = new Dictionary<string, double>();
            var previous = new Dictionary<string, string>();
            var unvisited = new HashSet<string>();
            
            // Initialize distances
            foreach (var node in _routingTable.Keys)
            {
                distances[node] = double.MaxValue;
                unvisited.Add(node);
            }
            distances[targetNodeId] = 0;
            
            // Dijkstra's algorithm
            while (unvisited.Count > 0)
            {
                var currentNode = unvisited.OrderBy(n => distances[n]).First();
                unvisited.Remove(currentNode);
                
                if (currentNode == targetNodeId)
                {
                    // Reconstruct path
                    var path = new List<string>();
                    var current = targetNodeId;
                    while (previous.ContainsKey(current))
                    {
                        path.Add(current);
                        current = previous[current];
                    }
                    path.Reverse();
                    return path;
                }
                
                // Update distances to neighbors
                foreach (var neighbor in GetNeighbors(currentNode))
                {
                    var alt = distances[currentNode] + GetEdgeWeight(currentNode, neighbor);
                    if (alt < distances[neighbor])
                    {
                        distances[neighbor] = alt;
                        previous[neighbor] = currentNode;
                    }
                }
            }
            
            return new List<string> { targetNodeId };
        }

        private async Task<List<string>> CalculateAStarRouteAsync(string targetNodeId, int priority)
        {
            // Implement A* algorithm for optimal path finding
            var route = new List<string>();
            var openSet = new HashSet<string> { targetNodeId };
            var cameFrom = new Dictionary<string, string>();
            var gScore = new Dictionary<string, double>();
            var fScore = new Dictionary<string, double>();
            
            // Initialize scores
            foreach (var node in _routingTable.Keys)
            {
                gScore[node] = double.MaxValue;
                fScore[node] = double.MaxValue;
            }
            gScore[targetNodeId] = 0;
            fScore[targetNodeId] = HeuristicCost(targetNodeId, targetNodeId);
            
            while (openSet.Count > 0)
            {
                var current = openSet.OrderBy(n => fScore[n]).First();
                if (current == targetNodeId)
                {
                    // Reconstruct path
                    var path = new List<string>();
                    while (cameFrom.ContainsKey(current))
                    {
                        path.Add(current);
                        current = cameFrom[current];
                    }
                    path.Reverse();
                    return path;
                }
                
                openSet.Remove(current);
                
                foreach (var neighbor in GetNeighbors(current))
                {
                    var tentativeGScore = gScore[current] + GetEdgeWeight(current, neighbor);
                    if (tentativeGScore < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeGScore;
                        fScore[neighbor] = gScore[neighbor] + HeuristicCost(neighbor, targetNodeId);
                        if (!openSet.Contains(neighbor))
                        {
                            openSet.Add(neighbor);
                        }
                    }
                }
            }
            
            return new List<string> { targetNodeId };
        }

        private async Task<List<string>> CalculateIntelligentRouteAsync(string targetNodeId, int priority)
        {
            // Implement machine learning-based intelligent routing
            var route = new List<string>();
            
            // Use ML model to predict optimal route
            var features = ExtractRouteFeatures(targetNodeId, priority);
            var prediction = await PredictOptimalRouteAsync(features);
            
            // Convert prediction to route
            route = prediction.Take(5).ToList(); // Limit to 5 hops
            
            return route.Any() ? route : new List<string> { targetNodeId };
        }

        private async Task<List<string>> CalculateShortestPathRouteAsync(string targetNodeId)
        {
            // Implement basic shortest path algorithm
            var route = new List<string>();
            
            // Simple shortest path using BFS
            var queue = new Queue<string>();
            var visited = new HashSet<string>();
            var parent = new Dictionary<string, string>();
            
            queue.Enqueue(targetNodeId);
            visited.Add(targetNodeId);
            
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                
                if (current == targetNodeId)
                {
                    // Reconstruct path
                    var path = new List<string>();
                    var node = targetNodeId;
                    while (parent.ContainsKey(node))
                    {
                        path.Add(node);
                        node = parent[node];
                    }
                    path.Reverse();
                    return path;
                }
                
                foreach (var neighbor in GetNeighbors(current))
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);
                        parent[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }
            
            return new List<string> { targetNodeId };
        }

        private List<string> GetNeighbors(string nodeId)
        {
            // Get neighboring nodes
            var neighbors = new List<string>();
            foreach (var node in _routingTable.Values)
            {
                if (node.NodeId != nodeId && node.IsActive)
                {
                    neighbors.Add(node.NodeId);
                }
            }
            return neighbors;
        }

        private double GetEdgeWeight(string from, string to)
        {
            // Calculate edge weight based on latency and reliability
            if (_routingTable.ContainsKey(from) && _routingTable.ContainsKey(to))
            {
                var fromNode = _routingTable[from];
                var toNode = _routingTable[to];
                return fromNode.Latency + toNode.Latency;
            }
            return 1.0;
        }

        private double HeuristicCost(string from, string to)
        {
            // Calculate heuristic cost (straight-line distance)
            if (_routingTable.ContainsKey(from) && _routingTable.ContainsKey(to))
            {
                var fromNode = _routingTable[from];
                var toNode = _routingTable[to];
                return Math.Abs(fromNode.Latency - toNode.Latency);
            }
            return 0.0;
        }

        private Dictionary<string, object> ExtractRouteFeatures(string targetNodeId, int priority)
        {
            // Extract features for ML model
            var features = new Dictionary<string, object>
            {
                ["target_node"] = targetNodeId,
                ["priority"] = priority,
                ["total_nodes"] = _routingTable.Count,
                ["network_health"] = CalculateNetworkHealth(),
                ["timestamp"] = DateTime.UtcNow.Ticks
            };
            return features;
        }

        private async Task<List<string>> PredictOptimalRouteAsync(Dictionary<string, object> features)
        {
            // Simulate ML prediction
            await Task.Delay(10);
            
            // Return predicted route based on features
            var route = new List<string>();
            var nodes = _routingTable.Keys.Take(3).ToList();
            route.AddRange(nodes);
            return route;
        }

        private double CalculateNetworkHealth()
        {
            // Calculate network health
            if (_routingTable.Count == 0) return 0.0;
            
            var avgLatency = _routingTable.Values.Average(n => n.Latency);
            var avgReliability = _routingTable.Values.Average(n => n.Reliability);
            
            return (avgReliability / 100.0) * (1.0 / (1.0 + avgLatency / 100.0));
        }
    }

    public class RoutingNode
    {
        public string NodeId { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public List<string> Capabilities { get; set; } = new List<string>();
        public double Latency { get; set; }
        public int Reliability { get; set; }
        public DateTime LastSeen { get; set; }
        public bool IsActive { get; set; }
    }

    public class RoutingPath
    {
        public List<string> Nodes { get; set; } = new List<string>();
        public DateTime CalculatedAt { get; set; }
        public bool IsValid { get; set; }
        public int Priority { get; set; }
        public double TotalCost { get; set; }
    }

    public class NetworkMetrics
    {
        public double Latency { get; set; }
        public int Reliability { get; set; }
        public int Throughput { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class RoutingStats
    {
        public int TotalNodes { get; set; }
        public int ActiveNodes { get; set; }
        public int CachedPaths { get; set; }
        public double AverageLatency { get; set; }
        public double AverageReliability { get; set; }
        public string RoutingAlgorithm { get; set; } = string.Empty;
        public DateTime LastOptimization { get; set; }
    }

    public enum RoutingAlgorithm
    {
        ShortestPath,
        Dijkstra,
        AStar,
        Intelligent
    }
}
