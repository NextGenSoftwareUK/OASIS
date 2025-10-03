using System;
using System.ComponentModel.DataAnnotations;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar
{
    /// <summary>
    /// Represents an avatar session in the OASIS SSO System
    /// </summary>
    public class AvatarSession
    {
        /// <summary>
        /// Unique session identifier
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Avatar ID this session belongs to
        /// </summary>
        [Required]
        public Guid AvatarId { get; set; }

        /// <summary>
        /// Name of the service/platform
        /// </summary>
        [Required]
        public string ServiceName { get; set; }

        /// <summary>
        /// Type of service (game, app, website, platform, service)
        /// </summary>
        [Required]
        public string ServiceType { get; set; }

        /// <summary>
        /// Type of device (desktop, mobile, tablet, console, vr)
        /// </summary>
        [Required]
        public string DeviceType { get; set; }

        /// <summary>
        /// Name of the device
        /// </summary>
        [Required]
        public string DeviceName { get; set; }

        /// <summary>
        /// Geographic location
        /// </summary>
        [Required]
        public string Location { get; set; }

        /// <summary>
        /// IP address of the session
        /// </summary>
        [Required]
        public string IpAddress { get; set; }

        /// <summary>
        /// Whether the session is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Last activity timestamp
        /// </summary>
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Login timestamp
        /// </summary>
        public DateTime LoginTime { get; set; } = DateTime.UtcNow;

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
        /// Session creation timestamp
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Session last updated timestamp
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Additional metadata
        /// </summary>
        public string Metadata { get; set; }

        /// <summary>
        /// Security token for the session
        /// </summary>
        public string SecurityToken { get; set; }

        /// <summary>
        /// Session expiration time
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }
}
