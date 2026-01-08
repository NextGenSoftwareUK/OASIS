using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Objects.OpenServ
{
    /// <summary>
    /// Request model for OpenSERV workflow execution
    /// </summary>
    public class OpenServWorkflowRequest
    {
        /// <summary>
        /// Workflow request content
        /// </summary>
        public string WorkflowRequest { get; set; } = string.Empty;

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
        /// Workflow parameters
        /// </summary>
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Context data for the workflow
        /// </summary>
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
    }
}





























