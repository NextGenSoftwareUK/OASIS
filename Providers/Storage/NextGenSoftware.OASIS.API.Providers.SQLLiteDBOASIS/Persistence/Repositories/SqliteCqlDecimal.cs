using System.Text.Json;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.API.Core.Serialization;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

// This intrinsic executes inside SQLite's predicate/order/reduction pipeline.
// It reads the original token, never a rounded json_each REAL or a hydrated row.
internal static class SqliteCqlDecimal
{
    internal const string Function = "cosmic_cql_decimal_key";
    internal const int MaximumTokenCharacters = 4096;
    internal static string Column(string alias)
        => Function + "(" + alias + ".json -> " + alias + ".fullkey) COLLATE BINARY";

    internal static string Key(string json)
    {
        if (json == null) return null;
        try
        {
            using var document = JsonDocument.Parse(json);
            var value = document.RootElement;
            if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var number)) return null;
            if (!StoredRecordValueEquality.Equivalent(new JRaw(value.GetRawText()), new JValue(number))) return null;
            return CosmicNumericScalarResults.DecimalOrderKey(number);
        }
        catch (JsonException) { return null; } // Not a numeric JSON scalar, never a substitute value.
    }
}
