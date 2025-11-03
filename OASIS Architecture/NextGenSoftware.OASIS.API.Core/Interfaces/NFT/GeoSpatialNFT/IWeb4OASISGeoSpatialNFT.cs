using System;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT
{
    public interface IWeb4OASISGeoSpatialNFT : IWeb4OASISNFT
    {
        Guid PlacedByAvatarId { get; set; }
        Guid OriginalWeb4OASISNFTId { get; set; }
        EnumValue<ProviderType> GeoNFTMetaDataProvider { get; set; }
        //EnumValue<ProviderType> OriginalOASISNFTProviderType { get; set; }
        DateTime PlacedOn { get; set; }
        long Lat { get; set; }
        long Long { get; set; }
        bool AllowOtherPlayersToAlsoCollect { get; set; }
        bool PermSpawn { get; set; }
        int GlobalSpawnQuantity { get; set; }
        int PlayerSpawnQuantity { get; set; }
        int RespawnDurationInSeconds { get; set; }
        byte[] Nft3DObject { get; set; }
        string Nft3DObjectURI { get; set; }
        byte[] Nft2DSprite { get; set; }
        string Nft2DSpriteURI { get; set; }
    }
}