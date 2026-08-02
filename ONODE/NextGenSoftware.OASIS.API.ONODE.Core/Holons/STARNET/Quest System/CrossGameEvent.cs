namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    /// <summary>
    /// An effect that fires in another game when an Objective is completed.
    /// Attached to Objective.CrossGameEventsOnComplete.
    /// </summary>
    public class CrossGameEvent
    {
        /// <summary>
        /// What happens in the target game:
        ///   SpawnEntity  — spawn EntityClassname at TargetMap (SpawnCount times)
        ///   UnlockPortal — activate portal PortalId in TargetGame/TargetMap
        ///   ShowNarration — display NarrationText in TargetGame HUD
        ///   TeleportTo   — trigger OmniverseKernel teleport to TargetGame/TargetMap
        /// </summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>Game the event fires in (e.g. "ODOOM", "OQUAKE").</summary>
        public string TargetGame { get; set; } = string.Empty;

        /// <summary>Map within that game. Null/empty = any map / current map.</summary>
        public string TargetMap { get; set; } = string.Empty;

        // SpawnEntity fields
        public string EntityClassname { get; set; } = string.Empty;
        public int SpawnCount { get; set; } = 1;

        // UnlockPortal fields
        public string PortalId { get; set; } = string.Empty;

        // ShowNarration fields
        public string NarrationText { get; set; } = string.Empty;
    }
}
