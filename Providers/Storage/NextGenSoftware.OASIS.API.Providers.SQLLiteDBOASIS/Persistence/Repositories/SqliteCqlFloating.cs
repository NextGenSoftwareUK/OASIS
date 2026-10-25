using System;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

// Interpret a numeric token as finite IEEE binary32/binary64 inside the native query.
// SQLite REAL extraction is not the conversion owner: it can round differently.
internal static class SqliteCqlFloating
{
    internal const string DoubleFunction = "cosmic_cql_double_key";
    internal const string SingleFunction = "cosmic_cql_single_key";
    internal static string Column(string alias, Type type)
        => (type == typeof(float) ? SingleFunction : DoubleFunction) + "(" + alias + ".json -> " + alias + ".fullkey) COLLATE BINARY";

    internal static string DoubleKey(string json) => Key(json, false);
    internal static string SingleKey(string json) => Key(json, true);

    private static string Key(string json, bool single)
    {
        if (json == null) return null;
        try
        {
            using var document = JsonDocument.Parse(json);
            var token = document.RootElement;
            if (token.ValueKind != JsonValueKind.Number) return null;
            double value;
            if (single)
            {
                // Parse the original token directly into binary32. Casting a
                // binary64 intermediate can double-round a near-halfway value.
                if (!token.TryGetSingle(out var number) || float.IsNaN(number) || float.IsInfinity(number)) return null;
                value = number;
            }
            else if (!token.TryGetDouble(out value) || double.IsNaN(value) || double.IsInfinity(value)) return null;

            // A nonzero external number below the declared domain must not turn
            // into a matching zero. Signed/exponent zero itself remains valid.
            if (value == 0d)
            {
                var raw = token.GetRawText();
                for (var index = 0; index < raw.Length && raw[index] != 'e' && raw[index] != 'E'; index++)
                    if (raw[index] >= '1' && raw[index] <= '9') return null;
            }
            return CosmicFloatingScalarResults.DoubleOrderKey(value);
        }
        catch (JsonException) { return null; }
    }
}
