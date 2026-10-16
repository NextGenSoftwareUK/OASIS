using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage
{
    /// <summary>
    /// JSON for OASIS entities held in external stores. Interface-typed members need embedded type names, so the
    /// binder only resolves NextGenSoftware types: stored data must never be able to instantiate arbitrary .NET types.
    /// Holon navigation members are not stored; relationships are persisted by id.
    /// </summary>
    public static class OasisJson
    {
        private static readonly JsonSerializerSettings Settings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            SerializationBinder = new OasisTypesOnlyBinder(),
            ContractResolver = new SkipHolonNavigationResolver(),
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            Converters = { new JsonElementValueConverter() }
        };

        public static string Serialize<T>(T value) => JsonConvert.SerializeObject(value, typeof(T), Settings);

        public static T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, Settings);

        /// <summary>Classifies invalid persisted JSON as data corruption, not provider unavailability.</summary>
        public static T DeserializeStored<T>(string json)
        {
            try { return Deserialize<T>(json); }
            catch (JsonException cause)
            {
                throw new System.IO.InvalidDataException("Stored canonical record JSON is invalid.", cause);
            }
        }

        public static object Deserialize(string json, Type type) => JsonConvert.DeserializeObject(json, type, Settings);

        // System.Text.Json object dictionaries contain JsonElement values. Persist their JSON value,
        // never CLR implementation/type metadata that the storage binder must correctly reject.
        private sealed class JsonElementValueConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType) => objectType == typeof(System.Text.Json.JsonElement);
            public override bool CanRead => false;
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
                => writer.WriteRawValue(((System.Text.Json.JsonElement)value).GetRawText());
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
                => throw new NotSupportedException("JSON values are read through the existing canonical serializer.");
        }

        private sealed class OasisTypesOnlyBinder : ISerializationBinder
        {
            private readonly DefaultSerializationBinder _inner = new();

            public Type BindToType(string assemblyName, string typeName)
            {
                var type = _inner.BindToType(assemblyName, typeName);
                if (!IsAllowed(type))
                    throw new JsonSerializationException($"Type '{typeName}' is not an OASIS type and cannot be deserialized from storage.");
                return type;
            }

            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
                => _inner.BindToName(serializedType, out assemblyName, out typeName);

            private static bool IsAllowed(Type type)
            {
                if (type.IsGenericType)
                    return IsAllowedDefinition(type.GetGenericTypeDefinition()) && type.GetGenericArguments().All(IsAllowed);
                if (type.IsArray)
                    return IsAllowed(type.GetElementType());
                return IsAllowedDefinition(type);
            }

            private static bool IsAllowedDefinition(Type type)
                => type.Namespace != null && (type.Namespace.StartsWith("NextGenSoftware.", StringComparison.Ordinal)
                    || type.Namespace == "System.Collections.Generic"
                    || type.Namespace == "System.Collections.ObjectModel"
                    || type.IsPrimitive || type == typeof(object) || type == typeof(string) || type == typeof(Guid) || type == typeof(DateTime) || type == typeof(decimal));
        }

        private sealed class SkipHolonNavigationResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);
                var type = property.PropertyType;
                if (typeof(IHolonBase).IsAssignableFrom(type) || IsHolonCollection(type))
                    property.ShouldSerialize = _ => false;
                return property;
            }

            private static bool IsHolonCollection(Type type)
                => type != typeof(string) && typeof(IEnumerable).IsAssignableFrom(type)
                   && type.IsGenericType && type.GetGenericArguments().Any(a => typeof(IHolonBase).IsAssignableFrom(a)
                       // Inventory entries are owned values, not navigation edges, despite inheriting holon identity.
                       && !typeof(IInventoryItem).IsAssignableFrom(a));
        }
    }
}
