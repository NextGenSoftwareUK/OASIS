using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class InstalledGeoHotSpot : InstalledSTARHolon, IInstalledGeoHotSpot
    {
        public InstalledGeoHotSpot() : base("GeoHotSpotDNAJSON")
        {
            this.HolonType = HolonType.InstalledGeoHotSpot;
        }
    }
}