using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Objects.OpenServ
{
    /// <summary>
    /// Response model for OpenSERV agent operations
    /// </summary>
    public class OpenServAgentResponse
    {
        /// <summary>
        /// Success status
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Response data
        /// </summary>
        public string Result { get; set; } = string.Empty;

        /// <summary>
        /// Error message if any
        /// </summary>
        public string Error { get; set; } = string.Empty;

        /// <summary>
        /// Response metadata
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Execution time in milliseconds
        /// </summary>
        public long ExecutionTimeMs { get; set; }
    }
}





























