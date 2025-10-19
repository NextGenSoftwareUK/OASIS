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
    [Route("api/v1/onode")]
    public class ONODEController : OASISControllerBase
    {
        private readonly ILogger<ONODEController> _logger;
        private readonly ONODEManager _onodeManager;

        public ONODEController(ILogger<ONODEController> logger, ONODEManager onodeManager)
        {
            _logger = logger;
            _onodeManager = onodeManager;
        }

        /// <summary>
        /// Get OASISDNA configuration for ONODE
        /// </summary>
        [HttpGet("oasisdna")]
        public async Task<IActionResult> GetOASISDNA()
        {
            try
            {
                var result = await _onodeManager.GetOASISDNAAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting OASISDNA configuration");
                return StatusCode(500, new { message = "Error getting OASISDNA configuration", error = ex.Message });
            }
        }

        /// <summary>
        /// Update OASISDNA configuration for ONODE
        /// </summary>
        [HttpPut("oasisdna")]
        public async Task<IActionResult> UpdateOASISDNA([FromBody] OASISDNA oasisdna)
        {
            try
            {
                var result = await _onodeManager.UpdateOASISDNAAsync(oasisdna);
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
        /// Get node status
        /// </summary>
        [HttpGet("status")]
        public async Task<IActionResult> GetNodeStatus()
        {
            try
            {
                var result = await _onodeManager.GetNodeStatusAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting node status");
                return StatusCode(500, new { message = "Error getting node status", error = ex.Message });
            }
        }

        /// <summary>
        /// Get node information
        /// </summary>
        [HttpGet("info")]
        public async Task<IActionResult> GetNodeInfo()
        {
            try
            {
                var result = await _onodeManager.GetNodeInfoAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting node information");
                return StatusCode(500, new { message = "Error getting node information", error = ex.Message });
            }
        }

        /// <summary>
        /// Start ONODE
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartNode()
        {
            try
            {
                var result = await _onodeManager.StartNodeAsync();
                if (result.IsError)
                {
                    return BadRequest(new { message = result.Message, errors = result.InnerMessages });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting node");
                return StatusCode(500, new { message = "Error starting node", error = ex.Message });
            }
        }

        /// <summary>
        /// Stop ONODE
        /// </summary>
        [HttpPost("stop")]
        public async Task<IActionResult> StopNode()
        {
            try
            {
                var result = await _onodeManager.StopNodeAsync();
                if (result.IsError)
                {
                    return BadRequest(new { message = result.Message, errors = result.InnerMessages });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping node");
                return StatusCode(500, new { message = "Error stopping node", error = ex.Message });
            }
        }

        /// <summary>
        /// Restart ONODE
        /// </summary>
        [HttpPost("restart")]
        public async Task<IActionResult> RestartNode()
        {
            try
            {
                var result = await _onodeManager.RestartNodeAsync();
                if (result.IsError)
                {
                    return BadRequest(new { message = result.Message, errors = result.InnerMessages });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting node");
                return StatusCode(500, new { message = "Error restarting node", error = ex.Message });
            }
        }

        /// <summary>
        /// Get node performance metrics
        /// </summary>
        [HttpGet("metrics")]
        public async Task<IActionResult> GetNodeMetrics()
        {
            try
            {
                var result = await _onodeManager.GetNodeMetricsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting node metrics");
                return StatusCode(500, new { message = "Error getting node metrics", error = ex.Message });
            }
        }

        /// <summary>
        /// Get node logs
        /// </summary>
        [HttpGet("logs")]
        public async Task<IActionResult> GetNodeLogs([FromQuery] int? lines = 100)
        {
            try
            {
                var result = await _onodeManager.GetNodeLogsAsync(lines ?? 100);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting node logs");
                return StatusCode(500, new { message = "Error getting node logs", error = ex.Message });
            }
        }

        /// <summary>
        /// Update node configuration
        /// </summary>
        [HttpPut("config")]
        public async Task<IActionResult> UpdateNodeConfig([FromBody] NodeConfigRequest request)
        {
            try
            {
                var result = await _onodeManager.UpdateNodeConfigAsync(request.Config);
                if (result.IsError)
                {
                    return BadRequest(new { message = result.Message, errors = result.InnerMessages });
                }
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating node configuration");
                return StatusCode(500, new { message = "Error updating node configuration", error = ex.Message });
            }
        }

        /// <summary>
        /// Get node configuration
        /// </summary>
        [HttpGet("config")]
        public async Task<IActionResult> GetNodeConfig()
        {
            try
            {
                var result = await _onodeManager.GetNodeConfigAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting node configuration");
                return StatusCode(500, new { message = "Error getting node configuration", error = ex.Message });
            }
        }

        /// <summary>
        /// Get connected peers
        /// </summary>
        [HttpGet("peers")]
        public async Task<IActionResult> GetConnectedPeers()
        {
            try
            {
                var result = await _onodeManager.GetConnectedPeersAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting connected peers");
                return StatusCode(500, new { message = "Error getting connected peers", error = ex.Message });
            }
        }

        /// <summary>
        /// Get node statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetNodeStats()
        {
            try
            {
                var result = await _onodeManager.GetNodeStatsAsync();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting node statistics");
                return StatusCode(500, new { message = "Error getting node statistics", error = ex.Message });
            }
        }
    }

    public class NodeConfigRequest
    {
        public Dictionary<string, object> Config { get; set; } = new Dictionary<string, object>();
    }
}
