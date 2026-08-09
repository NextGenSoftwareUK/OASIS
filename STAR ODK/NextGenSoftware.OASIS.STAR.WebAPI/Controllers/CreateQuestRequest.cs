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
{    public class CreateQuestRequest
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
}