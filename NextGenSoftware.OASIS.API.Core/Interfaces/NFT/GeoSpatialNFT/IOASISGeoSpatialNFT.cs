using NextGenSoftware.OASIS.API.Core.Enums;
using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT
{
    public interface IOASISGeoSpatialNFT : IOASISNFT
    {
        Guid PlacedByAvatarId { get; set; }
        Guid OriginalOASISNFTId { get; set; }
        ProviderType OriginalOASISNFTProviderType { get; set; }
        long Lat { get; set; }
        long Long { get; set; }
        bool AllowOtherPlayersToAlsoCollect { get; set; }
        bool PermSpawn { get; set; }
        int GlobalSpawnQuantity { get; set; }
        int PlayerSpawnQuantity { get; set; }
    }
}