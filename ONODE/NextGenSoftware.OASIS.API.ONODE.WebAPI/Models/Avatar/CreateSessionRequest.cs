using System.ComponentModel.DataAnnotations;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar
{
    /// <summary>
    /// Request model for creating a new avatar session
    /// </summary>
    public class CreateSessionRequest
    {
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
        /// Session expiration time (optional)
        /// </summary>
        public System.DateTime? ExpiresAt { get; set; }
    }
}
