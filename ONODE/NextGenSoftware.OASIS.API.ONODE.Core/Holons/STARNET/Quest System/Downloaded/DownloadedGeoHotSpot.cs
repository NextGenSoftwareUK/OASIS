using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedGeoHotSpot : DownloadedSTARNETHolon
    {
        public DownloadedGeoHotSpot() : base("GeoHotSpotDNAJSON")
        {
            this.HolonType = HolonType.DownloadedGeoHotSpot;
        }
    }
}