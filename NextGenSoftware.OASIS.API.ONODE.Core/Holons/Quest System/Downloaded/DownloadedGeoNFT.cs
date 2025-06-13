using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedGeoNFT : DownloadedSTARHolon, IDownloadedGeoNFT
    {
        public DownloadedGeoNFT() : base("GeoNFTDNAJSON")
        {
            this.HolonType = HolonType.DownloadedGeoNFT;
        }
    }
}