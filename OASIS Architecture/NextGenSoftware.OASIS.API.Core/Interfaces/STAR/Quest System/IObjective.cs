using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    /// <summary>
    /// An objective belonging to a Quest. Has requirement and progress dictionaries keyed by game id.
    /// The Objective (string) property is computed from the requirement dictionaries.
    ///
    /// Cross-game objectives: set GameSource to which game the player must be in to complete this
    /// objective, and MapName to which specific map/level. CrossGameEventsOnComplete fires effects
    /// in other games (spawn entity, unlock portal, narration) when this objective is completed.
    /// </summary>
    public interface IObjective : IQuestObjectiveDictionaries
    {
        Guid Id { get; set; }
        int Order { get; set; }
        string Title { get; set; }
        string Description { get; set; }
        /// <summary>Computed progress summary built from requirement/progress dictionaries (e.g. "Killed 1/10 monsters in ODOOM (10%)").</summary>
        string ProgressSummary { get; }
        bool IsCompleted { get; set; }
        DateTime? CompletedAt { get; set; }
        Guid? CompletedBy { get; set; }
        /// <summary>Optional GeoHotSpot to visit or trigger for this objective (e.g. media/text/link hotspot).</summary>
        Guid? LinkedGeoHotSpotId { get; set; }
        /// <summary>Optional URI for cross-app handoff (STAR CLI, OPortal, Telegram, web task, etc.).</summary>
        string ExternalHandoffUri { get; set; }
        /// <summary>Primary game this objective is completed in (e.g. "ODOOM", "OQUAKE"). Used by HUD tracker and game-side polling.</summary>
        string GameSource { get; set; }
        /// <summary>Specific map/level within GameSource where this objective happens (e.g. "E1M3", "e2m3"). Empty = any map.</summary>
        string MapName { get; set; }
    }
}
