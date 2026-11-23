using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.HEREMapsOASIS
{
    internal static class HereErrors
    {
        /// <summary>Reads HERE's error bodies: {"title":..,"status":..,"cause":..} or {"error":..,"error_description":..}.</summary>
        public static string Read(JsonElement root)
        {
            if (root.ValueKind != JsonValueKind.Object) return null;

            string title = ReadString(root, "title") ?? ReadString(root, "error");
            string cause = ReadString(root, "cause") ?? ReadString(root, "error_description");
            if (title == null) return cause;
            return cause == null ? title : title + ": " + cause;
        }

        private static string ReadString(JsonElement element, string name) =>
            element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
