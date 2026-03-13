using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    /// <summary>
    /// An objective belonging to a Quest. Requirement and progress are keyed by game id (e.g. ODOOM, OQUAKE).
    /// The Objective (string) property is computed from the requirement dictionaries.
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

        /// <summary>Human-readable description built from the requirement dictionaries. Serialized as "Objective" in JSON.</summary>
        [JsonPropertyName("Objective")]
        public string ObjectiveText
        {
            get
            {
                if (_objectiveStringDirty)
                {
                    _cachedObjectiveString = BuildObjectiveString();
                    _objectiveStringDirty = false;
                }
                return _cachedObjectiveString ?? string.Empty;
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

        public Objective()
        {
            Id = Guid.NewGuid();
        }

        /// <summary>Builds the human-readable Objective string from the requirement dictionaries.</summary>
        public string BuildObjectiveString()
        {
            var phrases = new List<string>();

            void AddPhrases(string label, IDictionary<string, IList<string>> dict)
            {
                if (dict == null) return;
                foreach (var kv in dict.Where(x => x.Value != null && x.Value.Count > 0))
                {
                    var items = string.Join(", ", kv.Value);
                    phrases.Add($"{label} {items} in {kv.Key}");
                }
            }

            void AddPhrasesSingle(string label, IDictionary<string, IList<string>> dict, string suffix = "")
            {
                if (dict == null) return;
                foreach (var kv in dict.Where(x => x.Value != null && x.Value.Count > 0))
                {
                    var val = kv.Value[0];
                    phrases.Add($"{label} {val}{suffix} in {kv.Key}");
                }
            }

            AddPhrases("Kill", NeedToKillMonsters);
            AddPhrases("Collect armor:", NeedToCollectArmor);
            AddPhrases("Collect ammo:", NeedToCollectAmmo);
            AddPhrases("Collect health:", NeedToCollectHealth);
            AddPhrases("Collect weapons:", NeedToCollectWeapons);
            AddPhrases("Collect powerups:", NeedToCollectPowerups);
            AddPhrases("Collect items:", NeedToCollectItems);
            AddPhrases("Collect keys:", NeedToCollectKeys);
            AddPhrasesSingle("Complete within", NeedToCompleteInMins, " mins");
            AddPhrasesSingle("Earn", NeedToEarnKarma, " Karma");
            AddPhrasesSingle("Earn", NeedToEarnXP, " XP");
            AddPhrases("Visit geo hotspots:", NeedToGoToGeoHotSpots);
            AddPhrases("Complete level:", NeedToCompleteLevel);
            AddPhrases("Use weapons:", NeedToUseWeapons);
            AddPhrases("Use powerups:", NeedToUsePowerups);
            AddPhrases("Visit locations:", NeedToVisitLocations);
            AddPhrasesSingle("Survive", NeedToSurviveMins, " mins");

            if (phrases.Count == 0)
                return string.Empty;
            return string.Join(" and ", phrases);
        }

        /// <summary>Call after modifying any requirement dictionary so the Objective string is recomputed.</summary>
        public void InvalidateObjectiveString()
        {
            _objectiveStringDirty = true;
        }
    }
}
