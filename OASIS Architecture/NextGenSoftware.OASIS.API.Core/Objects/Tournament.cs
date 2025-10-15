using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class Tournament
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; }
        public string Description { get; set; }
        public TournamentType TournamentType { get; set; }
        public CompetitionType CompetitionType { get; set; }
        public TournamentStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime RegistrationStart { get; set; }
        public DateTime RegistrationEnd { get; set; }
        public int MaxParticipants { get; set; }
        public int CurrentParticipants { get; set; }
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public List<string> Prerequisites { get; set; } = new List<string>();
        public List<TournamentReward> Rewards { get; set; } = new List<TournamentReward>();
        public List<TournamentParticipant> Participants { get; set; } = new List<TournamentParticipant>();
        public List<TournamentMatch> Matches { get; set; } = new List<TournamentMatch>();
        public Dictionary<string, object> Rules { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum TournamentStatus
    {
        Upcoming,
        Registration,
        Active,
        Completed,
        Cancelled,
        Paused
    }

    public class TournamentParticipant
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid AvatarId { get; set; }
        public string AvatarName { get; set; }
        public string AvatarUsername { get; set; }
        public long Score { get; set; }
        public int Rank { get; set; }
        public bool IsEliminated { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Stats { get; set; } = new Dictionary<string, object>();
    }

    public class TournamentMatch
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid Participant1Id { get; set; }
        public Guid Participant2Id { get; set; }
        public int Round { get; set; }
        public MatchStatus Status { get; set; }
        public long Participant1Score { get; set; }
        public long Participant2Score { get; set; }
        public Guid? WinnerId { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public Dictionary<string, object> MatchData { get; set; } = new Dictionary<string, object>();
    }

    public enum MatchStatus
    {
        Scheduled,
        InProgress,
        Completed,
        Cancelled
    }

    public class TournamentReward
    {
        public string Type { get; set; } // Karma, Experience, Item, Badge, Title, etc.
        public string Name { get; set; }
        public string Description { get; set; }
        public int Value { get; set; }
        public int Position { get; set; } // 1st, 2nd, 3rd, etc.
        public string ItemId { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
}
