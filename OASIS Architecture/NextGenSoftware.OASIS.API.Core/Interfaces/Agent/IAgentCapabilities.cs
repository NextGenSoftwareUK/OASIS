using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Agent
{
    /// <summary>
    /// Defines the capabilities and services that an agent can provide
    /// </summary>
    public interface IAgentCapabilities
    {
        /// <summary>
        /// List of services this agent can provide (e.g., "data-analysis", "image-generation", "report-writing")
        /// </summary>
        List<string> Services { get; set; }
        
        /// <summary>
        /// Pricing for each service in SOL (or other currency)
        /// Key: Service name, Value: Price per service unit
        /// </summary>
        Dictionary<string, decimal> Pricing { get; set; }
        
        /// <summary>
        /// Agent skills or capabilities (e.g., "Python", "Machine Learning", "API Integration")
        /// </summary>
        List<string> Skills { get; set; }
        
        /// <summary>
        /// Current status of the agent (Available, Busy, Offline, Maintenance)
        /// </summary>
        AgentStatus Status { get; set; }
        
        /// <summary>
        /// Agent reputation score based on completed tasks and payments
        /// </summary>
        decimal ReputationScore { get; set; }
        
        /// <summary>
        /// Maximum concurrent tasks this agent can handle
        /// </summary>
        int MaxConcurrentTasks { get; set; }
        
        /// <summary>
        /// Current number of active tasks
        /// </summary>
        int ActiveTasks { get; set; }
        
        /// <summary>
        /// Agent description or bio
        /// </summary>
        string Description { get; set; }
        
        /// <summary>
        /// Metadata for additional agent-specific information
        /// </summary>
        Dictionary<string, object> Metadata { get; set; }
    }
    
    /// <summary>
    /// Agent availability status
    /// </summary>
    public enum AgentStatus
    {
        Available,      // Ready to accept tasks
        Busy,          // Currently working on tasks
        Offline,        // Not available
        Maintenance    // Under maintenance
    }
}































