using System;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar
{
    /// <summary>
    /// Request body for setting the active quest and objective on the logged-in avatar (tracker state). Persisted on AvatarDetail; restored after beam-in.
    /// </summary>
    public class SetActiveQuestRequest
    {
        /// <summary>Quest to track (null or empty = clear).</summary>
        public Guid? ActiveQuestId { get; set; }
        /// <summary>Objective to highlight within the tracked quest (null or empty = clear).</summary>
        public Guid? ActiveObjectiveId { get; set; }
    }
}
