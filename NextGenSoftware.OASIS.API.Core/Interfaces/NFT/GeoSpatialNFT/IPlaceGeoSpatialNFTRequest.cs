using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT
{
    public interface IPlaceGeoSpatialNFTRequest : IPlaceGeoSpatialNFTRequestBase
    {
        public Guid OASISNFTId { get; set; }
        public string NFTHash { get; set; }
        public string NFTURL { get; set; }
        public Guid AvatarId { get; set; }
    }
}