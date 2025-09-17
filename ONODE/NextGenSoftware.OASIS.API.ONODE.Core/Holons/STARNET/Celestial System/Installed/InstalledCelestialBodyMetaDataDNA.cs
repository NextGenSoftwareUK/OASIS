using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledCelestialBodyMetaDataDNA : InstalledSTARNETHolon//, IInstalledCelestialBody
    {
        public InstalledCelestialBodyMetaDataDNA() : base("CelestialBodyMetaDataDNAJSON")
        {
            this.HolonType = HolonType.InstalledCelestialBodyMetaDataDNA;
        }
    }
}