using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities{

    [Table("AvatarInventory")]
    public class InventoryItemModel : InventoryItem
    {
        internal static readonly string[] RelationalColumnNames={
            nameof(Id),nameof(AvatarId),nameof(ItemId),nameof(PublishedSTARNETHolon),
            nameof(Image2D),nameof(Object3D),nameof(Image2DURI),nameof(Object3DURI),
            nameof(PropertiesJson),nameof(MetaDataJson),nameof(ExtendedValueJson),nameof(Name),nameof(Description),
            nameof(Quantity),nameof(Stack),nameof(GameSource),nameof(ItemType),nameof(NftId),nameof(GeoNFTId),
            nameof(Rarity),nameof(MaxQuantity),nameof(Weight),nameof(IsUsable),nameof(IsTradeable),
            nameof(AcquiredOn),nameof(LastUsedOn),nameof(HolonType)};
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id{ set; get; }
        // Public inventory identity is independent of SQLite's generated row key.
        public System.Guid ItemId { get; set; }
        public string PropertiesJson { get; set; } = "null";
        public string MetaDataJson { get; set; } = "{}";
        // Canonical owned-value fields without relational columns. Direct item columns
        // remain authoritative and are deliberately absent from this extension record.
        public string ExtendedValueJson { get; set; } = "{}";

        public string AvatarId{ set; get; }

        public InventoryItemModel(){}
        public InventoryItemModel(NextGenSoftware.OASIS.API.Core.Interfaces.IInventoryItem source){
            var projected=NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Project<InventoryItem>(source);
            var extended=Newtonsoft.Json.Linq.JObject.Parse(NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.Serialize(projected));
            foreach(var name in RelationalColumnNames)extended.Remove(name);
            // These two values have differently named relational JSON columns.
            extended.Remove("Properties");extended.Remove("MetaData");
            this.ExtendedValueJson=extended.ToString(Newtonsoft.Json.Formatting.None);
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
            ValidateExtendedValue(this.ExtendedValueJson);
            InventoryItem item = NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.DeserializeStored<InventoryItem>(this.ExtendedValueJson)
                ?? throw new System.IO.InvalidDataException("Stored inventory extension must be a JSON object.");
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

        private static void ValidateExtendedValue(string json)
        {
            Newtonsoft.Json.Linq.JObject value;
            try { value=Newtonsoft.Json.Linq.JObject.Parse(json); }
            catch(Newtonsoft.Json.JsonReaderException cause)
            { throw new System.IO.InvalidDataException("Stored inventory extension must be a JSON object.",cause); }
            foreach(var property in value.Properties()){
                var name=property.Name;
                if(System.Array.Exists(RelationalColumnNames,column=>string.Equals(column,name,System.StringComparison.OrdinalIgnoreCase))
                    || string.Equals(name,"Properties",System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name,"MetaData",System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name,"$type",System.StringComparison.OrdinalIgnoreCase))
                    throw new System.IO.InvalidDataException("Stored inventory extension contains a field owned by another storage contract: "+name);
                var member=typeof(InventoryItem).GetProperty(name,System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.IgnoreCase);
                if(member!=null && NextGenSoftware.OASIS.API.Core.Serialization.StoredRecordJson.IsNavigationMember(member.PropertyType))
                    throw new System.IO.InvalidDataException("Stored inventory extension contains navigation: "+name);
            }
        }
        
    }
}
