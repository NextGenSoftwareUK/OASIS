using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.GoogleMapsOASIS
{
    internal static class GoogleErrors
    {
        /// <summary>Reads Google's standard error body: {"error":{"code":..,"message":..,"status":..}}.</summary>
        public static string Read(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("error", out JsonElement error)) return null;
            if (error.ValueKind == JsonValueKind.String) return error.GetString();
            if (error.ValueKind != JsonValueKind.Object) return null;

            string status = error.TryGetProperty("status", out JsonElement s) ? s.GetString() : null;
            string message = error.TryGetProperty("message", out JsonElement m) ? m.GetString() : null;
            return status == null ? message : status + ": " + message;
        }
    }
}
