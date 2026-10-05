using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IKarmaWeightingProposal : IProposal
    {
        KarmaTypePositive? PositiveKarmaType { get; set; }
        KarmaTypeNegative? NegativeKarmaType { get; set; }
        int ProposedWeighting { get; set; }
        bool IsPositiveKarma { get; set; }
        string GetKarmaTypeString();
    }
}


