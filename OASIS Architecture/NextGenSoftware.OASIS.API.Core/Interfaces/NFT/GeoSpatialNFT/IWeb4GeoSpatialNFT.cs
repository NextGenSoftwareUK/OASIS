using System;
using NextGenSoftware.Utilities;
using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT
{
    public interface IWeb4GeoSpatialNFT : IWeb4NFT
    {
        Guid PlacedByAvatarId { get; set; }
        Guid OriginalWeb4OASISNFTId { get; set; }
        EnumValue<ProviderType> GeoNFTMetaDataProvider { get; set; }
        //EnumValue<ProviderType> OriginalOASISNFTProviderType { get; set; }
        DateTime PlacedOn { get; set; }
        double Lat { get; set; }
        double Long { get; set; }
        bool AllowOtherPlayersToAlsoCollect { get; set; }
        bool PermSpawn { get; set; }
        int GlobalSpawnQuantity { get; set; }
        int PlayerSpawnQuantity { get; set; }
        int RespawnDurationInSeconds { get; set; }
        byte[] Nft3DObject { get; set; }
        string Nft3DObjectURI { get; set; }
        byte[] Nft2DSprite { get; set; }
        string Nft2DSpriteURI { get; set; }

        public string SuccessMessageWhenCollected { get; set; } //TODO: Wire into client, Our World, STARCLI etc to show when player collects this NFT.
        public bool SpawnInSafeZone { get; set; }  //TODO: Wire into client, Our World, STARCLI etc to prevent this NFT from spawning in safe zones if true.
        public bool SpawnNearPlayer { get; set; } //TODO: Wire into client, Our World, STARCLI etc to spawn this NFT near the player if true (e.g. for quest rewards).
        public int SpawnWithinXMetersFromPlayer { get; set; } //TODO: Wire into client, Our World, STARCLI etc to spawn this NFT within X meters from the player if true (e.g. for quest rewards).
        public int SpawnXMetersAwayFromPlayer { get; set; } //TODO: Wire into client, Our World, STARCLI etc to spawn this NFT X meters away from the player if SpawnNearPlayer is true (e.g. for quest rewards).
        public bool IsVisibleOnMap { get; set; }  //TODO: Wire into client, Our World, STARCLI etc to show/hide this NFT on the map if true/false.
        //public bool IsVisibleInInventory { get; set; } = true; //TODO: Wire into client, Our World, STARCLI etc to show/hide this NFT in the player's inventory if true/false.
    }
}