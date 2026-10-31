using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Serializers;

// Native dates retain their indexed BSON shape. This companion belongs to the
// containing storage record, never to the caller's business metadata dictionary.
internal static class MongoMetadataPrecision
{
    internal const string Field = "MetaDataExactUtcTicks";
    private static string Escape(string name) => name.Replace("~", "~0").Replace("/", "~1");

    internal static BsonArray Capture(Dictionary<string, object> metadata)
    {
        var values = new BsonArray();
        var ancestors = new HashSet<object>(System.Collections.Generic.ReferenceEqualityComparer.Instance);
        void Visit(object value, string path)
        {
            if (value is JValue scalar) value = scalar.Value;
            if (value is DateTime date)
            {
                var ticks = MongoAuditDatePrecision.ExactTicks(date);
                if (ticks.HasValue) values.Add(new BsonDocument { { "Path", path }, { "Ticks", ticks.Value } });
                return;
            }
            if (value is not Dictionary<string, object> && value is not JObject && value is not JArray && value is not Array) return;
            if (value is byte[]) return;
            if (!ancestors.Add(value)) throw new FormatException("Cyclic metadata cannot be captured for storage.");
            try
            {
                if (value is Dictionary<string, object> dictionary)
                    foreach (var pair in dictionary) Visit(pair.Value, path + "/" + Escape(pair.Key));
                else if (value is JObject obj)
                    foreach (var pair in obj.Properties()) Visit(pair.Value, path + "/" + Escape(pair.Name));
                else
                {
                    var index = 0;
                    foreach (var item in (IEnumerable)value) Visit(item, path + "/" + (index++).ToString(CultureInfo.InvariantCulture));
                }
            }
            finally { ancestors.Remove(value); }
        }
        Visit(metadata, "");
        return values.Count == 0 ? null : values;
    }

    internal static void Restore(Dictionary<string, object> metadata, BsonArray precision)
    {
        if (precision == null) return;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in precision)
        {
            if (item is not BsonDocument row || row.ElementCount != 2
                || !row.TryGetValue("Path", out var pathValue) || !pathValue.IsString
                || !row.TryGetValue("Ticks", out var ticks) || !ticks.IsInt64)
                throw new FormatException("Metadata precision entry is invalid.");
            var path = pathValue.AsString;
            if (!path.StartsWith("/", StringComparison.Ordinal) || !seen.Add(path))
                throw new FormatException("Metadata precision path is invalid or duplicated.");
            var parts = path.Substring(1).Split('/'); object container = metadata;
            for (var index = 0; index < parts.Length; index++)
            {
                var encoded = parts[index]; var name = encoded.Replace("~1", "/").Replace("~0", "~");
                if (Escape(name) != encoded) throw new FormatException("Metadata precision path escaping is invalid.");
                object current; Action<object> assign;
                if (container is Dictionary<string, object> dictionary && dictionary.TryGetValue(name, out current))
                    assign = value => dictionary[name] = value;
                else if (container is object[] array && int.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out var position)
                    && position >= 0 && position < array.Length && position.ToString(CultureInfo.InvariantCulture) == name)
                { current = array[position]; assign = value => array[position] = value; }
                else throw new FormatException("Metadata precision path has no matching stored value.");
                if (index == parts.Length - 1)
                {
                    if (current is not DateTime projected) throw new FormatException("Metadata precision has no native BSON date projection.");
                    assign(MongoAuditDatePrecision.Restore(projected, true, ticks.AsInt64, "metadata"));
                }
                else container = current;
            }
        }
    }
}
