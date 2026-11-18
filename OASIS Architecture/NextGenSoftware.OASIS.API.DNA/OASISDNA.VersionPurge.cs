namespace NextGenSoftware.OASIS.API.DNA
{
    /// <summary>
    /// Opt-in purge of every earlier saved version of a DELETED holon. Set in OASIS_DNA.json under OASIS.VersionPurge
    /// (on Railway, the OASIS_DNA_JSON variable). With Enabled false (the default) nothing is ever purged, automatically or on request.
    /// The policy never applies to live holons. How the layers combine, most specific first:
    ///   1. a legal hold on the holon always blocks a purge;
    ///   2. the holon owner's choice (OPORTAL Settings > Security), if the owner has made one;
    ///   3. the calling app's choice (app-owner settings), if the app has made one;
    ///   4. ApplyToAll (default false).
    /// Enabled is the master: false forbids every purge whatever the avatar or app chose.
    /// </summary>
    public class VersionPurgeSettings
    {
        /// <summary>Master switch. Default false: no scheduled purge, no purge operation, no overhead.</summary>
        public bool Enabled { get; set; } = false;
        /// <summary>When true (and Enabled), deleted holons are purged unless their owner or app chose off. Default false: only avatars and apps that opted in.</summary>
        public bool ApplyToAll { get; set; } = false;
        /// <summary>Days after the deletion before earlier versions are purged. 0 = as soon as the worker next runs. Default 30. Fixed per holon when it is deleted.</summary>
        public int PurgeAfterDays { get; set; } = 30;
        /// <summary>Most holons one worker cycle will purge. Default 25. Each cycle makes one query for due entries.</summary>
        public int BatchSize { get; set; } = 25;
        /// <summary>Seconds between worker cycles (also the refresh interval of the in-memory avatar and app switches). Default 300.</summary>
        public int CycleSeconds { get; set; } = 300;
        /// <summary>Days the database host keeps backups, quoted in purge results because OASIS cannot purge backups. 0 (default) = unknown: the result says backups are outside OASIS control.</summary>
        public int BackupRetentionDays { get; set; } = 0;
    }
}
