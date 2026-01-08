using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Agent
{
    /// <summary>
    /// Unified interface for agent services that abstracts A2A, OpenSERV, and SERV infrastructure
    /// Provides a common contract for all agent service types
    /// </summary>
    public interface IUnifiedAgentService
    {
        /// <summary>
        /// Unique service identifier
        /// </summary>
        string ServiceId { get; set; }

        /// <summary>
        /// Service name
        /// </summary>
        string ServiceName { get; set; }

        /// <summary>
        /// Service type (A2A_Agent, OpenSERV_AI_Agent, etc.)
        /// </summary>
        string ServiceType { get; set; }

        /// <summary>
        /// Service description
        /// </summary>
        string Description { get; set; }

        /// <summary>
        /// List of capabilities/services this agent provides
        /// </summary>
        List<string> Capabilities { get; set; }

        /// <summary>
        /// Service endpoint URL
        /// </summary>
        string Endpoint { get; set; }

        /// <summary>
        /// Protocol used (A2A_JSON-RPC_2.0, OpenSERV_HTTP, etc.)
        /// </summary>
        string Protocol { get; set; }

        /// <summary>
        /// Current service status
        /// </summary>
        UnifiedServiceStatus Status { get; set; }

        /// <summary>
        /// Service health information
        /// </summary>
        UnifiedServiceHealth Health { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// Agent ID (if this service represents an A2A agent)
        /// </summary>
        Guid? AgentId { get; set; }

        /// <summary>
        /// Last health check timestamp
        /// </summary>
        DateTime? LastHealthCheck { get; set; }

        /// <summary>
        /// Service registration timestamp
        /// </summary>
        DateTime RegisteredAt { get; set; }

        /// <summary>
        /// Execute a service request
        /// </summary>
        Task<OASISResult<object>> ExecuteServiceAsync(string serviceName, Dictionary<string, object> parameters);

        /// <summary>
        /// Check service health
        /// </summary>
        Task<OASISResult<UnifiedServiceHealth>> CheckHealthAsync();

        /// <summary>
        /// Get service capabilities
        /// </summary>
        Task<OASISResult<List<string>>> GetCapabilitiesAsync();
    }

    /// <summary>
    /// Unified service status
    /// </summary>
    public enum UnifiedServiceStatus
    {
        Available,      // Service is available and ready
        Busy,          // Service is currently busy
        Offline,       // Service is offline
        Maintenance,   // Service is under maintenance
        Unhealthy      // Service is unhealthy
    }

    /// <summary>
    /// Service health information
    /// </summary>
    public class UnifiedServiceHealth
    {
        /// <summary>
        /// Overall health status
        /// </summary>
        public UnifiedServiceStatus Status { get; set; }

        /// <summary>
        /// Health check response time in milliseconds
        /// </summary>
        public long ResponseTimeMs { get; set; }

        /// <summary>
        /// Health check timestamp
        /// </summary>
        public DateTime CheckedAt { get; set; }

        /// <summary>
        /// Error message if unhealthy
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Additional health metrics
        /// </summary>
        public Dictionary<string, object> Metrics { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Whether the service is healthy
        /// </summary>
        public bool IsHealthy => Status == UnifiedServiceStatus.Available || Status == UnifiedServiceStatus.Busy;
    }
}

