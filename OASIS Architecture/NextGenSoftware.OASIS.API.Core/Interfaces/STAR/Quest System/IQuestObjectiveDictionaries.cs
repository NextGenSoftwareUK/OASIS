using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    /// <summary>
    /// Requirement and progress dictionaries keyed by game id (e.g. ODOOM, OQUAKE).
    /// Value = list of strings (e.g. type names, counts, identifiers).
    /// Shared by IObjective (per-objective) and IQuestBase (quest-level totals).
    /// </summary>
    public interface IQuestObjectiveDictionaries
    {
        // Requirements ("need to do")
        IDictionary<string, IList<string>> NeedToCollectArmor { get; set; }
        IDictionary<string, IList<string>> NeedToCollectAmmo { get; set; }
        IDictionary<string, IList<string>> NeedToCollectHealth { get; set; }
        IDictionary<string, IList<string>> NeedToCollectWeapons { get; set; }
        IDictionary<string, IList<string>> NeedToCollectPowerups { get; set; }
        IDictionary<string, IList<string>> NeedToCollectItems { get; set; }
        IDictionary<string, IList<string>> NeedToCollectKeys { get; set; }
        IDictionary<string, IList<string>> NeedToKillMonsters { get; set; }
        IDictionary<string, IList<string>> NeedToCompleteInMins { get; set; }
        IDictionary<string, IList<string>> NeedToEarnKarma { get; set; }
        IDictionary<string, IList<string>> NeedToEarnXP { get; set; }
        IDictionary<string, IList<string>> NeedToGoToGeoHotSpots { get; set; }
        IDictionary<string, IList<string>> NeedToCompleteLevel { get; set; }
        // Optional extras
        IDictionary<string, IList<string>> NeedToUseWeapons { get; set; }
        IDictionary<string, IList<string>> NeedToUsePowerups { get; set; }
        IDictionary<string, IList<string>> NeedToVisitLocations { get; set; }
        IDictionary<string, IList<string>> NeedToSurviveMins { get; set; }

        // Progress ("done so far")
        IDictionary<string, IList<string>> ArmorCollected { get; set; }
        IDictionary<string, IList<string>> AmmoCollected { get; set; }
        IDictionary<string, IList<string>> HealthCollected { get; set; }
        IDictionary<string, IList<string>> WeaponsCollected { get; set; }
        IDictionary<string, IList<string>> PowerupsCollected { get; set; }
        IDictionary<string, IList<string>> ItemsCollected { get; set; }
        IDictionary<string, IList<string>> KeysCollected { get; set; }
        IDictionary<string, IList<string>> MonstersKilled { get; set; }
        IDictionary<string, IList<string>> TimeStarted { get; set; }
        IDictionary<string, IList<string>> TimeEnded { get; set; }
        IDictionary<string, IList<string>> TimeTaken { get; set; }
        IDictionary<string, IList<string>> KarmaEarnt { get; set; }
        IDictionary<string, IList<string>> XPEarnt { get; set; }
        IDictionary<string, IList<string>> GeoHotSpotsArrived { get; set; }
        IDictionary<string, IList<string>> LevelsCompleted { get; set; }
    }
}
