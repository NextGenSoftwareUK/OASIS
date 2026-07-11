using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Web8.Core.Enums;
using NextGenSoftware.OASIS.Web8.Core.Models;

namespace NextGenSoftware.OASIS.Web8.Core.Managers
{
    /// <summary>
    /// The fractal holonic mesh network at the heart of WEB8. Nodes register themselves (any externally-reachable
    /// system that can accept a relayed message at an HTTP endpoint), declare weighted links to other nodes, and
    /// the manager computes real shortest-path routes via Dijkstra's algorithm. Routing is self-healing: a node
    /// that hasn't been seen within the liveness window, or that fails to accept a relay during a send, is
    /// automatically excluded and the route recomputed around it - "no single point of failure" implemented as
    /// real graph algorithms, not narrative.
    /// </summary>
    public class GalacticMeshManager : OASISManager
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        /// <summary>A node not heard from within this window is treated as down for routing purposes.</summary>
        public TimeSpan LivenessWindow { get; set; } = TimeSpan.FromMinutes(5);

        public GalacticMeshManager(Guid avatarId, OASISDNA OASISDNA = null) : base(avatarId, OASISDNA) { }

        public GalacticMeshManager(IOASISStorageProvider OASISStorageProvider, Guid avatarId, OASISDNA OASISDNA = null)
            : base(OASISStorageProvider, avatarId, OASISDNA) { }

        public async Task<OASISResult<GalacticNode>> RegisterNodeAsync(GalacticNode node)
        {
            OASISResult<GalacticNode> result = new OASISResult<GalacticNode>();

            Holon holon = new Holon(HolonType.GalacticNode) { Name = node.Name, Description = $"WEB8 mesh node ({node.Type})." };
            WriteNodeToMetaData(holon, node, new Dictionary<Guid, double>());

            OASISResult<IHolon> saveResult = await Data.SaveHolonAsync(holon, AvatarId, false);

            if (saveResult.IsError || saveResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Error registering mesh node. Reason: {saveResult.Message}");
                return result;
            }

            node.Id = saveResult.Result.Id;
            result.Result = node;
            return result;
        }

        public async Task<OASISResult<List<GalacticNode>>> GetNodesAsync()
        {
            OASISResult<List<GalacticNode>> result = new OASISResult<List<GalacticNode>>();
            OASISResult<IEnumerable<IHolon>> loadResult = await Data.LoadAllHolonsAsync(HolonType.GalacticNode);

            if (loadResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading mesh nodes. Reason: {loadResult.Message}");
                return result;
            }

            result.Result = (loadResult.Result ?? Enumerable.Empty<IHolon>()).Select(h => ReadNodeFromMetaData(h).node).ToList();
            return result;
        }

        /// <summary>Declares a bidirectional weighted link between two nodes - an edge in the mesh graph.</summary>
        public async Task<OASISResult<bool>> AddLinkAsync(Guid nodeAId, Guid nodeBId, double latencyMs)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            bool okA = await AddAdjacencyAsync(nodeAId, nodeBId, latencyMs);
            bool okB = await AddAdjacencyAsync(nodeBId, nodeAId, latencyMs);

            if (!okA || !okB)
            {
                OASISErrorHandling.HandleError(ref result, "One or both nodes for the link were not found.");
                return result;
            }

