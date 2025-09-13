using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using System;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Request
{
    public class PlaceGeoSpatialNFTRequest : PlaceGeoSpatialNFTRequestBase, IPlaceGeoSpatialNFTRequest
    {
        public Guid OriginalOASISNFTId { get; set; } //The OASISNFT ID (if it has been previously minted or imported into The OASIS).
        public ProviderType OriginalOASISNFTOffChainProvider { get; set; } //NOTE: The metadata may have been auto-replicated to other providers so it can be loaded from any of those providers also...

        //public string NFTHash { get; set; } //The hash generated when the NFT was minted.
        //public string NFTURL { get; set; } //The URL the NFT is on (if applicable).
        //public Guid PlacedByAvatarId { get; set; } //The Avatar ID that is placing this GeoNFT.
        //public ProviderType GeoNFTMetaDataProvider { get; set; }
    }
}