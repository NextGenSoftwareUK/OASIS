using System.Collections.Generic;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Models;

/// <summary>Game-keyed requirement/progress for a lite objective (same shape as <c>QuestObjectiveDictionariesRequest</c> / native clients).</summary>
public sealed class GameQuestObjectiveDictionariesLite
{
    public Dictionary<string, List<string>>? NeedToCollectArmor { get; set; }
    public Dictionary<string, List<string>>? NeedToCollectAmmo { get; set; }
    public Dictionary<string, List<string>>? NeedToCollectHealth { get; set; }
    public Dictionary<string, List<string>>? NeedToCollectWeapons { get; set; }
    public Dictionary<string, List<string>>? NeedToCollectPowerups { get; set; }
    public Dictionary<string, List<string>>? NeedToCollectItems { get; set; }
    public Dictionary<string, List<string>>? NeedToCollectKeys { get; set; }
    public Dictionary<string, List<string>>? NeedToKillMonsters { get; set; }
    public Dictionary<string, List<string>>? NeedToCompleteInMins { get; set; }
    public Dictionary<string, List<string>>? NeedToEarnKarma { get; set; }
    public Dictionary<string, List<string>>? NeedToEarnXP { get; set; }
    public Dictionary<string, List<string>>? NeedToGoToGeoHotSpots { get; set; }
    public Dictionary<string, List<string>>? NeedToCompleteLevel { get; set; }
    public Dictionary<string, List<string>>? NeedToUseWeapons { get; set; }
    public Dictionary<string, List<string>>? NeedToUsePowerups { get; set; }
    public Dictionary<string, List<string>>? NeedToVisitLocations { get; set; }
    public Dictionary<string, List<string>>? NeedToSurviveMins { get; set; }
    public Dictionary<string, List<string>>? ArmorCollected { get; set; }
    public Dictionary<string, List<string>>? AmmoCollected { get; set; }
    public Dictionary<string, List<string>>? HealthCollected { get; set; }
    public Dictionary<string, List<string>>? WeaponsCollected { get; set; }
    public Dictionary<string, List<string>>? PowerupsCollected { get; set; }
    public Dictionary<string, List<string>>? ItemsCollected { get; set; }
    public Dictionary<string, List<string>>? KeysCollected { get; set; }
    public Dictionary<string, List<string>>? MonstersKilled { get; set; }
    public Dictionary<string, List<string>>? TimeStarted { get; set; }
    public Dictionary<string, List<string>>? TimeEnded { get; set; }
    public Dictionary<string, List<string>>? TimeTaken { get; set; }
    public Dictionary<string, List<string>>? KarmaEarnt { get; set; }
    public Dictionary<string, List<string>>? XPEarnt { get; set; }
    public Dictionary<string, List<string>>? GeoHotSpotsArrived { get; set; }
    public Dictionary<string, List<string>>? LevelsCompleted { get; set; }
}

/// <summary>Objective row for games: authored title/body plus progress line and dictionaries.</summary>
public sealed class GameQuestObjectiveLite
{
    public string Id { get; set; } = string.Empty;
    /// <summary>Short header for list rows (maps to ONODE <see cref="NextGenSoftware.OASIS.API.ONODE.Core.Holons.Objective.Title"/>).</summary>
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsCompleted { get; set; }
    public int ProgressPercent { get; set; }
    /// <summary>Authored detail body (<c>Objective.Description</c>).</summary>
    public string Description { get; set; } = string.Empty;
    /// <summary>Computed progress line from requirement/progress dictionaries (<c>Objective.ProgressSummary</c>).</summary>
    public string ProgressSummary { get; set; } = string.Empty;
    public string GameSource { get; set; } = string.Empty;
    /// <summary>Optional GeoHotSpot id for this objective.</summary>
    public string? LinkedGeoHotSpotId { get; set; }
    /// <summary>Optional cross-app handoff URI for this objective.</summary>
    public string? ExternalHandoffUri { get; set; }
    /// <summary>Optional nested dictionaries (also serialized for JSON parsers that unwrap <c>MetaData</c> / <c>Dictionaries</c>).</summary>
    public GameQuestObjectiveDictionariesLite? Dictionaries { get; set; }
}

/// <summary>Quest row for in-game lists (popup, HUD). Full <see cref="NextGenSoftware.OASIS.API.ONODE.Core.Holons.Quest"/> graph remains in storage for STARNET/OASIS indexing.</summary>
public sealed class GameQuestSummaryLite
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Order { get; set; }
    public int ProgressPercent { get; set; }
    public string GameSource { get; set; } = string.Empty;
    public string ParentQuestId { get; set; } = string.Empty;
    public string ParentMissionId { get; set; } = string.Empty;
    public long RewardKarma { get; set; }
    public long RewardXP { get; set; }
    public List<string> Requirements { get; set; } = new();
    public List<string> PrerequisiteQuestIds { get; set; } = new();
    public string? CompletionNotes { get; set; }
    /// <summary>Optional quest-level GeoHotSpot id.</summary>
    public string? LinkedGeoHotSpotId { get; set; }
    /// <summary>Optional cross-app handoff URI.</summary>
    public string? ExternalHandoffUri { get; set; }
    public List<GameQuestObjectiveLite> Objectives { get; set; } = new();
}
