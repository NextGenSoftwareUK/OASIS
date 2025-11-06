
using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request
{
    public interface IPlaceWeb4GeoSpatialNFTRequestBase
    {
        public EnumValue<ProviderType> GeoNFTMetaDataProvider { get; set; }
        Guid PlacedByAvatarId { get; set; } //The Avatar ID that is placing this GeoNFT.
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
        public List<string> Tags { get; set; }
        //public ProviderType ProviderType { get; set; }
    }
}