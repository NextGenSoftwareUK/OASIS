using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.DNA
{
    /// <summary>
    /// Opt-in tamper-evident holon version history (sealed history). Set in OASIS_DNA.json under OASIS.HolonSeal (on Railway, edit the
    /// OASIS_DNA_JSON variable). This is the node-wide master switch and the key; each avatar (OPORTAL Settings > Security) and each
    /// app (app-owner settings) then chooses whether its data is sealed. A save is sealed when Enabled is true, a Key is set, and the
    /// holon owner's switch OR the calling app's switch is on. With Enabled false (the default) nothing is sealed and the save path is unchanged.
    /// Seals are HMAC-SHA256 chained from version to version. The Key is a secret: keep it only in the real deployment's DNA (never commit a value).
    /// </summary>
    public class HolonSealSettings
    {
        /// <summary>Master switch. Default false: no sealing, no checks, no overhead, even if avatars or apps have switched it on.</summary>
        public bool Enabled { get; set; } = false;
        /// <summary>Secret used to make seals. Empty (the default) = sealing unavailable and verify reports "unavailable". Use a long random value (at least 32 characters) and keep it out of source control.</summary>
        public string Key { get; set; } = "";
        /// <summary>Identifier of the current Key, stored with each seal so keys can be rotated. Change it whenever Key changes and move the old key to RetiredKeys.</summary>
        public string KeyId { get; set; } = "k1";
        /// <summary>Earlier keys by id, kept only so old seals still verify after a rotation. Never used to make new seals.</summary>
        public Dictionary<string, string> RetiredKeys { get; set; } = new Dictionary<string, string>();
        /// <summary>Seconds between refreshes of the in-memory avatar and app switches (changes made on another node show up within this time). Default 30.</summary>
        public int RefreshIntervalSeconds { get; set; } = 30;
    }
}
