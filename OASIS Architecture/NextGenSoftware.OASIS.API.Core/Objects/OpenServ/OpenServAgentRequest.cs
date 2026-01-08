using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Objects.OpenServ
{
    /// <summary>
    /// Request model for OpenSERV agent operations
    /// </summary>
    public class OpenServAgentRequest
    {
        /// <summary>
        /// OpenSERV agent ID
        /// </summary>
        public string AgentId { get; set; } = string.Empty;

        /// <summary>
        /// OpenSERV endpoint URL
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// API key for authentication
        /// </summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>
        /// Input data for the agent
        /// </summary>
        public string Input { get; set; } = string.Empty;

        /// <summary>
        /// Context data for the agent
        /// </summary>
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Additional parameters
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
    }
}





























