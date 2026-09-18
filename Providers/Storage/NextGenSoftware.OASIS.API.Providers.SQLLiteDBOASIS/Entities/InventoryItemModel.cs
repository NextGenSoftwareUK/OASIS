using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("AvatarInventory")]
    public class InventoryItemModel : InventoryItem
    {
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id{ set; get; }

        public string AvatarId{ set; get; }

        public InventoryItemModel(){}
        public InventoryItemModel(InventoryItem source){
            this.Name = source.Name;
            this.Description = source.Description;
            this.Quantity = source.Quantity;
            this.Stack = source.Stack;
            this.GameSource = source.GameSource;
            this.ItemType = source.ItemType;
            this.NftId = source.NftId;
            this.GeoNFTId = source.GeoNFTId;
            this.Rarity = source.Rarity;
            this.MaxQuantity = source.MaxQuantity;
            this.Weight = source.Weight;
            this.IsUsable = source.IsUsable;
            this.IsTradeable = source.IsTradeable;
            this.AcquiredOn = source.AcquiredOn;
            this.LastUsedOn = source.LastUsedOn;
            this.Properties = source.Properties == null ? null : new System.Collections.Generic.Dictionary<string, object>(source.Properties);
            this.HolonType = source.HolonType;
            if (source.MetaData != null)
                this.MetaData = new System.Collections.Generic.Dictionary<string, object>(source.MetaData);
        }

        public InventoryItem GetInventoryItem(){
            InventoryItem item = new InventoryItem();
            item.Name = this.Name;
            item.Description = this.Description;
            item.Quantity = this.Quantity;
            item.Stack = this.Stack;
            item.GameSource = this.GameSource;
            item.ItemType = this.ItemType;
            item.NftId = this.NftId;
            item.GeoNFTId = this.GeoNFTId;
            item.Rarity = this.Rarity;
            item.MaxQuantity = this.MaxQuantity;
            item.Weight = this.Weight;
            item.IsUsable = this.IsUsable;
            item.IsTradeable = this.IsTradeable;
            item.AcquiredOn = this.AcquiredOn;
            item.LastUsedOn = this.LastUsedOn;
            item.Properties = this.Properties == null ? null : new System.Collections.Generic.Dictionary<string, object>(this.Properties);
            item.HolonType = this.HolonType;
            /* Note: item.Id is not set from this.Id (table PK is long); avatar detail load may assign Id elsewhere if needed. */
            if (this.MetaData != null)
                item.MetaData = new System.Collections.Generic.Dictionary<string, object>(this.MetaData);
            return item;
        }
        
    }
}
