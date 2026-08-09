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
        private async Task<List<string>> CalculateAStarRouteAsync(string sourceNode, string destinationNode)
        {
            try
            {
                // A* algorithm implementation
                var openSet = new List<string> { sourceNode };
                var cameFrom = new Dictionary<string, string>();
                var gScore = new Dictionary<string, double> { { sourceNode, 0 } };
                var fScore = new Dictionary<string, double> { { sourceNode, await HeuristicAsync(sourceNode, destinationNode) } };
                
                while (openSet.Count > 0)
                {
                    var current = openSet.OrderBy(n => fScore.GetValueOrDefault(n, double.PositiveInfinity)).First();
                    
                    if (current == destinationNode)
                    {
                        return ReconstructPath(cameFrom, current);
                    }
                    
                    openSet.Remove(current);
                    var neighbors = await GetNodeNeighborsAsync(current);
                    
                    foreach (var neighbor in neighbors)
                    {
                        var tentativeGScore = gScore.GetValueOrDefault(current, double.PositiveInfinity) + 
                                            await GetEdgeWeightAsync(current, neighbor);
                        
                        if (tentativeGScore < gScore.GetValueOrDefault(neighbor, double.PositiveInfinity))
                        {
                            cameFrom[neighbor] = current;
                            gScore[neighbor] = tentativeGScore;
                            fScore[neighbor] = tentativeGScore + await HeuristicAsync(neighbor, destinationNode);
                            
                            if (!openSet.Contains(neighbor))
                            {
                                openSet.Add(neighbor);
                            }
                        }
                    }
                }
                
                // Return real alternative routes
                var routes = await GetAlternativeRoutesAsync(sourceNode, destinationNode);
                return routes;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error calculating A* route: {ex.Message}", ex);
                // Return real alternative routes
                var routes = await GetAlternativeRoutesAsync(sourceNode, destinationNode);
                return routes;
            }
        }

        private async Task<List<string>> CalculateBFSRouteAsync(string sourceNode, string destinationNode)
        {
            try
            {
                // Breadth-first search implementation
                var queue = new Queue<string>();
                var visited = new HashSet<string>();
                var parent = new Dictionary<string, string>();
                
                queue.Enqueue(sourceNode);
                visited.Add(sourceNode);
                
                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    
                    if (current == destinationNode)
                    {
                        return ReconstructPath(parent, current);
                    }
                    
                    var neighbors = await GetNodeNeighborsAsync(current);
                    foreach (var neighbor in neighbors)
                    {
                        if (!visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            parent[neighbor] = current;
                            queue.Enqueue(neighbor);
                        }
                    }
                }
                
                // Return real alternative routes
                var routes = await GetAlternativeRoutesAsync(sourceNode, destinationNode);
                return routes;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error calculating BFS route: {ex.Message}", ex);
                // Return real alternative routes
                var routes = await GetAlternativeRoutesAsync(sourceNode, destinationNode);
                return routes;
            }
        }

        // Additional helper methods
        private async Task<double> CalculateRouteQualityAsync(List<string> route)
        {
            if (route == null || route.Count < 2) return 0.0;
            
            var totalLatency = 0.0;
            var totalReliability = 1.0;
            
            for (int i = 0; i < route.Count - 1; i++)
            {
                totalLatency += await GetEdgeLatencyAsync(route[i], route[i + 1]);
                totalReliability *= await GetNodeReliabilityAsync(route[i + 1]);
            }
            
            // Quality score (0.0 to 1.0)
            var latencyScore = Math.Max(0, 1.0 - (totalLatency / 1000.0)); // Penalty for high latency
            var reliabilityScore = totalReliability;
            
            return (latencyScore + reliabilityScore) / 2.0;
        }

        private async Task<double> HeuristicAsync(string from, string to)
        {
            // Simple heuristic based on node distance
            return await GetEdgeLatencyAsync(from, to);
        }

        private List<string> ReconstructPath(Dictionary<string, string> cameFrom, string current)
        {
            var path = new List<string> { current };
            
            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Insert(0, current);
            }
            
            return path;
        }

        private bool IsSameRoute(List<string> route1, List<string> route2)
        {
            if (route1.Count != route2.Count) return false;
            
            for (int i = 0; i < route1.Count; i++)
            {
                if (route1[i] != route2[i]) return false;
            }
            
            return true;
        }

        // REAL helper methods used by the routing algorithms
        private async Task<List<string>> GetNetworkTopologyAsync()
        {
            // Use the live routing table as the source of truth for topology
            return _routingTable.Keys.ToList();
        }

        private async Task<List<string>> GetNodeNeighborsAsync(string nodeId)
        {
            // Neighbors are other active nodes with sufficient reliability
            return _routingTable.Values
                .Where(n => n.NodeId != nodeId && n.IsActive && n.Reliability >= 60)
                .Select(n => n.NodeId)
                .ToList();
        }

        private async Task<double> GetEdgeWeightAsync(string fromNode, string toNode)
        {
            // Edge weight is combination of latency and unreliability penalty
            var latency = await GetEdgeLatencyAsync(fromNode, toNode);
            var reliability = await GetNodeReliabilityAsync(toNode); // 0.0 - 1.0
            var penalty = (1.0 - reliability) * 500.0; // up to +500ms equivalent penalty
            return Math.Max(1.0, latency + penalty);
        }

        private async Task<bool> TestNodeConnectivityAsync(string nodeId)
        {
            if (!_routingTable.TryGetValue(nodeId, out var node) || string.IsNullOrWhiteSpace(node.Address))
                return false;

            var parts = node.Address.Split(':');
            var host = parts[0];
            var port = (parts.Length > 1 && int.TryParse(parts[1], out var p)) ? p : 8080;

            try
            {
                using (var client = new System.Net.Sockets.TcpClient())
                {
                    var connectTask = client.ConnectAsync(host, port);
                    var completed = await Task.WhenAny(connectTask, Task.Delay(1000));
                    if (completed != connectTask || !client.Connected)
                        return false;

                    var stream = client.GetStream();
                    string pingLine = "ONET_PING\n";
                    if (BuildAuthenticatedPing != null)
                    {
                        var (authNodeId, authSig) = await BuildAuthenticatedPing();
                        if (authNodeId != null && authSig != null)
                            pingLine = $"ONET_PING {authNodeId} {authSig}\n";
                    }
                    var ping = System.Text.Encoding.UTF8.GetBytes(pingLine);
                    await stream.WriteAsync(ping, 0, ping.Length);

                    var buffer = new byte[256];
                    var readTask = stream.ReadAsync(buffer, 0, buffer.Length);
                    completed = await Task.WhenAny(readTask, Task.Delay(1000));
                    if (completed != readTask)
                        return false;
                    var read = readTask.Result;
                    var response = System.Text.Encoding.UTF8.GetString(buffer, 0, read);
                    return response.IndexOf("ONET_PONG", StringComparison.OrdinalIgnoreCase) >= 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task<double> GetEdgeLatencyAsync(string fromNode, string toNode)
        {
            // Measure RTT via connectivity test stopwatch; fallback to stored latency
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var ok = await TestNodeConnectivityAsync(toNode);
            sw.Stop();
            if (ok)
                return Math.Max(1.0, sw.Elapsed.TotalMilliseconds);

            if (_routingTable.TryGetValue(toNode, out var to))
                return Math.Max(1.0, to.Latency);

            return 1000.0;
        }

        private async Task<double> GetNodeReliabilityAsync(string nodeId)
        {
            if (!_routingTable.TryGetValue(nodeId, out var node))
                return 0.0;

            var baseReliability = Math.Clamp(node.Reliability / 100.0, 0.0, 1.0);
            var ageSeconds = (DateTime.UtcNow - node.LastSeen).TotalSeconds;
            var freshness = ageSeconds <= 60 ? 1.0 : Math.Max(0.2, 1.0 - (ageSeconds - 60) / 300.0);
            return Math.Clamp(baseReliability * freshness, 0.0, 1.0);
        }

        // Overload to satisfy call sites without primary route parameter
        private async Task<List<string>> CalculateAlternativeRouteAsync(string sourceNode, string destinationNode)
        {
            var primary = await CalculateShortestPathAsync(sourceNode, destinationNode);
            return await CalculateAlternativeRouteAsync(sourceNode, destinationNode, primary);
        }

        // Real routing optimization work instead of Task.Delay
    }
}