            result.Result = true;
            return result;
        }

        /// <summary>Records a heartbeat for a node - keeps it inside the liveness window so routing continues to consider it healthy.</summary>
        public async Task<OASISResult<bool>> HeartbeatAsync(Guid nodeId)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IHolon> loadResult = await Data.LoadHolonAsync(nodeId, false);

            if (loadResult.IsError || loadResult.Result == null)
            {
                OASISErrorHandling.HandleError(ref result, $"Node {nodeId} not found.");
                return result;
            }

            (GalacticNode node, Dictionary<Guid, double> adjacency) = ReadNodeFromMetaData(loadResult.Result);
            node.LastSeenUtc = DateTime.UtcNow;
            WriteNodeToMetaData(loadResult.Result, node, adjacency);

            OASISResult<IHolon> saveResult = await Data.SaveHolonAsync(loadResult.Result, AvatarId, false);
            result.Result = !saveResult.IsError;
            return result;
        }

        /// <summary>
        /// Computes the shortest (lowest cumulative latency) path between two nodes via Dijkstra's algorithm,
        /// excluding any node outside the liveness window or explicitly passed in excludeNodeIds (self-healing).
        /// </summary>
        public async Task<OASISResult<List<Guid>>> ComputeRouteAsync(Guid sourceNodeId, Guid destinationNodeId, IEnumerable<Guid> excludeNodeIds = null)
        {
            OASISResult<List<Guid>> result = new OASISResult<List<Guid>>();
            (Dictionary<Guid, GalacticNode> nodes, Dictionary<Guid, Dictionary<Guid, double>> graph) = await BuildGraphAsync();

            HashSet<Guid> excluded = new HashSet<Guid>(excludeNodeIds ?? Enumerable.Empty<Guid>());
            DateTime livenessCutoff = DateTime.UtcNow - LivenessWindow;

            HashSet<Guid> healthyNodeIds = nodes.Values
                .Where(n => n.LastSeenUtc >= livenessCutoff && !excluded.Contains(n.Id))
                .Select(n => n.Id)
                .ToHashSet();

            if (!healthyNodeIds.Contains(sourceNodeId) || !healthyNodeIds.Contains(destinationNodeId))
            {
                OASISErrorHandling.HandleError(ref result, "Source or destination node is unhealthy/excluded/not registered.");
                return result;
            }

            List<Guid> path = Dijkstra(graph, healthyNodeIds, sourceNodeId, destinationNodeId);

            if (path == null)
            {
                OASISErrorHandling.HandleError(ref result, $"No route found from {sourceNodeId} to {destinationNodeId} across the currently healthy mesh.");
                return result;
            }

            result.Result = path;
            return result;
        }

        /// <summary>
        /// Routes and relays a message hop-by-hop to its destination via real HTTP forwarding. If a hop fails to
        /// accept the relay, that node is excluded and the route is recomputed from the last successful hop -
        /// self-healing failover in action, not merely described.
        /// </summary>
        public async Task<OASISResult<MeshRouteResult>> SendMessageAsync(MeshMessage message)
        {
            OASISResult<MeshRouteResult> result = new OASISResult<MeshRouteResult>();
            MeshRouteResult routeResult = new MeshRouteResult { MessageId = message.Id };

            HashSet<Guid> excluded = new HashSet<Guid>();
            Guid currentHop = message.SourceNodeId;
            int hopsTaken = 0;

            (Dictionary<Guid, GalacticNode> nodes, _) = await BuildGraphAsync();

            while (currentHop != message.DestinationNodeId && hopsTaken < message.Ttl)
            {
                OASISResult<List<Guid>> routeFromHere = await ComputeRouteAsync(currentHop, message.DestinationNodeId, excluded);

                if (routeFromHere.IsError || routeFromHere.Result == null || routeFromHere.Result.Count < 2)
                {
                    routeResult.RelayLog.Add($"No route available from node {currentHop} to {message.DestinationNodeId} - mesh may be partitioned.");
                    break;
                }

                Guid nextHop = routeFromHere.Result[1];

                if (!nodes.TryGetValue(nextHop, out GalacticNode nextNode) || string.IsNullOrEmpty(nextNode.EndpointUrl))
                {
                    excluded.Add(nextHop);
                    routeResult.RelayLog.Add($"Hop {nextHop} has no reachable endpoint - excluding and recomputing route.");
                    continue;
                }

                (bool relayed, double latencyMs, string error) = await TryRelayAsync(nextNode, message);

                if (!relayed)
                {
                    excluded.Add(nextHop);
                    routeResult.RelayLog.Add($"Relay to node '{nextNode.Name}' ({nextHop}) failed: {error}. Excluding and recomputing route (self-healing).");
                    continue;
                }

                routeResult.Path.Add(nextHop);
                routeResult.TotalLatencyMs += latencyMs;
                routeResult.RelayLog.Add($"Relayed to node '{nextNode.Name}' ({nextHop}) in {latencyMs:F1}ms.");
                currentHop = nextHop;
                hopsTaken++;
            }

            routeResult.Delivered = currentHop == message.DestinationNodeId;
            result.Result = routeResult;
            return result;
        }

        private async Task<(bool relayed, double latencyMs, string error)> TryRelayAsync(GalacticNode node, MeshMessage message)
        {
            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, node.EndpointUrl);
                httpRequest.Content = new StringContent(JsonSerializer.Serialize(message), Encoding.UTF8, "application/json");

                using HttpResponseMessage httpResponse = await _httpClient.SendAsync(httpRequest);
                sw.Stop();

                if (!httpResponse.IsSuccessStatusCode)
                    return (false, sw.Elapsed.TotalMilliseconds, $"HTTP {(int)httpResponse.StatusCode}");

                return (true, sw.Elapsed.TotalMilliseconds, null);
            }
            catch (Exception ex)
            {
                sw.Stop();
                return (false, sw.Elapsed.TotalMilliseconds, ex.Message);
            }
        }

        private async Task<bool> AddAdjacencyAsync(Guid nodeId, Guid neighbourId, double latencyMs)
        {
            OASISResult<IHolon> loadResult = await Data.LoadHolonAsync(nodeId, false);

            if (loadResult.IsError || loadResult.Result == null)
                return false;

            (GalacticNode node, Dictionary<Guid, double> adjacency) = ReadNodeFromMetaData(loadResult.Result);
            adjacency[neighbourId] = latencyMs;
            WriteNodeToMetaData(loadResult.Result, node, adjacency);

            OASISResult<IHolon> saveResult = await Data.SaveHolonAsync(loadResult.Result, AvatarId, false);
            return !saveResult.IsError;
        }

        private async Task<(Dictionary<Guid, GalacticNode> nodes, Dictionary<Guid, Dictionary<Guid, double>> graph)> BuildGraphAsync()
        {
            OASISResult<IEnumerable<IHolon>> loadResult = await Data.LoadAllHolonsAsync(HolonType.GalacticNode);
            Dictionary<Guid, GalacticNode> nodes = new Dictionary<Guid, GalacticNode>();
            Dictionary<Guid, Dictionary<Guid, double>> graph = new Dictionary<Guid, Dictionary<Guid, double>>();

            foreach (IHolon holon in loadResult.Result ?? Enumerable.Empty<IHolon>())
            {
                (GalacticNode node, Dictionary<Guid, double> adjacency) = ReadNodeFromMetaData(holon);
                nodes[node.Id] = node;
                graph[node.Id] = adjacency;
            }

            return (nodes, graph);
        }

        /// <summary>Standard Dijkstra shortest-path over the supplied weighted adjacency graph, restricted to the given set of healthy node ids.</summary>
        private static List<Guid> Dijkstra(Dictionary<Guid, Dictionary<Guid, double>> graph, HashSet<Guid> healthyNodeIds, Guid source, Guid destination)
        {
            Dictionary<Guid, double> distances = healthyNodeIds.ToDictionary(id => id, _ => double.PositiveInfinity);
            Dictionary<Guid, Guid> previous = new Dictionary<Guid, Guid>();
            HashSet<Guid> visited = new HashSet<Guid>();
            distances[source] = 0;

            while (visited.Count < healthyNodeIds.Count)
            {
                Guid current = distances
                    .Where(kv => !visited.Contains(kv.Key))
                    .OrderBy(kv => kv.Value)
                    .Select(kv => kv.Key)
                    .FirstOrDefault();

                if (current == default || double.IsPositiveInfinity(distances[current]))
                    break;

                if (current == destination)
                    break;

                visited.Add(current);

                if (!graph.TryGetValue(current, out Dictionary<Guid, double> neighbours))
                    continue;

                foreach (KeyValuePair<Guid, double> edge in neighbours)
                {
                    if (!healthyNodeIds.Contains(edge.Key) || visited.Contains(edge.Key))
                        continue;

                    double candidateDistance = distances[current] + edge.Value;

                    if (candidateDistance < distances.GetValueOrDefault(edge.Key, double.PositiveInfinity))
                    {
                        distances[edge.Key] = candidateDistance;
                        previous[edge.Key] = current;
                    }
                }
            }

            if (!distances.ContainsKey(destination) || double.IsPositiveInfinity(distances[destination]))
                return null;

            List<Guid> path = new List<Guid> { destination };
            Guid step = destination;

            while (previous.TryGetValue(step, out Guid prev))
            {
                path.Add(prev);
                step = prev;
            }

            path.Reverse();
            return path[0] == source ? path : null;
        }

        private static void WriteNodeToMetaData(IHolon holon, GalacticNode node, Dictionary<Guid, double> adjacency)
        {
            holon.MetaData["Name"] = node.Name;
            holon.MetaData["Type"] = node.Type.ToString();
            holon.MetaData["EndpointUrl"] = node.EndpointUrl;
            holon.MetaData["IsSovereign"] = node.IsSovereign;
            holon.MetaData["RegisteredUtc"] = node.RegisteredUtc.ToString("o");
            holon.MetaData["LastSeenUtc"] = node.LastSeenUtc.ToString("o");
            holon.MetaData["Adjacency"] = JsonSerializer.Serialize(adjacency.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value));
        }

        private static (GalacticNode node, Dictionary<Guid, double> adjacency) ReadNodeFromMetaData(IHolon holon)
        {
            GalacticNode node = new GalacticNode
            {
                Id = holon.Id,
                Name = holon.MetaData.TryGetValue("Name", out object n) ? n?.ToString() : holon.Name,
                EndpointUrl = holon.MetaData.TryGetValue("EndpointUrl", out object e) ? e?.ToString() : null,
                IsSovereign = holon.MetaData.TryGetValue("IsSovereign", out object s) && s != null && Convert.ToBoolean(s),
                RegisteredUtc = holon.MetaData.TryGetValue("RegisteredUtc", out object r) && r != null && DateTime.TryParse(r.ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime rd) ? rd : DateTime.UtcNow,
                LastSeenUtc = holon.MetaData.TryGetValue("LastSeenUtc", out object l) && l != null && DateTime.TryParse(l.ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime ld) ? ld : DateTime.UtcNow
            };

            if (holon.MetaData.TryGetValue("Type", out object t) && Enum.TryParse(t?.ToString(), true, out Enums.NodeType nodeType))
                node.Type = nodeType;

            Dictionary<Guid, double> adjacency = new Dictionary<Guid, double>();

            if (holon.MetaData.TryGetValue("Adjacency", out object adj) && adj != null)
            {
                Dictionary<string, double> raw = JsonSerializer.Deserialize<Dictionary<string, double>>(adj.ToString()) ?? new Dictionary<string, double>();
                foreach (KeyValuePair<string, double> kv in raw)
                    if (Guid.TryParse(kv.Key, out Guid neighbourId))
                        adjacency[neighbourId] = kv.Value;
            }

            return (node, adjacency);
        }
    }
}
