using System;
using System.Linq;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class QuestBase : TaskBase, IQuestBase
    {
        public QuestBase(string STARNETDNAJSONName = "QuestBaseDNAJSON") : base(STARNETDNAJSONName)
        {

        }

        [CustomOASISProperty()]
        public Guid ParentMissionId { get; set; }

        [CustomOASISProperty()]
        public int Order { get; set; } //The order that the quest's appear and need to be completed in (stages). Each stage/sub-quest can have 1 or more nfts and/or 1 or more hotspots assigned. Once they are all collected/visited/completed then that sub-quest is complete. Once all sub-quests are complete then the parent quest is complete and so on. Once all quests are complete then the mission is complete.

        public IQuest CurrentSubQuest
        {
            get
            {
                if (CompletedOn != DateTime.MinValue)
                {
                    if (Quests != null && Quests.Count > 0)
                        return Quests.OrderBy(x => x.Order).FirstOrDefault(x => x.CompletedOn == DateTime.MinValue);
                }

                return null;
            }
        }

        public int CurrentSubQuestNumber
        {
            get
            {
                if (CurrentSubQuest != null)
                    return CurrentSubQuest.Order;
                
                else return 0;
            }
        }

        public string SubQuestStatus
        {
            get
            {
                return $"Quest {CurrentSubQuestNumber}/{Quests.Count}";
            }
        }

        public QuestStatus Status { get; set; }
        public QuestType Type { get; set; }
        public QuestDifficulty Difficulty { get; set; }
        public long RewardKarma { get; set; }
        public long RewardXP { get; set; }
        public List<string> Requirements { get; set; } = new List<string>();
        /// <summary>Game (e.g. Doom, Quake) this quest/objective applies to. Used when quest is a sub-quest/objective.</summary>
        public string GameSource { get; set; }
        /// <summary>Item or action required to complete (e.g. Key, Health). Used when quest is a sub-quest/objective.</summary>
        public string ItemRequired { get; set; }
        /// <summary>True when this quest is a checklist item (objective) under a parent; false when it is a full sub-quest that can have its own children. Enables a parent to have both objectives and sub-quests.</summary>
        public bool IsObjective { get; set; }
        public string CompletionNotes { get; set; }


        [CustomOASISProperty()]
        public IList<IQuest> Quests { get; set; } = new List<IQuest>(); //TODO: Dont think is needed now because it is stored in the Dependencies.

        /// <summary>Quest IDs that must be completed before this quest can be started. Persisted in MetaData so LoadHolonsByMetaData and client can read it.</summary>
        [CustomOASISProperty(StoreAsJsonString = true)]
        public List<string> PrerequisiteQuestIds { get; set; } = new List<string>();

        /// <summary>Objectives belonging to this quest (Option B). Persisted as part of the Quest holon via MetaData (CustomOASISProperty + StoreAsJsonString).</summary>
        [CustomOASISProperty(StoreAsJsonString = true)]
        public List<Objective> Objectives { get; set; } = new List<Objective>();

        /// <summary>Explicit implementation for IQuestBase; delegates to Objectives so interface and serialization both work.</summary>
        IList<IObjective> IQuestBase.Objectives
        {
            get => Objectives == null ? new List<IObjective>() : new List<IObjective>(Objectives);
            set => Objectives = value as List<Objective> ?? (value != null ? value.Cast<Objective>().ToList() : new List<Objective>());
        }

        // IQuestObjectiveDictionaries – quest-level requirements and progress (key = game id e.g. ODOOM, OQUAKE)
        public IDictionary<string, IList<string>> NeedToCollectArmor { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> NeedToCollectAmmo { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> NeedToCollectHealth { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> NeedToCollectWeapons { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> NeedToCollectPowerups { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> NeedToCollectItems { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> NeedToCollectKeys { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> NeedToKillMonsters { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> NeedToCompleteInMins { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> NeedToEarnKarma { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> NeedToEarnXP { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> NeedToGoToGeoHotSpots { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> NeedToCompleteLevel { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> NeedToUseWeapons { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> NeedToUsePowerups { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> NeedToVisitLocations { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> NeedToSurviveMins { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> ArmorCollected { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> AmmoCollected { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> HealthCollected { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> WeaponsCollected { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> PowerupsCollected { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> ItemsCollected { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> KeysCollected { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> MonstersKilled { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> TimeStarted { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> TimeEnded { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> TimeTaken { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> KarmaEarnt { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> XPEarnt { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> GeoHotSpotsArrived { get; set; } = new Dictionary<string, IList<string>>();
        public IDictionary<string, IList<string>> LevelsCompleted { get; set; } = new Dictionary<string, IList<string>>();
    }
}