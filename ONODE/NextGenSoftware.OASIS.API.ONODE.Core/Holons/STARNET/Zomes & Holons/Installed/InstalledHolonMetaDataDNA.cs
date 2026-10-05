using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledHolonMetaDataDNA : InstalledSTARNETHolon, IInstalledHolon
    {
        public InstalledHolonMetaDataDNA() : base("HolonMetaDataDNAJSON")
        {
            this.HolonType = HolonType.InstalledHolonMetaDataDNA;
        }
    }
}