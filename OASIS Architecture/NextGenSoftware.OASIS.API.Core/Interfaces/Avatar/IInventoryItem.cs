using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IInventoryItem : ISTARNETHolon
    {
        byte[] Image2D { get; set; }
        Uri Image2DURI { get; set; }
        byte[] Object3D { get; set; }
        Uri Object3DURI { get; set; }
        /// <summary>Stack size. When adding with Stack=true, API increments this if item exists; otherwise new item gets this quantity. Default 1.</summary>
        int Quantity { get; set; }
        /// <summary>When adding: if true and item exists by name, increment Quantity; if false and item exists, return error "Item already exists". Default true.</summary>
        bool Stack { get; set; }
        /// <summary>Game/source that added this item (e.g. Quake, OQUAKE). Persisted on the holon.</summary>
        string GameSource { get; set; }
        /// <summary>Category of item (e.g. Ammo, Armor, Weapon, KeyItem). Persisted on the holon.</summary>
        string ItemType { get; set; }
        //InventoryItemType InventoryItemType { get; set; }
    }
}