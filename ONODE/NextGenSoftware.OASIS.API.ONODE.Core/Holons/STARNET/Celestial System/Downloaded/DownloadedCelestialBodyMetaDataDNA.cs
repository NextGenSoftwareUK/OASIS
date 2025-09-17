using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedCelestialBodyMetaDataDNA : DownloadedSTARNETHolon, IDownloadedCelestialBody
    {
        public DownloadedCelestialBodyMetaDataDNA() : base("CelestialBodyMetaDataDNAJSON")
        {
            this.HolonType = HolonType.DownloadedCelestialBodyMetaDataDNA;
        }
    }
}