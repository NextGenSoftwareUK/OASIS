using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public class STARGeoNFT : STARNETHolon, ISTARGeoNFT
    {
        public STARGeoNFT()
        {
            this.HolonType = HolonType.STARGeoNFT;
        }

        public NFTType NFTType { get; set; }
        public IOASISGeoSpatialNFT GeoNFT { get; set; } //TODO: Not sure if we need this?
        public Guid GeoNFTId { get; set; }
    }
}