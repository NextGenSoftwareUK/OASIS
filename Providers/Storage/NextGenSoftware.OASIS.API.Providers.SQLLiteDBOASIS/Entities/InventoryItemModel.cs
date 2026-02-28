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
            item.HolonType = this.HolonType;
            /* Note: item.Id is not set from this.Id (table PK is long); avatar detail load may assign Id elsewhere if needed. */
            if (this.MetaData != null)
                item.MetaData = new System.Collections.Generic.Dictionary<string, object>(this.MetaData);
            return item;
        }
        
    }
}