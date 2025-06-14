using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class OAPP : STARHolon, IOAPP
    {
        public OAPP() : base("OAPPDNAJSON")
        {
            this.HolonType = HolonType.OAPP;
        }

        public OAPPType OAPPType { get; set; }
    }
}