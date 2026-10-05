using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface ISimulationProposal : IProposal
    {
        Guid ProposedHolonId { get; set; }
        HolonType ProposedHolonType { get; set; }
        string ProposedHolonName { get; set; }
        string ProposedHolonDescription { get; set; }
        Guid ParentUniverseId { get; set; }
        IHolon ProposedHolon { get; set; }
    }
}


