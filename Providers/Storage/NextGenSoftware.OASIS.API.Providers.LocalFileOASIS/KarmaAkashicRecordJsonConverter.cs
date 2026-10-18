using System;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.LocalFileOASIS
{
    /// <summary>Resolve the known interface contract without enabling arbitrary JSON type names.</summary>
    internal sealed class KarmaAkashicRecordJsonConverter : JsonConverter
    {
        public override bool CanWrite => false;
        public override bool CanConvert(Type objectType) => objectType == typeof(IKarmaAkashicRecord);
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            => reader.TokenType == JsonToken.Null ? null : serializer.Deserialize<KarmaAkashicRecord>(reader);
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            => throw new NotSupportedException("Karma records use the normal concrete serializer.");
    }
}
