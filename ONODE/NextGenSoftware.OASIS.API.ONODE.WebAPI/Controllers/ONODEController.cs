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

        // ── Provider endpoints ────────────────────────────────────────────────────
        // Read-only in local mode; OPORTAL reads directly. Writes go via supervisor.

        /// <summary>
        /// Returns configured OASIS storage providers and their enabled state from OASISDNA.json.
        /// </summary>
        [HttpGet("providers")]
        public IActionResult GetProviders()
        {
            try
            {
                var path = OASISBootLoader.OASISBootLoader.OASISDNA != null
                    ? (Environment.GetEnvironmentVariable("OASISDNA_PATH") ?? "OASISDNA.json")
                    : "OASISDNA.json";

                // Resolve the actual path used by OASIS
                var resolved = Environment.GetEnvironmentVariable("OASISDNA_PATH")
                    ?? Path.Combine(AppContext.BaseDirectory, "OASISDNA.json");

                if (!System.IO.File.Exists(resolved))
                    return Ok(new List<object>());

                var json = System.IO.File.ReadAllText(resolved);
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;

                System.Text.Json.JsonElement providersEl;
                bool found = root.TryGetProperty("OASISDNA", out var oasisdna)
                    ? (oasisdna.TryGetProperty("StorageProviders", out var sp) && sp.TryGetProperty("Providers", out providersEl))
                    : (root.TryGetProperty("StorageProviders", out var sp2) && sp2.TryGetProperty("Providers", out providersEl));

                if (!found) return Ok(new List<object>());

                var list = new List<object>();
                foreach (var p in providersEl.EnumerateArray())
                {
                    list.Add(new
                    {
                        providerType = p.TryGetProperty("ProviderType", out var pt) ? pt.GetString() : "",
                        isEnabled    = p.TryGetProperty("IsEnabled",    out var ie) && ie.GetBoolean(),
                        priority     = p.TryGetProperty("Priority",     out var pr) ? pr.GetInt32() : 0,
                    });
                }
                return Ok(list);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading providers");
                return StatusCode(500, new { message = "Error reading providers", error = ex.Message });
            }
        }

        /// <summary>
        /// Proxies provider enable/disable/priority writes to ONODEService supervisor.
        /// Called by OPORTAL in local mode (same machine). If supervisor unavailable, 404.
        /// </summary>
        [HttpPut("providers/{providerType}/enable")]
        public async Task<IActionResult> EnableProvider(string providerType)
        {
            try
            {
                using var http = new System.Net.Http.HttpClient { BaseAddress = new Uri("http://127.0.0.1:8765/"), Timeout = TimeSpan.FromSeconds(5) };
                var resp = await http.PutAsync($"supervisor/providers/{Uri.EscapeDataString(providerType)}/enable", null);
                if (!resp.IsSuccessStatusCode) return StatusCode((int)resp.StatusCode, await resp.Content.ReadAsStringAsync());
                return Ok(new { message = $"{providerType} enabled. Restart service to apply." });
            }
            catch { return StatusCode(503, new { message = "ONODEService supervisor not reachable." }); }
        }

        [HttpPut("providers/{providerType}/disable")]
        public async Task<IActionResult> DisableProvider(string providerType)
        {
            try
            {
                using var http = new System.Net.Http.HttpClient { BaseAddress = new Uri("http://127.0.0.1:8765/"), Timeout = TimeSpan.FromSeconds(5) };
                var resp = await http.PutAsync($"supervisor/providers/{Uri.EscapeDataString(providerType)}/disable", null);
                if (!resp.IsSuccessStatusCode) return StatusCode((int)resp.StatusCode, await resp.Content.ReadAsStringAsync());
                return Ok(new { message = $"{providerType} disabled." });
            }
            catch { return StatusCode(503, new { message = "ONODEService supervisor not reachable." }); }
        }

        [HttpPut("providers/{providerType}/priority")]
        public async Task<IActionResult> SetProviderPriority(string providerType, [FromBody] JsonElement body)
        {
            try
            {
                using var http = new System.Net.Http.HttpClient { BaseAddress = new Uri("http://127.0.0.1:8765/"), Timeout = TimeSpan.FromSeconds(5) };
                var content = new System.Net.Http.StringContent(body.GetRawText(), System.Text.Encoding.UTF8, "application/json");
                var resp = await http.PutAsync($"supervisor/providers/{Uri.EscapeDataString(providerType)}/priority", content);
                if (!resp.IsSuccessStatusCode) return StatusCode((int)resp.StatusCode, await resp.Content.ReadAsStringAsync());
                return Ok(new { message = $"{providerType} priority updated." });
            }
            catch { return StatusCode(503, new { message = "ONODEService supervisor not reachable." }); }
        }

        // ── Holon Bridge endpoints ────────────────────────────────────────────────
        // Called by ONODEService (push) and OPORTAL-JS (read/command).
        // Node state is stored in memory (last-seen per nodeId) so OPORTAL can read
        // current status from anywhere without reaching the local machine directly.

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, AvatarNodeStateHolonDto> _nodeStates = new();
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, CommandHolonDto> _commands = new();

        // ── Audit log ─────────────────────────────────────────────────────────────
        private static readonly System.Collections.Concurrent.ConcurrentQueue<AuditLogEntryDto> _auditLog = new();
        private const int AuditLogMaxEntries = 1000;

        static void AppendAudit(string nodeId, string avatarId, string action, string? detail = null)
        {
            _auditLog.Enqueue(new AuditLogEntryDto
            {
                NodeId   = nodeId,
                AvatarId = avatarId,
                Action   = action,
                Detail   = detail,
                Timestamp = DateTime.UtcNow
            });
            while (_auditLog.Count > AuditLogMaxEntries)
                _auditLog.TryDequeue(out _);
        }

        // ── Rate limiting ─────────────────────────────────────────────────────────
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (int Count, DateTime WindowStart)> _rateLimits = new();
        private const int RateLimitMax = 10;
        private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(1);

        static bool CheckRateLimit(string nodeId)
        {
            var now = DateTime.UtcNow;
            var entry = _rateLimits.GetOrAdd(nodeId, _ => (0, now));
            if (now - entry.WindowStart > RateLimitWindow)
                entry = (0, now);

            if (entry.Count >= RateLimitMax) return false;
            _rateLimits[nodeId] = (entry.Count + 1, entry.WindowStart);
            return true;
        }

        /// <summary>
        /// ONODEService pushes current aggregate node state every ~5 seconds.
        /// </summary>
        [HttpPut("node-state/{nodeId}")]
        public IActionResult PushNodeState(string nodeId, [FromBody] AvatarNodeStateHolonDto state)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || state == null)
                return BadRequest(new { message = "nodeId and state body are required." });

            state.NodeId = nodeId;
            state.LastSeen = DateTime.UtcNow;
            _nodeStates[nodeId] = state;

            // Push to any OPORTAL browser clients watching this nodeId via WebSocket (local mode)
            _ = Hubs.ONODEWebSocketHub.BroadcastAsync(nodeId, state);

            return Ok(new { message = "Node state received." });
        }

        /// <summary>
        /// OPORTAL reads the latest pushed state for a given node (avatar).
        /// Returns 404 if the node has never synced or has not been seen for >5 minutes.
        /// </summary>
        [HttpGet("node-state/{nodeId}")]
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
        [HttpPost("commands")]
        public IActionResult PostCommand([FromBody] CommandHolonDto cmd)
        {
            if (cmd == null || string.IsNullOrWhiteSpace(cmd.TargetNodeId))
                return BadRequest(new { message = "Command body with TargetNodeId is required." });

            if (!CheckRateLimit(cmd.TargetNodeId))
                return StatusCode(429, new { message = $"Rate limit exceeded. Max {RateLimitMax} commands per minute per node." });

            cmd.Id = string.IsNullOrWhiteSpace(cmd.Id) ? Guid.NewGuid().ToString() : cmd.Id;
            cmd.Status = "Pending";
            cmd.IssuedAt = DateTime.UtcNow;
            _commands[cmd.Id] = cmd;

            AppendAudit(cmd.TargetNodeId, cmd.IssuedByAvatarId, $"Command:{cmd.Command}", cmd.Service ?? cmd.Payload);

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
        [HttpGet("commands/pending/{nodeId}")]
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
        [HttpPatch("commands/{commandId}")]
        public IActionResult UpdateCommand(string commandId, [FromBody] CommandStatusUpdateDto update)
        {
            if (!_commands.TryGetValue(commandId, out var cmd))
                return NotFound(new { message = "Command not found." });

            cmd.Status = update.Status;
            cmd.Result = update.Result;
            if (update.Status is "Done" or "Error")
            {
                cmd.CompletedAt = DateTime.UtcNow;
                AppendAudit(cmd.TargetNodeId, cmd.IssuedByAvatarId,
                    $"CommandResult:{cmd.Command}:{update.Status}", update.Result);
            }

            return Ok(cmd);
        }

        [HttpGet("commands/{commandId}")]
        public IActionResult GetCommand(string commandId)
        {
            if (!_commands.TryGetValue(commandId, out var cmd))
                return NotFound(new { message = "Command not found." });
            return Ok(cmd);
        }

        // ── Audit log endpoint ────────────────────────────────────────────────────

        [HttpGet("audit")]
        public IActionResult GetAuditLog([FromQuery] string? nodeId = null, [FromQuery] int limit = 200)
        {
            var entries = _auditLog
                .Where(e => nodeId == null || e.NodeId == nodeId)
                .TakeLast(Math.Min(limit, 1000))
                .OrderByDescending(e => e.Timestamp)
                .ToList();
            return Ok(entries);
        }

        // ── Active nodes endpoint ─────────────────────────────────────────────────

        [HttpGet("active-nodes")]
        public IActionResult GetActiveNodes()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-5);
            var nodes = _nodeStates.Values
                .Where(n => n.LastSeen >= cutoff)
                .Select(n => new
                {
                    nodeId       = n.NodeId,
                    avatarId     = n.AvatarId,
                    lastSeen     = n.LastSeen,
                    secondsAgo   = (int)(DateTime.UtcNow - n.LastSeen).TotalSeconds,
                    runningCount = n.Services?.Count(s => s.Status == "Running") ?? 0,
                    totalCount   = n.Services?.Count ?? 0,
                    metrics      = n.Metrics
                })
                .OrderBy(n => n.secondsAgo)
                .ToList();
            return Ok(nodes);
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

    public class AuditLogEntryDto
    {
        public string NodeId   { get; set; } = "";
        public string AvatarId { get; set; } = "";
        public string Action   { get; set; } = "";
        public string? Detail  { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class CommandStatusUpdateDto
    {
        public string Status { get; set; } = "";
        public string? Result { get; set; }
    }
}
