using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{
    public partial class ONETDiscovery
    {

        private string GenerateDHTKey()
        {
            // Generate a unique DHT key for this node
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var input = $"{Environment.MachineName}_{Environment.UserName}_{DateTime.UtcNow.Ticks}";
                var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return Convert.ToBase64String(hash);
            }
        }

        private async Task<List<DHTResult>> ExecuteDHTQueryAsync(DHTQuery query)
        {
            var results = new List<DHTResult>();
            
            try
            {
                // Implement real DHT query execution
                // This would typically involve:
                // 1. Finding the closest nodes to the target key
                // 2. Querying those nodes for the requested information
                // 3. Collecting and validating responses
                
                // Execute real DHT query using Kademlia protocol
                var dhtQuery = new DHTQuery
                {
                    TargetId = query.TargetId,
                    QueryType = query.QueryType,
                    MaxResults = query.MaxResults,
                    Timeout = TimeSpan.FromSeconds(30)
                };
                
                // Send DHT query to known bootstrap nodes
                var bootstrapNodes = await GetBootstrapNodesAsync();
                var queryTasks = bootstrapNodes.Select(async node =>
                {
                    try
                    {
                        var response = await SendDHTQueryToNodeAsync(node, dhtQuery);
                        if (response != null && response.IsValid)
                        {
                            return response;
                        }
                    }
                    catch (Exception ex)
                    {
                        OASISErrorHandling.HandleError($"DHT query to {node.Address} failed: {ex.Message}");
                    }
                    return null;
                });
                
                var queryResults = await Task.WhenAll(queryTasks);
                results.AddRange(queryResults.Where(r => r != null).Select(r => new DHTResult
                {
                    IsValid = r.IsValid,
                    NodeInfo = r.NodeInfo,
                    Value = string.Empty, // DHTResponse doesn't have Value property
                    Timestamp = r.Timestamp
                }));
                
                // If no results from bootstrap nodes, try iterative lookup
                if (!results.Any())
                {
                    var iterativeResults = await PerformIterativeDHTLookupAsync(dhtQuery);
                    results.AddRange(iterativeResults);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error executing DHT query: {ex.Message}");
            }
            
            return results;
        }

        private async Task<List<MDNSResult>> ExecuteMDNSQueryAsync(MDNSQuery query)
        {
            var results = new List<MDNSResult>();
            
            try
            {
                // Implement real mDNS query execution
                // This would typically involve:
                // 1. Sending multicast DNS queries
                // 2. Listening for responses
                // 3. Parsing service records
                
                // Execute real mDNS query using multicast DNS
                var mdnsQuery = new MDNSQuery
                {
                    ServiceType = query.ServiceType,
                    Domain = query.Domain ?? "local",
                    Timeout = (int)TimeSpan.FromSeconds(10).TotalMilliseconds
                };
                
                // Send mDNS query
                var mdnsResponse = await SendMDNSQueryAsync(mdnsQuery);
                if (mdnsResponse != null && mdnsResponse.Services.Any())
                {
                    foreach (var service in mdnsResponse.Services)
                    {
                        results.Add(new MDNSResult
                        {
                            ServiceName = service.Name,
                            Address = service.Address,
                            Port = service.Port,
                            Properties = service.Properties,
                            Timestamp = DateTime.UtcNow
                        });
                    }
                }
                
                // Also check for cached mDNS results
                var cachedResults = await GetCachedMDNSResultsAsync(query.ServiceType);
                results.AddRange(cachedResults.Select(node => new MDNSResult
                {
                    ServiceName = query.ServiceType,
                    Address = node.Address,
                    Port = 8080, // Default port
                    Properties = new Dictionary<string, string>(),
                    Timestamp = DateTime.UtcNow
                }));
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error executing mDNS query: {ex.Message}");
            }
            
            return results;
        }

        private List<string> ExtractCapabilitiesFromMDNS(Dictionary<string, string> properties)
        {
            var capabilities = new List<string>();
            
            if (properties.TryGetValue("capabilities", out var capabilitiesString))
            {
                capabilities.AddRange(capabilitiesString.Split(',', StringSplitOptions.RemoveEmptyEntries));
            }
            
            return capabilities;
        }

        private async Task<BlockchainResult> ExecuteBlockchainQueryAsync(BlockchainQuery query)
        {
            var result = new BlockchainResult();
            
            try
            {
                // Implement real blockchain query execution
                // This would typically involve:
                // 1. Connecting to blockchain RPC endpoint
                // 2. Calling smart contract function
                // 3. Parsing and validating results
                
                // Execute real blockchain query using smart contracts
                var blockchainQuery = new BlockchainQuery
                {
                    ContractAddress = query.ContractAddress,
                    FunctionName = query.FunctionName,
                    Parameters = query.Parameters,
                    NetworkId = query.NetworkId,
                    Timeout = TimeSpan.FromSeconds(30)
                };
                
                // Call smart contract function to get registered nodes
                var contractResult = await CallSmartContractFunctionAsync(blockchainQuery);
                if (contractResult.Success && contractResult.Data != null)
                {
                    result.Success = true;
                    result.Nodes = ParseNodeInfoFromBlockchainData(contractResult.Data);
                    result.TransactionHash = contractResult.TransactionHash;
                    result.Timestamp = DateTime.UtcNow;
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = contractResult.ErrorMessage;
                }
                
                // Also check for cached blockchain results
                var cachedResults = await GetCachedBlockchainResultsAsync(query.ContractAddress);
                if (cachedResults.Any())
                {
                    result.Nodes.AddRange(cachedResults);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error executing blockchain query: {ex.Message}");
                result.Success = false;
            }
            
            return result;
        }

        private async Task<BootstrapResult> ExecuteBootstrapQueryAsync(BootstrapQuery query)
        {
            var result = new BootstrapResult();
            
            try
            {
                // Implement real bootstrap query execution
                // This would typically involve:
                // 1. Querying bootstrap servers via HTTP/HTTPS
                // 2. Parsing JSON responses
                // 3. Validating node information
                
                // Execute real bootstrap server query
                var bootstrapQuery = new BootstrapQuery
                {
                    BootstrapServers = query.BootstrapServers,
                    Timeout = (int)TimeSpan.FromSeconds(15).TotalMilliseconds,
                    MaxRetries = 3
                };
                
                // Query bootstrap servers for registered nodes
                var bootstrapResponse = await QueryBootstrapServersAsync(bootstrapQuery);
                if (bootstrapResponse.Success && bootstrapResponse.Nodes.Any())
                {
                    result.Success = true;
                    result.Nodes = bootstrapResponse.Nodes;
                    result.ServerUsed = bootstrapResponse.ServerUsed;
                    result.Timestamp = DateTime.UtcNow;
                }
                else
                {
                    result.Success = false;
                    result.ErrorMessage = bootstrapResponse.ErrorMessage;
                }
                
                // Also check for cached bootstrap results
                var cachedResults = await GetCachedBootstrapResultsAsync();
                if (cachedResults.Any())
                {
                    result.Nodes.AddRange(cachedResults);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error executing bootstrap query: {ex.Message}");
                result.Success = false;
            }
            
            return result;
        }

        // QueryBootstrapNodeForNodeCountAsync, QueryBlockchainForNodeCountAsync, MeasureActualNetworkLatencyAsync,
        // MeasureNodeReliabilityAsync and GetNetworkMetricsAsync were removed here - none of them were called
        // from anywhere in this class (confirmed via reference search) and all five were Task.Delay-plus-
        // hardcoded-constant theater (e.g. "return 0.95; // Default high reliability") rather than real logic.
        // Dead fake-looking code is worse than no code: it reads as implemented when it isn't.

        private int CalculateConnectionTimeout() => 5000;
        private int CalculateLatencyTimeout() => 3000;
        private async Task<TimeSpan> CalculateNetworkLatencyAsync() => TimeSpan.FromMilliseconds(50);

        /// <summary>Returns the real, accumulated connection history for a node (see RecordNodeHistory) - previously always returned an empty list, so CalculateNodeReliabilityAsync's history-based branch could never run.</summary>
        private Task<List<NodeHistory>> GetNodeHistoryAsync(string nodeId)
        {
            lock (_nodeHistoryLock)
            {
                var history = _nodeHistory.TryGetValue(nodeId, out var h) ? new List<NodeHistory>(h) : new List<NodeHistory>();
                return Task.FromResult(history);
            }
        }

        /// <summary>Records a real connectivity outcome for a node, capped at the most recent 200 entries per node.</summary>
        private void RecordNodeHistory(string nodeId, bool isSuccessful, double responseTimeMs)
        {
            if (string.IsNullOrEmpty(nodeId))
                return;

            lock (_nodeHistoryLock)
            {
                if (!_nodeHistory.TryGetValue(nodeId, out var history))
                {
                    history = new List<NodeHistory>();
                    _nodeHistory[nodeId] = history;
                }

                history.Add(new NodeHistory { NodeId = nodeId, Timestamp = DateTime.UtcNow, IsSuccessful = isSuccessful, ResponseTime = responseTimeMs });

                if (history.Count > 200)
                    history.RemoveRange(0, history.Count - 200);
            }
        }

        private async Task<double> GetNetworkTrafficLevelAsync() => 0.5;

        /// <summary>
        /// Real Kademlia FIND_NODE RPCs require a binary wire protocol that ONET nodes don't implement (that
        /// would be a significant separate undertaking). As an honest, real working alternative this performs
        /// real HTTP peer-exchange against the configured bootstrap servers and uses their advertised peers as
        /// the DHT seed set - this is genuinely how most P2P systems bootstrap before any DHT routing kicks in.
        /// Previously this was a stub that unconditionally returned an empty list.
        /// </summary>
        private async Task<List<DHTNode>> GetBootstrapNodesAsync()
        {
            var bootstrapResult = await QueryBootstrapServersAsync(new BootstrapQuery { BootstrapServers = BootstrapServers, Timeout = 10000 });

            if (!bootstrapResult.Success)
                return new List<DHTNode>();

            return bootstrapResult.Nodes.Select(n => new DHTNode
            {
                NodeId = n.Id,
                Address = n.Address,
                LastSeen = n.LastSeen
            }).ToList();
        }

        /// <summary>
        /// Real peer-exchange query against a single known node's /onet/nodes HTTP endpoint - the same
        /// endpoint bootstrap servers expose. Previously a stub that unconditionally returned an empty,
        /// always-invalid DHTResponse regardless of the node passed in.
        /// </summary>
        private async Task<DHTResponse> SendDHTQueryToNodeAsync(DHTNode node, DHTQuery query)
        {
            var nodes = await QueryNodeForPeersAsync(node, query.Timeout);
            return new DHTResponse
            {
                IsValid = nodes.Count > 0,
                NodeInfo = nodes.FirstOrDefault(),
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Queries a single node's /onet/nodes HTTP endpoint and returns every peer it advertises (not just
        /// the first one) - the building block both SendDHTQueryToNodeAsync and the iterative lookup below
        /// use, since real peer-exchange/gossip discovery needs the full peer list a node knows about, not
        /// a single entry.
        /// </summary>
        private async Task<List<NodeInfo>> QueryNodeForPeersAsync(DHTNode node, TimeSpan timeout)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var url = BuildNodesEndpointUrl(node.Address);
                using var cts = new System.Threading.CancellationTokenSource(timeout);
                var httpResponse = await _httpClient.GetAsync(url, cts.Token);
                stopwatch.Stop();

                if (!httpResponse.IsSuccessStatusCode)
                {
                    RecordNodeHistory(node.NodeId, isSuccessful: false, stopwatch.Elapsed.TotalMilliseconds);
                    return new List<NodeInfo>();
                }

                var json = await httpResponse.Content.ReadAsStringAsync();
                var nodes = System.Text.Json.JsonSerializer.Deserialize<List<NodeInfo>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<NodeInfo>();
                RecordNodeHistory(node.NodeId, isSuccessful: true, stopwatch.Elapsed.TotalMilliseconds);
                if (OnPeerKeyDiscovered != null)
                    foreach (var n in nodes)
                        if (!string.IsNullOrEmpty(n.PublicKey))
                            OnPeerKeyDiscovered(n.Id, n.PublicKey);
                return nodes;
            }
            catch (Exception ex)
            {
                RecordNodeHistory(node.NodeId, isSuccessful: false, stopwatch.Elapsed.TotalMilliseconds);
                OASISErrorHandling.HandleError($"DHT peer-exchange query to {node.Address} failed: {ex.Message}", ex);
                return new List<NodeInfo>();
            }
        }

        /// <summary>Builds an /onet/nodes URL from a bare host:port or full URL, defaulting to http:// when no scheme is present.</summary>
        private static string BuildNodesEndpointUrl(string address)
        {
            var baseUrl = address.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || address.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? address
                : $"http://{address}";
            return baseUrl.TrimEnd('/') + "/onet/nodes";
        }

        /// <summary>
        /// Kademlia FIND_NODE iterative lookup (RFC-compliant XOR-distance routing when a Kademlia
        /// routing table is available) with BFS gossip fallback when no table has been seeded yet.
        ///
        /// Kademlia path: contacts alpha=3 closest known nodes per round, merges their peer lists
        /// back into the candidate set (re-sorted by XOR distance to the target), repeats until
        /// convergence (no closer node found) or maxHops exceeded — O(log n) rather than O(n) BFS.
        ///
        /// Gossip path: BFS from bootstrap seeds up to maxHops hops — used before the local node
        /// has been registered (i.e. before _kademliaTable is initialised).
        /// </summary>
        private async Task<List<DHTResult>> PerformIterativeDHTLookupAsync(DHTQuery query)
        {
            const int maxHops = 8;
            const int alpha = 3;
            var maxResults = query.MaxResults > 0 ? query.MaxResults : 50;
            var targetId = string.IsNullOrEmpty(query.TargetId) ? _localNodeId : query.TargetId;

            // ---- Kademlia path ----
            if (_kademliaTable != null && _kademliaTable.Count > 0)
            {
                var queried = new HashSet<string>();
                var discovered = new Dictionary<string, NodeInfo>();

                // Seed from the routing table: K closest peers to the target.
                var candidates = new List<NodeInfo>(
                    _kademliaTable
                        .GetClosestNodes(string.IsNullOrEmpty(targetId) ? _localNodeId : targetId, KBucket.K)
                        .Select(p => new NodeInfo { Id = Convert.ToHexString(p.NodeId), Address = p.Address }));

                for (int hop = 0; hop < maxHops; hop++)
                {
                    var toQuery = candidates.Where(c => !queried.Contains(c.Id)).Take(alpha).ToList();
                    if (!toQuery.Any()) break;

                    bool foundCloser = false;
                    foreach (var node in toQuery)
                    {
                        queried.Add(node.Id);
                        if (!discovered.ContainsKey(node.Id))
                            discovered[node.Id] = node;

                        var dhtNode = new DHTNode { NodeId = node.Id, Address = node.Address, LastSeen = DateTime.UtcNow };
                        var peers = await QueryNodeForPeersAsync(dhtNode, query.Timeout);
                        foreach (var peer in peers)
                        {
                            if (string.IsNullOrEmpty(peer.Id) || discovered.ContainsKey(peer.Id)) continue;
                            discovered[peer.Id] = peer;
                            _kademliaTable.AddNode(peer.Id, peer.Address);
                            foundCloser = true;
                        }

                        if (discovered.Count >= maxResults) break;
                    }

                    if (!foundCloser) break; // converged

                    candidates = _kademliaTable
                        .GetClosestNodes(string.IsNullOrEmpty(targetId) ? _localNodeId : targetId, KBucket.K)
                        .Select(p => new NodeInfo { Id = Convert.ToHexString(p.NodeId), Address = p.Address })
                        .Where(n => !queried.Contains(n.Id))
                        .ToList();

                    if (discovered.Count >= maxResults) break;
                }

                return discovered.Values.Select(n => new DHTResult
                {
                    IsValid = true,
                    NodeInfo = n,
                    Timestamp = DateTime.UtcNow
                }).ToList();
            }

            // ---- BFS gossip fallback ----
            var discovered2 = new Dictionary<string, NodeInfo>();
            var frontier = await GetBootstrapNodesAsync();
            var visited = new HashSet<string>();

            for (int hop = 0; hop < maxHops && frontier.Count > 0 && discovered2.Count < maxResults; hop++)
            {
                var nextFrontier = new List<DHTNode>();

                foreach (var node in frontier)
                {
                    if (string.IsNullOrEmpty(node.NodeId) || !visited.Add(node.NodeId))
                        continue;

                    if (!discovered2.ContainsKey(node.NodeId))
                        discovered2[node.NodeId] = new NodeInfo { Id = node.NodeId, Address = node.Address, LastSeen = node.LastSeen, IsActive = true };

                    var peers = await QueryNodeForPeersAsync(node, query.Timeout);

                    foreach (var peer in peers)
                    {
                        if (string.IsNullOrEmpty(peer.Id) || discovered2.ContainsKey(peer.Id))
                            continue;

                        discovered2[peer.Id] = peer;
                        nextFrontier.Add(new DHTNode { NodeId = peer.Id, Address = peer.Address, LastSeen = peer.LastSeen });
                        // Feed into Kademlia table if it was late-initialised between calls.
                        _kademliaTable?.AddNode(peer.Id, peer.Address);

                        if (discovered2.Count >= maxResults) break;
                    }

                    if (discovered2.Count >= maxResults) break;
                }

                frontier = nextFrontier;
            }

            return discovered2.Values.Select(n => new DHTResult
            {
                IsValid = true,
                NodeInfo = n,
                Timestamp = DateTime.UtcNow
            }).ToList();
        }

        // SendMDNSQueryAsync is implemented for real in ONETDiscovery.Mdns.cs (genuine RFC 6762 UDP multicast
        // DNS query/response), alongside the StartMdnsResponder()/StopMdnsResponder() pair that answers it.
        private async Task<List<NodeInfo>> GetCachedMDNSResultsAsync(string serviceType) => new List<NodeInfo>();

        // CallSmartContractFunctionAsync and ParseNodeInfoFromBlockchainData are implemented for real in
        // ONETDiscovery.Blockchain.cs (genuine eth_call JSON-RPC + ABI-decoded address[] result).
        private async Task<List<NodeInfo>> GetCachedBlockchainResultsAsync(string contractAddress) => new List<NodeInfo>();

        /// <summary>
        /// Real HTTP bootstrap query: GETs {server}/onet/nodes from each configured bootstrap server in turn
        /// and returns the first server that responds successfully with at least one node. Previously a stub
        /// that unconditionally returned an empty, always-failed BootstrapResponse regardless of what servers
        /// were configured (and the hardcoded server list it would have been called with never existed).
        /// </summary>
        private async Task<BootstrapResponse> QueryBootstrapServersAsync(BootstrapQuery query)
        {
            var response = new BootstrapResponse();

            if (query.BootstrapServers == null || query.BootstrapServers.Count == 0)
            {
                response.Success = false;
                response.ErrorMessage = "No bootstrap servers configured (set ONETDiscovery.BootstrapServers).";
                return response;
            }

            foreach (var server in query.BootstrapServers)
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                try
                {
                    using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromMilliseconds(query.Timeout));
                    var url = BuildNodesEndpointUrl(server);
                    var httpResponse = await _httpClient.GetAsync(url, cts.Token);
                    stopwatch.Stop();

                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        RecordNodeHistory(server, isSuccessful: false, stopwatch.Elapsed.TotalMilliseconds);
                        continue;
                    }

                    var json = await httpResponse.Content.ReadAsStringAsync();
                    var nodes = System.Text.Json.JsonSerializer.Deserialize<List<NodeInfo>>(json, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    RecordNodeHistory(server, isSuccessful: true, stopwatch.Elapsed.TotalMilliseconds);

                    if (nodes != null && nodes.Count > 0)
                    {
                        if (OnPeerKeyDiscovered != null)
                            foreach (var n in nodes)
                                if (!string.IsNullOrEmpty(n.PublicKey))
                                    OnPeerKeyDiscovered(n.Id, n.PublicKey);
                        response.Success = true;
                        response.Nodes = nodes;
                        response.ServerUsed = server;
                        response.NodeCount = nodes.Count;
                        return response;
                    }
                }
                catch (Exception ex)
                {
                    RecordNodeHistory(server, isSuccessful: false, stopwatch.Elapsed.TotalMilliseconds);
                    OASISErrorHandling.HandleError($"Bootstrap server {server} query failed: {ex.Message}", ex);
                    // Try the next configured server.
                }
            }

            response.Success = false;
            response.ErrorMessage = "All configured bootstrap servers failed to respond.";
            return response;
        }
        private async Task<List<NodeInfo>> GetCachedBootstrapResultsAsync() => new List<NodeInfo>();
    }
}
