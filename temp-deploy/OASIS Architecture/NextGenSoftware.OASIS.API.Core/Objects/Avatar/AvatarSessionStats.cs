using System;

namespace NextGenSoftware.OASIS.API.Core.Objects.Avatar
{
    /// <summary>
    /// Represents session statistics for an avatar
    /// </summary>
    public class AvatarSessionStats
    {
        /// <summary>
        /// Total number of sessions ever created
        /// </summary>
        public int TotalSessionsCreated { get; set; }

        /// <summary>
        /// Number of currently active sessions
        /// </summary>
        public int ActiveSessions { get; set; }

        /// <summary>
        /// Number of sessions in the last 24 hours
        /// </summary>
        public int SessionsLast24Hours { get; set; }

        /// <summary>
        /// Number of sessions in the last 7 days
        /// </summary>
        public int SessionsLast7Days { get; set; }

        /// <summary>
        /// Number of sessions in the last 30 days
        /// </summary>
        public int SessionsLast30Days { get; set; }

        /// <summary>
        /// Average session duration in minutes
        /// </summary>
        public double AverageSessionDurationMinutes { get; set; }

        /// <summary>
        /// Longest session duration in minutes
        /// </summary>
        public double LongestSessionDurationMinutes { get; set; }

        /// <summary>
        /// Most used device type
        /// </summary>
        public string MostUsedDeviceType { get; set; }

        /// <summary>
        /// Most used service type
        /// </summary>
        public string MostUsedServiceType { get; set; }

        /// <summary>
        /// Most common location
        /// </summary>
        public string MostCommonLocation { get; set; }

        /// <summary>
        /// Last session timestamp
        /// </summary>
        public DateTime? LastSessionTime { get; set; }

        /// <summary>
        /// Security incidents count
        /// </summary>
        public int SecurityIncidents { get; set; } = 0;

        /// <summary>
        /// Number of different devices used
        /// </summary>
        public int UniqueDevicesUsed { get; set; }

        /// <summary>
        /// Number of different locations
        /// </summary>
        public int UniqueLocationsUsed { get; set; }

        /// <summary>
        /// Trust score (0-100)
        /// </summary>
        public int TrustScore { get; set; } = 100;
    }
}

