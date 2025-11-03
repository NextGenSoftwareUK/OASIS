using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class STARZome : STARNETHolon, ISTARZome
    {
        public STARZome() : base("STARZomeDNAJSON")
        {
            this.HolonType = HolonType.STARZome;
        }

        public ZomeType ZomeType { get; set; }
        public IZome Zome { get; set; }
        public Guid ZomeId { get; set; }
    }
}