using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar
{
    /// <summary>
    /// Request model for updating an existing avatar session
    /// </summary>
    public class UpdateSessionRequest
    {
        /// <summary>
        /// Whether the session is active
        /// </summary>
        public bool? IsActive { get; set; }

        /// <summary>
        /// Last activity timestamp
        /// </summary>
        public DateTime? LastActivity { get; set; }

        /// <summary>
        /// Geographic location
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// IP address of the session
        /// </summary>
        public string IpAddress { get; set; }

        /// <summary>
        /// User agent string
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// Platform information
        /// </summary>
        public string Platform { get; set; }

        /// <summary>
        /// Version information
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Additional metadata
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Session expiration time
        /// </summary>
        public DateTime? ExpiresAt { get; set; }

        /// <summary>
        /// Security token for the session
        /// </summary>
        public string SecurityToken { get; set; }
    }
}
