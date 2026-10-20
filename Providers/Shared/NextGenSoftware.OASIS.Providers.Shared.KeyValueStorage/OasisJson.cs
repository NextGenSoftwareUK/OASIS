using System;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage
{
    /// <summary>Provider-compatible entry point for the single canonical record serializer in Core.</summary>
    public static class OasisJson
    {
        /// <summary>Parse an external record object without narrowing untyped JSON numbers or activating CLR types.</summary>
        public static Newtonsoft.Json.Linq.JObject ParseStoredObject(string json)
        {
            try
            {
                using var document=System.Text.Json.JsonDocument.Parse(json);
                if(document.RootElement.ValueKind!=System.Text.Json.JsonValueKind.Object)
                    throw new System.IO.InvalidDataException("Stored canonical record JSON must be an object.");
                return (Newtonsoft.Json.Linq.JObject)StoredRecordJson.CaptureRuntimeToken(document.RootElement);
            }
            catch(System.Text.Json.JsonException cause)
            {
                throw new System.IO.InvalidDataException("Stored canonical record JSON is invalid.",cause);
            }
        }

        public static string Serialize<T>(T value) => StoredRecordJson.Serialize(value);
        public static T Deserialize<T>(string json) => StoredRecordJson.Deserialize<T>(json);
        /// <summary>Decode a storage DTO through the canonical exact metadata and restricted typed-field reader.</summary>
        public static T DeserializeMetadataRecord<T>(string json) => StoredRecordJson.DeserializeMetadataRecord<T>(json);
        public static T Project<T>(object value) => StoredRecordJson.Project<T>(value);
        public static T DeserializeStored<T>(string json) => StoredRecordJson.DeserializeStored<T>(json);
        public static object Deserialize(string json, Type type) => StoredRecordJson.Deserialize(json,type);
    }
}
