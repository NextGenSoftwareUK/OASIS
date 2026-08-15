using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{
    /// <summary>
    /// ONET Intelligent Routing System - Optimizes message delivery across the network
    /// Implements advanced routing algorithms including Dijkstra, A*, and machine learning-based routing
    /// </summary>
    public partial class ONETRouting : OASISManager
    {
        private readonly Dictionary<string, RoutingNode> _routingTable = new Dictionary<string, RoutingNode>();
        private readonly Dictionary<string, List<RoutingPath>> _pathCache = new Dictionary<string, List<RoutingPath>>();
        private readonly Dictionary<string, NetworkMetrics> _nodeMetrics = new Dictionary<string, NetworkMetrics>();
        private RoutingAlgorithm _algorithm = RoutingAlgorithm.Intelligent;

        /// <summary>
        /// Wired by ONETProtocol to produce an authenticated PING payload.
        /// Returns (localNodeId, base64Signature) or (null, null) when no key is available yet.
        /// </summary>
        public Func<Task<(string? nodeId, string? sig)>>? BuildAuthenticatedPing { get; set; }

        public ONETRouting(IOASISStorageProvider storageProvider, OASISDNA oasisdna = null) : base(storageProvider, oasisdna)
        {
        }

        public async Task InitializeAsync()
        {
            // Initialize routing system
            // Initialize routing algorithms based on OASIS DNA configuration
            await InitializeRoutingAlgorithmsAsync();
        }

        public async Task StartAsync()
        {
            await StartRoutingAsync();
        }

        // Events
        public event EventHandler<RouteUpdatedEventArgs> RouteUpdated;
        public event EventHandler<RouteFailedEventArgs> RouteFailed;

        public async Task StopAsync()
        {
            try
            {
                // Stop routing operations
                LoggingManager.Log("ONET Routing stopped successfully", Logging.LogType.Info);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError($"Error stopping ONET Routing: {ex.Message}", ex);
            }
        }
        private bool _isRoutingActive = false;
        private readonly object _routingLock = new object();
    }

    public enum RoutingAlgorithm
    {
        ShortestPath,
        Dijkstra,
        AStar,
        Intelligent
    }

    public class RoutingTableEntry
    {
        public string Id { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public List<string> Hops { get; set; } = new List<string>();
        public double Quality { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastUsed { get; set; }
    }

    public class RoutingNode
    {
        public string NodeId { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public List<string> Capabilities { get; set; } = new List<string>();
        public double Latency { get; set; }
        public int Reliability { get; set; }
        public DateTime LastSeen { get; set; }
        public bool IsActive { get; set; }
    }

    public class RoutingPath
    {
        public List<string> Nodes { get; set; } = new List<string>();
        public DateTime CalculatedAt { get; set; }
        public bool IsValid { get; set; }
        public int Priority { get; set; }
        public double TotalCost { get; set; }
        public double Efficiency { get; set; }
        public double Stability { get; set; }
    }

    public class RoutingStats
    {
        public int TotalNodes { get; set; }
        public int ActiveNodes { get; set; }
        public int CachedPaths { get; set; }
        public double AverageLatency { get; set; }
        public double AverageReliability { get; set; }
        public string RoutingAlgorithm { get; set; } = string.Empty;
        public DateTime LastOptimization { get; set; }
    }

    public class ErrorLog
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Message { get; set; } = string.Empty;
        public double Severity { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Source { get; set; } = string.Empty;
    }

}
