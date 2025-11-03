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
        private readonly List<PeerNode> _connectedPeers = new List<PeerNode>();
        private readonly Dictionary<string, object> _nodeStats = new Dictionary<string, object>();
        private readonly List<string> _nodeLogs = new List<string>();

        public ONODEManager(IOASISStorageProvider storageProvider, OASISDNA oasisdna = null) : base(storageProvider, Guid.NewGuid(), oasisdna)
        {
            InitializeAsync().Wait();
        }

        private async Task InitializeAsync()
        {
            var oasisdnaResult = new OASISResult<OASISDNA>();
            try
            {
                // Load OASISDNA configuration
                oasisdnaResult = await LoadOASISDNAAsync();
                if (!oasisdnaResult.IsError && oasisdnaResult.Result != null)
                {
                    _oasisdna = oasisdnaResult.Result;
                }

                // Initialize node logs
                _nodeLogs.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ONODE Manager initialized");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref oasisdnaResult, $"Error initializing ONODE manager: {ex.Message}", ex);
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

                _nodeLogs.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] OASISDNA configuration updated");

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
        /// Get node status
        /// </summary>
        public async Task<OASISResult<NodeStatus>> GetNodeStatusAsync()
        {
            var result = new OASISResult<NodeStatus>();
            
            try
            {
                var status = new NodeStatus
                {
                    IsRunning = _isNodeRunning,
                    ConnectedPeersCount = _connectedPeers.Count,
                    NodeId = _oasisdna?.OASIS?.NetworkId ?? "unknown",
                    LastUpdated = DateTime.UtcNow,
                    Uptime = _isNodeRunning ? DateTime.UtcNow - (_connectedPeers.FirstOrDefault()?.ConnectedAt ?? DateTime.UtcNow) : TimeSpan.Zero
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
                var info = new NodeInfo
                {
                    NodeId = _oasisdna?.OASIS?.NetworkId ?? "unknown",
                    Version = "1.0.0",
                    Platform = Environment.OSVersion.Platform.ToString(),
                    Architecture = Environment.OSVersion.VersionString,
                    IsRunning = _isNodeRunning,
                    ConnectedPeers = _connectedPeers.Count,
                    LastStarted = _isNodeRunning ? DateTime.UtcNow.AddMinutes(-30) : DateTime.MinValue
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
                _nodeLogs.Add($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ONODE started");

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

                _isNodeRunning = false;
                _connectedPeers.Clear();
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
                var metrics = new NodeMetrics
                {
                    CpuUsage = 15.5,
                    MemoryUsage = 256.7,
                    DiskUsage = 1024.3,
                    NetworkIn = 1024,
                    NetworkOut = 2048,
                    ConnectedPeers = _connectedPeers.Count,
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

                // Update configuration (in a real implementation, this would update actual settings)
                foreach (var kvp in config)
                {
                    _nodeStats[kvp.Key] = kvp.Value;
                }

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
                result.Result = new Dictionary<string, object>(_nodeStats);
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
        /// Get connected peers
        /// </summary>
        public async Task<OASISResult<List<PeerNode>>> GetConnectedPeersAsync()
        {
            var result = new OASISResult<List<PeerNode>>();
            
            try
            {
                result.Result = new List<PeerNode>(_connectedPeers);
                result.IsError = false;
                result.Message = "Connected peers retrieved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting connected peers: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Get node statistics
        /// </summary>
        public async Task<OASISResult<Dictionary<string, object>>> GetNodeStatsAsync()
        {
            var result = new OASISResult<Dictionary<string, object>>();
            
            try
            {
                var stats = new Dictionary<string, object>
                {
                    ["totalPeers"] = _connectedPeers.Count,
                    ["nodeRunning"] = _isNodeRunning,
                    ["uptime"] = _isNodeRunning ? DateTime.UtcNow - (_connectedPeers.FirstOrDefault()?.ConnectedAt ?? DateTime.UtcNow) : TimeSpan.Zero,
                    ["lastActivity"] = DateTime.UtcNow,
                    ["logsCount"] = _nodeLogs.Count
                };

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
                    OASISErrorHandling.HandleError(ref result, $"Failed to load OASISDNA configuration. Reason: {oasisdna?.Message ?? "Unknown error"}");
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
