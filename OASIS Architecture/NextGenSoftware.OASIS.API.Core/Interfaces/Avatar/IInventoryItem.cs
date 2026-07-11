using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using System;
using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IInventoryItem : ISTARNETHolon
    {
        byte[] Image2D { get; set; }
        Uri Image2DURI { get; set; }
        //public string ThumbnailUrl { get; set; }
        byte[] Object3D { get; set; }
        Uri Object3DURI { get; set; }
        
        /// <summary>Stack size. When adding with Stack=true, API increments this if item exists; otherwise new item gets this quantity. Default 1.</summary>
        int Quantity { get; set; }
        
        /// <summary>When adding: if true and item exists by name, increment Quantity; if false and item exists, return error "Item already exists". Default true.</summary>
        bool Stack { get; set; }
        //public bool IsStackable { get; set; } //TODO: Rename to this asap!

        /// <summary>Game/source that added this item (e.g. Quake, OQUAKE). Persisted on the holon.</summary>
        string GameSource { get; set; }
        
        /// <summary>Category of item (e.g. Ammo, Armor, Weapon, KeyItem). Persisted on the holon.</summary>
        //string ItemType { get; set; }
        InventoryItemType ItemType { get; set; }

        /// <summary>NFT ID when this item was minted (e.g. from WEB4 NFTHolon). Persisted so clients can show [NFT] prefix in overlays.</summary>
        //string NftId { get; set; }
        Guid NftId { get; set; }
        public string Rarity { get; set; }
        public int MaxQuantity { get; set; }
        public float Weight { get; set; }
        public bool IsUsable { get; set; }
        public bool IsTradeable { get; set; }
        //public string OwnerAvatarId { get; set; }
        public DateTime AcquiredOn { get; set; }
        public DateTime LastUsedOn { get; set; }
        public Dictionary<string, object> Properties { get; set; }
    }
}