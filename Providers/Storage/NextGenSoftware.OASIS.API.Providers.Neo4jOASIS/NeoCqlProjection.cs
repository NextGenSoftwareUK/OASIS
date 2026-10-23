using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

// A replacement property map for a query projection node, derived from the exact
// persisted snapshot rather than from caller objects or stringified lookup tokens.
internal static class NeoCqlProjection
{
    internal const long SchemaVersion = 2;

    internal static string PathKey(IReadOnlyList<string> segments)
        => "m_" + Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(segments)))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

    internal static Dictionary<string, object> Build(string snapshot)
    {
        using var document = JsonDocument.Parse(snapshot, new JsonDocumentOptions { MaxDepth = 128 });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new System.IO.InvalidDataException("Neo4j CQL snapshot must be an object.");
        var result = new Dictionary<string, object>(StringComparer.Ordinal) { ["SchemaVersion"] = SchemaVersion };
        var pending = new Stack<(JsonElement Value, string[] Path)>();
        JsonElement metadata = default;
        var metadataCount = 0;
        foreach (var property in root.EnumerateObject())
            if (string.Equals(property.Name, "MetaData", StringComparison.OrdinalIgnoreCase))
            {
                if (++metadataCount > 1) throw new System.IO.InvalidDataException("Ambiguous Neo4j CQL metadata field.");
                metadata = property.Value;
            }
        if (metadataCount != 0)
        {
            if (metadata.ValueKind != JsonValueKind.Object)
                throw new System.IO.InvalidDataException("Neo4j CQL metadata must be an object.");
            foreach (var item in metadata.EnumerateObject()) pending.Push((item.Value, new[] { item.Name }));
        }
        while (pending.Count != 0)
        {
            var (value, path) = pending.Pop();
            var key = PathKey(path);
            if (result.ContainsKey(key + "_kind")) throw new System.IO.InvalidDataException("Duplicate Neo4j CQL metadata path.");
            switch (value.ValueKind)
            {
                case JsonValueKind.Null: result[key + "_kind"] = "null"; break;
                case JsonValueKind.String:
                    result[key + "_kind"] = "string"; result[key + "_value"] = value.GetString(); break;
                case JsonValueKind.True:
                case JsonValueKind.False:
                    result[key + "_kind"] = "boolean"; result[key + "_value"] = value.GetBoolean(); break;
                case JsonValueKind.Number:
                    // Do not turn floating-point/decimal JSON or integral-looking decimal
                    // lexemes into an integer domain. Wider numeric admission comes later.
                    var raw = value.GetRawText();
                    if (raw.IndexOfAny(new[] { '.', 'e', 'E' }) < 0
                        && long.TryParse(raw, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var integer))
                    { result[key + "_kind"] = "integer"; result[key + "_value"] = integer; }
                    else { result[key + "_kind"] = "number"; result[key + "_value"] = raw; }
                    break;
                case JsonValueKind.Object:
                    result[key + "_kind"] = "object";
                    foreach (var child in value.EnumerateObject())
                    {
                        var nested = new string[path.Length + 1]; Array.Copy(path, nested, path.Length); nested[path.Length] = child.Name;
                        pending.Push((child.Value, nested));
                    }
                    break;
                case JsonValueKind.Array: result[key + "_kind"] = "array"; break;
                default: throw new System.IO.InvalidDataException("Unsupported Neo4j CQL metadata value.");
            }
        }
        // Reuse the canonical reader's defaults and GUID normalization. Missing
        // historical JSON fields must query like their materialized CLR values.
        var record = Core.Serialization.StoredRecordJson.DeserializeStoredHolon(snapshot);
        result["f_Id"] = record.Id.ToString("D");
        if (record.Name != null) result["f_Name"] = record.Name;
        if (record.Description != null) result["f_Description"] = record.Description;
        result["f_Version"] = (long)record.Version;
        result["f_HolonType"] = (long)record.HolonType;
        result["f_IsActive"] = record.IsActive; result["f_IsPublic"] = record.IsPublic;
        result["f_ParentHolonId"] = record.ParentHolonId.ToString("D");
        result["f_VersionId"] = record.VersionId.ToString("D");
        result["f_PreviousVersionId"] = record.PreviousVersionId.ToString("D");
        result["f_CreatedByAvatarId"] = record.CreatedByAvatarId.ToString("D");
        result["f_ModifiedByAvatarId"] = record.ModifiedByAvatarId.ToString("D");
        return result;
    }
}
