using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class HolonMetaDataDNA : STARNETHolon//, ISTARHolon
    {
        public HolonMetaDataDNA() : base("HolonMetaDataDNAJSON")
        {
            this.HolonType = HolonType.HolonMetaDataDNA;
        }
    }
}