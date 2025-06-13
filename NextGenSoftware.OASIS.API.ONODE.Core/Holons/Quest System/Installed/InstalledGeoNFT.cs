using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledGeoNFT : InstalledSTARHolon, IInstalledGeoNFT
    {
        public InstalledGeoNFT() : base("GeoNFTDNAJSON")
        {
            this.HolonType = HolonType.InstalledGeoNFT;
        }
    }
}
