using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Requests
{
    public interface IUpdateWeb4GeoNFTRequest : IUpdateWeb4NFTRequest
    {
        bool? AllowOtherPlayersToAlsoCollect { get; set; }
        int? GlobalSpawnQuantity { get; set; }
        long? Lat { get; set; }
        long? Long { get; set; }
        byte[] Nft2DSprite { get; set; }
        string Nft2DSpriteURI { get; set; }
        byte[] Nft3DObject { get; set; }
        string Nft3DObjectURI { get; set; }
        bool? PermSpawn { get; set; }
        int? PlayerSpawnQuantity { get; set; }
        int? RespawnDurationInSeconds { get; set; }
    }
}