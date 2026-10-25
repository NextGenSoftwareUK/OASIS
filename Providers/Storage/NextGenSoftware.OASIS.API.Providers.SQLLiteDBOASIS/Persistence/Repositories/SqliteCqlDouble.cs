using System;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

// Interpret a numeric token as finite IEEE binary64 inside the native query.
// SQLite REAL extraction is not the conversion owner: it can round differently.
internal static class SqliteCqlDouble
{
    internal const string Function = "cosmic_cql_double_key";
    internal static string Column(string alias)
        => Function + "(" + alias + ".json -> " + alias + ".fullkey) COLLATE BINARY";

    internal static string Key(string json)
    {
        if (json == null) return null;
        try
        {
            using var document = JsonDocument.Parse(json);
            var token = document.RootElement;
            if (token.ValueKind != JsonValueKind.Number || !token.TryGetDouble(out var value)
                || double.IsNaN(value) || double.IsInfinity(value)) return null;

            // A nonzero external number below binary64's domain must not turn
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
