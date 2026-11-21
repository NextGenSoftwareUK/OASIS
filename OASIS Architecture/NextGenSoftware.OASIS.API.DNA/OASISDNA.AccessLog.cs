using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.DNA
{
    /// <summary>
    /// Opt-in record of who READ a holon (ids only, never content). Off by default; when Enabled is false
    /// nothing is recorded, queued or written. Set in OASIS_DNA.json under OASIS.AccessLog (on Railway, edit the
    /// OASIS_DNA_JSON variable). Events are stored in the MongoDBOASIS database (collection AccessLogEvents).
    /// </summary>
    public class AccessLogSettings
    {
        /// <summary>Master switch. Default false: no recording, no queue, no writes, no overhead.</summary>
        public bool Enabled { get; set; } = false;
        /// <summary>App ids (X-OASIS-App-Id header) to record. Empty = every app, once Enabled.</summary>
        public List<string> Apps { get; set; } = new List<string>();
        /// <summary>Holon types to record (e.g. "Holon", a custom type name). Empty = all types.</summary>
        public List<string> HolonTypes { get; set; } = new List<string>();
        /// <summary>Operations to record: LoadHolon, LoadHolonsForParent, LoadAllHolons, LoadHolonsByMetaData, LoadHolonByMetaData, Search. Empty = all read operations.</summary>
        public List<string> Operations { get; set; } = new List<string>();
        /// <summary>Also store the caller's IP address. Off by default (personal data).</summary>
        public bool CaptureIp { get; set; } = false;
        /// <summary>Events older than this are removed automatically (Mongo TTL index). 0 = keep forever (storage grows without limit).</summary>
        public int RetentionDays { get; set; } = 365;
        /// <summary>Most events held in memory waiting to be written. When full new events are dropped and counted, never blocking a read.</summary>
        public int QueueCapacity { get; set; } = 10000;
        /// <summary>Seconds between background writes of the queue.</summary>
        public int FlushIntervalSeconds { get; set; } = 5;
        /// <summary>Most events written per batch.</summary>
        public int BatchSize { get; set; } = 500;
        /// <summary>A bulk read stores ONE event listing at most this many holon ids (plus the total count).</summary>
        public int MaxHolonIdsPerEvent { get; set; } = 200;
    }
}
