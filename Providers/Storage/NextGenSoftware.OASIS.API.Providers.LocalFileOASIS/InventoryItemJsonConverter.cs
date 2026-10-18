using System;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.LocalFileOASIS
{
    internal sealed class InventoryItemJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(IInventoryItem);
        public override bool CanWrite => false;
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => reader.TokenType == JsonToken.Null ? null : serializer.Deserialize<InventoryItem>(reader);
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => throw new NotSupportedException("Inventory serialization uses the concrete record contract.");
    }
}
