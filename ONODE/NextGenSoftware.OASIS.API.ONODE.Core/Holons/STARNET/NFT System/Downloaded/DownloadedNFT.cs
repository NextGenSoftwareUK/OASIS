using System;
using NextGenSoftware.OASIS.API.Core.CustomAttrbiutes;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class DownloadedNFT : DownloadedSTARNETHolon, IDownloadedNFT
    {
        public DownloadedNFT() : base("NFTDNAJSON")
        {
            this.HolonType = HolonType.DownloadedNFT;
        }

        [CustomOASISProperty]
        public NFTType NFTType { get; set; }
        //public IOASISGeoSpatialNFT GeoNFT { get; set; } //TODO: Not sure if we need this?

        [CustomOASISProperty]
        public Guid OASISNFTId { get; set; }
    }
}