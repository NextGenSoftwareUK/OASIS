using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Objects.Avatar
{
    /// <summary>
    /// Represents session management data for an avatar
    /// </summary>
    public class AvatarSessionManagement
    {
        /// <summary>
        /// Total number of sessions
        /// </summary>
        public int TotalSessions { get; set; }

        /// <summary>
        /// Number of active sessions
        /// </summary>
        public int ActiveSessions { get; set; }

        /// <summary>
        /// List of all sessions
        /// </summary>
        public List<AvatarSession> Sessions { get; set; } = new List<AvatarSession>();

        /// <summary>
        /// Current location of the avatar
        /// </summary>
        public string CurrentLocation { get; set; }

        /// <summary>
        /// Last login timestamp
        /// </summary>
        public DateTime? LastLogin { get; set; }

        /// <summary>
        /// Security status
        /// </summary>
        public string SecurityStatus { get; set; } = "Secure";

        /// <summary>
        /// Number of failed login attempts
        /// </summary>
        public int FailedLoginAttempts { get; set; } = 0;

        /// <summary>
        /// Whether two-factor authentication is enabled
        /// </summary>
        public bool TwoFactorEnabled { get; set; } = false;

        /// <summary>
        /// Session timeout in minutes
        /// </summary>
        public int SessionTimeoutMinutes { get; set; } = 30;
    }
}

