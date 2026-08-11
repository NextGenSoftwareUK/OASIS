using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{
    public partial class ONETProtocol : OASISManager
    {
        private static ONETProtocol? _instance;
        private static readonly object _lock = new object();

        /// <summary>
        /// Returns the process-wide singleton, creating it on first call.
        /// All callers share one TCP listener so only one port 38470 binding exists per process.
        /// </summary>
        public static ONETProtocol GetInstance(IOASISStorageProvider storageProvider, OASISDNA? oasisdna = null)
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new ONETProtocol(storageProvider, oasisdna);
                }
            }
            return _instance;
        }
        private readonly Dictionary<string, ONETNode> _connectedNodes = new Dictionary<string, ONETNode>();
        private readonly Dictionary<string, ONETBridge> _networkBridges = new Dictionary<string, ONETBridge>();

        private readonly ONETConsensus _consensus;
        private readonly ONETRouting _routing;
        private readonly ONETSecurity _security;
        private readonly ONETDiscovery _discovery;
        private readonly ONETAPIGateway _apiGateway;
        private bool _isNetworkRunning = false;
        private OASISDNA? _oasisdna;
        private string? _localNodeId;

        /// <summary>
        /// TCP port this node listens on for ONET_PING/ONET_PONG connectivity probes (see ONETRouting's
        /// TestNodeConnectivityAsync, which sends ONET_PING and expects ONET_PONG back). Configurable so
        /// multiple local nodes can run side by side during testing.
        /// </summary>
        public int ListenPort { get; set; } = 38470;

        private System.Net.Sockets.TcpListener? _pingResponderListener;
        private System.Threading.CancellationTokenSource? _pingResponderCts;

        // Events
        public event EventHandler<NodeConnectedEventArgs> NodeConnected;
        public event EventHandler<NodeDisconnectedEventArgs> NodeDisconnected;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public async Task<OASISResult<string>> GetNetworkIdAsync()
        {
            var result = new OASISResult<string>();
            try
            {
                result.Result = _oasisdna?.OASIS?.NetworkId ?? "onet-network";
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting network ID: {ex.Message}", ex);
            }
            return result;
        }


        public ONETProtocol(IOASISStorageProvider storageProvider, OASISDNA oasisdna = null) : base(storageProvider, oasisdna)
        {
            _consensus = new ONETConsensus(storageProvider, oasisdna);
            _routing = new ONETRouting(storageProvider, oasisdna);
            _security = new ONETSecurity(storageProvider, oasisdna);
            _discovery = new ONETDiscovery(storageProvider, oasisdna);
            _apiGateway = new ONETAPIGateway(storageProvider, oasisdna);

            // Wire discovery→security key registration so any public key received during
            // peer-exchange is immediately available for PING/HTTP signature verification.
            _discovery.OnPeerKeyDiscovered = RegisterNodePublicKey;

            // Wire routing→security so outbound PINGs are authenticated.
            _routing.BuildAuthenticatedPing = async () =>
            {
                if (_localNodeId == null) return (null, null);
                var sig = await _security.SignMessageForNodeAsync(_localNodeId, "ONET_PING");
                return (_localNodeId, sig);
            };
        }
    }


    public class ONETNode
    {
        public string Id { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime ConnectedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<string> Capabilities { get; set; } = new List<string>();
        public double Latency { get; set; }
        public int Reliability { get; set; }
        public string PublicKey { get; set; } = string.Empty;
    }

    public class ONETBridge
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public List<string> Capabilities { get; set; } = new List<string>();
    }

    public class ONETMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string SourceNodeId { get; set; } = string.Empty;
        public string TargetNodeId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public int Priority { get; set; } = 1;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? DeliveredAt { get; set; }
        public string DeliveryStatus { get; set; } = "Pending";
        public List<string> RoutingPath { get; set; } = new List<string>();
        public SecurityMetadata? SecurityMetadata { get; set; }
    }

    public class ONETTopology
    {
        public List<ONETNode> Nodes { get; set; } = new List<ONETNode>();
        public List<ONETBridge> Bridges { get; set; } = new List<ONETBridge>();
        public double NetworkHealth { get; set; }
        public string ConsensusStatus { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
    }
}
