using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Agent
{
    /// <summary>
    /// Agent Card - Official A2A Protocol structure for agent discovery
    /// Based on: https://github.com/a2aproject/A2A
    /// </summary>
    public interface IAgentCard
    {
        /// <summary>
        /// Unique agent identifier (Avatar ID)
        /// </summary>
        string AgentId { get; set; }
        
        /// <summary>
        /// Agent name
        /// </summary>
        string Name { get; set; }
        
        /// <summary>
        /// Agent version
        /// </summary>
        string Version { get; set; }
        
        /// <summary>
        /// Agent capabilities (services and skills)
        /// </summary>
        AgentCapabilitiesInfo Capabilities { get; set; }
        
        /// <summary>
        /// Connection information for this agent
        /// </summary>
        AgentConnectionInfo Connection { get; set; }
        
        /// <summary>
        /// Additional metadata (pricing, status, description, etc.)
        /// </summary>
        Dictionary<string, object> Metadata { get; set; }
        
        /// <summary>
        /// Owner Avatar ID - The User avatar that owns this agent
        /// </summary>
        string OwnerAvatarId { get; set; }
    }
    
    /// <summary>
    /// Agent capabilities information
    /// </summary>
    public class AgentCapabilitiesInfo
    {
        /// <summary>
        /// List of services this agent provides
        /// </summary>
        public List<string> Services { get; set; } = new List<string>();
        
        /// <summary>
        /// List of skills this agent has
        /// </summary>
        public List<string> Skills { get; set; } = new List<string>();
    }
    
    /// <summary>
    /// Agent connection information
    /// </summary>
    public class AgentConnectionInfo
    {
        /// <summary>
        /// A2A protocol endpoint URL (e.g., "https://api.oasisweb4.com/api/a2a/jsonrpc")
        /// </summary>
        public string Endpoint { get; set; }
        
        /// <summary>
        /// Protocol version (e.g., "jsonrpc2.0")
        /// </summary>
        public string Protocol { get; set; } = "jsonrpc2.0";
        
        /// <summary>
        /// Authentication scheme information
        /// </summary>
        public AgentAuthInfo Auth { get; set; }
    }
    
    /// <summary>
    /// Agent authentication information
    /// </summary>
    public class AgentAuthInfo
    {
        /// <summary>
        /// Authentication scheme (e.g., "bearer", "oauth2", "apikey")
        /// </summary>
        public string Scheme { get; set; } = "bearer";
        
        /// <summary>
        /// Optional credentials or token endpoint
        /// </summary>
        public string Credentials { get; set; }
    }
}































