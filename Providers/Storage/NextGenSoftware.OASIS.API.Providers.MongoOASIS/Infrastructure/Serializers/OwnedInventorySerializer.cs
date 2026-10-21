using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Helpers;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Serializers;

/// <summary>Inventory embedded in an avatar is an owned value, not a hydrated holon graph.</summary>
internal sealed class OwnedInventorySerializer : SerializerBase<IList<InventoryItem>>, IBsonArraySerializer
{
    public bool TryGetItemSerializationInfo(out BsonSerializationInfo serializationInfo)
    {
        serializationInfo = new BsonSerializationInfo(null, BsonSerializer.LookupSerializer<InventoryItem>(), typeof(InventoryItem));
        return true;
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IList<InventoryItem> value)
    {
        if (value == null) { context.Writer.WriteNull(); return; }
        context.Writer.WriteStartArray();
        foreach (var item in value)
        {
            if (item == null) { context.Writer.WriteNull(); continue; }
            // Reuse the canonical owned-value projection before accessing navigation getters.
            var document = DataHelper.CopyInventoryItem(item).ToBsonDocument();
            document.Remove("Children");
            BsonDocumentSerializer.Instance.Serialize(context, document);
        }
        context.Writer.WriteEndArray();
    }

    public override IList<InventoryItem> Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        if (context.Reader.GetCurrentBsonType() == BsonType.Null) { context.Reader.ReadNull(); return null; }
        var items = new List<InventoryItem>();
        context.Reader.ReadStartArray();
        while (context.Reader.ReadBsonType() != BsonType.EndOfDocument)
        {
            if (context.Reader.GetCurrentBsonType() == BsonType.Null) { context.Reader.ReadNull(); items.Add(null); continue; }
            var document = BsonDocumentSerializer.Instance.Deserialize(context);
            document.Remove("Children");
            items.Add(DataHelper.CopyInventoryItem(BsonSerializer.Deserialize<InventoryItem>(document)));
        }
        context.Reader.ReadEndArray();
        return items;
    }
}
