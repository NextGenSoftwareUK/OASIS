using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using System;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class OASISGeoSpatialNFT : OASISNFT, IOASISGeoSpatialNFT
    {
        public Guid PlacedByAvatarId { get; set; }
        public Guid OriginalOASISNFTId { get; set; }
        public ProviderType OriginalOASISNFTProviderType { get; set; }
        public long Lat { get; set; }
        public long Long { get; set; }

        /// <summary>
        /// If true this NFT will still be visible for other players to collect even if another player has already collected it.
        /// </summary>
        public bool AllowOtherPlayersToAlsoCollect { get; set; } = true;

        /// <summary>
        /// If true this NFT will always be present on the map no matter how many times it is collected by any player.
        /// </summary>
        public bool PermSpawn { get; set; } = true;

        /// <summary>
        /// The number of times this NFT can be collected in total for all players. Set to -1 for infinite. This is only applicable if AllowOtherPlayersToAlsoCollect is set to true.
        /// </summary>
        public int GlobalSpawnQuantity { get; set; } = 1;

        /// <summary>
        /// The number of times this NFT can be collected per player. Set to -1 for infinite. GlobalSpawnQuantity takes priority (if it is 0 then PlayerSpawnQuantity is used).
        /// </summary>
        public int PlayerSpawnQuantity { get; set; } = 0;
    }
}