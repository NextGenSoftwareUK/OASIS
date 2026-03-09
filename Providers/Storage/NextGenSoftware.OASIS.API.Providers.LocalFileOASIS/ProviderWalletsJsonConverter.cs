using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.LocalFileOASIS
{
    /// <summary>
    /// Deserializes JSON to Dictionary&lt;ProviderType, List&lt;IProviderWallet&gt;&gt; by instantiating concrete <see cref="ProviderWallet"/> so Newtonsoft can deserialize avatar files that contain ProviderWallets.
    /// </summary>
    public sealed class ProviderWalletsJsonConverter : JsonConverter<Dictionary<ProviderType, List<IProviderWallet>>>
    {
        public override Dictionary<ProviderType, List<IProviderWallet>> ReadJson(JsonReader reader, Type objectType, Dictionary<ProviderType, List<IProviderWallet>> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var token = JToken.Load(reader);
            if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
                return existingValue ?? new Dictionary<ProviderType, List<IProviderWallet>>();

            var result = new Dictionary<ProviderType, List<IProviderWallet>>();
            var obj = token as JObject;
            if (obj == null)
                return result;

            foreach (var prop in obj.Properties())
            {
                if (!Enum.TryParse<ProviderType>(prop.Name, ignoreCase: true, out var providerType))
                    continue;
                List<ProviderWallet>? list = null;
                if (prop.Value != null)
                    list = prop.Value.ToObject<List<ProviderWallet>>(serializer);
                result[providerType] = list == null ? new List<IProviderWallet>() : new List<IProviderWallet>(list);
            }

            return result;
        }

        public override void WriteJson(JsonWriter writer, Dictionary<ProviderType, List<IProviderWallet>> value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            writer.WriteStartObject();
            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key.ToString());
                serializer.Serialize(writer, kvp.Value);
            }
            writer.WriteEndObject();
        }
    }
}
