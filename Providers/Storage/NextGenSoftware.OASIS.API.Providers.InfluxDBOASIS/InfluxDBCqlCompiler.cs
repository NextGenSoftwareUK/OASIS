using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.InfluxDBOASIS;

public static class InfluxDBCqlCompiler
{
    public static CosmicQueryCapabilities Capabilities => BoundedIdentityCqlCompiler.Capabilities;

    public static OASISResult<IReadOnlyList<Guid>> Compile<T>(CosmicQuery<T> query, ProviderType authority) where T : IHolon
        => BoundedIdentityCqlCompiler.Compile(query, authority, ProviderType.InfluxDBOASIS);

    /// <summary>Flux UTF-8 hex literals cannot introduce quotes, code or ${} interpolation.</summary>
    public static string FluxStringLiteral(string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        var bytes = new UTF8Encoding(false, true).GetBytes(value);
        return "\""+string.Concat(bytes.Select(b => "\\x"+b.ToString("x2", System.Globalization.CultureInfo.InvariantCulture)))+"\"";
    }

    public static string BuildNativeFlux(string bucket, IReadOnlyList<Guid> ids)
    {
        if (string.IsNullOrEmpty(bucket)) throw new ArgumentException("Explicit bucket required.", nameof(bucket));
        if (ids == null || ids.Count < 1 || ids.Count > 100 || ids.Distinct().Count() != ids.Count)
            throw new ArgumentException("One to 100 distinct admitted identities required.", nameof(ids));
        var predicate = string.Join(" or ", ids.Select(id => "r.id == "+FluxStringLiteral(id.ToString("D"))));
        // Writes use provider-generated UTC timestamps. Read the complete post-epoch
        // range, including future clock skew, rather than hiding points after now().
        // Joining maxima retains tied heads so duplicate validation rejects ambiguity.
        return "rows = from(bucket: "+FluxStringLiteral(bucket)+")\n"+
            "  |> range(start: 0, stop: time(v: 9223372036854775806))\n"+
            "  |> filter(fn: (r) => r._measurement == \"oasis_holons\" and ("+predicate+"))\n"+
            "  |> filter(fn: (r) => r._field == \"data_json\" or r._field == \"is_deleted\")\n"+
            "  |> pivot(rowKey: [\"_time\"], columnKey: [\"_field\"], valueColumn: \"_value\")\n"+
            "  |> group(columns: [\"id\"])\n"+
            "heads = rows |> max(column: \"_time\") |> keep(columns: [\"id\", \"_time\"])\n"+
            "join(tables: {rows: rows, heads: heads}, on: [\"id\", \"_time\"])\n"+
            "  |> map(fn: (r) => ({id: r.id, complete: exists r.data_json and exists r.is_deleted, "+
            "data_json: if exists r.data_json then r.data_json else \"\", "+
            "is_deleted: if exists r.is_deleted then r.is_deleted else false}))";
    }
}
