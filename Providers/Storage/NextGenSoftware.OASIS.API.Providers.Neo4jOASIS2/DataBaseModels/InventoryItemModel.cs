using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Neo4jOgm.Attribute;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.DataBaseModels{

    [NeoNodeEntity("AvatarInventory", "AvatarInventory")]

    public class InventoryItemModel : InventoryItem
    {
        [NeoNodeId]
        public long? Id { get; set; }
    

        public string AvatarId{ set; get; }

        public InventoryItemModel(){}
        public InventoryItemModel(InventoryItem source){

            this.Name=source.Name;
            this.Description=source.Description;
            this.Quantity=source.Quantity;
            this.Stack=source.Stack;
        }

        public InventoryItem GetInventoryItem(){

            InventoryItem item=new InventoryItem();

            item.Name=this.Name;
            item.Description=this.Description;
            item.Quantity=this.Quantity;
            item.Stack=this.Stack;

            return(item);
        }
        
    }
}