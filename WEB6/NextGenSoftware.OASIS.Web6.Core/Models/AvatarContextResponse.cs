using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.Web6.Core.Models
{
    /// <summary>
    /// Rich avatar context block assembled by StarnetContextManager and returned by GET /v1/context/avatar/{avatarId}.
    /// </summary>
    public class AvatarContextResponse
    {
        public Guid AvatarId { get; set; }
        public string DisplayName { get; set; }
        public int KarmaScore { get; set; }
        public string KarmaLevel { get; set; }
        public List<QuestSummary> ActiveQuests { get; set; } = new List<QuestSummary>();
        public List<string> WorldMemberships { get; set; } = new List<string>();
        public DateTime AssembledAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class QuestSummary
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Progress { get; set; }
    }
}
