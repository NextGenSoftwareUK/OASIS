using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedGeoNFTCollection : DownloadedSTARNETHolon, IDownloadedGeoNFTCollection
    {
        public DownloadedGeoNFTCollection() : base("GeoNFTCollectionDNAJSON")
        {
            this.HolonType = HolonType.DownloadedGeoNFTCollection;
        }

        [CustomOASISProperty]
        public NFTCollectionType NFTCollectionType { get; set; }
    }
}