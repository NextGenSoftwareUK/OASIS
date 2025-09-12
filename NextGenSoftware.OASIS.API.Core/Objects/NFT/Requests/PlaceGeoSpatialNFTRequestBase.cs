using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Request
{
    public class PlaceGeoSpatialNFTRequestBase : IPlaceGeoSpatialNFTRequestBase
    {
        public EnumValue<ProviderType> GeoNFTMetaDataProvider { get; set; }
        public Guid PlacedByAvatarId { get; set; }  //The Avatar ID that is placing this GeoNFT.
        public long Lat { get; set; }
        public long Long { get; set; }
        public bool AllowOtherPlayersToAlsoCollect { get; set; }
        public bool PermSpawn { get; set; }
        public int GlobalSpawnQuantity { get; set; }
        public int PlayerSpawnQuantity { get; set; }
        public int RespawnDurationInSeconds { get; set; }
        public byte[] Nft3DObject { get; set; }
        public string Nft3DObjectURI { get; set; }
        public byte[] Nft2DSprite { get; set; }
        public string Nft2DSpriteURI { get; set; }
    }
}