using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS
{
    internal static class MapboxErrors
    {
        /// <summary>Reads Mapbox error bodies: {"message": ...} and, for Directions, {"code": ..., "message": ...}.</summary>
        public static string Read(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object) return null;

            string code = root.TryGetProperty("code", out JsonElement c) && c.ValueKind == JsonValueKind.String ? c.GetString() : null;
            string message = root.TryGetProperty("message", out JsonElement m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null;
            if (code == null) return message;
            return message == null ? code : code + ": " + message;
        }
    }
}
