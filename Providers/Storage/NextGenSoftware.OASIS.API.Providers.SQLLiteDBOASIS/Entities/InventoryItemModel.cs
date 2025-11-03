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

            this.Name=source.Name;
            this.Description=source.Description;
            //this.Quantity=source.Quantity;
        }

        public InventoryItem GetInventoryItem(){

            InventoryItem item=new InventoryItem();

            item.Name=this.Name;
            item.Description=this.Description;
           // item.Quantity=this.Quantity;

            return(item);
        }
        
    }
}