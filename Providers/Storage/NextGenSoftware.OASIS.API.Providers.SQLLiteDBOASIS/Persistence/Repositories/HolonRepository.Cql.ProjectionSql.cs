using System.Linq;
using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private sealed partial class CqlSqlTranslator
    {
        internal bool IndexedProjectionColumns { get; set; }

        internal (string State, string Value) ProjectionOperand(CosmicProjectionColumn column, string prefix, bool comparable = false)
        {
            if (column == null) throw new ArgumentNullException(nameof(column));
            if (column.Function == CosmicProjectionFunction.None) return ProjectionOperand(column.Path, prefix, comparable);
            if(column.IsUtcDatePart)
            {
                if(column.Path.ValueType!=typeof(DateTime) || (Nullable.GetUnderlyingType(column.ValueType)??column.ValueType)!=typeof(int))
                    throw new NotSupportedException("UTC date parts require DateTime input and Int32 output.");
                var utcSource=ProjectionOperand(column.Path,prefix+"utc");
                return (utcSource.State,"CASE WHEN ("+utcSource.State+")=2 THEN "+
                    SqliteCqlUtcDateParts.Value(column.Function,utcSource.Value)+" ELSE NULL END");
            }
            if (column.Function == CosmicProjectionFunction.DoubleAffine)
            {
                if(column.Path.ValueType != typeof(double) || column.ValueType != typeof(double) || !column.DoubleMultiplier.HasValue || !column.DoubleOffset.HasValue)
                    throw new NotSupportedException("Native Double arithmetic requires an exact Double path and captured coefficients.");
                var doubleSource=ProjectionOperand(column.Path,prefix,comparable);
                // Metadata Doubles project as exact order keys; arithmetic reads the stored JSON number instead.
                var rawDouble=column.Path.IsMetadata
                    ? "("+MetadataSubquery(column.Path.Segments, alias => alias+".value", _ => "1", prefix+"dv")+" LIMIT 1)"
                    : doubleSource.Value;
                var doubleComputed="((CAST("+rawDouble+" AS REAL))*"+Bind(column.DoubleMultiplier.Value)+"+"+Bind(column.DoubleOffset.Value)+")";
                // A finite result has an absolute value no greater than Double.MaxValue; overflow (Inf) and NaN fail this test.
                var finite="abs("+doubleComputed+") <= 1.7976931348623157e308";
                // The result is published in the same exact order-key form as a selected Double (17 significant digits round-trip).
                var encoded=SqliteCqlFloating.DoubleFunction+"(printf('%.17g',"+doubleComputed+"))";
                return ("CASE WHEN ("+doubleSource.State+")=2 AND NOT coalesce("+finite+",0) THEN 3 ELSE ("+doubleSource.State+") END",
                    "CASE WHEN ("+doubleSource.State+")=2 AND coalesce("+finite+",0) THEN "+encoded+" ELSE NULL END");
            }
            if (column.Function == CosmicProjectionFunction.Int32Affine)
            {
                if(column.Path.ValueType != typeof(int) || column.ValueType != typeof(int) || !column.Int32Multiplier.HasValue || !column.Int32Offset.HasValue)
                    throw new NotSupportedException("Native Int32 arithmetic requires an exact Int32 path and captured coefficients.");
                var arithmeticSource=ProjectionOperand(column.Path,prefix,comparable);
                var computed="(("+arithmeticSource.Value+")*"+Bind((long)column.Int32Multiplier.Value)+"+"+Bind((long)column.Int32Offset.Value)+")";
                // SQLite integer division and remainder truncate toward zero, matching C#.
                if(column.Int32Divisor.HasValue)computed="("+computed+(column.Int32IsModulo?" % ":" / ")+Bind((long)column.Int32Divisor.Value)+")";
                return ("CASE WHEN ("+arithmeticSource.State+")=2 AND "+computed+" NOT BETWEEN -2147483648 AND 2147483647 THEN 3 ELSE ("+arithmeticSource.State+") END",
                    "CASE WHEN ("+arithmeticSource.State+")=2 AND "+computed+" BETWEEN -2147483648 AND 2147483647 THEN "+computed+" ELSE NULL END");
            }
            if (column.Function == CosmicProjectionFunction.TextToInt32Case)
            {
                if(column.Path.ValueType != typeof(string) || column.ValueType != typeof(int)
                    || column.Int32Cases == null || column.Int32Cases.Count == 0 || !column.Int32Replacement.HasValue)
                    throw new NotSupportedException("Native text-to-Int32 CASE requires an exact String path and captured literal branches.");
                var numericSource=ProjectionOperand(column.Path,prefix,comparable);
                var numericBranches=string.Concat(column.Int32Cases.Select(entry=>" WHEN "+Bind(entry.Key)+" THEN "+Bind((long)entry.Value)));
                return ("CASE WHEN ("+numericSource.State+")=2 THEN 2 ELSE 3 END",
                    "CASE WHEN ("+numericSource.State+")=2 THEN CASE ("+numericSource.Value+")"+numericBranches+" ELSE "+Bind((long)column.Int32Replacement.Value)+" END ELSE NULL END");
            }
            if (column.Function == CosmicProjectionFunction.TextValueCase)
            {
                if(column.Path.ValueType != typeof(string) || column.ValueType != typeof(string)
                    || column.TextCases == null || column.TextCases.Count == 0 || column.WhenFalse == null)
                    throw new NotSupportedException("Native multi-branch text CASE requires an exact String path and captured literal branches.");
                var caseSource=ProjectionOperand(column.Path,prefix,comparable);
                var branches=string.Concat(column.TextCases.Select(entry=>" WHEN "+Bind(entry.Key)+" THEN "+Bind(entry.Value)));
                return ("CASE WHEN ("+caseSource.State+")=2 THEN 2 ELSE 3 END",
                    "CASE WHEN ("+caseSource.State+")=2 THEN CASE ("+caseSource.Value+")"+branches+" ELSE "+Bind(column.WhenFalse)+" END ELSE NULL END");
            }
            if (column.Function == CosmicProjectionFunction.BooleanTextCase)
            {
                if(column.Path.ValueType != typeof(bool) || column.ValueType != typeof(string)
                    || column.WhenTrue == null || column.WhenFalse == null)
                    throw new NotSupportedException("Native conditional text requires an exact Boolean path and captured literal branches.");
                var conditional=ProjectionOperand(column.Path,prefix,comparable);
                var whenTrue=Bind(column.WhenTrue);var whenFalse=Bind(column.WhenFalse);
                return ("CASE WHEN ("+conditional.State+")=2 THEN 2 ELSE 3 END",
                    "CASE WHEN ("+conditional.State+")=2 THEN CASE WHEN ("+conditional.Value+")=1 THEN "+whenTrue+" ELSE "+whenFalse+" END ELSE NULL END");
            }
            if ((column.Function == CosmicProjectionFunction.TextCoalesce || column.Function == CosmicProjectionFunction.Int32Coalesce))
            {
                var coalesceIsText=column.Function==CosmicProjectionFunction.TextCoalesce;
              if(coalesceIsText
                  ? column.Path.ValueType!=typeof(string) || column.ValueType!=typeof(string) || column.NullReplacement==null
                  : column.Path.ValueType!=typeof(int?) || column.ValueType!=typeof(int) || !column.Int32Replacement.HasValue)
                    throw new NotSupportedException("Native coalescing requires exact source and captured replacement domains.");
                var input=ProjectionOperand(column.Path,prefix,comparable);
                var replacement=(coalesceIsText ? Bind(column.NullReplacement) : Bind(column.Int32Replacement.Value));
                return ("CASE WHEN ("+input.State+") IN (0,1) THEN 2 ELSE ("+input.State+") END",
                    "CASE WHEN ("+input.State+") IN (0,1) THEN "+replacement+" ELSE ("+input.Value+") END");
            }
            if (column.Function != CosmicProjectionFunction.Utf16Length ||
                column.Path.ValueType != typeof(string) ||
                (Nullable.GetUnderlyingType(column.ValueType) ?? column.ValueType) != typeof(int))
                throw new NotSupportedException("The computed projection has no admitted native implementation.");
            var source = ProjectionOperand(column.Path, prefix, comparable);
            return (source.State, "CASE WHEN (" + source.State + ")=2 THEN "
                + SqliteCqlReadBudget.TextStringLengthFunction + "(" + source.Value + ") ELSE NULL END");
        }

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
            if(IndexedProjectionColumns)
            {
                // The index has its own explicit physical schema (including
                // VersionId/IsDeleted); do not borrow the entity-table field map.
                if(path.Segments.Count!=1||!SqliteCosmicChainIndexStore.CanonicalProjectionCapabilities.CanonicalFields.Any(
                    field=>field.Name==path.Segments[0]&&field.ValueType==path.ValueType))
                    throw new NotSupportedException("Indexed canonical projection column is not enrolled.");
                native=CanonicalColumn(path.Segments[0]);
            }
            else FieldOperand(path, prefix, (column, _) => { native = column; return "1"; });
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
                var operand = ProjectionOperand(columns[index], "cqlproject" + index);
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

        internal string TranslateNumericAggregate(CosmicQueryPath path, string predicate, string ordering, bool checkedPrefix = false)
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
                + " AS v" + (checkedPrefix ? ",row_number() OVER (ORDER BY " + ordering + ") AS ordinal" : "")
                + " FROM Holon WHERE " + predicate + " ORDER BY " + ordering + " LIMIT $countTake OFFSET $countOffset),"
                + " bounded AS (SELECT s,v" + (checkedPrefix ? ",ordinal" : "") + " FROM selected"
                + (checkedPrefix ? " ORDER BY ordinal" : "") + " LIMIT " + sentinel + "),"
                + " aggregated AS (SELECT count(*) AS inputs,COALESCE(sum(CASE WHEN " + invalid + " THEN 1 ELSE 0 END),0) AS invalid,"
                + "COALESCE(sum(CASE WHEN s=2 THEN 1 ELSE 0 END),0) AS present,"
                + string.Join(",", fields.Select(field => "COALESCE(sum(CASE WHEN s=2 THEN " + field.Value + " ELSE 0 END),0) AS " + field.Name))
                + (checkedPrefix ? "," + SqliteCqlCheckedInt64Average.Function + "(CASE WHEN s=2 THEN v ELSE NULL END ORDER BY ordinal) AS prefixOverflow" : "")
                + " FROM bounded) SELECT invalid,present," + string.Join(",", fields.Select(field => field.Name)) + ",CASE WHEN inputs>"
                + maximum.ToString(System.Globalization.CultureInfo.InvariantCulture) + " THEN 1 ELSE 0 END AS overWork"
                + (checkedPrefix ? ",prefixOverflow" : "") + " FROM aggregated";
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
