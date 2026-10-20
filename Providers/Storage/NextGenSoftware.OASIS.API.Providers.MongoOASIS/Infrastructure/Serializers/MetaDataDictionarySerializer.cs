using System;
using System.Collections.Generic;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Serializers
{
    /// <summary>
    /// Custom serializer for Dictionary&lt;string, object&gt; that handles JsonElement deserialization.
    /// Converts JsonElement values to proper .NET types during deserialization.
    /// </summary>
    public class MetaDataDictionarySerializer : DictionarySerializerBase<Dictionary<string, object>>
    {
        public MetaDataDictionarySerializer() : base(MongoDB.Bson.Serialization.Options.DictionaryRepresentation.Document)
        {
        }

        protected override Dictionary<string, object> CreateInstance()
        {
            return new Dictionary<string, object>();
        }

        protected override Dictionary<string, object> DeserializeValue(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var reader = context.Reader;
            var currentType = reader.GetCurrentBsonType();

            if (currentType == BsonType.Array)
            {
                // Stored as ArrayOfArrays: [ ["key1", value1], ["key2", value2], ... ]
                return DeserializeArrayOfArrays(reader);
            }

            // Stored as Document: { "key1": value1, "key2": value2, ... }
            reader.ReadStartDocument();
            var dictionary = new Dictionary<string, object>();

            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var key = reader.ReadName();
                var bsonType = reader.GetCurrentBsonType();
                dictionary[key] = ReadBsonValue(reader, bsonType);
            }

            reader.ReadEndDocument();
            return dictionary;
        }

        private static Dictionary<string, object> DeserializeArrayOfArrays(IBsonReader reader)
        {
            reader.ReadStartArray();
            var dictionary = new Dictionary<string, object>();

            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                // Each element is [ key, value ]
                reader.ReadStartArray();
                var keyType = reader.ReadBsonType();
                var key = keyType == BsonType.String ? reader.ReadString() : (ReadBsonValue(reader, keyType)?.ToString() ?? "");
                var valueBsonType = reader.ReadBsonType();
                dictionary[key] = ReadBsonValue(reader, valueBsonType);
                reader.ReadEndArray();
            }

            reader.ReadEndArray();
            return dictionary;
        }

        private static object ReadBsonValue(IBsonReader reader, BsonType bsonType)
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
                    return DateTimeOffset.FromUnixTimeMilliseconds(reader.ReadDateTime()).UtcDateTime;

                case BsonType.ObjectId:
                    return reader.ReadObjectId().ToString();

                case BsonType.Binary:
                    return ReadBinaryValue(reader);

                case BsonType.Array:
                    return DeserializeArray(reader);

                case BsonType.Document:
                    return DeserializeDocument(reader);
            }

            // Portable metadata has no lossless mapping for these native-only types.
            // Never return success with a silently substituted null that a later save could overwrite.
            throw new BsonSerializationException($"Mongo metadata BSON type {bsonType} has no supported lossless portable representation.");
        }

        /// <summary>Read BSON binary; handles both Binary (generic bytes) and UuidLegacy/UuidStandard so deserialization does not throw.</summary>
        private static object ReadBinaryValue(IBsonReader reader)
        {
            var binaryData = reader.ReadBinaryData();
            var subType = binaryData.SubType;
            var bytes = binaryData.Bytes;
            if (subType == BsonBinarySubType.UuidStandard)
                return binaryData.ToGuid(GuidRepresentation.Standard).ToString();
            if (subType == BsonBinarySubType.UuidLegacy)
            {
                // Retain this provider's existing C# legacy byte-order contract; subtype 3 does not identify other legacy encodings.
                if (bytes != null && bytes.Length == 16)
                    return binaryData.ToGuid(GuidRepresentation.CSharpLegacy).ToString();
                return bytes ?? Array.Empty<byte>();
            }
            return bytes ?? Array.Empty<byte>();
        }

        private static object DeserializeArray(IBsonReader reader)
        {
            reader.ReadStartArray();
            var list = new List<object>();

            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var bsonType = reader.GetCurrentBsonType();
                list.Add(ReadBsonValue(reader, bsonType));
            }

            reader.ReadEndArray();
            return list.ToArray();
        }

        private static object DeserializeDocument(IBsonReader reader)
        {
            reader.ReadStartDocument();
            var dict = new Dictionary<string, object>();

            while (reader.ReadBsonType() != BsonType.EndOfDocument)
            {
                var key = reader.ReadName();
                var bsonType = reader.GetCurrentBsonType();
                dict[key] = ReadBsonValue(reader, bsonType);
            }

            reader.ReadEndDocument();
            return dict;
        }

        private static object ConvertBsonValue(BsonValue bsonValue)
        {
            if (bsonValue == null || bsonValue.IsBsonNull)
                return null;

            switch (bsonValue.BsonType)
            {
                case BsonType.Boolean: return bsonValue.AsBoolean;
                case BsonType.Int32: return bsonValue.AsInt32;
                case BsonType.Int64: return bsonValue.AsInt64;
                case BsonType.Double: return bsonValue.AsDouble;
                case BsonType.Decimal128: return (decimal)bsonValue.AsDecimal128;
                case BsonType.String: return bsonValue.AsString;
                case BsonType.DateTime: return bsonValue.ToUniversalTime();
                case BsonType.ObjectId: return bsonValue.AsObjectId.ToString();
                case BsonType.Binary: return bsonValue.AsBsonBinaryData.Bytes;
                case BsonType.Array:
                    var array = new List<object>();
                    foreach (var item in bsonValue.AsBsonArray)
                        array.Add(ConvertBsonValue(item));
                    return array.ToArray();
                case BsonType.Document:
                    var dict = new Dictionary<string, object>();
                    foreach (var element in bsonValue.AsBsonDocument)
                        dict[element.Name] = ConvertBsonValue(element.Value);
                    return dict;
                default:
                    return bsonValue.ToString();
            }
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

            if (value is JRaw raw)
            {
                using var document=JsonDocument.Parse(raw.ToString(Newtonsoft.Json.Formatting.None));
                SerializeJsonElement(writer,document.RootElement);
                return;
            }
            if (value is Newtonsoft.Json.Linq.JObject jsonObject)
            {
                writer.WriteStartDocument();
                foreach (var property in jsonObject.Properties())
                { writer.WriteName(property.Name); SerializeValue(writer, property.Value); }
                writer.WriteEndDocument();
                return;
            }
            if (value is Newtonsoft.Json.Linq.JArray jsonArray)
            {
                writer.WriteStartArray();
                foreach (var item in jsonArray) SerializeValue(writer, item);
                writer.WriteEndArray();
                return;
            }
            if (value is Newtonsoft.Json.Linq.JValue jsonValue)
            {
                if (jsonValue.Type == Newtonsoft.Json.Linq.JTokenType.Undefined)
                    throw new BsonSerializationException("Undefined metadata JSON is not a persistent value.");
                SerializeValue(writer, jsonValue.Value);
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
            {
                var date = (DateTime)value;
                var utc = date.Kind == DateTimeKind.Local ? date.ToUniversalTime() : DateTime.SpecifyKind(date, DateTimeKind.Utc);
                writer.WriteDateTime(new BsonDateTime(utc).MillisecondsSinceEpoch);
            }
            else if (valueType == typeof(Guid))
                writer.WriteString(((Guid)value).ToString());
            else if (value is byte[] bytes)
                writer.WriteBinaryData(new BsonBinaryData(bytes));
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
            else if (value is JsonElement je)
            {
                SerializeJsonElement(writer, je);
            }
            else
            {
                // Supported complex metadata uses JSON text; encoding failures must
                // remain errors rather than silently substituting a null value.
                WriteComplexTypeAsJson(writer, value);
            }
        }

        /// <summary>Encode supported complex metadata as JSON text. Unsupported values fail the enclosing save; deserialization returns the stored string.</summary>
        private static void WriteComplexTypeAsJson(IBsonWriter writer, object value)
        {
            try
            {
                var options = new JsonSerializerOptions { MaxDepth = 32 };
                var json = JsonSerializer.Serialize(value, options);
                writer.WriteString(json);
            }
            catch (Exception cause)
            {
                throw new BsonSerializationException("Mongo metadata value cannot be serialized without data loss.", cause);
            }
        }

        private static void SerializeJsonElement(IBsonWriter writer, JsonElement je)
        {
            switch (je.ValueKind)
            {
                case JsonValueKind.Null:
                    writer.WriteNull();
                    break;
                case JsonValueKind.String:
                    writer.WriteString(je.GetString());
                    break;
                case JsonValueKind.Number:
                    if (je.TryGetInt32(out int i32))
                        writer.WriteInt32(i32);
                    else if (je.TryGetInt64(out long i64))
                        writer.WriteInt64(i64);
                    else if (je.TryGetDecimal(out decimal dec)
                        && StoredRecordValueEquality.Equivalent(new JRaw(je.GetRawText()),new JValue(dec)))
                        writer.WriteDecimal128(dec);
                    else if (je.TryGetDouble(out double d) && !double.IsNaN(d) && !double.IsInfinity(d)
                        && StoredRecordValueEquality.Equivalent(new JRaw(je.GetRawText()),new JValue(d)))
                        writer.WriteDouble(d);
                    else
                        throw new BsonSerializationException("Metadata JSON number has no exact supported BSON/CLR round-trip representation.");
                    break;
                case JsonValueKind.True:
                    writer.WriteBoolean(true);
                    break;
                case JsonValueKind.False:
                    writer.WriteBoolean(false);
                    break;
                case JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (var item in je.EnumerateArray())
                        SerializeJsonElement(writer, item);
                    writer.WriteEndArray();
                    break;
                case JsonValueKind.Object:
                    writer.WriteStartDocument();
                    foreach (var prop in je.EnumerateObject())
                    {
                        writer.WriteName(prop.Name);
                        SerializeJsonElement(writer, prop.Value);
                    }
                    writer.WriteEndDocument();
                    break;
                default:
                    writer.WriteString(je.ToString());
                    break;
            }
        }
    }
}



