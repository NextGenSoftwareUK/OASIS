using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

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