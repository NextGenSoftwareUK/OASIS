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
            // Quantity property removed from InventoryItem
        }

        public InventoryItem GetInventoryItem(){

            InventoryItem item=new InventoryItem();

            item.Name=this.Name;
            item.Description=this.Description;
            // Quantity property removed from InventoryItem

            return(item);
        }
        
    }
}