using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive.Synchronization;

namespace NextGenSoftware.OASIS.Edge.Runtime
{
    /// <summary>Uses generated metadata for typed payloads and direct parsing for opaque JSON projections.</summary>
    public sealed class EdgePayloadSerializer : IEdgePayloadSerializer
    {
        private readonly JsonSerializerContext _applicationContext;

        /// <param name="applicationContext">Generated metadata for the host's additional entity types.</param>
        public EdgePayloadSerializer(JsonSerializerContext applicationContext = null) =>
            _applicationContext = applicationContext;

        public string Serialize<T>(T value) => value is JToken token
            ? token.ToString(Newtonsoft.Json.Formatting.None)
            : JsonSerializer.Serialize(value, TypeInfo<T>());

        public T Deserialize<T>(string json)
        {
            if (typeof(T) == typeof(JObject)) return (T)(object)JObject.Parse(json);
            if (typeof(T) == typeof(JArray)) return (T)(object)JArray.Parse(json);
            if (typeof(T) == typeof(JToken)) return (T)(object)JToken.Parse(json);
            return JsonSerializer.Deserialize(json, TypeInfo<T>());
        }

        private JsonTypeInfo<T> TypeInfo<T>() =>
            (HyperDriveJsonContext.Default.GetTypeInfo(typeof(T)) ?? _applicationContext?.GetTypeInfo(typeof(T)))
                as JsonTypeInfo<T> ?? throw new NotSupportedException(
                    $"Register generated JSON metadata for '{typeof(T)}' in EdgeRuntimeOptions.PayloadSerializer.");
    }
}
