using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class LeaderboardEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AvatarId { get; set; }
        public string AvatarName { get; set; }
        public string AvatarUsername { get; set; }
        public CompetitionType CompetitionType { get; set; }
        public long Score { get; set; }
        public int Rank { get; set; }
        public int PreviousRank { get; set; }
        public int RankChange { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public DateTime SeasonStart { get; set; }
        public DateTime SeasonEnd { get; set; }
        public SeasonType SeasonType { get; set; }
        public LeagueType CurrentLeague { get; set; }
        public LeagueType PreviousLeague { get; set; }
        public bool LeaguePromoted { get; set; }
        public bool LeagueDemoted { get; set; }
        public Dictionary<string, object> Stats { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Achievements { get; set; } = new Dictionary<string, object>();
        public List<string> Badges { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
