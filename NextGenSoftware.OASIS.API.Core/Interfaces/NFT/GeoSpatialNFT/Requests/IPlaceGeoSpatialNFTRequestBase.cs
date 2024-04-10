using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request
{
    public interface IPlaceGeoSpatialNFTRequestBase
    {
        public long Lat { get; set; }
        public long Long { get; set; }
        public bool AllowOtherPlayersToAlsoCollect { get; set; }
        public bool PermSpawn { get; set; }
        public int GlobalSpawnQuantity { get; set; }
        public int PlayerSpawnQuantity { get; set; }
        //public ProviderType ProviderType { get; set; }
    }
}