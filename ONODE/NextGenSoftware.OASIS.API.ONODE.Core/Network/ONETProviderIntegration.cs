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
    /// ONET Provider Integration - Integrates all Web2 and Web3 providers with ONET network
    /// Creates a unified provider ecosystem that works seamlessly across all OASIS providers
    /// </summary>
    public class ONETProviderIntegration : OASISManager
    {
        private readonly ONETProtocol _onetProtocol;
        private readonly Dictionary<ProviderType, ProviderBridge> _providerBridges = new Dictionary<ProviderType, ProviderBridge>();
        private readonly Dictionary<string, ProviderNode> _providerNodes = new Dictionary<string, ProviderNode>();
        private readonly Dictionary<ProviderCategory, List<ProviderType>> _providerCategories = new Dictionary<ProviderCategory, List<ProviderType>>();

        /// <summary>
        /// Real per-provider call history (latency + success/failure), recorded by RouteThroughProviderAsync
        /// on every real call. CalculateProviderLatencyAsync/CalculateProviderReliabilityAsync read from this
        /// instead of a hardcoded per-ProviderType switch statement - the only genuinely "calculated" signal
        /// available here, since these are logical provider-category bridges, not independently network-
        /// addressable ONET nodes with their own real connection to measure.
        /// </summary>
        private readonly Dictionary<ProviderType, List<(double latencyMs, bool success)>> _providerCallHistory = new();
        private readonly object _providerCallHistoryLock = new object();

        public ONETProviderIntegration(IOASISStorageProvider storageProvider, OASISDNA oasisdna = null) : base(storageProvider, oasisdna)
        {
            _onetProtocol = ONETProtocol.GetInstance(storageProvider, oasisdna);
            InitializeProviderBridges();
            InitializeProviderCategories();
        }
        private bool _isIntegrated = false;

        /// <summary>
        /// Initialize ONET-Provider integration
        /// </summary>
        public async Task<OASISResult<bool>> InitializeIntegrationAsync()
        {
            var result = new OASISResult<bool>();
            
            try
            {
                // Initialize ONET Protocol
                await _onetProtocol.StartNetworkAsync();
                
                // Initialize all provider bridges
                await InitializeAllProviderBridgesAsync();
                
                // Register providers with ONET network
                await RegisterProvidersWithONETAsync();
                
                // Create provider routing tables
                await CreateProviderRoutingTablesAsync();
                
                _isIntegrated = true;
                
                result.Result = true;
                result.IsError = false;
                result.Message = "ONET-Provider integration initialized successfully - All Web2/Web3 providers unified!";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error initializing ONET-Provider integration: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Route request through optimal provider via ONET network
        /// </summary>
        public async Task<OASISResult<T>> RouteThroughProviderAsync<T>(
            ProviderType providerType,
            IRequest request,
            ProviderCategory category = ProviderCategory.Storage)
        {
            var result = new OASISResult<T>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                if (!_isIntegrated)
                {
                    OASISErrorHandling.HandleError(ref result, "ONET-Provider integration not initialized");
                    return result;
                }

                // Find optimal provider node
                var providerNode = await FindOptimalProviderNodeAsync(providerType, category);
                if (providerNode == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"No optimal provider node found for {providerType}");
                    return result;
                }

                // Create provider request message
                var providerMessage = new ONETMessage
                {
                    Content = CreateProviderRequest(providerType, request),
                    MessageType = "PROVIDER_REQUEST",
                    SourceNodeId = "local",
                    TargetNodeId = providerNode.NodeId
                };

                // Send through ONET network
                var onetResult = await _onetProtocol.SendMessageAsync(providerMessage);
                if (onetResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"ONET provider routing failed: {onetResult.Message}");
                    return result;
                }

                // Process provider response
                var providerResponse = await ProcessProviderResponseAsync<T>(onetResult.Result, providerType);
                if (providerResponse.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Provider response processing failed: {providerResponse.Message}");
                    return result;
                }

                result.Result = providerResponse.Result;
                result.IsError = false;
                result.Message = $"Request routed successfully through {providerType} provider via ONET";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error routing through provider: {ex.Message}", ex);
            }
            finally
            {
                stopwatch.Stop();
                RecordProviderCall(providerType, stopwatch.Elapsed.TotalMilliseconds, !result.IsError);
            }

            return result;
        }

        /// <summary>
        /// Records a real call outcome for a provider, capped at the most recent 200 entries, and refreshes
        /// that provider's ProviderNode.Latency/Reliability so FindOptimalProviderNodeAsync's ordering
        /// reflects real recent performance - previously these fields were set once at routing-table
        /// creation time and never updated again no matter how the provider actually performed afterward.
        /// </summary>
        private void RecordProviderCall(ProviderType providerType, double latencyMs, bool success)
        {
            List<(double latencyMs, bool success)> historySnapshot;

            lock (_providerCallHistoryLock)
            {
                if (!_providerCallHistory.TryGetValue(providerType, out var history))
                {
                    history = new List<(double latencyMs, bool success)>();
                    _providerCallHistory[providerType] = history;
                }

                history.Add((latencyMs, success));
                if (history.Count > 200)
                    history.RemoveRange(0, history.Count - 200);

                historySnapshot = new List<(double latencyMs, bool success)>(history);
            }

            var nodeId = $"{providerType}_node";
            if (_providerNodes.TryGetValue(nodeId, out var node))
            {
                node.Latency = historySnapshot.Average(h => h.latencyMs);
                node.Reliability = (int)Math.Round(100.0 * historySnapshot.Count(h => h.success) / historySnapshot.Count);
            }
        }

        /// <summary>
        /// Get all available providers through ONET
        /// </summary>
        public async Task<OASISResult<List<ProviderInfo>>> GetAvailableProvidersAsync()
        {
            var result = new OASISResult<List<ProviderInfo>>();
            
            try
            {
                var providers = new List<ProviderInfo>();
                
                // Get blockchain providers
                var blockchainProviders = await GetBlockchainProvidersAsync();
                providers.AddRange(blockchainProviders);
                
                // Get cloud providers
                var cloudProviders = await GetCloudProvidersAsync();
                providers.AddRange(cloudProviders);
                
                // Get storage providers
                var storageProviders = await GetStorageProvidersAsync();
                providers.AddRange(storageProviders);
                
                // Get network providers
                var networkProviders = await GetNetworkProvidersAsync();
                providers.AddRange(networkProviders);
                
                result.Result = providers;
                result.IsError = false;
                result.Message = $"Found {providers.Count} available providers through ONET";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting available providers: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Get provider statistics through ONET
        /// </summary>
        public async Task<OASISResult<ProviderStats>> GetProviderStatsAsync()
        {
            var result = new OASISResult<ProviderStats>();
            
            try
            {
                var stats = new ProviderStats
                {
                    TotalProviders = _providerBridges.Count,
                    ActiveProviders = _providerBridges.Values.Count(p => p.IsActive),
                    // GetValueOrDefault rather than the indexer - previously a KeyNotFoundException waiting
                    // to happen if any of these four ProviderCategory keys were ever not seeded.
                    BlockchainProviders = _providerCategories.GetValueOrDefault(ProviderCategory.Blockchain)?.Count ?? 0,
                    CloudProviders = _providerCategories.GetValueOrDefault(ProviderCategory.Cloud)?.Count ?? 0,
                    StorageProviders = _providerCategories.GetValueOrDefault(ProviderCategory.Storage)?.Count ?? 0,
                    NetworkProviders = _providerCategories.GetValueOrDefault(ProviderCategory.Network)?.Count ?? 0,
                    TotalNodes = _providerNodes.Count,
                    NetworkUptime = await GetNetworkUptimeAsync(),
                    LastUpdated = DateTime.UtcNow
                };

                result.Result = stats;
                result.IsError = false;
                result.Message = "Provider statistics retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting provider stats: {ex.Message}", ex);
            }

            return result;
        }

        private void InitializeProviderBridges()
        {
            // Initialize blockchain provider bridges
            _providerBridges[ProviderType.EthereumOASIS] = new ProviderBridge
            {
                ProviderType = ProviderType.EthereumOASIS,
                Category = ProviderCategory.Blockchain,
                Name = "Ethereum",
                Description = "Ethereum blockchain integration",
                Capabilities = new List<string> { "Smart Contracts", "DeFi", "NFTs", "DApps" },
                IsActive = true
            };

            _providerBridges[ProviderType.SolanaOASIS] = new ProviderBridge
            {
                ProviderType = ProviderType.SolanaOASIS,
                Category = ProviderCategory.Blockchain,
                Name = "Solana",
                Description = "Solana blockchain integration",
                Capabilities = new List<string> { "High Performance", "Low Fees", "NFTs", "DeFi" },
                IsActive = true
            };

            _providerBridges[ProviderType.BitcoinOASIS] = new ProviderBridge
            {
                ProviderType = ProviderType.BitcoinOASIS,
                Category = ProviderCategory.Blockchain,
                Name = "Bitcoin",
                Description = "Bitcoin blockchain integration",
                Capabilities = new List<string> { "Digital Gold", "Store of Value", "Payments" },
                IsActive = true
            };

            _providerBridges[ProviderType.PolygonOASIS] = new ProviderBridge
            {
                ProviderType = ProviderType.PolygonOASIS,
                Category = ProviderCategory.Blockchain,
                Name = "Polygon",
                Description = "Polygon blockchain integration",
                Capabilities = new List<string> { "Layer 2", "Low Fees", "Ethereum Compatible" },
                IsActive = true
            };

            _providerBridges[ProviderType.CardanoOASIS] = new ProviderBridge
            {
                ProviderType = ProviderType.CardanoOASIS,
                Category = ProviderCategory.Blockchain,
                Name = "Cardano",
                Description = "Cardano blockchain integration",
                Capabilities = new List<string> { "Academic Research", "Sustainability", "Smart Contracts" },
                IsActive = true
            };

            // Initialize cloud provider bridges
            _providerBridges[ProviderType.AWSOASIS] = new ProviderBridge
            {
                ProviderType = ProviderType.AWSOASIS,
                Category = ProviderCategory.Cloud,
                Name = "AWS",
                Description = "Amazon Web Services integration",
                Capabilities = new List<string> { "Cloud Computing", "Storage", "Database", "AI/ML" },
                IsActive = true
            };

            _providerBridges[ProviderType.AzureStorageOASIS] = new ProviderBridge
            {
                ProviderType = ProviderType.AzureStorageOASIS,
                Category = ProviderCategory.Cloud,
                Name = "Azure",
                Description = "Microsoft Azure integration",
                Capabilities = new List<string> { "Cloud Computing", "Enterprise", "AI/ML", "DevOps" },
                IsActive = true
            };

            _providerBridges[ProviderType.GoogleCloudOASIS] = new ProviderBridge
            {
                ProviderType = ProviderType.GoogleCloudOASIS,
                Category = ProviderCategory.Cloud,
                Name = "Google Cloud",
                Description = "Google Cloud Platform integration",
                Capabilities = new List<string> { "Cloud Computing", "AI/ML", "Big Data", "Analytics" },
                IsActive = true
            };

            // Initialize storage provider bridges
            _providerBridges[ProviderType.IPFSOASIS] = new ProviderBridge
            {
                ProviderType = ProviderType.IPFSOASIS,
                Category = ProviderCategory.Storage,
                Name = "IPFS",
                Description = "InterPlanetary File System",
                Capabilities = new List<string> { "Decentralized Storage", "Content Addressing", "P2P" },
                IsActive = true
            };

            _providerBridges[ProviderType.HoloOASIS] = new ProviderBridge
            {
                ProviderType = ProviderType.HoloOASIS,
                Category = ProviderCategory.Storage,
                Name = "Holochain",
                Description = "Holochain distributed storage",
                Capabilities = new List<string> { "Distributed Apps", "Agent-Centric", "P2P" },
                IsActive = true
            };

            // Initialize network provider bridges
            _providerBridges[ProviderType.ActivityPubOASIS] = new ProviderBridge
            {
                ProviderType = ProviderType.ActivityPubOASIS,
                Category = ProviderCategory.Network,
                Name = "ActivityPub",
                Description = "ActivityPub protocol integration",
                Capabilities = new List<string> { "Social Networking", "Federation", "Interoperability" },
                IsActive = true
            };

            _providerBridges[ProviderType.SOLIDOASIS] = new ProviderBridge
            {
                ProviderType = ProviderType.SOLIDOASIS,
                Category = ProviderCategory.Network,
                Name = "SOLID",
                Description = "SOLID protocol integration",
                Capabilities = new List<string> { "Data Ownership", "Privacy", "Interoperability" },
                IsActive = true
            };
        }

        private void InitializeProviderCategories()
        {
            _providerCategories[ProviderCategory.Blockchain] = new List<ProviderType>
            {
                ProviderType.EthereumOASIS,
                ProviderType.SolanaOASIS,
                ProviderType.BitcoinOASIS,
                ProviderType.PolygonOASIS,
                ProviderType.CardanoOASIS,
                ProviderType.ArbitrumOASIS,
                ProviderType.AvalancheOASIS,
                ProviderType.NEAROASIS,
                ProviderType.PolkadotOASIS,
                ProviderType.CosmosBlockChainOASIS
            };

            _providerCategories[ProviderCategory.Cloud] = new List<ProviderType>
            {
                ProviderType.AWSOASIS,
                ProviderType.AzureStorageOASIS,
                ProviderType.GoogleCloudOASIS
            };

            _providerCategories[ProviderCategory.Storage] = new List<ProviderType>
            {
                ProviderType.IPFSOASIS,
                ProviderType.HoloOASIS,
                ProviderType.PinataOASIS,
                ProviderType.ThreeFoldOASIS
            };

            _providerCategories[ProviderCategory.Network] = new List<ProviderType>
            {
                ProviderType.ActivityPubOASIS,
                ProviderType.SOLIDOASIS,
                ProviderType.ScuttlebuttOASIS
            };
        }

        private async Task InitializeAllProviderBridgesAsync()
        {
            // Initialize all provider bridges
            foreach (var bridge in _providerBridges.Values)
            {
                await InitializeProviderBridgeAsync(bridge);
            }
        }

        private async Task RegisterProvidersWithONETAsync()
        {
            // Register all providers with ONET network
            foreach (var bridge in _providerBridges.Values)
            {
                await RegisterProviderWithONETAsync(bridge);
            }
        }

        private async Task CreateProviderRoutingTablesAsync()
        {
            // Create routing tables for all providers
            foreach (var bridge in _providerBridges.Values)
            {
                var providerNode = new ProviderNode
                {
                    NodeId = $"{bridge.ProviderType}_node",
                    ProviderType = bridge.ProviderType,
                    Category = bridge.Category,
                    Name = bridge.Name,
                    Status = "Active",
                    Latency = await CalculateProviderLatencyAsync(bridge.ProviderType),
                    Reliability = await CalculateProviderReliabilityAsync(bridge.ProviderType),
                    Capabilities = bridge.Capabilities
                };

                _providerNodes[providerNode.NodeId] = providerNode;
            }
        }

        private async Task<ProviderNode?> FindOptimalProviderNodeAsync(ProviderType providerType, ProviderCategory category)
        {
            // Find optimal provider node based on type and category
            var nodes = _providerNodes.Values.Where(n => 
                n.ProviderType == providerType || 
                n.Category == category);

            // Select node with best performance
            return nodes.OrderByDescending(n => n.Reliability)
                       .ThenBy(n => n.Latency)
                       .FirstOrDefault();
        }

        private string CreateProviderRequest(ProviderType providerType, IRequest request)
        {
            // Create provider request for ONET transmission
            var providerRequest = new
            {
                ProviderType = providerType.ToString(),
                RequestType = request.RequestType,
                Parameters = request.Parameters,
                Timestamp = DateTime.UtcNow
            };

            return System.Text.Json.JsonSerializer.Serialize(providerRequest);
        }

        private async Task<OASISResult<T>> ProcessProviderResponseAsync<T>(ONETMessage message, ProviderType providerType)
        {
            var result = new OASISResult<T>();
            
            try
            {
                // Deserialize provider response
                var response = System.Text.Json.JsonSerializer.Deserialize<ProviderResponse>(message.Content);
                if (response == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize provider response");
                    return result;
                }

                if (response.Success)
                {
                    result.Result = System.Text.Json.JsonSerializer.Deserialize<T>(response.Data.ToString());
                    result.IsError = false;
                    result.Message = $"Provider {providerType} response successful";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, response.ErrorMessage ?? "Provider response failed");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error processing provider response: {ex.Message}", ex);
            }

            return result;
        }

        private async Task InitializeProviderBridgeAsync(ProviderBridge bridge)
        {
            // Initialize individual provider bridge
            bridge.InitializedAt = DateTime.UtcNow;
            bridge.Status = "Active";
        }

        /// <summary>
        /// Registers a provider bridge with ONET's real discovery layer via ONETProtocol's
        /// RegisterNodeForDiscoveryAsync passthrough. Previously this ran three Task.Run blocks that each
        /// looped over hardcoded service-name lists (mDNS/DHT/Blockchain/Bootstrap,
        /// ShortestPath/Intelligent/LoadBalanced/Adaptive, ProofOfStake/.../PBFT) doing nothing but
        /// Task.Delay and logging - no actual registration with any discovery, routing or consensus
        /// component ever happened.
        /// </summary>
        private async Task RegisterProviderWithONETAsync(ProviderBridge bridge)
        {
            var nodeId = $"{bridge.ProviderType}_node";
            var registerResult = await _onetProtocol.RegisterNodeForDiscoveryAsync(nodeId, bridge.Name, bridge.Capabilities);

            if (registerResult.IsError)
                OASISErrorHandling.HandleError($"Error registering provider {bridge.ProviderType} with ONET discovery: {registerResult.Message}");
            else
                LoggingManager.Log($"Provider {bridge.ProviderType} registered with ONET discovery as node {nodeId}.", Logging.LogType.Info);
        }

        /// <summary>
        /// Average real call latency from RouteThroughProviderAsync's recorded history. Previously a
        /// hardcoded per-ProviderType switch with made-up numbers (e.g. "EthereumOASIS => 15.5") that never
        /// changed regardless of how the provider actually performed. Falls back to a neutral 20ms estimate
        /// only when no calls have been made yet for this provider - there is no real signal to report until
        /// then, so a single fixed fallback (rather than per-type guesses) is the most honest option.
        /// </summary>
        private Task<double> CalculateProviderLatencyAsync(ProviderType providerType)
        {
            lock (_providerCallHistoryLock)
            {
                if (_providerCallHistory.TryGetValue(providerType, out var history) && history.Count > 0)
                    return Task.FromResult(history.Average(h => h.latencyMs));
            }

            return Task.FromResult(20.0);
        }

        /// <summary>
        /// Real success-rate percentage from recorded call history. Previously a hardcoded per-ProviderType
        /// switch (e.g. "BitcoinOASIS => 99") with no relationship to actual outcomes. Falls back to a
        /// neutral 90% only when no calls have been recorded yet.
        /// </summary>
        private Task<int> CalculateProviderReliabilityAsync(ProviderType providerType)
        {
            lock (_providerCallHistoryLock)
            {
                if (_providerCallHistory.TryGetValue(providerType, out var history) && history.Count > 0)
                    return Task.FromResult((int)Math.Round(100.0 * history.Count(h => h.success) / history.Count));
            }

            return Task.FromResult(90);
        }

        private async Task<List<ProviderInfo>> GetBlockchainProvidersAsync()
        {
            var providers = new List<ProviderInfo>();
            var blockchainTypes = _providerCategories.GetValueOrDefault(ProviderCategory.Blockchain) ?? new List<ProviderType>();
            
            foreach (var providerType in blockchainTypes)
            {
                if (_providerBridges.ContainsKey(providerType))
                {
                    var bridge = _providerBridges[providerType];
                    providers.Add(new ProviderInfo
                    {
                        Type = providerType,
                        Category = ProviderCategory.Blockchain,
                        Name = bridge.Name,
                        Description = bridge.Description,
                        IsActive = bridge.IsActive,
                        Capabilities = bridge.Capabilities
                    });
                }
            }
            
            return providers;
        }

        private async Task<List<ProviderInfo>> GetCloudProvidersAsync()
        {
            var providers = new List<ProviderInfo>();
            var cloudTypes = _providerCategories.GetValueOrDefault(ProviderCategory.Cloud) ?? new List<ProviderType>();
            
            foreach (var providerType in cloudTypes)
            {
                if (_providerBridges.ContainsKey(providerType))
                {
                    var bridge = _providerBridges[providerType];
                    providers.Add(new ProviderInfo
                    {
                        Type = providerType,
                        Category = ProviderCategory.Cloud,
                        Name = bridge.Name,
                        Description = bridge.Description,
                        IsActive = bridge.IsActive,
                        Capabilities = bridge.Capabilities
                    });
                }
            }
            
            return providers;
        }

        private async Task<List<ProviderInfo>> GetStorageProvidersAsync()
        {
            var providers = new List<ProviderInfo>();
            var storageTypes = _providerCategories.GetValueOrDefault(ProviderCategory.Storage) ?? new List<ProviderType>();
            
            foreach (var providerType in storageTypes)
            {
                if (_providerBridges.ContainsKey(providerType))
                {
                    var bridge = _providerBridges[providerType];
                    providers.Add(new ProviderInfo
                    {
                        Type = providerType,
                        Category = ProviderCategory.Storage,
                        Name = bridge.Name,
                        Description = bridge.Description,
                        IsActive = bridge.IsActive,
                        Capabilities = bridge.Capabilities
                    });
                }
            }
            
            return providers;
        }

        private async Task<List<ProviderInfo>> GetNetworkProvidersAsync()
        {
            var providers = new List<ProviderInfo>();
            var networkTypes = _providerCategories.GetValueOrDefault(ProviderCategory.Network) ?? new List<ProviderType>();
            
            foreach (var providerType in networkTypes)
            {
                if (_providerBridges.ContainsKey(providerType))
                {
                    var bridge = _providerBridges[providerType];
                    providers.Add(new ProviderInfo
                    {
                        Type = providerType,
                        Category = ProviderCategory.Network,
                        Name = bridge.Name,
                        Description = bridge.Description,
                        IsActive = bridge.IsActive,
                        Capabilities = bridge.Capabilities
                    });
                }
            }
            
            return providers;
        }

        /// <summary>
        /// Real uptime estimate: the fraction of registered provider bridges currently marked active.
        /// Previously hardcoded to 99.9% regardless of actual bridge state.
        /// </summary>
        private Task<double> GetNetworkUptimeAsync()
        {
            if (_providerBridges.Count == 0)
                return Task.FromResult(0.0);

            var activeFraction = (double)_providerBridges.Values.Count(b => b.IsActive) / _providerBridges.Count;
            return Task.FromResult(Math.Round(activeFraction * 100.0, 2));
        }
    }

    public class ProviderBridge
    {
        public ProviderType ProviderType { get; set; }
        public ProviderCategory Category { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Capabilities { get; set; } = new List<string>();
        public bool IsActive { get; set; }
        public DateTime InitializedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ProviderNode
    {
        public string NodeId { get; set; } = string.Empty;
        public ProviderType ProviderType { get; set; }
        public ProviderCategory Category { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public double Latency { get; set; }
        public int Reliability { get; set; }
        public List<string> Capabilities { get; set; } = new List<string>();
    }

    public class ProviderInfo
    {
        public ProviderType Type { get; set; }
        public ProviderCategory Category { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public List<string> Capabilities { get; set; } = new List<string>();
    }

    public class ProviderResponse
    {
        public bool Success { get; set; }
        public object Data { get; set; } = new object();
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class ProviderStats
    {
        public int TotalProviders { get; set; }
        public int ActiveProviders { get; set; }
        public int BlockchainProviders { get; set; }
        public int CloudProviders { get; set; }
        public int StorageProviders { get; set; }
        public int NetworkProviders { get; set; }
        public int TotalNodes { get; set; }
        public double NetworkUptime { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
