using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    /// <summary>
    /// Generic Proposal holon that can be reused for various voting scenarios
    /// (e.g., Simulation proposals, Karma weighting votes, etc.)
    /// </summary>
    public class Proposal : Holon, IProposal
    {
        public Proposal() : base(HolonType.Proposal)
        {
            Votes = new Dictionary<Guid, bool>();
        }

        [CustomOASISProperty]
        public Guid CreatedByAvatarId { get; set; }

        [CustomOASISProperty]
        public string CreatedByAvatarName { get; set; }

        [CustomOASISProperty]
        public DateTime CreatedDate { get; set; }

        [CustomOASISProperty(StoreAsJsonString = true)]
        public Dictionary<Guid, bool> Votes { get; set; } // AvatarId -> true=accept, false=reject

        [CustomOASISProperty]
        public string ProposalType { get; set; } // e.g., "Simulation", "KarmaWeighting", etc.

        [CustomOASISProperty]
        public string ProposalCategory { get; set; } // e.g., "Universe", "KarmaTypePositive.Helpful", etc.

        // Computed properties (not stored)
        public int AcceptVotes => Votes?.Values.Count(v => v) ?? 0;

        public int RejectVotes => Votes?.Values.Count(v => !v) ?? 0;

        public int TotalVotes => Votes?.Count ?? 0;

        /// <summary>
        /// Checks if a user has already voted on this proposal
        /// </summary>
        public bool HasUserVoted(Guid avatarId)
        {
            return Votes != null && Votes.ContainsKey(avatarId);
        }

        /// <summary>
        /// Gets a user's vote (if they voted)
        /// </summary>
        public bool? GetUserVote(Guid avatarId)
        {
            if (Votes != null && Votes.ContainsKey(avatarId))
                return Votes[avatarId];
            return null;
        }

        /// <summary>
        /// Adds a vote (checks if user already voted)
        /// </summary>
        public bool AddVote(Guid avatarId, bool accept)
        {
            if (Votes == null)
                Votes = new Dictionary<Guid, bool>();

            if (Votes.ContainsKey(avatarId))
                return false; // User already voted

            Votes[avatarId] = accept;
            return true;
        }
    }
}

