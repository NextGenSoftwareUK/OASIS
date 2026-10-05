using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IProposal : IHolon
    {
        Guid CreatedByAvatarId { get; set; }
        string CreatedByAvatarName { get; set; }
        DateTime CreatedDate { get; set; }
        Dictionary<Guid, bool> Votes { get; set; }
        string ProposalType { get; set; }
        string ProposalCategory { get; set; }
        int AcceptVotes { get; }
        int RejectVotes { get; }
        int TotalVotes { get; }

        bool HasUserVoted(Guid avatarId);
        bool? GetUserVote(Guid avatarId);
        bool AddVote(Guid avatarId, bool accept);
    }
}


