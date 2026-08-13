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
{
    /// <summary>
    /// Quest management endpoints for creating, updating, and managing STAR quests.
    /// Quests are interactive challenges and objectives that avatars can complete for rewards and progression.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public partial class QuestsController : STARControllerBase
    {
        private static readonly STARAPI _starAPI = new STARAPI(new STARDNA());
        private readonly ILogger<QuestsController> _logger;

        public QuestsController(ILogger<QuestsController> logger)
        {
            _logger = logger;
        }

        protected override STARAPI GetStarAPI() => _starAPI;






















































    }

    public partial class CreateQuestRequest
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        //public HolonType HolonSubType { get; set; } = HolonType.Quest;
        public QuestType QuestType { get; set; } = QuestType.MainQuest;
        public string SourceFolderPath { get; set; } = "";
        public ISTARNETCreateOptions<Quest, STARNETDNA>? CreateOptions { get; set; } = null;
        /// <summary>Optional quest-level GeoHotSpot (e.g. visit or trigger media/text/link).</summary>
        public Guid? LinkedGeoHotSpotId { get; set; }
        /// <summary>Optional cross-app handoff URI (OPortal, CLI, web, messaging).</summary>
        public string? ExternalHandoffUri { get; set; }
        /// <summary>Optional list of objectives (sub-quests) to create with the quest. Each gets a distinct ID so CompleteQuestObjective can be used.</summary>
        public List<QuestObjectiveRequest>? Objectives { get; set; }
    }

    /// <summary>Game-keyed requirement/progress dictionaries for objectives (matches backend IQuestObjectiveDictionaries). All optional.</summary>
    public partial class QuestObjectiveDictionariesRequest
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
    public partial class QuestObjectiveRequest
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string GameSource { get; set; } = "";
        public int Order { get; set; } = -1;
        public bool IsCompleted { get; set; }
        public DateTime? CompletedAt { get; set; }
        public Guid? CompletedBy { get; set; }
        public Guid? LinkedGeoHotSpotId { get; set; }
        public string? ExternalHandoffUri { get; set; }
        public QuestObjectiveDictionariesRequest? Dictionaries { get; set; }
    }

    public partial class AddQuestObjectiveRequest
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string GameSource { get; set; } = "";
        public int Order { get; set; } = -1;
        public Guid? LinkedGeoHotSpotId { get; set; }
        public string? ExternalHandoffUri { get; set; }
        public QuestObjectiveDictionariesRequest? Dictionaries { get; set; }
    }

    /// <summary>Request body for adding a sub-quest (full child quest).</summary>
    public partial class AddSubQuestRequest
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string GameSource { get; set; } = "";
        public int Order { get; set; } = -1;
        public QuestObjectiveDictionariesRequest? Dictionaries { get; set; }
    }

    public partial class EditQuestRequest
    {
        public STARNETDNA NewDNA { get; set; } = null;
    }

    public partial class CompleteQuestObjectiveRequest
    {
        public string GameSource { get; set; } = "";
        public string CompletionNotes { get; set; } = "";
    }

    /// <summary>Body for POST api/quests/objectives/complete â€” questId and objectiveId may be GUID strings or client slugs (e.g. cross_dimensional_keycard_hunt, doom_red_keycard).</summary>
    public partial class CompleteQuestObjectiveIdentifiersRequest
    {
        public string QuestId { get; set; } = "";
        public string ObjectiveId { get; set; } = "";
        public string GameSource { get; set; } = "";
        public string CompletionNotes { get; set; } = "";
    }

    /// <summary>Realtime quest progress from game (Doom/Quake): kills, XP, pickups by type, level time. Objective dictionaries (NeedToCollectArmor etc.) are keyed by game source (ODOOM, Quake, OQUAKE).</summary>
    public partial class QuestProgressRequest
    {
        /// <summary>Optional avatar profile active objective id; server applies deltas to incomplete objectives in this order: this id first, then by objective Order.</summary>
        public Guid? ActiveObjectiveId { get; set; }
        public string GameSource { get; set; } = "ODOOM";
        public int MonstersKilledDelta { get; set; }
        public int XpEarnedDelta { get; set; }
        public int KeysCollectedDelta { get; set; }
        public int ArmorCollectedDelta { get; set; }
        public int HealthCollectedDelta { get; set; }
        public int WeaponsCollectedDelta { get; set; }
        public int PowerupsCollectedDelta { get; set; }
        public int AmmoCollectedDelta { get; set; }
        public string ItemCollectedName { get; set; } = "";
        public int GenericItemPickup { get; set; }
        public int? LevelTimeSeconds { get; set; }
        /// <summary>Specific monster classname killed this tick (e.g. "cyberdemon"). Empty = any-type kill only.</summary>
        public string? MonsterKilledClassname { get; set; }
    }
}
