#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Common;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{
    public class ONODEManager : OASISManager
    {
        private OASISDNA? _oasisdna;
        private bool _isNodeRunning = false;
        private DateTime _nodeStartTime = DateTime.MinValue;
        private readonly Dictionary<string, object> _nodeConfig = new Dictionary<string, object>();
        private readonly List<string> _nodeLogs = new List<string>();

        // ONETManager is injected so StartNodeAsync can join ONET and peers/stats delegate to it.
        private readonly ONETManager? _onetManager;

        public ONODEManager(IOASISStorageProvider storageProvider, OASISDNA? oasisdna = null, ONETManager? onetManager = null)
            : base(storageProvider, Guid.NewGuid(), oasisdna)
        {
            _onetManager = onetManager;
            _oasisdna = oasisdna;
            _nodeLogs.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ONODE Manager initialized");
        }

        /// <summary>
        /// Get OASISDNA configuration — returns the instance passed to the constructor (sourced from ONETManager).
        /// </summary>
        public Task<OASISResult<OASISDNA>> GetOASISDNAAsync()
        {
            return Task.FromResult(new OASISResult<OASISDNA>
            {
                Result = _oasisdna,
                IsError = false,
                Message = "OASISDNA configuration retrieved successfully"
            });
        }

        /// <summary>
        /// Update OASISDNA in-memory. To also persist the change use ONETManager.UpdateOASISDNAAsync.
        /// </summary>
        public Task<OASISResult<bool>> UpdateOASISDNAAsync(OASISDNA oasisdna)
        {
            _oasisdna = oasisdna;
            _nodeLogs.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] OASISDNA configuration updated");
            return Task.FromResult(new OASISResult<bool> { Result = true, IsError = false, Message = "OASISDNA updated" });
        }

        /// <summary>
        /// Get node status
        /// </summary>
        public async Task<OASISResult<NodeStatus>> GetNodeStatusAsync()
        {
            var result = new OASISResult<NodeStatus>();
            
            try
            {
                int peerCount = 0;
                if (_onetManager != null)
                {
                    var peersResult = await _onetManager.GetConnectedNodesAsync();
                    if (!peersResult.IsError) peerCount = peersResult.Result?.Count ?? 0;
                }

                var status = new NodeStatus
                {
                    IsRunning = _isNodeRunning,
                    ConnectedPeersCount = peerCount,
                    NodeId = _oasisdna?.OASIS?.ONET?.NodeId ?? _oasisdna?.OASIS?.NetworkId ?? "unknown",
                    LastUpdated = DateTime.UtcNow,
                    Uptime = _isNodeRunning ? DateTime.UtcNow - _nodeStartTime : TimeSpan.Zero
                };

                result.Result = status;
                result.IsError = false;
                result.Message = "Node status retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting node status: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Get node information
        /// </summary>
        public async Task<OASISResult<NodeInfo>> GetNodeInfoAsync()
        {
            var result = new OASISResult<NodeInfo>();
            
            try
            {
                int peerCount = 0;
                if (_onetManager != null)
                {
                    var peersResult = await _onetManager.GetConnectedNodesAsync();
                    if (!peersResult.IsError) peerCount = peersResult.Result?.Count ?? 0;
                }

                var info = new NodeInfo
                {
                    NodeId = _oasisdna?.OASIS?.ONET?.NodeId ?? _oasisdna?.OASIS?.NetworkId ?? "unknown",
                    Version = "2.0.0",
                    Platform = Environment.OSVersion.Platform.ToString(),
                    Architecture = Environment.OSVersion.VersionString,
                    IsRunning = _isNodeRunning,
                    ConnectedPeers = peerCount,
                    LastStarted = _isNodeRunning ? _nodeStartTime : DateTime.MinValue
                };

                result.Result = info;
                result.IsError = false;
                result.Message = "Node information retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting node information: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Start ONODE
        /// </summary>
        public async Task<OASISResult<bool>> StartNodeAsync()
        {
            var result = new OASISResult<bool>();
            
            try
            {
                if (_isNodeRunning)
                {
                    OASISErrorHandling.HandleError(ref result, "Node is already running");
                    return result;
                }

                _isNodeRunning = true;
                _nodeStartTime = DateTime.UtcNow;
                _nodeLogs.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ONODE started");

                // Join ONET so this node is discoverable by peers.
                if (_onetManager != null)
                {
                    var netResult = await _onetManager.StartNetworkAsync();
                    if (netResult.IsError)
                        _nodeLogs.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] WARNING: ONET network start failed: {netResult.Message}");
                    else
                        _nodeLogs.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ONET network started successfully");
                }

                result.Result = true;
                result.IsError = false;
                result.Message = "Node started successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error starting node: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Stop ONODE
        /// </summary>
        public async Task<OASISResult<bool>> StopNodeAsync()
        {
            var result = new OASISResult<bool>();
            
            try
            {
                if (!_isNodeRunning)
                {
                    OASISErrorHandling.HandleError(ref result, "Node is not running");
                    return result;
                }

                if (_onetManager != null)
                {
                    var netResult = await _onetManager.StopNetworkAsync();
                    if (netResult.IsError)
                        _nodeLogs.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] WARNING: ONET network stop failed: {netResult.Message}");
                }

                _isNodeRunning = false;
                _nodeLogs.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ONODE stopped");

                result.Result = true;
                result.IsError = false;
                result.Message = "Node stopped successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error stopping node: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Restart ONODE
        /// </summary>
        public async Task<OASISResult<bool>> RestartNodeAsync()
        {
            var result = new OASISResult<bool>();
            
            try
            {
                // Stop the node first
                var stopResult = await StopNodeAsync();
                if (stopResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error stopping node: {stopResult.Message}");
                    return result;
                }

                // Wait a moment
                await Task.Delay(1000);

                // Start the node
                var startResult = await StartNodeAsync();
                if (startResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error starting node: {startResult.Message}");
                    return result;
                }

                result.Result = true;
                result.IsError = false;
                result.Message = "Node restarted successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error restarting node: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Get node performance metrics
        /// </summary>
        public async Task<OASISResult<NodeMetrics>> GetNodeMetricsAsync()
        {
            var result = new OASISResult<NodeMetrics>();
            
            try
            {
                int peerCount = 0;
                if (_onetManager != null)
                {
                    var peersResult = await _onetManager.GetConnectedNodesAsync();
                    if (!peersResult.IsError) peerCount = peersResult.Result?.Count ?? 0;
                }

                var proc = System.Diagnostics.Process.GetCurrentProcess();
                var metrics = new NodeMetrics
                {
                    CpuUsage = 0, // real CPU % requires background sampling; placeholder
                    MemoryUsage = Math.Round(proc.WorkingSet64 / 1024.0 / 1024.0, 1),
                    DiskUsage = 0,
                    NetworkIn = 0,
                    NetworkOut = 0,
                    ConnectedPeers = peerCount,
                    LastUpdated = DateTime.UtcNow
                };

                result.Result = metrics;
                result.IsError = false;
                result.Message = "Node metrics retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting node metrics: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Get node logs
        /// </summary>
        public async Task<OASISResult<List<string>>> GetNodeLogsAsync(int lines = 100)
        {
            var result = new OASISResult<List<string>>();
            
            try
            {
                var logs = _nodeLogs.TakeLast(lines).ToList();
                result.Result = logs;
                result.IsError = false;
                result.Message = "Node logs retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting node logs: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Update node configuration
        /// </summary>
        public async Task<OASISResult<bool>> UpdateNodeConfigAsync(Dictionary<string, object> config)
        {
            var result = new OASISResult<bool>();

            try
            {
                if (config == null || config.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Configuration cannot be null or empty");
                    return result;
                }

                foreach (var kvp in config)
                    _nodeConfig[kvp.Key] = kvp.Value;

                _nodeLogs.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Node configuration updated");
                result.Result = true;
                result.IsError = false;
                result.Message = "Node configuration updated successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error updating node configuration: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Get node configuration
        /// </summary>
        public async Task<OASISResult<Dictionary<string, object>>> GetNodeConfigAsync()
        {
            var result = new OASISResult<Dictionary<string, object>>();

            try
            {
                result.Result = new Dictionary<string, object>(_nodeConfig);
                result.IsError = false;
                result.Message = "Node configuration retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting node configuration: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Get connected peers — delegates to ONETManager so the list reflects real ONET state.
        /// </summary>
        public async Task<OASISResult<List<PeerNode>>> GetConnectedPeersAsync()
        {
            var result = new OASISResult<List<PeerNode>>();

            try
            {
                if (_onetManager != null)
                {
                    var nodesResult = await _onetManager.GetConnectedNodesAsync();
                    result.Result = nodesResult.Result?
                        .Select(n => new PeerNode { Id = n.Id, Address = n.Address, ConnectedAt = n.ConnectedAt, Status = n.Status })
                        .ToList() ?? new List<PeerNode>();
                    result.IsError = nodesResult.IsError;
                    result.Message = nodesResult.IsError ? nodesResult.Message : "Connected peers retrieved successfully";
                }
                else
                {
                    result.Result = new List<PeerNode>();
                    result.IsError = false;
                    result.Message = "No ONET manager available";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting connected peers: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Get node statistics — merges local ONODE state with live ONET network stats.
        /// </summary>
        public async Task<OASISResult<Dictionary<string, object>>> GetNodeStatsAsync()
        {
            var result = new OASISResult<Dictionary<string, object>>();

            try
            {
                var stats = new Dictionary<string, object>
                {
                    ["nodeRunning"] = _isNodeRunning,
                    ["uptime"] = _isNodeRunning ? (object)(DateTime.UtcNow - _nodeStartTime) : TimeSpan.Zero,
                    ["lastActivity"] = DateTime.UtcNow,
                    ["logsCount"] = _nodeLogs.Count,
                    ["nodeId"] = _oasisdna?.OASIS?.ONET?.NodeId ?? ""
                };

                if (_onetManager != null)
                {
                    var netStats = await _onetManager.GetNetworkStatsAsync();
                    if (!netStats.IsError && netStats.Result != null)
                        foreach (var kv in netStats.Result)
                            stats[$"onet_{kv.Key}"] = kv.Value;
                }

                result.Result = stats;
                result.IsError = false;
                result.Message = "Node statistics retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting node statistics: {ex.Message}", ex);
            }

            return result;
        }

    }

    public class NodeStatus
    {
        public bool IsRunning { get; set; }
        public int ConnectedPeersCount { get; set; }
        public string NodeId { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public TimeSpan Uptime { get; set; }
    }

    public class NodeInfo
    {
        public string NodeId { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public string Architecture { get; set; } = string.Empty;
        public bool IsRunning { get; set; }
        public int ConnectedPeers { get; set; }
        public DateTime LastStarted { get; set; }
    }

    public class NodeMetrics
    {
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
        public long NetworkIn { get; set; }
        public long NetworkOut { get; set; }
        public int ConnectedPeers { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    public class PeerNode
    {
        public string Id { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
    }
}
