using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{
    public partial class ONETDiscovery : OASISManager
    {
        private readonly Dictionary<string, DiscoveredNode> _discoveredNodes = new Dictionary<string, DiscoveredNode>();
        private readonly Dictionary<string, DiscoveryMethod> _discoveryMethods = new Dictionary<string, DiscoveryMethod>();
        private readonly List<DiscoveryListener> _discoveryListeners = new List<DiscoveryListener>();
        private readonly Dictionary<string, RoutingEntry> _routingTable = new Dictionary<string, RoutingEntry>();
        private string _localNodeId = string.Empty;
        private bool _isDiscoveryActive = false;

        // Real Kademlia routing table — populated whenever a peer is discovered via any method.
        // Used by PerformIterativeDHTLookupAsync for O(log n) FIND_NODE lookups instead of O(n) BFS gossip.
        private KademliaRoutingTable? _kademliaTable;

        private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

        /// <summary>
        /// Wired by ONETProtocol to call RegisterNodePublicKey whenever a peer's public key is
        /// learned from peer-exchange (/onet/nodes) or bootstrap responses.
        /// </summary>
        public Action<string, string>? OnPeerKeyDiscovered { get; set; }

        /// <summary>
        /// Real, operator-configured bootstrap server URLs (each expected to expose a GET /onet/nodes
        /// endpoint returning a JSON array of NodeInfo). Defaults to empty - there is no public ONET network
        /// running yet, so previously hardcoded hostnames like "bootstrap1.onet.network" were never real,
        /// reachable servers. An empty list now honestly produces "no bootstrap servers configured" instead
        /// of querying fabricated DNS names that don't exist.
        /// </summary>
        public List<string> BootstrapServers { get; set; } = new List<string>();

        /// <summary>
        /// Real, operator-configured ONET registry smart contract address. Empty by default since no such
        /// contract has been deployed yet (see BootstrapServers remarks above for the same reasoning).
        /// </summary>
        public string ONETRegistryContractAddress { get; set; } = string.Empty;

        private readonly Dictionary<string, List<NodeHistory>> _nodeHistory = new Dictionary<string, List<NodeHistory>>();
        private readonly object _nodeHistoryLock = new object();

        private const string PeerCacheFileName = "onet-peers.json";

        private string GetDataDirectory()
            => OASISDNA?.OASIS?.DataDirectory ?? NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.OASISHyperDrive.DataDirectory;

        public ONETDiscovery(IOASISStorageProvider storageProvider, OASISDNA oasisdna = null) : base(storageProvider, oasisdna)
        {
            if (oasisdna?.OASIS?.ONET != null)
            {
                if (oasisdna.OASIS.ONET.BootstrapServers?.Count > 0)
                    BootstrapServers = oasisdna.OASIS.ONET.BootstrapServers;

                if (!string.IsNullOrWhiteSpace(oasisdna.OASIS.ONET.NodeId))
                    _localNodeId = oasisdna.OASIS.ONET.NodeId;
            }
        }

    }

    // Supporting classes
    public class DiscoveryStats
    {
        public int TotalDiscoveredNodes { get; set; }
        public int ActiveNodes { get; set; }
        public int DiscoveryMethods { get; set; }
        public DateTime LastDiscovery { get; set; }
        public double DiscoveryRate { get; set; }
    }

    public class DHTQuery
    {
        public string TargetKey { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public DHTQueryType QueryType { get; set; }
        public int MaxResults { get; set; }
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }

    public enum DHTQueryType
    {
        FindNodes,
        FindValue,
        StoreValue
    }

    // Supporting classes for ONETDiscovery
    public class DiscoveredNode
    {
        public string Id { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public List<string> Capabilities { get; set; } = new List<string>();
        public DateTime DiscoveredAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime LastSeen { get; set; }
        public double Latency { get; set; }
        public int Reliability { get; set; }
        public DiscoveryMethod DiscoveryMethod { get; set; } = new DiscoveryMethod();
    }

    public class DiscoveryMethod
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int Priority { get; set; }
    }

    public class DiscoveryListener
    {
        public virtual async Task OnNodesDiscoveredAsync(List<DiscoveredNode> nodes)
        {
            await Task.CompletedTask;
        }
    }

    public class RoutingEntry
    {
        public string NodeId { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Latency { get; set; }
        public int Reliability { get; set; }
        public DateTime LastSeen { get; set; }
        public bool IsActive { get; set; }
    }

    public class DHTResult
    {
        public bool IsValid { get; set; }
        public NodeInfo? NodeInfo { get; set; }
        public string Value { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class MDNSQuery
    {
        public string ServiceType { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        public int Timeout { get; set; } = 10000;
        public int MaxResults { get; set; } = 10;
    }

    public class MDNSResult
    {
        public string ServiceName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int Port { get; set; }
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
        public DateTime Timestamp { get; set; }
    }

    public class MDNSResponse
    {
        public List<MDNSService> Services { get; set; } = new List<MDNSService>();
    }

    public class MDNSService
    {
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int Port { get; set; }
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }

    public class BlockchainQuery
    {
        public string ContractAddress { get; set; } = string.Empty;
        public string FunctionName { get; set; } = string.Empty;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public string NetworkId { get; set; } = string.Empty;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    }

    public class BlockchainResult
    {
        public bool Success { get; set; }
        public List<NodeInfo> Nodes { get; set; } = new List<NodeInfo>();
        public string TransactionHash { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class BootstrapQuery
    {
        public List<string> BootstrapServers { get; set; } = new List<string>();
        public int Timeout { get; set; } = 10000;
        public TimeSpan TimeoutSpan { get; set; } = TimeSpan.FromSeconds(15);
        public int MaxRetries { get; set; } = 3;
    }

    public class BootstrapResult
    {
        public bool Success { get; set; }
        public List<NodeInfo> Nodes { get; set; } = new List<NodeInfo>();
        public string ServerUsed { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class BootstrapResponse
    {
        public bool Success { get; set; }
        public List<NodeInfo> Nodes { get; set; } = new List<NodeInfo>();
        public string ServerUsed { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public int NodeCount { get; set; }
    }

    public class NodeInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public List<string> Capabilities { get; set; } = new List<string>();
        public DateTime LastSeen { get; set; }
        public bool IsActive { get; set; }
        public string PublicKey { get; set; } = string.Empty;
    }

    public class NodeHistory
    {
        public string NodeId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public bool IsSuccessful { get; set; }
        public double ResponseTime { get; set; }
    }

    public class DHTNode
    {
        public string Address { get; set; } = string.Empty;
        public string NodeId { get; set; } = string.Empty;
        public int Reliability { get; set; }
        public double Latency { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class DHTResponse
    {
        public bool IsValid { get; set; }
        public NodeInfo? NodeInfo { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class ContractResult
    {
        public bool Success { get; set; }
        public object? Data { get; set; }
        public string TransactionHash { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
    }

}
