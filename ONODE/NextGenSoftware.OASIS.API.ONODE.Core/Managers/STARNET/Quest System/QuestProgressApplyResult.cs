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
{    /// <summary>Result of applying quest progress (percent complete, quest finished, events to dispatch, items to grant).</summary>
    public class QuestProgressApplyResult
    {
        public bool QuestCompleted { get; set; }
        public int ObjectivesCompleted { get; set; }
        public int PercentComplete { get; set; }
        public string Message { get; set; }
        /// <summary>
        /// CrossGameEvents the caller should dispatch immediately after receiving this result.
        /// Populated from CrossGameEventsOnComplete (objectives that completed this round) and
        /// CrossGameEventsOnActivate (the next objective that is now active).
        /// The API controller or game-side client is responsible for actually dispatching these
        /// (e.g. via OGEngineClient, or returning them in the response for the game to poll).
        /// </summary>
        public List<CrossGameEvent> CrossGameEventsToDispatch { get; set; } = new List<CrossGameEvent>();
        /// <summary>
        /// InventoryItem Holon IDs to grant to the avatar for objectives and quests completed in this update.
        /// The API controller should call InventoryItemManager.GrantAsync (or equivalent) for each ID.
        /// </summary>
        public List<Guid> InventoryItemsToGrant { get; set; } = new List<Guid>();
    }
}