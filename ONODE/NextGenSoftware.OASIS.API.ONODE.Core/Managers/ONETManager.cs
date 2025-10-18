using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class ONETManager : OASISManager
    {
        private OASISDNA? _oasisdna;
        private readonly ONETProtocol _onetProtocol;
        private readonly ONETConsensus _consensus;
        private readonly ONETRouting _routing;
        private readonly ONETSecurity _security;
        private readonly ONETDiscovery _discovery;
        private readonly ONETAPIGateway _apiGateway;

        private readonly List<ONETNode> _connectedNodes = new List<ONETNode>();
        private bool _isNetworkRunning = false;

        public ONETManager(IOASISStorageProvider storageProvider, OASISDNA oasisdna = null) : base(storageProvider, Guid.NewGuid(), oasisdna)
        {
            _onetProtocol = new ONETProtocol(storageProvider, oasisdna);
            _consensus = new ONETConsensus(storageProvider, oasisdna);
            _routing = new ONETRouting(storageProvider, oasisdna);
            _security = new ONETSecurity(storageProvider, oasisdna);
            _discovery = new ONETDiscovery(storageProvider, oasisdna);
            _apiGateway = new ONETAPIGateway(storageProvider, oasisdna);
            InitializeAsync().Wait();
        }

        private async Task InitializeAsync()
        {
            try
            {
                // Load OASISDNA configuration
                var oasisdnaResult = await LoadOASISDNAAsync();
                if (!oasisdnaResult.IsError && oasisdnaResult.Result != null)
                {
                    _oasisdna = oasisdnaResult.Result;
                }
            }
            catch (Exception ex)
            {
                var errorResult = new OASISResult<OASISDNA>();
                OASISErrorHandling.HandleError(ref errorResult, $"Error initializing ONET manager: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Get OASISDNA configuration
        /// </summary>
        public async Task<OASISResult<OASISDNA>> GetOASISDNAAsync()
        {
            var result = new OASISResult<OASISDNA>();
            
            try
            {
                if (_oasisdna == null)
                {
                    var loadResult = await LoadOASISDNAAsync();
                    if (loadResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error loading OASISDNA: {loadResult.Message}");
                        return result;
                    }
                    _oasisdna = loadResult.Result;
                }

                result.Result = _oasisdna;
                result.IsError = false;
                result.Message = "OASISDNA configuration retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting OASISDNA configuration: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Update OASISDNA configuration
        /// </summary>
        public async Task<OASISResult<bool>> UpdateOASISDNAAsync(OASISDNA oasisdna)
        {
            var result = new OASISResult<bool>();
            
            try
            {
                if (oasisdna == null)
                {
                    OASISErrorHandling.HandleError(ref result, "OASISDNA configuration cannot be null");
                    return result;
                }

                // Update the configuration
                _oasisdna = oasisdna;

                // Save the configuration
                var saveResult = await SaveOASISDNAAsync(oasisdna);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error saving OASISDNA: {saveResult.Message}");
                    return result;
                }

                result.Result = true;
                result.IsError = false;
                result.Message = "OASISDNA configuration updated successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error updating OASISDNA configuration: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Get network status
        /// </summary>
        public async Task<OASISResult<NetworkStatus>> GetNetworkStatusAsync()
        {
            var result = new OASISResult<NetworkStatus>();
            
            try
            {
                // Get network topology from ONET Protocol
                var topologyResult = await _onetProtocol.GetNetworkTopologyAsync();
                if (topologyResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting network topology: {topologyResult.Message}");
                    return result;
                }

                var topology = topologyResult.Result;
                var status = new NetworkStatus
                {
                    IsRunning = topology.NetworkHealth > 0,
                    ConnectedNodesCount = topology.Nodes.Count,
                    NetworkId = _oasisdna?.OASIS?.NetworkId ?? "onet-network",
                    LastUpdated = topology.LastUpdated
                };

                result.Result = status;
                result.IsError = false;
                result.Message = "Network status retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting network status: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Get connected nodes
        /// </summary>
        public async Task<OASISResult<List<NetworkNode>>> GetConnectedNodesAsync()
        {
            var result = new OASISResult<List<NetworkNode>>();
            
            try
            {
                // Get connected nodes from ONET Protocol
                var topologyResult = await _onetProtocol.GetNetworkTopologyAsync();
                if (topologyResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting network topology: {topologyResult.Message}");
                    return result;
                }

                var topology = topologyResult.Result;
                var connectedNodes = topology.Nodes.Select(node => new NetworkNode
                {
                    Id = node.Id,
                    Address = node.Address,
                    ConnectedAt = node.ConnectedAt,
                    Status = node.Status
                }).ToList();

                result.Result = connectedNodes;
                result.IsError = false;
                result.Message = "Connected nodes retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting connected nodes: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Connect to a specific node
        /// </summary>
        public async Task<OASISResult<bool>> ConnectToNodeAsync(string nodeId, string nodeAddress)
        {
            var result = new OASISResult<bool>();
            
            try
            {
                if (string.IsNullOrEmpty(nodeId) || string.IsNullOrEmpty(nodeAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Node ID and address are required");
                    return result;
                }

                // Use ONET Protocol to establish connection
                var connectResult = await _onetProtocol.ConnectToNodeAsync(nodeId, nodeAddress);
                if (connectResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to connect to node: {connectResult.Message}");
                    return result;
                }

                result.Result = true;
                result.IsError = false;
                result.Message = "Successfully connected to ONET node";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error connecting to node: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Disconnect from a specific node
        /// </summary>
        public async Task<OASISResult<bool>> DisconnectFromNodeAsync(string nodeId)
        {
            var result = new OASISResult<bool>();
            
            try
            {
                if (string.IsNullOrEmpty(nodeId))
                {
                    OASISErrorHandling.HandleError(ref result, "Node ID is required");
                    return result;
                }

                var node = _connectedNodes.FirstOrDefault(n => n.Id == nodeId);
                if (node == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Node is not connected");
                    return result;
                }

                _connectedNodes.Remove(node);

                result.Result = true;
                result.IsError = false;
                result.Message = "Successfully disconnected from node";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error disconnecting from node: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Get network statistics
        /// </summary>
        public async Task<OASISResult<Dictionary<string, object>>> GetNetworkStatsAsync()
        {
            var result = new OASISResult<Dictionary<string, object>>();
            
            try
            {
                var stats = new Dictionary<string, object>
                {
                    ["totalNodes"] = _connectedNodes.Count,
                    ["networkRunning"] = _isNetworkRunning,
                    ["uptime"] = DateTime.UtcNow - (_connectedNodes.FirstOrDefault()?.ConnectedAt ?? DateTime.UtcNow),
                    ["lastActivity"] = DateTime.UtcNow
                };

                result.Result = stats;
                result.IsError = false;
                result.Message = "Network statistics retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting network statistics: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Start P2P network
        /// </summary>
        public async Task<OASISResult<bool>> StartNetworkAsync()
        {
            var result = new OASISResult<bool>();
            
            try
            {
                // Start the ONET Protocol network
                var startResult = await _onetProtocol.StartNetworkAsync();
                if (startResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to start ONET network: {startResult.Message}");
                    return result;
                }

                result.Result = true;
                result.IsError = false;
                result.Message = "ONET P2P network started successfully - Web2 and Web3 unified!";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error starting network: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Stop P2P network
        /// </summary>
        public async Task<OASISResult<bool>> StopNetworkAsync()
        {
            var result = new OASISResult<bool>();
            
            try
            {
                // Stop the ONET Protocol network
                var stopResult = await _onetProtocol.StopNetworkAsync();
                if (stopResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to stop ONET network: {stopResult.Message}");
                    return result;
                }

                result.Result = true;
                result.IsError = false;
                result.Message = "ONET P2P network stopped successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error stopping network: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Get network topology
        /// </summary>
        public async Task<OASISResult<NetworkTopology>> GetNetworkTopologyAsync()
        {
            var result = new OASISResult<NetworkTopology>();
            
            try
            {
                var topology = new NetworkTopology
                {
                    Nodes = _connectedNodes,
                    Connections = new List<NetworkConnection>(),
                    LastUpdated = DateTime.UtcNow
                };

                result.Result = topology;
                result.IsError = false;
                result.Message = "Network topology retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting network topology: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Broadcast message to network
        /// </summary>
        public async Task<OASISResult<bool>> BroadcastMessageAsync(string message, string messageType = "general")
        {
            var result = new OASISResult<bool>();
            
            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    OASISErrorHandling.HandleError(ref result, "Message cannot be empty");
                    return result;
                }

                // Use ONET Protocol to broadcast message
                var onetMessage = new ONETMessage
                {
                    Content = message,
                    MessageType = messageType,
                    SourceNodeId = "local",
                    TargetNodeId = "broadcast"
                };

                var broadcastResult = await _onetProtocol.SendMessageAsync(onetMessage);
                if (broadcastResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to broadcast message: {broadcastResult.Message}");
                    return result;
                }

                result.Result = true;
                result.IsError = false;
                result.Message = "Message broadcasted successfully through ONET network";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error broadcasting message: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Call unified API - The GOD API that unifies Web2 and Web3
        /// </summary>
        public async Task<OASISResult<object>> CallUnifiedAPIAsync(string endpoint, object parameters, string networkType = "auto")
        {
            var result = new OASISResult<object>();
            
            try
            {
                // Use ONET Protocol to call unified API
                var apiResult = await _onetProtocol.CallUnifiedAPIAsync(endpoint, parameters, networkType);
                if (apiResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Unified API call failed: {apiResult.Message}");
                    return result;
                }

                result.Result = apiResult.Result;
                result.IsError = false;
                result.Message = "Unified API call successful - Web2 and Web3 unified!";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error calling unified API: {ex.Message}", ex);
            }

            return result;
        }

        private async Task<OASISResult<OASISDNA>> LoadOASISDNAAsync()
        {
            var result = new OASISResult<OASISDNA>();
            
            try
            {
                // Load from the actual OASISDNA system
                var oasisdna = await OASISDNAHelper.LoadOASISDNAAsync();
                if (oasisdna == null)
                {
                    // Create default configuration if none exists
                    oasisdna = new OASISDNA
                    {
                        OASIS = new OASIS
                        {
                            OASISAPIURL = "https://api.oasis.network",
                            SettingsLookupHolonId = Guid.Empty,
                            StatsCacheEnabled = false,
                            StatsCacheTtlSeconds = 45,
                            Logging = new LoggingSettings
                            {
                                LogToConsole = true,
                                LogToFile = true,
                                LogLevel = "Info"
                            }
                        }
                    };
                }

                result.Result = oasisdna;
                result.IsError = false;
                result.Message = "OASISDNA configuration loaded successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading OASISDNA: {ex.Message}", ex);
            }

            return result;
        }

        private async Task<OASISResult<bool>> SaveOASISDNAAsync(OASISDNA oasisdna)
        {
            var result = new OASISResult<bool>();
            
            try
            {
                // Save using the actual OASISDNA system
                var saveResult = await OASISDNAHelper.SaveOASISDNAAsync(oasisdna);
                if (saveResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error saving OASISDNA: {saveResult.Message}");
                    return result;
                }

                result.Result = true;
                result.IsError = false;
                result.Message = "OASISDNA configuration saved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving OASISDNA: {ex.Message}", ex);
            }

            return result;
        }
    }

    public class NetworkStatus
    {
        public bool IsRunning { get; set; }
        public int ConnectedNodesCount { get; set; }
        public string NetworkId { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }

    public class NetworkNode
    {
        public string Id { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class NetworkTopology
    {
        public List<NetworkNode> Nodes { get; set; } = new List<NetworkNode>();
        public List<NetworkConnection> Connections { get; set; } = new List<NetworkConnection>();
        public DateTime LastUpdated { get; set; }
    }

    public class NetworkConnection
    {
        public string FromNodeId { get; set; } = string.Empty;
        public string ToNodeId { get; set; } = string.Empty;
        public string ConnectionType { get; set; } = string.Empty;
        public DateTime EstablishedAt { get; set; }
    }
}
