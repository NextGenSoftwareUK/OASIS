using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private sealed partial class CqlSqlTranslator
    {
        private (string State, string Value) ProjectionOperand(CosmicQueryPath path, string prefix, bool comparable = false)
        {
            if (path.IsMetadata)
            {
                var count = "(" + MetadataSubquery(path.Segments, _ => "count(*)", _ => "1", prefix + "c") + ")";
                var kind = "(" + MetadataSubquery(path.Segments, alias =>
                    "CASE WHEN " + alias + ".type='null' THEN 1 WHEN " + MetadataTypeGuard(path.ValueType, alias)
                    + " THEN 2 ELSE 3 END", _ => "1", prefix + "s") + " LIMIT 1)";
                var state = "CASE WHEN " + count + ">1 THEN 3 ELSE COALESCE(" + kind + ",0) END";
                var value = "(" + MetadataSubquery(path.Segments, alias => comparable ? MetadataColumn(alias, path.ValueType)
                    : alias + ".value", _ => "1", prefix + "v") + " LIMIT 1)";
                return (state, value);
            }
            string native = null;
            FieldOperand(path, prefix, (column, _) => { native = column; return "1"; });
            if (native == null) throw new NotSupportedException("Canonical projection field is unsupported.");
            // Map SQL storage into the same type/value guard used for metadata.
            // Every participating canonical value is checked before an extremum.
            var scalar = "(" + native + ")";
            var kindValue = path.ValueType == typeof(bool)
                ? "CASE WHEN typeof(" + scalar + ")='integer' AND " + scalar + " IN (0,1) THEN CASE WHEN "
                    + scalar + "=1 THEN 'true' ELSE 'false' END ELSE typeof(" + scalar + ") END"
                : "typeof(" + scalar + ")";
            var aliasName = prefix + "domain";
            var guard = "(SELECT " + MetadataTypeGuard(path.ValueType, aliasName) + " FROM (SELECT " + scalar
                + " AS value," + kindValue + " AS type) AS " + aliasName + ")";
            return ("CASE WHEN " + scalar + " IS NULL THEN 1 WHEN " + guard + " THEN 2 ELSE 3 END", native);
        }

        internal string TranslateProjection(IReadOnlyList<CosmicProjectionColumn> columns)
        {
            var sql = new List<string>();
            for (var index = 0; index < columns.Count; index++)
            {
                var operand = ProjectionOperand(columns[index].Path, "cqlproject" + index);
                sql.Add(operand.State + " AS p" + index + "s");
                sql.Add(operand.Value + " AS p" + index + "v");
            }
            return string.Join(", ", sql);
        }

        internal string TranslateExtremum(CosmicQueryPath path, CosmicScalarOperation operation,
            string predicate, string ordering)
        {
            var operand = ProjectionOperand(path, "cqlextreme", comparable: true);
            var nullable = !path.ValueType.IsValueType || Nullable.GetUnderlyingType(path.ValueType) != null;
            var bad = nullable ? "s=3" : "s<>2";
            var aggregate = operation == CosmicScalarOperation.Min ? "MIN" : operation == CosmicScalarOperation.Max ? "MAX"
                : throw new NotSupportedException("Unknown selected scalar operation.");
            var over = "typeof(extremum)='text' AND length(CAST(extremum AS BLOB))>" + CosmicProjectionResults.MaximumPayloadLimit;
            return "/* COSMIC extremum */ WITH selected AS (SELECT " + operand.State + " AS s," + operand.Value
                + " AS v FROM Holon WHERE " + predicate + " ORDER BY " + ordering + " LIMIT $countTake OFFSET $countOffset),"
                + " aggregated AS (SELECT COALESCE(sum(CASE WHEN " + bad + " THEN 1 ELSE 0 END),0) AS invalid,"
                + "COALESCE(sum(CASE WHEN s=2 THEN 1 ELSE 0 END),0) AS present,"
                + aggregate + "(CASE WHEN s=2 THEN v ELSE NULL END) AS extremum FROM selected) "
                + "SELECT invalid,present,CASE WHEN " + over + " THEN 1 ELSE 0 END AS overBudget,"
                + "CASE WHEN " + over + " THEN NULL ELSE extremum END AS value FROM aggregated";
        }
    }
}
