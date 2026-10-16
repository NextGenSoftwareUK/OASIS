using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("AvatarInventory")]
    public class InventoryItemModel : InventoryItem
    {
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id{ set; get; }
        // Public inventory identity is independent of SQLite's generated row key.
        public System.Guid ItemId { get; set; }
        public string PropertiesJson { get; set; } = "null";
        public string MetaDataJson { get; set; } = "{}";

        public string AvatarId{ set; get; }

        public InventoryItemModel(){}
        public InventoryItemModel(NextGenSoftware.OASIS.API.Core.Interfaces.IInventoryItem source){
            this.ItemId = source.Id;
            this.PublishedSTARNETHolon = source.PublishedSTARNETHolon == null ? null : (byte[])source.PublishedSTARNETHolon.Clone();
            this.Image2D = source.Image2D == null ? null : (byte[])source.Image2D.Clone();
            this.Object3D = source.Object3D == null ? null : (byte[])source.Object3D.Clone();
            this.Image2DURI = source.Image2DURI;
            this.Object3DURI = source.Object3DURI;
            this.PropertiesJson = System.Text.Json.JsonSerializer.Serialize(source.Properties);
            this.MetaDataJson = System.Text.Json.JsonSerializer.Serialize(source.MetaData ?? new System.Collections.Generic.Dictionary<string, object>());
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
            item.Id = this.ItemId;
            item.PublishedSTARNETHolon = this.PublishedSTARNETHolon == null ? null : (byte[])this.PublishedSTARNETHolon.Clone();
            item.Image2D = this.Image2D == null ? null : (byte[])this.Image2D.Clone();
            item.Object3D = this.Object3D == null ? null : (byte[])this.Object3D.Clone();
            item.Image2DURI = this.Image2DURI;
            item.Object3DURI = this.Object3DURI;
            item.Properties = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(this.PropertiesJson);
            item.MetaData = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(this.MetaDataJson)
                ?? throw new System.IO.InvalidDataException("Stored inventory metadata must be a JSON object.");
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
            item.HolonType = this.HolonType;
            return item;
        }
        
    }
}
