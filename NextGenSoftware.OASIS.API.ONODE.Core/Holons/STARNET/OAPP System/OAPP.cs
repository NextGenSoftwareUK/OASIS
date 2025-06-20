using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class OAPP : STARNETHolon, IOAPP
    {
        public OAPP() : base("OAPPDNAJSON")
        {
            this.HolonType = HolonType.OAPP;
        }

        public OAPPType OAPPType { get; set; }
    }
}