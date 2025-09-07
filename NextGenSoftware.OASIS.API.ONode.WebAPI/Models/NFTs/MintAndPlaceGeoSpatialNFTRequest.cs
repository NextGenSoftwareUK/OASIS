
namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.NFT
{
    public class MintAndPlaceGeoSpatialNFTRequest : MintNFTTransactionRequest
    {
        //public string Title { get; set; }
        //public string Description { get; set; }
        //public byte[] Image { get; set; }
        //public string ImageUrl { get; set; }
        //public byte[] Thumbnail { get; set; }
        //public string ThumbnailUrl { get; set; }
        //public decimal Price { get; set; }
        //public string Symbol { get; set; }
        //public decimal Discount { get; set; }
        //public string MemoText { get; set; }
        //public int NumberToMint { get; set; }
        //public bool StoreNFTMetaDataOnChain { get; set; }
        //public Dictionary<string, object> MetaData { get; set; }
        //public string OffChainProvider { get; set; }
        //public string OnChainProvider { get; set; }
        //public string NFTOffChainMetaType { get; set; }
        //public string JSONMetaDataURL { get; set; }
        //public string NFTStandardType { get; set; }
        public long Lat { get; set; }
        public long Long { get; set; }
        public bool AllowOtherPlayersToAlsoCollect { get; set; }
        public bool PermSpawn { get; set; }
        public int GlobalSpawnQuantity { get; set; }
        public int PlayerSpawnQuantity { get; set; }
    }
}