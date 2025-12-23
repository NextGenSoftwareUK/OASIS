using System;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    /// <summary>
    /// Simulation-specific proposal that extends the generic Proposal holon
    /// Used for The Grand Simulation proposals
    /// </summary>
    public class SimulationProposal : Proposal, ISimulationProposal
    {
        public SimulationProposal() : base()
        {
            ProposalType = "Simulation";
        }

        [CustomOASISProperty]
        public Guid ProposedHolonId { get; set; } // The body/space being proposed

        [CustomOASISProperty]
        public HolonType ProposedHolonType { get; set; }

        [CustomOASISProperty]
        public string ProposedHolonName { get; set; }

        [CustomOASISProperty]
        public string ProposedHolonDescription { get; set; }

        [CustomOASISProperty]
        public Guid ParentUniverseId { get; set; } // Parent universe ID (top level for proposals)

        /// <summary>
        /// The actual holon being proposed (loaded separately if needed)
        /// </summary>
        public IHolon ProposedHolon { get; set; }
    }
}


