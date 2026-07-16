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
using System.Linq;
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

        // ── Holon Bridge endpoints ────────────────────────────────────────────────
        // Called by ONODEService (push) and OPORTAL-JS (read/command).
        // Node state is stored in memory (last-seen per nodeId) so OPORTAL can read
        // current status from anywhere without reaching the local machine directly.

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, AvatarNodeStateHolonDto> _nodeStates = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, CommandHolonDto> _commands = new();

        /// <summary>
        /// ONODEService pushes current aggregate node state every ~5 seconds.
        /// </summary>
        [HttpPut("holons/node-state/{nodeId}")]
        public IActionResult PushNodeState(string nodeId, [FromBody] AvatarNodeStateHolonDto state)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || state == null)
                return BadRequest(new { message = "nodeId and state body are required." });

            state.NodeId = nodeId;
            state.LastSeen = DateTime.UtcNow;
            _nodeStates[nodeId] = state;
            return Ok(new { message = "Node state received." });
        }

        /// <summary>
        /// OPORTAL reads the latest pushed state for a given node (avatar).
        /// Returns 404 if the node has never synced or has not been seen for >5 minutes.
        /// </summary>
        [HttpGet("holons/node-state/{nodeId}")]
        public IActionResult GetNodeState(string nodeId)
        {
            if (!_nodeStates.TryGetValue(nodeId, out var state))
                return NotFound(new { message = "No state on record for this node." });

            if ((DateTime.UtcNow - state.LastSeen).TotalMinutes > 5)
                return NotFound(new { message = "Node state is stale (>5 min)." });

            return Ok(state);
        }

        /// <summary>
        /// OPORTAL posts a command; ONODEService polls for Pending commands.
        /// </summary>
        [HttpPost("holons/commands")]
        public IActionResult PostCommand([FromBody] CommandHolonDto cmd)
        {
            if (cmd == null || string.IsNullOrWhiteSpace(cmd.TargetNodeId))
                return BadRequest(new { message = "Command body with TargetNodeId is required." });

            cmd.Id = string.IsNullOrWhiteSpace(cmd.Id) ? Guid.NewGuid().ToString() : cmd.Id;
            cmd.Status = "Pending";
            cmd.IssuedAt = DateTime.UtcNow;
            _commands[cmd.Id] = cmd;

            // Prune completed commands older than 10 minutes to avoid unbounded growth
            var cutoff = DateTime.UtcNow.AddMinutes(-10);
            foreach (var kv in _commands)
                if (kv.Value.Status is "Done" or "Error" && kv.Value.CompletedAt < cutoff)
                    _commands.TryRemove(kv.Key, out _);

            return Ok(new { commandId = cmd.Id });
        }

        /// <summary>
        /// ONODEService polls for commands pending against its nodeId.
        /// </summary>
        [HttpGet("holons/commands/pending/{nodeId}")]
        public IActionResult GetPendingCommands(string nodeId)
        {
            var pending = _commands.Values
                .Where(c => c.TargetNodeId == nodeId && c.Status == "Pending")
                .OrderBy(c => c.IssuedAt)
                .ToList();
            return Ok(pending);
        }

        /// <summary>
        /// ONODEService updates a command's status (Executing → Done/Error).
        /// OPORTAL polls this to get the result.
        /// </summary>
        [HttpPatch("holons/commands/{commandId}")]
        public IActionResult UpdateCommand(string commandId, [FromBody] CommandStatusUpdateDto update)
        {
            if (!_commands.TryGetValue(commandId, out var cmd))
                return NotFound(new { message = "Command not found." });

            cmd.Status = update.Status;
            cmd.Result = update.Result;
            if (update.Status is "Done" or "Error")
                cmd.CompletedAt = DateTime.UtcNow;

            return Ok(cmd);
        }

        /// <summary>
        /// OPORTAL polls this to read a single command's current status/result.
        /// </summary>
        [HttpGet("holons/commands/{commandId}")]
        public IActionResult GetCommand(string commandId)
        {
            if (!_commands.TryGetValue(commandId, out var cmd))
                return NotFound(new { message = "Command not found." });
            return Ok(cmd);
        }
    }

    public class NodeConfigRequest
    {
        public Dictionary<string, object> Config { get; set; } = new Dictionary<string, object>();
    }

    public class HolonServiceStateDto
    {
        public string Id { get; set; } = "";
        public string Status { get; set; } = "Unknown";
        public int Port { get; set; }
        public double UptimeSeconds { get; set; }
        public int? Pid { get; set; }
    }

    public class HolonMetricsDto
    {
        public int PeersConnected { get; set; }
        public long BytesReadPerSec { get; set; }
        public long BytesWrittenPerSec { get; set; }
        public double RequestsPerSec { get; set; }
    }

    public class AvatarNodeStateHolonDto
    {
        public string NodeId { get; set; } = "";
        public string AvatarId { get; set; } = "";
        public string Version { get; set; } = "";
        public DateTime LastSeen { get; set; }
        public List<HolonServiceStateDto> Services { get; set; } = new();
        public HolonMetricsDto? Metrics { get; set; }
    }

    public class CommandHolonDto
    {
        public string Id { get; set; } = "";
        public string TargetNodeId { get; set; } = "";
        public string IssuedByAvatarId { get; set; } = "";
        public string Command { get; set; } = "";
        public string? Service { get; set; }
        public string? Payload { get; set; }
        public string Status { get; set; } = "Pending";
        public string? Result { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class CommandStatusUpdateDto
    {
        public string Status { get; set; } = "";
        public string? Result { get; set; }
    }
}
