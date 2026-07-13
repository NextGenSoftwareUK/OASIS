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
using System;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    [ApiController]
    [Route("api/v1/onode")]
    public class ONODEController : OASISControllerBase
    {
        private readonly ILogger<ONODEController> _logger;

        // Static cached Task — same pattern as ONETController — avoids per-request reconstruction
        // and the sync-over-async deadlock risk of Task.Run(...).Result.
        private static Task<ONODEManager>? _onodeManagerTask;
        private static readonly object _onodeManagerLock = new object();

        public ONODEController(ILogger<ONODEController> logger)
        {
            _logger = logger;
        }

        private static Task<ONODEManager> GetOnodeManagerAsync()
        {
            if (_onodeManagerTask != null)
                return _onodeManagerTask;

            lock (_onodeManagerLock)
            {
                _onodeManagerTask ??= InitializeOnodeManagerAsync();
                return _onodeManagerTask;
            }
        }

        private static async Task<ONODEManager> InitializeOnodeManagerAsync()
        {
            OASISResult<IOASISStorageProvider> providerResult = await OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync();
            if (providerResult == null || providerResult.IsError || providerResult.Result == null)
                throw new InvalidOperationException($"Unable to initialize ONODEManager because default provider activation failed: {providerResult?.Message}");

            // Re-use the already-initialised ONETManager singleton so ONODEManager shares the same
            // running network instance (not a fresh one with its own discovery loop).
            var onetManager = await ONETController.GetOnetManagerStaticAsync();

            return new ONODEManager(providerResult.Result, OASISBootLoader.OASISBootLoader.OASISDNA, onetManager);
        }

        /// <summary>
        /// Get OASISDNA configuration for ONODE
        /// </summary>
        [HttpGet("oasisdna")]
        public async Task<IActionResult> GetOASISDNA()
        {
            try
            {
                var result = await (await GetOnodeManagerAsync()).GetOASISDNAAsync();

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
        /// Update OASISDNA configuration for ONODE
        /// </summary>
        [HttpPut("oasisdna")]
        public async Task<IActionResult> UpdateOASISDNA([FromBody] OASISDNA oasisdna)
        {
            if (oasisdna == null)
                return BadRequest(new { message = "The request body is required. Please provide a valid OASISDNA configuration object." });
            try
            {
                var result = await (await GetOnodeManagerAsync()).UpdateOASISDNAAsync(oasisdna);
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
                var result = await (await GetOnodeManagerAsync()).GetNodeStatusAsync();

                // Return test data if setting is enabled and result is null, has error, or result is null
                if (UseTestDataWhenLiveDataNotAvailable && (result == null || result.IsError || result.Result == null))
                {
                    return Ok(new OASISResult<object>
                    {
                        Result = new { status = "online", version = "1.0.0" },
                        IsError = false,
                        Message = "Node status retrieved successfully (using test data)"
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
                        Result = new { status = "online", version = "1.0.0" },
                        IsError = false,
                        Message = "Node status retrieved successfully (using test data)"
                    });
                }
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
                var result = await (await GetOnodeManagerAsync()).GetNodeInfoAsync();
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
                var result = await (await GetOnodeManagerAsync()).StartNodeAsync();
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
                var result = await (await GetOnodeManagerAsync()).StopNodeAsync();
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
                var result = await (await GetOnodeManagerAsync()).RestartNodeAsync();
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
                var result = await (await GetOnodeManagerAsync()).GetNodeMetricsAsync();
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
                var result = await (await GetOnodeManagerAsync()).GetNodeLogsAsync(lines ?? 100);
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
            if (request == null)
                return BadRequest(new { message = "The request body is required. Please provide a valid JSON body with Config." });
            try
            {
                var result = await (await GetOnodeManagerAsync()).UpdateNodeConfigAsync(request.Config);
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
                var result = await (await GetOnodeManagerAsync()).GetNodeConfigAsync();
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
                var result = await (await GetOnodeManagerAsync()).GetConnectedPeersAsync();
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
                var result = await (await GetOnodeManagerAsync()).GetNodeStatsAsync();
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
