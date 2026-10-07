using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.Core.Network;
using NextGenSoftware.OASIS.Common;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/v1/onet")]
    [Authorize(AvatarType.Wizard)]
    public class ONETController : OASISControllerBase
    {
        private readonly ILogger<ONETController> _logger;

        // ONETManager owns long-running background loops (consensus voting, routing table maintenance, etc.)
        // and a P2P network connection - it must be a true process-wide singleton, not rebuilt per request.
        // ASP.NET Core creates a new ONETController instance per request, so previously storing the manager
        // as an instance field meant every single request reconstructed the whole ONET stack (including
        // spinning up new background Task.Run loops with no disposal of the old ones - a thread/task leak)
        // and the lock around it only ever protected a single-use instance field, never actually preventing
        // duplicate construction across requests. A static field shared by all controller instances fixes
        // both problems. The lazy-init is done via a cached Task (not Task.Run(...).Result, which blocked
        // the calling thread synchronously and risked deadlocking under a captured SynchronizationContext).
        private static Task<ONETManager> _onetManagerTask;
        private static readonly object _onetManagerLock = new object();

        public ONETController(ILogger<ONETController> logger)
        {
            _logger = logger;
        }

        private static Task<ONETManager> GetOnetManagerAsync()
            => GetOnetManagerStaticAsync();

        /// <summary>
        /// Exposed as internal so ONODEController can share the same singleton instance
        /// rather than constructing a second ONETManager with its own discovery loop.
        /// </summary>
        internal static Task<ONETManager> GetOnetManagerStaticAsync()
        {
            if (_onetManagerTask != null && !_onetManagerTask.IsFaulted && !_onetManagerTask.IsCanceled)
                return _onetManagerTask;

            lock (_onetManagerLock)
            {
                if (_onetManagerTask == null || _onetManagerTask.IsFaulted || _onetManagerTask.IsCanceled)
                    _onetManagerTask = InitializeOnetManagerAsync();
                return _onetManagerTask;
            }
        }

        private static async Task<ONETManager> InitializeOnetManagerAsync()
        {
            OASISResult<IOASISStorageProvider> providerResult = await OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync();
            if (providerResult == null || providerResult.IsError || providerResult.Result == null)
                throw new InvalidOperationException($"Unable to initialize ONETManager because default provider activation failed: {providerResult?.Message}");

            var manager = new ONETManager(providerResult.Result, OASISBootLoader.OASISBootLoader.OASISDNA);
            await manager.InitializeAsync();
            return manager;
        }

        /// <summary>
        /// ONET configuration for this node. NodePrivateKey and ONETApiKey are always returned empty.
        /// </summary>
        [HttpGet("config")]
        public async Task<IActionResult> GetONETConfig()
        {
            try
            {
                var result = await (await GetOnetManagerAsync()).GetRedactedONETConfigAsync();
                return result.IsError ? StatusCode(500, result) : Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ONET configuration");
                return StatusCode(500, new { message = "Error getting ONET configuration", error = ex.Message });
            }
        }

        /// <summary>
        /// Update operator-editable ONET settings. Node identity (NodeId / keys) cannot be changed here; ONETApiKey
        /// is only replaced when a non-empty value is supplied.
        /// </summary>
        [HttpPut("config")]
        public async Task<IActionResult> UpdateONETConfig([FromBody] ONETConfig config)
        {
            if (config == null)
                return BadRequest(new { message = "The request body is required. Please provide an ONET configuration object." });
            try
            {
                var result = await (await GetOnetManagerAsync()).UpdateONETConfigAsync(config);
                return result.IsError ? BadRequest(new { message = result.Message, errors = result.InnerMessages }) : Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating ONET configuration");
                return StatusCode(500, new { message = "Error updating ONET configuration", error = ex.Message });
            }
        }
        /// <summary>
        /// Get P2P network status
        /// </summary>
        [HttpGet("network/status")]
        public async Task<IActionResult> GetNetworkStatus()
        {
            try
            {
                var result = await (await GetOnetManagerAsync()).GetNetworkStatusAsync();

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return Ok(new OASISResult<object>
                    {
                        Result = new { status = "online", nodes = 0 },
                        IsError = false,
                        Message = "Network status retrieved successfully (using test data)"
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<object>
                    {
                        Result = new { status = "online", nodes = 0 },
                        IsError = false,
                        Message = "Network status retrieved successfully (using test data)"
                    });
                }
                _logger.LogError(ex, "Error getting network status");
                return StatusCode(500, new { message = "Error getting network status", error = ex.Message });
            }
        }

        /// <summary>
        /// Get connected nodes (operator view).
        /// </summary>
        [HttpGet("network/nodes")]
        public async Task<IActionResult> GetConnectedNodes()
        {
            try
            {
                var result = await (await GetOnetManagerAsync()).GetConnectedNodesAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting connected nodes");
                return StatusCode(500, new { message = "Error getting connected nodes", error = ex.Message });
            }
        }

        /// <summary>
        /// Peer exchange for ONET nodes. No avatar login: the caller authenticates as a registered node with
        /// X-ONET-NodeId, X-ONET-Timestamp (unix seconds) and X-ONET-Signature (ECDSA-P256 over
        /// ONETSecurity.BuildFreshSignedMessage(PeerExchangePurpose, nodeId, timestamp)). Each signature is
        /// accepted once. Returns a JSON array of NodeInfo.
        /// </summary>
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        [HttpGet("peers")]
        public async Task<IActionResult> GetPeers()
        {
            var nodeId = Request.Headers["X-ONET-NodeId"].FirstOrDefault();
            var timestamp = Request.Headers["X-ONET-Timestamp"].FirstOrDefault();
            var signature = Request.Headers["X-ONET-Signature"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(nodeId) || string.IsNullOrWhiteSpace(signature) || !long.TryParse(timestamp, out var unixSeconds))
                return Unauthorized(new { message = "X-ONET-NodeId, X-ONET-Timestamp and X-ONET-Signature headers are required." });

            try
            {
                var manager = await GetOnetManagerAsync();
                if (!await manager.VerifyFreshRequestSignatureAsync(nodeId, ONETSecurity.PeerExchangePurpose, unixSeconds, signature))
                    return Unauthorized(new { message = "Invalid, expired, replayed or unregistered node signature. Register via POST api/v1/onet/nodes/register first." });

                var result = await manager.GetConnectedNodesAsync();
                if (result.IsError)
                    return StatusCode(500, new { message = result.Message });

                return Ok(result.Result.Where(n => n.Id != nodeId).Select(n => new NextGenSoftware.OASIS.API.ONODE.Core.Network.NodeInfo
                {
                    Id = n.Id,
                    Address = n.Address,
                    Capabilities = n.Capabilities,
                    LastSeen = n.ConnectedAt,
                    IsActive = true,
                    PublicKey = n.PublicKey
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting connected nodes");
                return StatusCode(500, new { message = "Error getting connected nodes", error = ex.Message });
            }
        }

        /// <summary>
        /// Connect to a specific node
        /// </summary>
        [HttpPost("network/connect")]
        public async Task<IActionResult> ConnectToNode([FromBody] ConnectNodeRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "The request body is required. Please provide a valid JSON body with NodeId and NodeAddress." });
            try
            {
                var result = await (await GetOnetManagerAsync()).ConnectToNodeAsync(request.NodeId, request.NodeAddress);
                if (result.IsError)
                {
                    return BadRequest(new { message = result.Message, errors = result.InnerMessages });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error connecting to node");
                return StatusCode(500, new { message = "Error connecting to node", error = ex.Message });
            }
        }

        /// <summary>
        /// Disconnect from a specific node
        /// </summary>
        [HttpPost("network/disconnect")]
        public async Task<IActionResult> DisconnectFromNode([FromBody] DisconnectNodeRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "The request body is required. Please provide a valid JSON body with NodeId." });
            try
            {
                var result = await (await GetOnetManagerAsync()).DisconnectFromNodeAsync(request.NodeId);
                if (result.IsError)
                {
                    return BadRequest(new { message = result.Message, errors = result.InnerMessages });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting from node");
                return StatusCode(500, new { message = "Error disconnecting from node", error = ex.Message });
            }
        }

        /// <summary>
        /// Get network statistics
        /// </summary>
        [HttpGet("network/stats")]
        public async Task<IActionResult> GetNetworkStats()
        {
            try
            {
                var result = await (await GetOnetManagerAsync()).GetNetworkStatsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting network statistics");
                return StatusCode(500, new { message = "Error getting network statistics", error = ex.Message });
            }
        }

        /// <summary>
        /// Start P2P network
        /// </summary>
        [HttpPost("network/start")]
        public async Task<IActionResult> StartNetwork()
        {
            try
            {
                var result = await (await GetOnetManagerAsync()).StartNetworkAsync();
                if (result.IsError)
                {
                    return BadRequest(new { message = result.Message, errors = result.InnerMessages });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting network");
                return StatusCode(500, new { message = "Error starting network", error = ex.Message });
            }
        }

        /// <summary>
        /// Stop P2P network
        /// </summary>
        [HttpPost("network/stop")]
        public async Task<IActionResult> StopNetwork()
        {
            try
            {
                var result = await (await GetOnetManagerAsync()).StopNetworkAsync();
                if (result.IsError)
                {
                    return BadRequest(new { message = result.Message, errors = result.InnerMessages });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping network");
                return StatusCode(500, new { message = "Error stopping network", error = ex.Message });
            }
        }

        /// <summary>
        /// Get network topology
        /// </summary>
        [HttpGet("network/topology")]
        public async Task<IActionResult> GetNetworkTopology()
        {
            try
            {
                var result = await (await GetOnetManagerAsync()).GetNetworkTopologyAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting network topology");
                return StatusCode(500, new { message = "Error getting network topology", error = ex.Message });
            }
        }

        /// <summary>
        /// Register a community ONODE's public key with this bootstrap server.
        /// Community nodes call this on startup so this server can verify their future ECDSA-signed requests
        /// (X-ONET-NodeId / X-ONET-Signature headers on GET /onet/network/nodes and other authenticated calls).
        /// </summary>
        [Microsoft.AspNetCore.Authorization.AllowAnonymous]
        [HttpPost("nodes/register")]
        public async Task<IActionResult> RegisterNode([FromBody] RegisterNodeRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "Request body is required with NodeId and PublicKey." });
            if (string.IsNullOrWhiteSpace(request.NodeId))
                return BadRequest(new { message = "NodeId is required." });
            if (string.IsNullOrWhiteSpace(request.PublicKey))
                return BadRequest(new { message = "PublicKey is required." });

            try
            {
                var manager = await GetOnetManagerAsync();

                // API key guard — skipped when ONETApiKey is empty (open bootstrap servers or local dev).
                var dnaResult = await manager.GetOASISDNAAsync();
                var apiKey = dnaResult.Result?.OASIS?.ONET?.ONETApiKey;
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    var supplied = Request.Headers["X-ONET-API-Key"].FirstOrDefault() ?? string.Empty;
                    if (!System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                            System.Text.Encoding.UTF8.GetBytes(supplied), System.Text.Encoding.UTF8.GetBytes(apiKey)))
                        return Unauthorized(new { message = "Invalid or missing X-ONET-API-Key header." });
                }

                if (!manager.RegisterNodePublicKey(request.NodeId, request.PublicKey))
                    return BadRequest(new { message = "NodeId must be the lowercase hex SHA-256 of a valid ECDSA-P256 SubjectPublicKeyInfo PublicKey." });

                // Binding an address to a node id requires proof of the node's private key; otherwise anyone could
                // publish a real node's public key with their own address.
                if (!string.IsNullOrWhiteSpace(request.NodeAddress))
                {
                    if (!long.TryParse(Request.Headers["X-ONET-Timestamp"].FirstOrDefault(), out var unixSeconds) ||
                        !await manager.VerifyFreshRequestSignatureAsync(request.NodeId, ONETSecurity.RegisterPurpose(request.NodeAddress),
                            unixSeconds, Request.Headers["X-ONET-Signature"].FirstOrDefault() ?? string.Empty))
                        return Unauthorized(new { message = "Registering a NodeAddress requires X-ONET-Timestamp and X-ONET-Signature proving ownership of the node key." });

                    var connect = await manager.ConnectToNodeAsync(request.NodeId, request.NodeAddress);
                    if (connect.IsError)
                        return BadRequest(new { message = $"Node key registered but address could not be connected: {connect.Message}" });
                }

                return Ok(new { message = "Node registered successfully.", nodeId = request.NodeId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering node {NodeId}", request.NodeId);
                return StatusCode(500, new { message = "Error registering node", error = ex.Message });
            }
        }

        /// <summary>
        /// Broadcast message to network
        /// </summary>
        [HttpPost("network/broadcast")]
        public async Task<IActionResult> BroadcastMessage([FromBody] BroadcastMessageRequest request)
        {
            if (request == null)
                return BadRequest(new { message = "The request body is required. Please provide a valid JSON body with Message and MessageType." });
            try
            {
                var result = await (await GetOnetManagerAsync()).BroadcastMessageAsync(request.Message, request.MessageType);
                if (result.IsError)
                {
                    return BadRequest(new { message = result.Message, errors = result.InnerMessages });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting message");
                return StatusCode(500, new { message = "Error broadcasting message", error = ex.Message });
            }
        }
    }

    public class ConnectNodeRequest
    {
        public string NodeId { get; set; } = string.Empty;
        public string NodeAddress { get; set; } = string.Empty;
    }

    public class RegisterNodeRequest
    {
        public string NodeId { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        /// <summary>Optional externally-reachable address (host:port) for TCP peer connections.</summary>
        public string? NodeAddress { get; set; }
    }

    public class DisconnectNodeRequest
    {
        public string NodeId { get; set; } = string.Empty;
    }

    public class BroadcastMessageRequest
    {
        public string Message { get; set; } = string.Empty;
        public string MessageType { get; set; } = "general";
    }
}
