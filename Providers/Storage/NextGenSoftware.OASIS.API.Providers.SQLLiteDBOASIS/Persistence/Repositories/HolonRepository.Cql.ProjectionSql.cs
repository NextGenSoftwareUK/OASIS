using System.Linq;
using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private sealed partial class CqlSqlTranslator
    {
        internal (string State, string Value) ProjectionOperand(CosmicQueryPath path, string prefix, bool comparable = false)
        {
            if (path.IsMetadata)
            {
                var count = "(" + MetadataSubquery(path.Segments, _ => "count(*)", _ => "1", prefix + "c") + ")";
                var kind = "(" + MetadataSubquery(path.Segments, alias =>
                    "CASE WHEN " + alias + ".type='null' THEN 1 WHEN " + MetadataTypeGuard(path.ValueType, alias)
                    + " THEN 2 ELSE 3 END", _ => "1", prefix + "s") + " LIMIT 1)";
                var state = "CASE WHEN " + count + ">1 THEN 3 ELSE COALESCE(" + kind + ",0) END";
                var metadataType = Nullable.GetUnderlyingType(path.ValueType) ?? path.ValueType;
                var normalized = comparable || metadataType == typeof(ulong) || metadataType == typeof(decimal) || metadataType == typeof(double) || metadataType == typeof(float);
                var value = "(" + MetadataSubquery(path.Segments, alias => normalized ? MetadataColumn(alias, path.ValueType)
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

        private static (string High, string Low) IntegralParts(Type declared, string value)
        {
            var type = Nullable.GetUnderlyingType(declared) ?? declared;
            if (type == typeof(ulong))
            {
                // UInt64 is a canonical 20-digit key. Its two decimal chunks
                // each fit Int64. This intermediate stays below 2^62, so every
                // operation remains SQLite INTEGER, including above Int64.MaxValue.
                var upper = "CAST(substr(" + value + ",1,10) AS INTEGER)";
                var lower = "CAST(substr(" + value + ",11,10) AS INTEGER)";
                var remainder = "(" + upper + "*1410065408+" + lower + ")";
                return ("(" + upper + "*2+" + remainder + "/4294967296)", "(" + remainder + "%4294967296)");
            }
            if (!type.IsEnum && type != typeof(byte) && type != typeof(sbyte) && type != typeof(short)
                && type != typeof(ushort) && type != typeof(int) && type != typeof(uint) && type != typeof(long))
                throw new NotSupportedException("Native integral parts require an exact integer domain.");
            return ("(" + value + "/4294967296)", "(" + value + "%4294967296)");
        }

        private static string[] DecimalParts(string value)
        {
            // Decimal keys carry a sign plus57 scaled digits. Sign-dependent
            // padding restores seven full base1e9 words before complement decode.
            var negative = "substr(" + value + ",1,1)='0'";
            var digits = "((CASE WHEN " + negative + " THEN '999999' ELSE '000000' END)||substr(" + value + ",2))";
            return Enumerable.Range(0, CosmicDecimalScalarResults.LimbCount).Select(index =>
            {
                var word = "CAST(substr(" + digits + "," + (index * 9 + 1) + ",9) AS INTEGER)";
                return "(CASE WHEN " + negative + " THEN " + word + "-999999999 ELSE " + word + " END)";
            }).ToArray();
        }

        internal string TranslateNumericAggregate(CosmicQueryPath path, string predicate, string ordering)
        {
            var operand = ProjectionOperand(path, "cqlnumeric", comparable: true);
            var optional = Nullable.GetUnderlyingType(path.ValueType) != null;
            var invalid = optional ? "s=3" : "s<>2";
            var maximum = CosmicNumericScalarResults.MaximumParticipatingValues;
            var sentinel = ((long)maximum + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (FloatingNumeric(path.ValueType))
            {
                var function = (Nullable.GetUnderlyingType(path.ValueType) ?? path.ValueType) == typeof(float)
                    ? SqliteCqlFloatingAggregates.SingleFunction : SqliteCqlFloatingAggregates.DoubleFunction;
                return "/* COSMIC floating aggregate */ WITH selected AS (SELECT " + operand.State + " AS s," + operand.Value
                    + " AS v FROM Holon WHERE " + predicate + " ORDER BY " + ordering + " LIMIT $countTake OFFSET $countOffset),"
                    + " bounded AS (SELECT s,v FROM selected LIMIT " + sentinel + "), aggregated AS (SELECT count(*) AS inputs,"
                    + "COALESCE(sum(CASE WHEN " + invalid + " THEN 1 ELSE 0 END),0) AS invalid,"
                    + "COALESCE(sum(CASE WHEN s=2 THEN 1 ELSE 0 END),0) AS present,"
                    + function + "(CASE WHEN s=2 THEN v ELSE NULL END) AS packet FROM bounded)"
                    + " SELECT invalid,present,packet,CASE WHEN inputs>" + maximum + " THEN 1 ELSE 0 END AS overWork,inputs FROM aggregated";
            }
            var decimalInput = (Nullable.GetUnderlyingType(path.ValueType) ?? path.ValueType) == typeof(decimal);
            var fields = decimalInput ? DecimalParts("v").Select((value, index) => (Name: "p" + index, Value: value)).ToArray()
                : new[] { (Name: "high", Value: IntegralParts(path.ValueType, "v").High),
                    (Name: "low", Value: IntegralParts(path.ValueType, "v").Low) };
            // The extra input is a work-limit sentinel, never a truncated success.
            // Even max+1 rows keep both signed limb sums within Int64. Validate
            // all mandatory traversal scopes separately before the source page.
            return "/* COSMIC numeric aggregate */ WITH selected AS (SELECT " + operand.State + " AS s," + operand.Value
                + " AS v FROM Holon WHERE " + predicate + " ORDER BY " + ordering + " LIMIT $countTake OFFSET $countOffset),"
                + " bounded AS (SELECT s,v FROM selected LIMIT " + sentinel + "),"
                + " aggregated AS (SELECT count(*) AS inputs,COALESCE(sum(CASE WHEN " + invalid + " THEN 1 ELSE 0 END),0) AS invalid,"
                + "COALESCE(sum(CASE WHEN s=2 THEN 1 ELSE 0 END),0) AS present,"
                + string.Join(",", fields.Select(field => "COALESCE(sum(CASE WHEN s=2 THEN " + field.Value + " ELSE 0 END),0) AS " + field.Name))
                + " FROM bounded) SELECT invalid,present," + string.Join(",", fields.Select(field => field.Name)) + ",CASE WHEN inputs>"
                + maximum.ToString(System.Globalization.CultureInfo.InvariantCulture) + " THEN 1 ELSE 0 END AS overWork FROM aggregated";
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
