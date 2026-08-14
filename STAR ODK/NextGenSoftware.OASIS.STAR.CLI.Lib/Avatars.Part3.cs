using System.Globalization;
using System.Linq;
using System.Text.Json;
using NextGenSoftware.Utilities;
using NextGenSoftware.CLI.Engine;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.STAR.CLI.Lib
{
    public partial class Avatars
    {

        private static string GetInventoryTabLabel(string? itemType)
        {
            if (string.IsNullOrWhiteSpace(itemType))
                return "Other";

            string lower = itemType.Trim().ToLowerInvariant();

            // Match in priority order (e.g. KeyItem contains "item").
            if (lower.Contains("key"))
                return "Keys";
            if (lower.Contains("weapon"))
                return "Weapons";
            if (lower.Contains("ammo"))
                return "Ammo";
            if (lower.Contains("armour") || lower.Contains("armor"))
                return "Armor";
            if (lower.Contains("monster"))
                return "Monsters";
            if (lower.Contains("powerup") || lower.Contains("power-up") || lower.Contains("health"))
                return "Items";
            if (lower.Contains("item"))
                return "Items";

            return "Other";
        }

        private static string FormatStarnetDnaJsonValue(object? raw)
        {
            if (raw == null)
                return "None";

            // Prevent serializing JsonElement as `{ "ValueKind": 3 }` in JSON output.
            if (raw is JsonElement je)
            {
                switch (je.ValueKind)
                {
                    case JsonValueKind.String:
                        return je.GetString() ?? "None";
                    case JsonValueKind.Number:
                        if (je.TryGetInt32(out var i32))
                            return i32.ToString(CultureInfo.InvariantCulture);
                        if (je.TryGetInt64(out var i64))
                            return i64.ToString(CultureInfo.InvariantCulture);
                        return je.GetRawText();
                    case JsonValueKind.Null:
                    case JsonValueKind.Undefined:
                        return "None";
                    default:
                        return je.GetRawText();
                }
            }

            // Some code paths end up storing a JsonElement-ish string like `{"ValueKind":3}`.
            // Convert that into a plain number/string.
            try
            {
                string s = raw.ToString();
                if (string.IsNullOrWhiteSpace(s))
                    return "None";

                if (s.Contains("\"ValueKind\"", StringComparison.Ordinal))
                {
                    using var doc = JsonDocument.Parse(s);
                    if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                        doc.RootElement.TryGetProperty("ValueKind", out JsonElement vk))
                    {
                        if (vk.ValueKind == JsonValueKind.Number)
                            return vk.GetRawText();
                        if (vk.ValueKind == JsonValueKind.String)
                            return vk.GetString() ?? "None";
                    }
                }

                return s;
            }
            catch
            {
                return raw.ToString() ?? "None";
            }
        }
    }
}
