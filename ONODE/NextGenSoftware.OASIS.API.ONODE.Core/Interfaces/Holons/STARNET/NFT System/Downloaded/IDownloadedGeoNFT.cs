using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IDownloadedGeoNFT : IDownloadedSTARNETHolon
    {
        public NFTType NFTType { get; set; }
        public Guid GeoNFTId { get; set; }
    }
}