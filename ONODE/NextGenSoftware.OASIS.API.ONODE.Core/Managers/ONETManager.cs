using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.API.Providers.HoloOASIS;
using NextGenSoftware.OASIS.Common;

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
        
        // P2P Network Support
        private IP2PNetworkProvider _p2pNetworkProvider;
        private P2PNetworkType _networkType = P2PNetworkType.Internal;
        private HoloOASIS? _holoOASIS;

        private readonly List<ONETNode> _connectedNodes = new List<ONETNode>();
        private bool _isNetworkRunning = false;
        private DateTime _networkStartTime = DateTime.MinValue;

        private readonly IOASISStorageProvider _storageProvider;
        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        public ONETManager(IOASISStorageProvider storageProvider, OASISDNA oasisdna = null, P2PNetworkType networkType = P2PNetworkType.Internal) : base(storageProvider, Guid.NewGuid(), oasisdna)
        {
            _storageProvider = storageProvider;

            // Resolve network type: DNA takes priority over the constructor parameter default.
            if (oasisdna?.OASIS?.ONET != null && !string.IsNullOrWhiteSpace(oasisdna.OASIS.ONET.NetworkType))
            {
                _networkType = oasisdna.OASIS.ONET.NetworkType.Equals("HoloNET", StringComparison.OrdinalIgnoreCase)
                    ? P2PNetworkType.HoloNET
                    : P2PNetworkType.Internal;
            }
            else
            {
                _networkType = networkType;
            }

            if (!string.IsNullOrWhiteSpace(oasisdna?.OASIS?.DataDirectory))
                OASISHyperDrive.DataDirectory = oasisdna.OASIS.DataDirectory;

            _onetProtocol = ONETProtocol.GetInstance(storageProvider, oasisdna);
            _consensus = new ONETConsensus(storageProvider, oasisdna);
            _routing = new ONETRouting(storageProvider, oasisdna);
            _security = new ONETSecurity(storageProvider, oasisdna);
            _discovery = new ONETDiscovery(storageProvider, oasisdna);
            _apiGateway = new ONETAPIGateway(storageProvider, oasisdna);

            // Initialize P2P network provider based on type. Done synchronously in the constructor
            // (rather than via an async InitAsync the caller must remember to call) because every public
            // method on this class assumes _p2pNetworkProvider is already set - so it must exist before
            // the constructor returns.
            InitializeP2PNetworkProvider();
        }

        private void InitializeP2PNetworkProvider()
        {
            switch (_networkType)
            {
                case P2PNetworkType.Internal:
                    _p2pNetworkProvider = new InternalP2PNetworkProvider(
                        _onetProtocol, _consensus, _routing, _security, _discovery, _apiGateway);
                    break;

                case P2PNetworkType.HoloNET:
                    // The HoloNET P2P provider needs a real IHoloNETClientBase. The natural source of one is
                    // the HoloOASIS storage provider itself (HoloOASIS.HoloNETClientAppAgent implements
                    // IHoloNETClientBase) - previously this branch always passed null/null into
                    // HoloNETP2PProvider's constructor, which throws ArgumentNullException immediately,
                    // making P2PNetworkType.HoloNET permanently unusable regardless of what was passed in.
                    _holoOASIS = _storageProvider as HoloOASIS;
                    if (_holoOASIS == null)
                    {
                        throw new InvalidOperationException(
                            "P2PNetworkType.HoloNET requires the storageProvider passed to ONETManager's " +
                            "constructor to be a HoloOASIS instance (so its HoloNETClientAppAgent can be used " +
                            "as the underlying HoloNET client). Pass a HoloOASIS storage provider, or use " +
                            "P2PNetworkType.Internal instead.");
                    }
                    _p2pNetworkProvider = new HoloNETP2PProvider(_holoOASIS.HoloNETClientAppAgent, _storageProvider, OASISDNA);
                    break;

                default:
                    throw new ArgumentException($"Unsupported P2P network type: {_networkType}");
            }
        }

        /// <summary>
        /// Loads the live OASISDNA configuration into this manager. Must be called (and awaited) once after
        /// construction before relying on GetOASISDNAAsync()/UpdateOASISDNAAsync() to reflect the persisted
        /// configuration - constructors cannot be async, so this can no longer run via a blocking
        /// InitializeAsync().Wait() call inside the constructor (that was a sync-over-async deadlock risk).
        /// GetOASISDNAAsync() also lazily loads on first call if this was never invoked.
        /// </summary>
        public async Task<OASISResult<OASISDNA>> InitializeAsync()
        {
            var result = new OASISResult<OASISDNA>();

            try
            {
                var oasisdnaResult = await LoadOASISDNAAsync();
                if (!oasisdnaResult.IsError && oasisdnaResult.Result != null)
                    _oasisdna = oasisdnaResult.Result;

                // Generate a stable ECDSA-P256 NodeId/keypair on first run and persist it back to DNA.
                if (_oasisdna?.OASIS?.ONET != null)
                {
                    bool keypairDirty = false;

                    if (string.IsNullOrWhiteSpace(_oasisdna.OASIS.ONET.NodePrivateKey) ||
                        string.IsNullOrWhiteSpace(_oasisdna.OASIS.ONET.NodePublicKey))
                    {
                        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
                        _oasisdna.OASIS.ONET.NodePrivateKey = Convert.ToBase64String(ecdsa.ExportECPrivateKey());
                        _oasisdna.OASIS.ONET.NodePublicKey  = Convert.ToBase64String(ecdsa.ExportSubjectPublicKeyInfo());
                        keypairDirty = true;
                    }

                    if (string.IsNullOrWhiteSpace(_oasisdna.OASIS.ONET.NodeId) || keypairDirty)
                    {
                        var pubBytes = Convert.FromBase64String(_oasisdna.OASIS.ONET.NodePublicKey);
                        _oasisdna.OASIS.ONET.NodeId = Convert.ToHexString(SHA256.HashData(pubBytes)).ToLowerInvariant();
                        keypairDirty = true;
                    }

                    if (keypairDirty)
                        await SaveOASISDNAAsync(_oasisdna);

                    // Register our public key with this node's own protocol so inbound ECDSA checks work.
                    _onetProtocol.RegisterNodePublicKey(_oasisdna.OASIS.ONET.NodeId, _oasisdna.OASIS.ONET.NodePublicKey);

                    // Push registration to each bootstrap server so they can verify future signed requests.
                    if (_oasisdna.OASIS.ONET.AutoRegisterOnBootstrap &&
                        _oasisdna.OASIS.ONET.BootstrapServers?.Count > 0)
                    {
                        await RegisterWithBootstrapServersAsync(
                            _oasisdna.OASIS.ONET.NodeId,
                            _oasisdna.OASIS.ONET.NodePublicKey,
                            _oasisdna.OASIS.ONET.BootstrapServers);
                    }
                }

                result.Result = _oasisdna;
                result.IsError = oasisdnaResult.IsError;
                result.Message = oasisdnaResult.Message;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error initializing ONET manager: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Register this node's public key with every bootstrap server so they can verify our
        /// future ECDSA-signed requests (X-ONET-NodeId / X-ONET-Signature headers).
        /// Failures are logged but never throw — network may not be available yet at startup.
        /// </summary>
        private async Task RegisterWithBootstrapServersAsync(string nodeId, string publicKey, List<string> bootstrapServers)
        {
            var payload = JsonSerializer.Serialize(new { nodeId, publicKey });
            var content = new StringContent(payload, Encoding.UTF8, "application/json");

            foreach (var server in bootstrapServers)
            {
                try
                {
                    var url = $"{server.TrimEnd('/')}/api/v1/onet/nodes/register";
                    var response = await _httpClient.PostAsync(url, new StringContent(payload, Encoding.UTF8, "application/json"));
                    if (!response.IsSuccessStatusCode)
                    {
                        var warn = new OASISResult<bool>();
                        OASISErrorHandling.HandleWarning(ref warn, $"Bootstrap registration at {url} returned {(int)response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    var warn = new OASISResult<bool>();
                    OASISErrorHandling.HandleWarning(ref warn, $"Could not register with bootstrap server {server}: {ex.Message}", ex);
                }
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
                // Use P2P network provider to get network status
                var statusResult = await _p2pNetworkProvider.GetNetworkStatusAsync();
                if (statusResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error getting network status: {statusResult.Message}");
                    return result;
                }

                result.Result = statusResult.Result;
                result.IsError = false;
                result.Message = $"Network status retrieved successfully using {_networkType}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting network status: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// <summary>
        /// Verify that a remote node's signature over <paramref name="message"/> is valid.
        /// Used by HTTP endpoints to authenticate peer requests via X-ONET-NodeId / X-ONET-Signature headers.
        /// </summary>
        public Task<bool> VerifyRequestSignatureAsync(string nodeId, string message, string base64Signature)
            => _onetProtocol.VerifyRequestSignatureAsync(nodeId, message, base64Signature);

        /// <summary>
        /// Register a peer's base-64 ECDSA-P256 public key so subsequent VerifyRequestSignatureAsync
        /// calls from that node will succeed. Called by the /onet/nodes/register REST endpoint and
        /// automatically during peer-exchange (OnPeerKeyDiscovered).
        /// </summary>
        public void RegisterNodePublicKey(string nodeId, string base64PublicKey)
            => _onetProtocol.RegisterNodePublicKey(nodeId, base64PublicKey);

        /// Get connected nodes
        /// </summary>
        public async Task<OASISResult<List<ONETNode>>> GetConnectedNodesAsync()
        {
            var result = new OASISResult<List<ONETNode>>();
            
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
                result.Result = topology.Nodes;
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
                    ["networkType"] = _networkType.ToString(),
                    ["nodeId"] = _oasisdna?.OASIS?.ONET?.NodeId ?? "",
                    ["uptime"] = _isNetworkRunning ? (object)(DateTime.UtcNow - _networkStartTime) : TimeSpan.Zero,
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
                // Use P2P network provider to start network
                var startResult = await _p2pNetworkProvider.StartNetworkAsync();
                if (startResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to start P2P network: {startResult.Message}");
                    return result;
                }

                _isNetworkRunning = true;
                _networkStartTime = DateTime.UtcNow;
                result.Result = true;
                result.IsError = false;
                result.Message = $"ONET P2P network started successfully using {_networkType} - Web2 and Web3 unified!";
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
                // Use P2P network provider to stop network
                var stopResult = await _p2pNetworkProvider.StopNetworkAsync();
                if (stopResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to stop P2P network: {stopResult.Message}");
                    return result;
                }

                _isNetworkRunning = false;
                result.Result = true;
                result.IsError = false;
                result.Message = $"ONET P2P network stopped successfully using {_networkType}";
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
        public async Task<OASISResult<NextGenSoftware.OASIS.API.ONODE.Core.Network.NetworkTopology>> GetNetworkTopologyAsync()
        {
            var result = new OASISResult<NextGenSoftware.OASIS.API.ONODE.Core.Network.NetworkTopology>();

            try
            {
                var topology = new NextGenSoftware.OASIS.API.ONODE.Core.Network.NetworkTopology
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
                var oasisdna = await OASISDNAManager.LoadDNAAsync();

                if (oasisdna != null && oasisdna.Result != null && !oasisdna.IsError)
                {
                    result.Result = oasisdna.Result;
                    result.IsError = false;
                    result.Message = "OASISDNA configuration loaded successfully";
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Failed to load OASISDNA configuration. Reason: {oasisdna.Message}");
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
                var saveResult = await OASISDNAManager.SaveDNAAsync();
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


}
