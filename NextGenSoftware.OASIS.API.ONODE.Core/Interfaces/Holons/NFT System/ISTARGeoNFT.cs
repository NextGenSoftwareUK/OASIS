using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.ONODE.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Holons
{
    public interface ISTARGeoNFT : ISTARNETHolon
    {
        public NFTType NFTType { get; set; }
        IOASISGeoSpatialNFT GeoNFT { get; set; }
        public Guid GeoNFTId { get; set; }
    }
}