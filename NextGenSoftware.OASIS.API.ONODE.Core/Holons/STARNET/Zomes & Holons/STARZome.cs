using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class STARZome : STARNETHolon, ISTARZome
    {
        public STARZome()
        {
            this.HolonType = HolonType.STARZome;
        }

        public ZomeType ZomeType { get; set; }
        public IZome Zome { get; set; }
        public Guid ZomeId { get; set; }
    }
}