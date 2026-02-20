using System;
using System.Collections.Generic;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Serializers
{
    /// <summary>
    /// Custom serializer for Dictionary&lt;string, object&gt; that handles JsonElement deserialization.
    /// Converts JsonElement values to proper .NET types during deserialization.
    /// </summary>
    public class MetaDataDictionarySerializer : DictionarySerializerBase<Dictionary<string, object>>
    {
        public MetaDataDictionarySerializer() : base(DictionaryRepresentation.Document)
        {
        }

        protected override Dictionary<string, object> CreateDeserializedValue(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            return new Dictionary<string, object>();
        }

        protected override void AddItem(BsonDeserializationContext context, Dictionary<string, object> dictionary, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            var bsonType = reader.GetCurrentBsonType();

            if (bsonType == BsonType.EndOfDocument)
            {
                reader.ReadEndDocument();
                return;
            }

            var key = reader.ReadName();
            object value = DeserializeValue(reader, bsonType);
            
            dictionary[key] = value;
        }

        private object DeserializeValue(IBsonReader reader, BsonType bsonType)
        {
            switch (bsonType)
            {
                case BsonType.Null:
                    reader.ReadNull();
                    return null;

                case BsonType.Boolean:
                    return reader.ReadBoolean();

                case BsonType.Int32:
                    return reader.ReadInt32();

                case BsonType.Int64:
                    return reader.ReadInt64();

                case BsonType.Double:
                    return reader.ReadDouble();

                case BsonType.Decimal128:
                    return (decimal)reader.ReadDecimal128();

                case BsonType.String:
                    return reader.ReadString();

                case BsonType.DateTime:
                    return reader.ReadDateTime();

                case BsonType.ObjectId:
                    return reader.ReadObjectId().ToString();

                case BsonType.Guid:
                    return reader.ReadGuid();

                case BsonType.Binary:
                    return reader.ReadBytes();

                case BsonType.Array:
                    return DeserializeArray(reader);

                case BsonType.Document:
                    return DeserializeDocument(reader);

                default:
                    // For unknown types, read as BsonValue and convert
                    var bsonValue = BsonValue.ReadFrom(reader);
                    return ConvertBsonValue(bsonValue);
            }
        }

        private object DeserializeArray(IBsonReader reader)
        {
            reader.ReadStartArray();
            var list = new List<object>();

            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var bsonType = reader.GetCurrentBsonType();
                list.Add(DeserializeValue(reader, bsonType));
            }

            reader.ReadEndArray();
            return list.ToArray();
        }

        private object DeserializeDocument(IBsonReader reader)
        {
            reader.ReadStartDocument();
            var dict = new Dictionary<string, object>();

            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var key = reader.ReadName();
                var bsonType = reader.GetCurrentBsonType();
                dict[key] = DeserializeValue(reader, bsonType);
            }

            reader.ReadEndDocument();
            return dict;
        }

        private object ConvertBsonValue(BsonValue bsonValue)
        {
            if (bsonValue == null || bsonValue.IsBsonNull)
                return null;

            if (bsonValue.IsBsonBoolean)
                return bsonValue.AsBoolean;

            if (bsonValue.IsBsonInt32)
                return bsonValue.AsInt32;

            if (bsonValue.IsBsonInt64)
                return bsonValue.AsInt64;

            if (bsonValue.IsBsonDouble)
                return bsonValue.AsDouble;

            if (bsonValue.IsBsonDecimal128)
                return (decimal)bsonValue.AsDecimal128;

            if (bsonValue.IsBsonString)
                return bsonValue.AsString;

            if (bsonValue.IsBsonDateTime)
                return bsonValue.ToUniversalTime();

            if (bsonValue.IsBsonObjectId)
                return bsonValue.AsObjectId.ToString();

            if (bsonValue.IsBsonBinaryData)
                return bsonValue.AsBsonBinaryData.Bytes;

            if (bsonValue.IsBsonArray)
            {
                var array = new List<object>();
                foreach (var item in bsonValue.AsBsonArray)
                {
                    array.Add(ConvertBsonValue(item));
                }
                return array.ToArray();
            }

            if (bsonValue.IsBsonDocument)
            {
                var dict = new Dictionary<string, object>();
                foreach (var element in bsonValue.AsBsonDocument)
                {
                    dict[element.Name] = ConvertBsonValue(element.Value);
                }
                return dict;
            }

            // Fallback: convert to string
            return bsonValue.ToString();
        }

        protected override void SerializeValue(BsonSerializationContext context, BsonSerializationArgs args, Dictionary<string, object> dictionary)
        {
            var writer = context.Writer;
            writer.WriteStartDocument();

            foreach (var kvp in dictionary)
            {
                writer.WriteName(kvp.Key);
                SerializeValue(writer, kvp.Value);
            }

            writer.WriteEndDocument();
        }

        private void SerializeValue(IBsonWriter writer, object value)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            var valueType = value.GetType();

            if (valueType == typeof(bool))
                writer.WriteBoolean((bool)value);
            else if (valueType == typeof(int))
                writer.WriteInt32((int)value);
            else if (valueType == typeof(long))
                writer.WriteInt64((long)value);
            else if (valueType == typeof(double))
                writer.WriteDouble((double)value);
            else if (valueType == typeof(decimal))
                writer.WriteDecimal128((decimal)value);
            else if (valueType == typeof(string))
                writer.WriteString((string)value);
            else if (valueType == typeof(DateTime))
                writer.WriteDateTime(((DateTime)value).ToUniversalTime());
            else if (valueType == typeof(Guid))
                writer.WriteGuid((Guid)value);
            else if (valueType.IsArray)
            {
                writer.WriteStartArray();
                foreach (var item in (Array)value)
                {
                    SerializeValue(writer, item);
                }
                writer.WriteEndArray();
            }
            else if (value is Dictionary<string, object> dict)
            {
                writer.WriteStartDocument();
                foreach (var kvp in dict)
                {
                    writer.WriteName(kvp.Key);
                    SerializeValue(writer, kvp.Value);
                }
                writer.WriteEndDocument();
            }
            else
            {
                // For other types, serialize as BSON value
                var bsonValue = BsonValue.Create(value);
                bsonValue.WriteTo(writer);
            }
        }
    }
}


