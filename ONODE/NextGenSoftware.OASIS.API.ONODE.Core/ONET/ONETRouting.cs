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
{    /// <summary>
    /// ONET Intelligent Routing System - Optimizes message delivery across the network.
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

    }
}