using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Managers;
using NextGenSoftware.OASIS.STAR.DNA;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
namespace NextGenSoftware.OASIS.API.ONODE.Core.Managers
{    /// <summary>In-game progress delta for ApplyQuestProgressAsync (kills, XP, pickups by type, level time). Matches Objective progress dictionaries: ArmorCollected, HealthCollected, WeaponsCollected, PowerupsCollected, AmmoCollected, ItemsCollected, KeysCollected.</summary>
    public class QuestProgressDelta
    {
        /// <summary>Optional profile active objective; when set, that incomplete objective is updated before others (then Order, Id). Omit when the caller does not specify one.</summary>
        public Guid? ActiveObjectiveId { get; set; }
        public int MonstersKilledDelta { get; set; }
        /// <summary>Classname of the specific monster killed (e.g. "cyberdemon", "cacodemon"). When set, matched against NeedToKillMonstersByType requirements. Leave empty when classname is unknown (falls back to NeedToKillMonsters any-type tracking).</summary>
        public string MonsterKilledClassname { get; set; }
        public int XpEarnedDelta { get; set; }
        public int KeysCollectedDelta { get; set; }
        public int ArmorCollectedDelta { get; set; }
        public int HealthCollectedDelta { get; set; }
        public int WeaponsCollectedDelta { get; set; }
        public int PowerupsCollectedDelta { get; set; }
        public int AmmoCollectedDelta { get; set; }
        public string ItemCollectedName { get; set; }
        public int GenericItemPickup { get; set; }
        public int? LevelTimeSeconds { get; set; }
    }
}