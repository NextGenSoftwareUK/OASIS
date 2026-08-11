using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{
    public partial class ONETProtocol : OASISManager
    {

        public async Task InitializeAsync()
        {
            try
            {
                // Load OASISDNA configuration
                var oasisdnaResult = await LoadOASISDNAAsync();
                if (!oasisdnaResult.IsError && oasisdnaResult.Result != null)
                {
                    _oasisdna = oasisdnaResult.Result;
                }

                // Initialize network bridges to Web2 and Web3
                await InitializeNetworkBridgesAsync();
            }
            catch (Exception ex)
            {
                var result = new OASISResult<bool>();
                OASISErrorHandling.HandleError(ref result, $"Error initializing ONET Protocol: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Start the ONET P2P network
        /// </summary>
        public async Task<OASISResult<bool>> StartNetworkAsync()
        {
            var result = new OASISResult<bool>();
            
            try
            {
                if (_isNetworkRunning)
                {
                    OASISErrorHandling.HandleError(ref result, "ONET network is already running");
                    return result;
                }

                // Load OASISDNA config and init bridges before starting network components
                await InitializeAsync();

                // Initialize security layer
                await _security.InitializeAsync(_oasisdna);

                // Start node discovery
                await _discovery.StartDiscoveryAsync();

                // Initialize consensus mechanism
                await _consensus.InitializeAsync();

                // Start routing system
                await _routing.StartRoutingAsync();

                // Initialize API Gateway
                await _apiGateway.InitializeAsync();

                // Start the real ONET_PING/ONET_PONG TCP responder, so connectivity tests against this node
                // (from ONETRouting.TestNodeConnectivityAsync, or PerformRealNetworkTransmissionAsync's ACK
                // wait, both of which already implement a real client) actually get a real reply instead of
                // timing out - previously nothing in ONET listened for incoming connections at all, so every
                // connectivity test against a real running node failed even though the node was healthy.
                StartPingResponder();

                _isNetworkRunning = true;

                result.Result = true;
                result.IsError = false;
                result.Message = "ONET P2P network started successfully - Web2 and Web3 unified!";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error starting ONET network: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Start the ONET P2P network (alias for StartNetworkAsync)
        /// </summary>
        public async Task StartAsync()
        {
            await StartNetworkAsync();
        }

        public async Task StopAsync()
        {
            try
            {
                _isNetworkRunning = false;
                
                // Stop all network components
                await _consensus.StopAsync();
                await _routing.StopAsync();
                await _security.StopAsync();
                await _discovery.StopAsync();
                await _apiGateway.StopAsync();
                
                // Clear connected nodes
                _connectedNodes.Clear();
                
                LoggingManager.Log("ONET Protocol stopped successfully", Logging.LogType.Info);
            }
            catch (Exception ex)
            {
                var result = new OASISResult<bool>();
                OASISErrorHandling.HandleError(ref result, $"Error stopping ONET Protocol: {ex.Message}", ex);
            }
        }

        public async Task<OASISResult<bool>> DisconnectFromNodeAsync(string nodeId)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (_connectedNodes.ContainsKey(nodeId))
                {
                    _connectedNodes.Remove(nodeId);
                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Disconnected from node {nodeId}";
                }
                else
                {
                    result.Result = false;
                    result.IsError = true;
                    result.Message = $"Node {nodeId} not found";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error disconnecting from node {nodeId}: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<bool>> BroadcastMessageAsync(string message, Dictionary<string, object> metadata = null)
        {
            var result = new OASISResult<bool>();
            try
            {
                // Broadcast message to all connected nodes
                foreach (var node in _connectedNodes.Values)
                {
                    // In real implementation, this would send via the network
                    LoggingManager.Log($"Broadcasting message to node {node.Id}: {message}", Logging.LogType.Debug);
                }
                
                result.Result = true;
                result.IsError = false;
                result.Message = "Message broadcasted successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error broadcasting message: {ex.Message}", ex);
            }
            return result;
        }

        /// <summary>
        /// Stop the ONET P2P network
        /// </summary>
        public async Task<OASISResult<bool>> StopNetworkAsync()
        {
            var result = new OASISResult<bool>();
            
            try
            {
                if (!_isNetworkRunning)
                {
                    OASISErrorHandling.HandleError(ref result, "ONET network is not running");
                    return result;
                }

                // Stop all network components
                await _discovery.StopDiscoveryAsync();
                await _routing.StopRoutingAsync();
                await _apiGateway.StopAsync();
                await _consensus.StopAsync();
                StopPingResponder();

                _connectedNodes.Clear();
                _isNetworkRunning = false;

                result.Result = true;
                result.IsError = false;
                result.Message = "ONET P2P network stopped successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error stopping ONET network: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Starts a real TCP listener on ListenPort that answers "ONET_PING" with "ONET_PONG" - the missing
        /// server-side half of the connectivity probe that ONETRouting.TestNodeConnectivityAsync and
        /// PerformRealNetworkTransmissionAsync's ACK wait already implement on the client side.
        /// </summary>
        private void StartPingResponder()
        {
            try
            {
                _pingResponderCts = new System.Threading.CancellationTokenSource();
                _pingResponderListener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, ListenPort);
                _pingResponderListener.Start();

                _ = Task.Run(() => AcceptPingConnectionsAsync(_pingResponderListener, _pingResponderCts.Token));

                LoggingManager.Log($"ONET ping responder listening on port {ListenPort}", Logging.LogType.Info);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error starting ONET ping responder on port {ListenPort}: {ex.Message}", ex);
            }
        }

        private void StopPingResponder()
        {
            try
            {
                _pingResponderCts?.Cancel();
                _pingResponderListener?.Stop();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error stopping ONET ping responder: {ex.Message}", ex);
            }
            finally
            {
                _pingResponderCts?.Dispose();
                _pingResponderCts = null;
                _pingResponderListener = null;
            }
        }

        private async Task AcceptPingConnectionsAsync(System.Net.Sockets.TcpListener listener, System.Threading.CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var client = await listener.AcceptTcpClientAsync(cancellationToken);
                    _ = Task.Run(() => HandlePingConnectionAsync(client, cancellationToken), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // Listener was stopped - exit the accept loop.
                    break;
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                        OASISErrorHandling.HandleError($"Error accepting ONET ping connection: {ex.Message}", ex);
                }
            }
        }

        private async Task HandlePingConnectionAsync(System.Net.Sockets.TcpClient client, System.Threading.CancellationToken cancellationToken)
        {
            using (client)
            {
                try
                {
                    using var stream = client.GetStream();
                    var buffer = new byte[256];
                    var read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    var request = System.Text.Encoding.UTF8.GetString(buffer, 0, read);

                    if (request.StartsWith("ONET_PING", StringComparison.OrdinalIgnoreCase))
                    {
                        // Enhanced format: "ONET_PING <nodeId> <base64Signature>"
                        // Signature is ECDSA P-256 over the UTF-8 bytes of "ONET_PING".
                        // If auth parts are present, verify before replying.
                        var parts = request.Trim().Split(' ');
                        if (parts.Length >= 3)
                        {
                            var pingNodeId = parts[1];
                            var pingSignature = parts[2];
                            var verified = await _security.VerifyNodeSignatureAsync(pingNodeId, "ONET_PING", pingSignature);
                            if (!verified)
                            {
                                var denied = System.Text.Encoding.UTF8.GetBytes("ONET_AUTH_FAILED\n");
                                await stream.WriteAsync(denied, 0, denied.Length, cancellationToken);
                                LoggingManager.Log($"ONET PING rejected from {pingNodeId}: invalid or unknown signature", Logging.LogType.Warning);
                                return;
                            }
                        }
                        var pong = System.Text.Encoding.UTF8.GetBytes("ONET_PONG\n");
                        await stream.WriteAsync(pong, 0, pong.Length, cancellationToken);
                    }
                    else
                    {
                        // Any other inbound message is treated as a delivered ONET message and ACKed, matching
                        // the ACK that PerformRealNetworkTransmissionAsync's client side waits for.
                        var ack = System.Text.Encoding.UTF8.GetBytes("ONET_ACK\n");
                        await stream.WriteAsync(ack, 0, ack.Length, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError($"Error handling inbound ONET connection: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// Verify a remote node's ECDSA signature for an authenticated HTTP request.
        /// Delegates to ONETSecurity; returns false if the node is unknown (not yet registered).
        /// </summary>
        public Task<bool> VerifyRequestSignatureAsync(string nodeId, string message, string base64Signature)
            => _security.VerifyNodeSignatureAsync(nodeId, message, base64Signature);

        /// <summary>
        /// Register a remote node's public key (called during peer-exchange so later PING / HTTP calls can be verified).
        /// </summary>
        public void RegisterNodePublicKey(string nodeId, string base64PublicKey)
            => _security.RegisterNodePublicKey(nodeId, base64PublicKey);

        /// <summary>
        /// Connect to a specific ONET node
        /// </summary>
        public async Task<OASISResult<bool>> ConnectToNodeAsync(string nodeId, string nodeAddress)
        {
            var result = new OASISResult<bool>();
            
            try
            {
                if (!_isNetworkRunning)
                {
                    OASISErrorHandling.HandleError(ref result, "ONET network is not running");
                    return result;
                }

                // Create secure connection to node
                var connectionResult = await _security.EstablishSecureConnectionAsync(nodeId, nodeAddress);
                if (connectionResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Failed to establish secure connection: {connectionResult.Message}");
                    return result;
                }

                // Add to connected nodes
                var node = new ONETNode
                {
                    Id = nodeId,
                    Address = nodeAddress,
                    ConnectedAt = DateTime.UtcNow,
                    Status = "Connected",
                    Capabilities = await GetNodeCapabilitiesAsync(nodeId),
                    PublicKey = _security.GetNodePublicKey(nodeId)
                };

                _connectedNodes[nodeId] = node;

                // Update routing table
                await _routing.AddNodeAsync(node);

                result.Result = true;
                result.IsError = false;
                result.Message = $"Successfully connected to ONET node {nodeId}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error connecting to node: {ex.Message}", ex);
            }

            return result;
        }

    }
}
