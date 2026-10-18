using System;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage
{
    /// <summary>Provider-compatible entry point for the single canonical record serializer in Core.</summary>
    public static class OasisJson
    {
        public static string Serialize<T>(T value) => StoredRecordJson.Serialize(value);
        public static T Deserialize<T>(string json) => StoredRecordJson.Deserialize<T>(json);
        public static T Project<T>(object value) => StoredRecordJson.Project<T>(value);
        public static T DeserializeStored<T>(string json) => StoredRecordJson.DeserializeStored<T>(json);
        public static object Deserialize(string json, Type type) => StoredRecordJson.Deserialize(json,type);
    }
}
