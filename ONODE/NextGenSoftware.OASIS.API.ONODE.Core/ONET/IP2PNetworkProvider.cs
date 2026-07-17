using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{
    /// <summary>
    /// Interface for P2P network providers (Internal ONET or HoloNET)
    /// </summary>
    public interface IP2PNetworkProvider
    {
        /// <summary>
        /// Initialize the P2P network
        /// </summary>
        Task<OASISResult<bool>> InitializeAsync();
        
        /// <summary>
        /// Start the P2P network
        /// </summary>
        Task<OASISResult<bool>> StartNetworkAsync();
        
        /// <summary>
        /// Stop the P2P network
        /// </summary>
        Task<OASISResult<bool>> StopNetworkAsync();
        
        /// <summary>
        /// Get network status
        /// </summary>
        Task<OASISResult<NetworkStatus>> GetNetworkStatusAsync();
        
        /// <summary>
        /// Get connected nodes
        /// </summary>
        Task<OASISResult<List<ONETNode>>> GetConnectedNodesAsync();
        
        /// <summary>
        /// Connect to a specific node
        /// </summary>
        Task<OASISResult<bool>> ConnectToNodeAsync(string nodeId, string endpoint);
        
        /// <summary>
        /// Disconnect from a node
        /// </summary>
        Task<OASISResult<bool>> DisconnectFromNodeAsync(string nodeId);
        
        /// <summary>
        /// Broadcast a message to the network
        /// </summary>
        Task<OASISResult<bool>> BroadcastMessageAsync(string message, Dictionary<string, object> metadata = null);
        
        /// <summary>
        /// Send a message to a specific node
        /// </summary>
        Task<OASISResult<bool>> SendMessageAsync(string nodeId, string message, Dictionary<string, object> metadata = null);
        
        /// <summary>
        /// Get network topology
        /// </summary>
        Task<OASISResult<NetworkTopology>> GetNetworkTopologyAsync();
        
        /// <summary>
        /// Get network health metrics
        /// </summary>
        Task<OASISResult<NetworkHealth>> GetNetworkHealthAsync();
        
        /// <summary>
        /// Event fired when a new node connects
        /// </summary>
        event EventHandler<NodeConnectedEventArgs> NodeConnected;
        
        /// <summary>
        /// Event fired when a node disconnects
        /// </summary>
        event EventHandler<NodeDisconnectedEventArgs> NodeDisconnected;
        
        /// <summary>
        /// Event fired when a message is received
        /// </summary>
        event EventHandler<MessageReceivedEventArgs> MessageReceived;
    }
    
    /// <summary>
    /// Network status information
    /// </summary>
    public class NetworkStatus
    {
        public bool IsRunning { get; set; }
        public int ConnectedNodes { get; set; }
        public string NetworkId { get; set; }
        public DateTime LastActivity { get; set; }
        public double NetworkHealth { get; set; }
    }
    
    /// <summary>
    /// Network topology information
    /// </summary>
    public class NetworkTopology
    {
        public List<ONETNode> Nodes { get; set; } = new List<ONETNode>();
        public List<NetworkConnection> Connections { get; set; } = new List<NetworkConnection>();
        public DateTime LastUpdated { get; set; }
    }
    
    /// <summary>
    /// Network connection between nodes
    /// </summary>
    public class NetworkConnection
    {
        public string FromNodeId { get; set; }
        public string ToNodeId { get; set; }
        public double Latency { get; set; }
        public double Bandwidth { get; set; }
        public bool IsActive { get; set; }
    }
    
    /// <summary>
    /// Network health metrics
    /// </summary>
    public class NetworkHealth
    {
        public double OverallHealth { get; set; }
        public double Latency { get; set; }
        public double Throughput { get; set; }
        public int ActiveConnections { get; set; }
        public int FailedConnections { get; set; }
        public DateTime LastChecked { get; set; }
    }
    
    /// <summary>
    /// Event args for node connected event
    /// </summary>
    public class NodeConnectedEventArgs : EventArgs
    {
        public string NodeId { get; set; }
        public string Endpoint { get; set; }
        public DateTime ConnectedAt { get; set; }
    }
    
    /// <summary>
    /// Event args for node disconnected event
    /// </summary>
    public class NodeDisconnectedEventArgs : EventArgs
    {
        public string NodeId { get; set; }
        public string Reason { get; set; }
        public DateTime DisconnectedAt { get; set; }
    }
    
    /// <summary>
    /// Event args for message received event
    /// </summary>
    public class MessageReceivedEventArgs : EventArgs
    {
        public string FromNodeId { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> Metadata { get; set; }
        public DateTime ReceivedAt { get; set; }
    }
}
