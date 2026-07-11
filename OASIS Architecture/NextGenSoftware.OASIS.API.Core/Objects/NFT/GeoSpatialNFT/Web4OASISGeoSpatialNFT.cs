using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects.NFT
{
    public class Web4OASISGeoSpatialNFT : Web4NFT, IWeb4GeoSpatialNFT
    {
        public Guid PlacedByAvatarId { get; set; }
        public Guid OriginalWeb4OASISNFTId { get; set; }
        //public ProviderType OriginalOASISNFTProviderType { get; set; }
        //public EnumValue<ProviderType> GeoNFTMetaDataOffChainProvider { get; set; }
        public EnumValue<ProviderType> GeoNFTMetaDataProvider { get; set; }
        public DateTime PlacedOn { get; set; }
        public double Lat { get; set; }
        public double Long { get; set; }

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
        public int RespawnDurationInSeconds { get; set; } = 60;

        public byte[] Nft3DObject { get; set; }
        public string Nft3DObjectURI { get; set; }
        public byte[] Nft2DSprite { get; set; }
        public string Nft2DSpriteURI { get; set; }

        public string SuccessMessageWhenCollected { get; set; } //TODO: Wire into client, Our World, STARCLI etc to show when player collects this NFT.
        public bool SpawnInSafeZone { get; set; } = true; //TODO: Wire into client, Our World, STARCLI etc to prevent this NFT from spawning in safe zones if true.
        public bool SpawnNearPlayer { get; set; } = false; //TODO: Wire into client, Our World, STARCLI etc to spawn this NFT near the player if true (e.g. for quest rewards).
        public int SpawnWithinXMetersFromPlayer { get; set; } = 77; //TODO: Wire into client, Our World, STARCLI etc to spawn this NFT within X meters from the player if true (e.g. for quest rewards).
        public int SpawnXMetersAwayFromPlayer { get; set; } = 77; //TODO: Wire into client, Our World, STARCLI etc to spawn this NFT X meters away from the player if SpawnNearPlayer is true (e.g. for quest rewards).
        public bool IsVisibleOnMap { get; set; } = true; //TODO: Wire into client, Our World, STARCLI etc to show/hide this NFT on the map if true/false.
        //public bool IsVisibleInInventory { get; set; } = true; //TODO: Wire into client, Our World, STARCLI etc to show/hide this NFT in the player's inventory if true/false.
    }
}