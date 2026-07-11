using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
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
        {
            if (_onetManagerTask != null)
                return _onetManagerTask;

            lock (_onetManagerLock)
            {
                _onetManagerTask ??= InitializeOnetManagerAsync();
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
        /// Get OASISDNA configuration for ONET
        /// </summary>
        [HttpGet("oasisdna")]
        public async Task<IActionResult> GetOASISDNA()
        {
            try
            {
                var result = await (await GetOnetManagerAsync()).GetOASISDNAAsync();

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return Ok(new OASISResult<OASISDNA>
                    {
                        Result = null,
                        IsError = false,
                        Message = "OASISDNA retrieved successfully (using test data)"
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return test data if setting is enabled, otherwise return error
                if (UseTestDataWhenLiveDataNotAvailable)
                {
                    return Ok(new OASISResult<OASISDNA>
                    {
                        Result = null,
                        IsError = false,
                        Message = "OASISDNA retrieved successfully (using test data)"
                    });
                }
                _logger.LogError(ex, "Error getting OASISDNA configuration");
                return StatusCode(500, new { message = "Error getting OASISDNA configuration", error = ex.Message });
            }
        }

        /// <summary>
        /// Update OASISDNA configuration for ONET
        /// </summary>
        [HttpPut("oasisdna")]
        public async Task<IActionResult> UpdateOASISDNA([FromBody] OASISDNA oasisdna)
        {
            if (oasisdna == null)
                return BadRequest(new { message = "The request body is required. Please provide a valid OASISDNA configuration object." });
            try
            {
                var result = await (await GetOnetManagerAsync()).UpdateOASISDNAAsync(oasisdna);
                if (result.IsError)
                {
                    return BadRequest(new { message = result.Message, errors = result.InnerMessages });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating OASISDNA configuration");
                return StatusCode(500, new { message = "Error updating OASISDNA configuration", error = ex.Message });
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
        /// Get connected nodes. Peer-to-peer callers must authenticate by supplying:
        ///   X-ONET-NodeId: &lt;their nodeId&gt;
        ///   X-ONET-Signature: &lt;base64 ECDSA-P256 signature over "GET /onet/network/nodes"&gt;
        /// Human/browser callers without those headers receive the node list without enforcement
        /// (auth enforcement can be tightened in SecurityConfig once all real peers are registered).
        /// </summary>
        [HttpGet("network/nodes")]
        public async Task<IActionResult> GetConnectedNodes()
        {
            try
            {
                var nodeId = Request.Headers["X-ONET-NodeId"].FirstOrDefault();
                var signature = Request.Headers["X-ONET-Signature"].FirstOrDefault();

                if (!string.IsNullOrWhiteSpace(nodeId) && !string.IsNullOrWhiteSpace(signature))
                {
                    var manager = await GetOnetManagerAsync();
                    var valid = await manager.VerifyRequestSignatureAsync(nodeId, "GET /onet/network/nodes", signature);
                    if (!valid)
                        return Unauthorized(new { message = "Invalid or unrecognised node signature. Register your public key via /onet/network/connect first." });
                }

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
