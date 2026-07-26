using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    /// <summary>
    /// An objective belonging to a Quest. Requirement and progress are keyed by game id (e.g. ODOOM, OQUAKE).
    /// Title/Description are authored text. ProgressSummary is always computed from requirement + progress dictionaries.
    /// </summary>
    public class Objective : IObjective
    {
        [CustomOASISProperty()]
        public Guid Id { get; set; }
        [CustomOASISProperty()]
        public int Order { get; set; }
        private string _cachedObjectiveString;
        private bool _objectiveStringDirty = true;

        [CustomOASISProperty()]
        public bool IsCompleted { get; set; }
        [CustomOASISProperty()]
        public DateTime? CompletedAt { get; set; }
        [CustomOASISProperty()]
        public Guid? CompletedBy { get; set; }
        /// <summary>Completion percentage 0–100 from requirement vs progress dictionaries. Updated when progress is applied.</summary>
        [CustomOASISProperty()]
        public int ProgressPercent { get; set; }

        [CustomOASISProperty()]
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;
        [CustomOASISProperty()]
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>Optional GeoHotSpot to associate with this objective (visit or trigger).</summary>
        [CustomOASISProperty()]
        public Guid? LinkedGeoHotSpotId { get; set; }

        /// <summary>Optional URI for cross-app handoff (next step outside current game).</summary>
        [CustomOASISProperty()]
        public string ExternalHandoffUri { get; set; }

        /// <summary>Computed progress text from Need* + progress dictionaries.</summary>
        [JsonPropertyName("ProgressSummary")]
        public string ProgressSummary
        {
            get
            {
                if (_objectiveStringDirty)
                {
                    _cachedObjectiveString = BuildProgressSummaryString();
                    _objectiveStringDirty = false;
                }
                return _cachedObjectiveString ?? string.Empty;
            }
            set
            {
                _cachedObjectiveString = value ?? string.Empty;
                _objectiveStringDirty = false;
            }
        }

        // Requirements
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> NeedToCollectArmor { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> NeedToCollectAmmo { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> NeedToCollectHealth { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> NeedToCollectWeapons { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> NeedToCollectPowerups { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> NeedToCollectItems { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> NeedToCollectKeys { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> NeedToKillMonsters { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> NeedToCompleteInMins { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> NeedToEarnKarma { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> NeedToEarnXP { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> NeedToGoToGeoHotSpots { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> NeedToCompleteLevel { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> NeedToUseWeapons { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> NeedToUsePowerups { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> NeedToVisitLocations { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> NeedToSurviveMins { get; set; } = new Dictionary<string, IList<string>>();

        // Progress
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> ArmorCollected { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> AmmoCollected { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> HealthCollected { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> WeaponsCollected { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> PowerupsCollected { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> ItemsCollected { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> KeysCollected { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> MonstersKilled { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> TimeStarted { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> TimeEnded { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> TimeTaken { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> KarmaEarnt { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> XPEarnt { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> GeoHotSpotsArrived { get; set; } = new Dictionary<string, IList<string>>();
        [CustomOASISProperty(StoreAsJsonString = true)]
        public IDictionary<string, IList<string>> LevelsCompleted { get; set; } = new Dictionary<string, IList<string>>();

        [CustomOASISProperty]
        public string CompletedMessage { get; set; }

        public Objective()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>Builds the human-readable progress summary from Need* + progress dictionaries (e.g. Killed 1/10 monsters in ODOOM (10%)).</summary>
        public string BuildProgressSummaryString()
        {
            var phrases = new List<string>();

            static int ParseFirst(IList<string>? list)
            {
                if (list == null || list.Count == 0) return 0;
                return int.TryParse(list[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0;
            }

            void AddMonsterLines()
            {
                if (NeedToKillMonsters == null) return;
                foreach (var kv in NeedToKillMonsters)
                {
                    var reqList = kv.Value;
                    if (reqList == null || reqList.Count == 0) continue;
                    if (!int.TryParse(reqList[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var required) || required <= 0) continue;
                    var current = 0;
                    if (MonstersKilled != null && MonstersKilled.TryGetValue(kv.Key, out var pl) && pl != null && pl.Count > 0)
                        int.TryParse(pl[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out current);
                    var pct = Math.Min(100, (int)Math.Floor((double)current * 100 / required));
                    phrases.Add($"Killed {current}/{required} monsters in {kv.Key} ({pct}%)");
                }
            }

            void AddKeyed(string verb, string nounPlural, IDictionary<string, IList<string>>? need, IDictionary<string, IList<string>>? progress)
            {
                if (need == null) return;
                foreach (var kv in need)
                {
                    var game = kv.Key;
                    var reqList = kv.Value;
                    if (reqList == null || reqList.Count == 0) continue;
                    if (!int.TryParse(reqList[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var required) || required <= 0) continue;
                    var current = 0;
                    if (progress != null && progress.TryGetValue(game, out var pl) && pl != null && pl.Count > 0)
                        int.TryParse(pl[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out current);
                    var pct = Math.Min(100, (int)Math.Floor((double)current * 100 / required));
                    phrases.Add($"{verb} {current}/{required} {nounPlural} in {game} ({pct}%)");
                }
            }

            AddMonsterLines();
            AddKeyed("Collected", "armor", NeedToCollectArmor, ArmorCollected);
            AddKeyed("Collected", "ammo", NeedToCollectAmmo, AmmoCollected);
            AddKeyed("Collected", "health", NeedToCollectHealth, HealthCollected);
            AddKeyed("Collected", "weapons", NeedToCollectWeapons, WeaponsCollected);
            AddKeyed("Collected", "powerups", NeedToCollectPowerups, PowerupsCollected);
            AddKeyed("Collected", "items", NeedToCollectItems, ItemsCollected);
            AddKeyed("Collected", "keys", NeedToCollectKeys, KeysCollected);
            AddKeyed("Completed", "levels", NeedToCompleteLevel, LevelsCompleted);
            AddKeyed("Earned", "karma", NeedToEarnKarma, KarmaEarnt);
            AddKeyed("Earned", "XP", NeedToEarnXP, XPEarnt);
            AddKeyed("Visited", "hot spots", NeedToGoToGeoHotSpots, GeoHotSpotsArrived);
            AddKeyed("Used", "weapons", NeedToUseWeapons, WeaponsCollected);
            AddKeyed("Used", "powerups", NeedToUsePowerups, PowerupsCollected);

            void AddPhrasesSingle(string label, IDictionary<string, IList<string>>? dict, string suffix = "")
            {
                if (dict == null) return;
                foreach (var kv in dict.Where(x => x.Value != null && x.Value.Count > 0))
                {
                    var val = kv.Value[0];
                    phrases.Add($"{label} {val}{suffix} in {kv.Key}");
                }
            }

            void AddMultiValueLabel(string label, IDictionary<string, IList<string>>? dict)
            {
                if (dict == null) return;
                foreach (var kv in dict.Where(x => x.Value != null && x.Value.Count > 0))
                {
                    var items = string.Join(", ", kv.Value);
                    phrases.Add($"{label} {items} in {kv.Key}");
                }
            }

            AddPhrasesSingle("Complete within", NeedToCompleteInMins, " mins");
            AddMultiValueLabel("Visit locations:", NeedToVisitLocations);
            AddPhrasesSingle("Survive", NeedToSurviveMins, " mins");

            if (phrases.Count == 0)
                return string.Empty;
            return string.Join(" and ", phrases);
        }

        /// <summary>
        /// Backfill only: when persisted data still has no authored strings (e.g. rows created before MetaData deserialize used case-insensitive binding).
        /// Prefer fixing load path so <see cref="Title"/> and <see cref="Description"/> hydrate from storage.
        /// </summary>
        public void EnsureAuthoredStringsFromComputedProgress()
        {
            var summary = BuildProgressSummaryString();
            if (string.IsNullOrWhiteSpace(summary))
                return;
            if (string.IsNullOrWhiteSpace(Title))
            {
                var headline = summary;
                var idx = summary.IndexOf(" and ", StringComparison.Ordinal);
                if (idx > 0)
                    headline = summary.Substring(0, idx);
                Title = headline;
            }
            if (string.IsNullOrWhiteSpace(Description))
                Description = summary;
        }

        /// <summary>Call after modifying any requirement dictionary so ProgressSummary is recomputed.</summary>
        public void InvalidateObjectiveString()
        {
            _objectiveStringDirty = true;
        }

        /// <summary>Clears all progress dictionaries (Collected, Killed, Earnt, time/level progress, etc.) and completion fields.
        /// Does not modify any Need* requirement dictionaries.</summary>
        public void ResetProgressDictionariesOnly()
        {
            QuestObjectiveProgressDictionaryHelper.ClearProgressOnly(this);
            IsCompleted = false;
            CompletedAt = null;
            CompletedBy = null;
            ProgressPercent = 0;
            InvalidateObjectiveString();
        }
    }

    /// <summary>Clears only <see cref="IQuestObjectiveDictionaries"/> progress properties; leaves Need* unchanged.</summary>
    internal static class QuestObjectiveProgressDictionaryHelper
    {
        internal static void ClearProgressOnly(IQuestObjectiveDictionaries d)
        {
            if (d == null) return;
            d.ArmorCollected = new Dictionary<string, IList<string>>();
            d.AmmoCollected = new Dictionary<string, IList<string>>();
            d.HealthCollected = new Dictionary<string, IList<string>>();
            d.WeaponsCollected = new Dictionary<string, IList<string>>();
            d.PowerupsCollected = new Dictionary<string, IList<string>>();
            d.ItemsCollected = new Dictionary<string, IList<string>>();
            d.KeysCollected = new Dictionary<string, IList<string>>();
            d.MonstersKilled = new Dictionary<string, IList<string>>();
            d.TimeStarted = new Dictionary<string, IList<string>>();
            d.TimeEnded = new Dictionary<string, IList<string>>();
            d.TimeTaken = new Dictionary<string, IList<string>>();
            d.KarmaEarnt = new Dictionary<string, IList<string>>();
            d.XPEarnt = new Dictionary<string, IList<string>>();
            d.GeoHotSpotsArrived = new Dictionary<string, IList<string>>();
            d.LevelsCompleted = new Dictionary<string, IList<string>>();
        }
    }
}