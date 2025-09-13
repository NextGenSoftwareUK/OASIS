
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.NFT
{
    public class MintAndPlaceGeoSpatialNFTRequest : MintNFTTransactionRequest
    {
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
        public string GeoNFTMetaDataProvider { get; set; }
    }
}