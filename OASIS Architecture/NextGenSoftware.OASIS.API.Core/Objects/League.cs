using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class League
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Description { get; set; }
        public LeagueType LeagueType { get; set; }
        public CompetitionType CompetitionType { get; set; }
        public long MinScore { get; set; }
        public long MaxScore { get; set; }
        public int MaxPlayers { get; set; }
        public int CurrentPlayers { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? SeasonStart { get; set; }
        public DateTime? SeasonEnd { get; set; }
        public SeasonType SeasonType { get; set; }
        public List<LeagueReward> Rewards { get; set; } = new List<LeagueReward>();
        public Dictionary<string, object> Requirements { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class LeagueReward
    {
        public string Type { get; set; } // Karma, Experience, Item, Badge, Title, etc.
        public string Name { get; set; }
        public string Description { get; set; }
        public int Value { get; set; }
        public string ItemId { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
