using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class STARHolon : STARNETHolon, ISTARHolon
    {
        public STARHolon()
        {
            this.HolonType = HolonType.STARHolon;
        }

        public HolonType HolonType { get; set; }
        public IHolon Holon { get; set; }
        public Guid HolonId { get; set; }
    }
}