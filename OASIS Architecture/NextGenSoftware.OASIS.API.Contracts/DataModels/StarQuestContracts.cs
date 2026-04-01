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
    /// Objective DTO: Title + Description authored by content. <see cref="ProgressSummary"/> is the HUD progress line: set from JSON when the API embeds it (e.g. game lite read model), otherwise computed from <see cref="Dictionaries"/>.
    /// </summary>
    public sealed class StarQuestObjective
    {
        private string _progressSummary = string.Empty;

        public string Id { get; set; } = string.Empty;
        /// <summary>Required UI title. Always used as the objective header in game UI.</summary>
        public string Title { get; set; } = string.Empty;
        /// <summary>Required UI description. Always used in the objective detail body.</summary>
        public string Description { get; set; } = string.Empty;
        public string GameSource { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string CompletedBy { get; set; }
        /// <summary>Requirement and progress dictionaries keyed by game id (e.g. ODOOM, OQUAKE). Matches backend Objective.</summary>
        public StarQuestObjectiveDictionaries Dictionaries { get; set; }

        /// <summary>Progress line for HUD. When set (non-whitespace) from API/JSON, that value is used; otherwise computed from requirement/progress dictionaries (same rules as backend Objective).</summary>
        public string ProgressSummary
        {
            get =>
                !string.IsNullOrWhiteSpace(_progressSummary)
                    ? _progressSummary.Trim()
                    : GetProgressSummaryFromDictionaries();
            set => _progressSummary = string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private string GetProgressSummaryFromDictionaries()
        {
            if (Dictionaries == null)
                return string.Empty;

            var lines = new List<string>();

            static int ParseFirst(List<string> values)
            {
                if (values == null || values.Count == 0) return 0;
                return int.TryParse(values[0], out var parsed) ? parsed : 0;
            }

            static string WithPercent(string phrase, int current, int required)
            {
                var pct = required > 0 ? Math.Min(100, (int)Math.Floor((double)current * 100 / required)) : 0;
                return $"{phrase} ({pct}%)";
            }

            void AddKeyed(string verb, string nounPlural, Dictionary<string, List<string>> need, Dictionary<string, List<string>> progress)
            {
                if (need == null) return;
                foreach (var kv in need)
                {
                    var required = ParseFirst(kv.Value);
                    if (required <= 0) continue;
                    var current = progress != null && progress.TryGetValue(kv.Key, out var p) ? ParseFirst(p) : 0;
                    lines.Add(WithPercent($"{verb} {current}/{required} {nounPlural} in {kv.Key}", current, required));
                }
            }

            AddKeyed("Killed", "monsters", Dictionaries.NeedToKillMonsters, Dictionaries.MonstersKilled);
            AddKeyed("Collected", "keys", Dictionaries.NeedToCollectKeys, Dictionaries.KeysCollected);
            AddKeyed("Collected", "items", Dictionaries.NeedToCollectItems, Dictionaries.ItemsCollected);
            AddKeyed("Collected", "armor", Dictionaries.NeedToCollectArmor, Dictionaries.ArmorCollected);
            AddKeyed("Collected", "ammo", Dictionaries.NeedToCollectAmmo, Dictionaries.AmmoCollected);
            AddKeyed("Collected", "health", Dictionaries.NeedToCollectHealth, Dictionaries.HealthCollected);
            AddKeyed("Collected", "weapons", Dictionaries.NeedToCollectWeapons, Dictionaries.WeaponsCollected);
            AddKeyed("Collected", "powerups", Dictionaries.NeedToCollectPowerups, Dictionaries.PowerupsCollected);
            AddKeyed("Earned", "XP", Dictionaries.NeedToEarnXP, Dictionaries.XPEarnt);
            AddKeyed("Earned", "karma", Dictionaries.NeedToEarnKarma, Dictionaries.KarmaEarnt);
            AddKeyed("Completed", "levels", Dictionaries.NeedToCompleteLevel, Dictionaries.LevelsCompleted);

            if (lines.Count > 0)
                return string.Join(" and ", lines);

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
