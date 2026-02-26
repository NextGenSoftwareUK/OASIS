using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Objects
{
    public class InventoryItem : STARNETHolon, IInventoryItem
    {
        public InventoryItem()
        {
            this.HolonType = HolonType.InventoryItem;
            this.STARNETHolonDNAJSONName = "InventoryItemDNAJSON";
        }

        public byte[] Image2D { get; set; }
        public Uri Image2DURI { get; set; }
        public byte[] Object3D { get; set; }
        public Uri Object3DURI { get; set; }
        /// <summary>Stack size. When adding with Stack=true, API increments this if item exists; otherwise new item gets this quantity. Default 1.</summary>
        public int Quantity { get; set; } = 1;
        /// <summary>When adding: if true and item exists by name, increment Quantity; if false and item exists, return error "Item already exists". Default true.</summary>
        public bool Stack { get; set; } = true;
        //public InventoryItemType InventoryItemType { get; set; }
    }
}