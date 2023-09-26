using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT.Request
{
    public class PlaceGeoSpatialNFTRequestBase : IPlaceGeoSpatialNFTRequestBase
    {
        public long Lat { get; set; }
        public long Long { get; set; }
        public bool AllowOtherPlayersToAlsoCollect { get; set; }
        public bool PermSpawn { get; set; }
        public int GlobalSpawnQuantity { get; set; }
        public int PlayerSpawnQuantity { get; set; }
        public ProviderType ProviderType { get; set; }
    }
}