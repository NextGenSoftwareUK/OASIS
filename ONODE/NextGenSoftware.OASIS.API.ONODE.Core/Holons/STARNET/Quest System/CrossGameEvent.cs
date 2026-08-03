namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    /// <summary>
    /// An effect that fires in another game when an Objective is activated or completed,
    /// or when its linked GeoHotSpot is triggered.
    /// Attached to Objective.CrossGameEventsOnActivate / CrossGameEventsOnComplete / CrossGameEventsOnGeoHotSpotTriggered.
    /// </summary>
    public class CrossGameEvent
    {
        /// <summary>
        /// What happens in the target game:
        ///   SpawnEntity   — spawn EntityClassname (monster, item, NPC) at TargetMap (SpawnCount times)
        ///   UnlockPortal  — activate portal PortalId in TargetGame/TargetMap
        ///   ShowNarration — display NarrationText in TargetGame HUD
        ///   TeleportTo    — trigger OmniverseKernel teleport to TargetGame/TargetMap
        ///   PlayAudio     — play AudioUrl clip in TargetGame (optional: AudioTitle for HUD label)
        ///   PlayVideo     — play VideoUrl in TargetGame or overlay (optional: VideoTitle)
        ///   OpenWebsite   — open WebsiteUrl in the OASIS browser overlay
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>Game the event fires in (e.g. "ODOOM", "OQUAKE").</summary>
        public string TargetGame { get; set; } = string.Empty;

        /// <summary>Map within that game. Null/empty = any map / current map.</summary>
        public string TargetMap { get; set; } = string.Empty;

        // SpawnEntity fields
        /// <summary>Entity classname to spawn (monster, item, or NPC — e.g. "monster_cyberdemon", "weapon_shotgun", "item_key5").</summary>
        public string EntityClassname { get; set; } = string.Empty;
        public int SpawnCount { get; set; } = 1;
        /// <summary>Category hint for HUD display: "Monster", "Item", or "NPC". Does not affect engine spawn logic.</summary>
        public string EntityCategory { get; set; } = "Monster";

        // UnlockPortal fields
        public string PortalId { get; set; } = string.Empty;

        // ShowNarration fields
        public string NarrationText { get; set; } = string.Empty;

        // PlayAudio fields
        public string AudioUrl { get; set; } = string.Empty;
        public string AudioTitle { get; set; } = string.Empty;

        // PlayVideo fields
        public string VideoUrl { get; set; } = string.Empty;
        public string VideoTitle { get; set; } = string.Empty;

        // OpenWebsite fields
        public string WebsiteUrl { get; set; } = string.Empty;
    }
}
