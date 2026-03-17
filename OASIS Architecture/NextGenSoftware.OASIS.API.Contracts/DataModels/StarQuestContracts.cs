using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Contracts
{
    /// <summary>
    /// Game-keyed requirement/progress dictionary (key = game id e.g. ODOOM, OQUAKE; value = list of strings).
    /// Matches backend IQuestObjectiveDictionaries. Used for real-time tallies (e.g. 8/20 monsters killed).
    /// </summary>
    public sealed class StarQuestObjectiveDictionaries
    {
        public Dictionary<string, List<string>> NeedToCollectArmor { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> NeedToCollectAmmo { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> NeedToCollectHealth { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> NeedToCollectWeapons { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> NeedToCollectPowerups { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> NeedToCollectItems { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> NeedToCollectKeys { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> NeedToKillMonsters { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> NeedToCompleteInMins { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> NeedToEarnKarma { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> NeedToEarnXP { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> NeedToGoToGeoHotSpots { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> NeedToCompleteLevel { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> NeedToUseWeapons { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> NeedToUsePowerups { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> NeedToVisitLocations { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> NeedToSurviveMins { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> ArmorCollected { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> AmmoCollected { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> HealthCollected { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> WeaponsCollected { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> PowerupsCollected { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> ItemsCollected { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> KeysCollected { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> MonstersKilled { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> TimeStarted { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> TimeEnded { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> TimeTaken { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> KarmaEarnt { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> XPEarnt { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> GeoHotSpotsArrived { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, List<string>> LevelsCompleted { get; set; } = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Objective DTO matching backend Objective: Id, Order, ObjectiveText/Description, IsCompleted, CompletedAt, CompletedBy, and requirement/progress dictionaries.
    /// </summary>
    public sealed class StarQuestObjective
    {
        public string Id { get; set; } = string.Empty;
        /// <summary>Human-readable description. Maps to backend ObjectiveText when set; otherwise backend may compute from requirement dicts.</summary>
        public string Description { get; set; } = string.Empty;
        public string GameSource { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string CompletedBy { get; set; }
        /// <summary>Requirement and progress dictionaries keyed by game id (e.g. ODOOM, OQUAKE). Matches backend Objective.</summary>
        public StarQuestObjectiveDictionaries Dictionaries { get; set; }

        /// <summary>Convenience: first value from NeedToCollectItems[GameSource] or Description. For display when Dictionaries not used.</summary>
        public string SummaryText => GetSummaryText();

        private string GetSummaryText()
        {
            if (Dictionaries?.NeedToCollectItems != null && Dictionaries.NeedToCollectItems.TryGetValue(GameSource, out var list) && list?.Count > 0)
                return string.Join(", ", list);
            if (Dictionaries?.NeedToCollectKeys != null && Dictionaries.NeedToCollectKeys.TryGetValue(GameSource, out var keys) && keys?.Count > 0)
                return "Keys: " + string.Join(", ", keys);
            if (!string.IsNullOrWhiteSpace(Description)) return Description;
            return string.Empty;
        }
    }

    /// <summary>
    /// Quest DTO matching backend Quest/QuestBase: Id, Name, Description, Status, Order, GameSource, reward/requirements, and quest-level dictionaries.
    /// </summary>
    public sealed class StarQuestInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Order { get; set; }
        public string GameSource { get; set; } = string.Empty;
        public List<string> Requirements { get; set; } = new List<string>();
        public long RewardKarma { get; set; }
        public long RewardXP { get; set; }
        public string CompletionNotes { get; set; }
        public string ParentMissionId { get; set; } = string.Empty;
        public string ParentQuestId { get; set; } = string.Empty;
        public List<StarQuestObjective> Objectives { get; set; } = new List<StarQuestObjective>();
        public List<string> PrerequisiteQuestIds { get; set; } = new List<string>();
        /// <summary>Quest-level requirement/progress dictionaries (key = game id). Matches backend QuestBase.</summary>
        public StarQuestObjectiveDictionaries Dictionaries { get; set; }
    }
}
