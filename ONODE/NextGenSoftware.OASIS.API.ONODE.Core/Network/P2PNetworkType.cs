using System;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Network
{
    /// <summary>
    /// Defines the type of P2P network implementation for ONET
    /// </summary>
    public enum P2PNetworkType
    {
        /// <summary>
        /// Internal P2P network implementation (current ONET implementation)
        /// Uses custom ONET protocol, consensus, routing, and security mechanisms
        /// </summary>
        Internal,
        
        /// <summary>
        /// HoloNET P2P network implementation
        /// Uses Holochain's native P2P capabilities through HoloNET client
        /// Leverages Holochain's built-in consensus, routing, and security features
        /// </summary>
        HoloNET
    }
}
