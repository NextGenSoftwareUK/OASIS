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
{    public class QuestProgressRequest
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