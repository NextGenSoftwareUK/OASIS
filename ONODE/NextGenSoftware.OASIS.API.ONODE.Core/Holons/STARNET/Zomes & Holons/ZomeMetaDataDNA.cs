using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class ZomeMetaDataDNA : STARNETHolon//, ISTARZome
    {
        public ZomeMetaDataDNA() : base("ZomeMetaDataDNAJSON")
        {
            this.HolonType = HolonType.ZomeMetaDataDNA;
        }
    }
}