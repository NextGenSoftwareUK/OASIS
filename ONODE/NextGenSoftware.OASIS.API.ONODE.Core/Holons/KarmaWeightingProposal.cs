using System;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    /// <summary>
    /// Karma weighting proposal that extends the generic Proposal holon
    /// Used for voting on karma weightings
    /// </summary>
    public class KarmaWeightingProposal : Proposal, IKarmaWeightingProposal
    {
        public KarmaWeightingProposal() : base()
        {
            ProposalType = "KarmaWeighting";
        }

        [CustomOASISProperty]
        public KarmaTypePositive? PositiveKarmaType { get; set; }

        [CustomOASISProperty]
        public KarmaTypeNegative? NegativeKarmaType { get; set; }

        [CustomOASISProperty]
        public int ProposedWeighting { get; set; }

        [CustomOASISProperty]
        public bool IsPositiveKarma { get; set; } // true for positive, false for negative

        /// <summary>
        /// Gets the karma type string for the proposal category
        /// </summary>
        public string GetKarmaTypeString()
        {
            if (IsPositiveKarma && PositiveKarmaType.HasValue)
                return $"PositiveKarma.{PositiveKarmaType.Value}";
            else if (!IsPositiveKarma && NegativeKarmaType.HasValue)
                return $"NegativeKarma.{NegativeKarmaType.Value}";
            return "Unknown";
        }
    }
}


