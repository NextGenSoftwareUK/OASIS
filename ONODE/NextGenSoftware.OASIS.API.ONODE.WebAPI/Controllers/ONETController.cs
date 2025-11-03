using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/v1/onet")]
    public class ONETController : OASISControllerBase
    {
        private readonly ILogger<ONETController> _logger;
        private readonly ONETManager _onetManager;

        public ONETController(ILogger<ONETController> logger, ONETManager onetManager)
        {
            _logger = logger;
            _onetManager = onetManager;
        }

        /// <summary>
        /// Get OASISDNA configuration for ONET
        /// </summary>
        [HttpGet("oasisdna")]
        public async Task<IActionResult> GetOASISDNA()
        {
            try
            {
                var result = await _onetManager.GetOASISDNAAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
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
            try
            {
                var result = await _onetManager.UpdateOASISDNAAsync(oasisdna);
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
                var result = await _onetManager.GetNetworkStatusAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting network status");
                return StatusCode(500, new { message = "Error getting network status", error = ex.Message });
            }
        }

        /// <summary>
        /// Get connected nodes
        /// </summary>
        [HttpGet("network/nodes")]
        public async Task<IActionResult> GetConnectedNodes()
        {
            try
            {
                var result = await _onetManager.GetConnectedNodesAsync();
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
            try
            {
                var result = await _onetManager.ConnectToNodeAsync(request.NodeId, request.NodeAddress);
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
            try
            {
                var result = await _onetManager.DisconnectFromNodeAsync(request.NodeId);
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
                var result = await _onetManager.GetNetworkStatsAsync();
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
                var result = await _onetManager.StartNetworkAsync();
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
                var result = await _onetManager.StopNetworkAsync();
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
                var result = await _onetManager.GetNetworkTopologyAsync();
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
            try
            {
                var result = await _onetManager.BroadcastMessageAsync(request.Message, request.MessageType);
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
