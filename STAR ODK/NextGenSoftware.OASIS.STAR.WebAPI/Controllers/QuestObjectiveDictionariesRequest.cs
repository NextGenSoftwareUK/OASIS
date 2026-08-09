using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Exceptions;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.Native.EndPoint;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.STAR.WebAPI.Models;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using NextGenSoftware.OASIS.STAR.WebAPI.Helpers;

namespace NextGenSoftware.OASIS.STAR.WebAPI.Controllers
{    public class QuestObjectiveDictionariesRequest
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

    /// <summary>Objective (sub-quest) payload for create or add objective. Matches backend Objective; optional Dictionaries for full requirement/progress.</summary>
}