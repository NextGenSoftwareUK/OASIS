using System;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private sealed partial class CqlSqlTranslator
    {
        private sealed class GroupFilterCell
        {
            internal string Value, High, Low;
            internal Type Type;
            internal bool IsSum => High != null;
            internal string IsNull => IsSum ? "0" : "(" + Value + " IS NULL)";
        }
        private static (string High, string Low) GroupedIntegralParts(string high, string low)
        {
            var remainder = "(" + low + "%4294967296)";
            var quotient = "(" + high + "+" + low + "/4294967296)";
            return ("(CASE WHEN " + remainder + "<0 THEN " + quotient + "-1 ELSE " + quotient + " END)",
                "(CASE WHEN " + remainder + "<0 THEN " + remainder + "+4294967296 ELSE " + remainder + " END)");
        }
        private static (string High, string Low) GroupedParts(GroupFilterCell cell)
            => cell.IsSum ? GroupedIntegralParts(cell.High, cell.Low)
                : GroupedIntegralParts("(" + cell.Value + "/4294967296)", "(" + cell.Value + "%4294967296)");
        private static string GroupCollation(Type declared)
        {
            var type = Nullable.GetUnderlyingType(declared) ?? declared;
            return type == typeof(Guid) ? " COLLATE NOCASE"
                : type == typeof(string) || type == typeof(DateTime) ? " COLLATE BINARY" : "";
        }
        private GroupFilterCell GroupCell<T, TKey, TResult>(CosmicGroupedSelection<T, TKey, TResult> query,
            CosmicQueryPath path) where T : IHolon
        {
            var index = !path.IsMetadata && path.Segments.Count == 1
                ? query.Columns.ToList().FindIndex(column => column.Alias == path.Segments[0] && column.ValueType == path.ValueType) : -1;
            if (index < 0) throw new NotSupportedException("Grouped filter must bind an exact selected output.");
            var column = query.Columns[index];
            if (column.Operation == CosmicScalarOperation.Average)
                throw new NotSupportedException("Exact normalized Average filtering is not enrolled.");
            var cell = new GroupFilterCell { Type = column.ValueType };
            if (column.KeyIndex >= 0) cell.Value = "k" + column.KeyIndex + "v";
            else if (column.Operation is CosmicScalarOperation.Count or CosmicScalarOperation.LongCount) cell.Value = "inputs";
            else if (column.Operation == CosmicScalarOperation.Sum) { cell.High = "a" + index + "h"; cell.Low = "a" + index + "l"; }
            else if (column.Operation is CosmicScalarOperation.Min or CosmicScalarOperation.Max) cell.Value = "a" + index + "v";
            else throw new NotSupportedException("Unknown grouped filter operation.");
            if (!cell.IsSum) cell.Value += GroupCollation(column.ValueType);
            return cell;
        }
        private static string ComparisonSymbol(CosmicQueryComparisonOperator operation) => operation switch
        {
            CosmicQueryComparisonOperator.Equal => "=", CosmicQueryComparisonOperator.NotEqual => "<>",
            CosmicQueryComparisonOperator.GreaterThan => ">", CosmicQueryComparisonOperator.GreaterThanOrEqual => ">=",
            CosmicQueryComparisonOperator.LessThan => "<", CosmicQueryComparisonOperator.LessThanOrEqual => "<=",
            _ => throw new NotSupportedException("Unknown scalar comparison operator.")
        };
        private static string GroupNullComparison(GroupFilterCell left, GroupFilterCell right,
            CosmicQueryComparisonOperator operation, string raw)
        {
            if (operation == CosmicQueryComparisonOperator.Equal)
                return "(CASE WHEN " + left.IsNull + " THEN " + right.IsNull + " WHEN " + right.IsNull + " THEN 0 ELSE COALESCE((" + raw + "),0) END)";
            if (operation == CosmicQueryComparisonOperator.NotEqual)
                return "(CASE WHEN " + left.IsNull + " THEN NOT " + right.IsNull + " WHEN " + right.IsNull + " THEN 1 ELSE COALESCE((" + raw + "),0) END)";
            return "(CASE WHEN " + left.IsNull + " OR " + right.IsNull + " THEN 0 ELSE COALESCE((" + raw + "),0) END)";
        }
        private static string GroupComparison(GroupFilterCell left, GroupFilterCell right, CosmicQueryComparisonOperator operation)
        {
            var symbol = ComparisonSymbol(operation);
            string raw;
            if (left.IsSum || right.IsSum)
            {
                var first = GroupedParts(left); var second = GroupedParts(right);
                var equal = "(" + first.High + "=" + second.High + " AND " + first.Low + "=" + second.Low + ")";
                raw = operation == CosmicQueryComparisonOperator.Equal ? equal
                    : operation == CosmicQueryComparisonOperator.NotEqual ? "NOT " + equal
                    : "(" + first.High + (operation is CosmicQueryComparisonOperator.GreaterThan or CosmicQueryComparisonOperator.GreaterThanOrEqual ? ">" : "<")
                        + second.High + " OR (" + first.High + "=" + second.High + " AND " + first.Low + symbol + second.Low + "))";
            }
            else raw = left.Value + symbol + right.Value;
            return GroupNullComparison(left, right, operation, raw);
        }
        private string GroupLiteralComparison(GroupFilterCell left, CosmicQueryPath path,
            CosmicQueryLiteral literal, CosmicQueryComparisonOperator operation)
        {
            if (literal.Kind != CosmicQueryScalarKind.Null && literal.Kind != path.ScalarKind)
            {
                if (path.ScalarKind != CosmicQueryScalarKind.Int64) throw new NotSupportedException("Unsupported grouped mixed scalar comparison.");
                var boundary = IntegerBoundary(literal, operation, long.MinValue, long.MaxValue);
                if (boundary.Constant.HasValue)
                    return GroupNullComparison(left, new GroupFilterCell { Value = "0", Type = path.ValueType }, operation,
                        boundary.Constant.Value ? "1" : "0");
                return GroupComparison(left, new GroupFilterCell { Value = Bind(boundary.Boundary), Type = path.ValueType }, operation);
            }
            object value = literal.Value;
            if (literal.Kind == CosmicQueryScalarKind.Guid) value = ((Guid)value).ToString("D");
            else if (literal.Kind == CosmicQueryScalarKind.UtcDateTime) value = SqliteCqlDate.Literal((DateTime)value);
            else if (literal.Kind == CosmicQueryScalarKind.Boolean) value = (bool)value ? 1L : 0L;
            return GroupComparison(left, new GroupFilterCell { Value = value == null ? "NULL" : Bind(value), Type = path.ValueType }, operation);
        }
        private string TranslateGroupPredicate<T, TKey, TResult>(CosmicGroupedSelection<T, TKey, TResult> query,
            CosmicQueryPredicate predicate) where T : IHolon
        {
            if (predicate is CosmicQueryBoolean boolean) return boolean.Value ? "1" : "0";
            if (predicate is CosmicQueryLogical logical)
                return "(" + TranslateGroupPredicate(query, logical.Left) + (logical.IsAnd ? " AND " : " OR ") + TranslateGroupPredicate(query, logical.Right) + ")";
            if (predicate is CosmicQueryNot not) return "(NOT " + TranslateGroupPredicate(query, not.Operand) + ")";
            if (predicate is CosmicQueryComparison comparison)
                return GroupLiteralComparison(GroupCell(query, comparison.Path), comparison.Path, comparison.Literal, comparison.Operator);
            if (predicate is CosmicQueryFieldComparison fields)
                return GroupComparison(GroupCell(query, fields.Left), GroupCell(query, fields.Right), fields.Operator);
            if (predicate is CosmicQueryMembership membership)
            {
                var cell = GroupCell(query, membership.Path);
                return membership.Values.Count == 0 ? "0" : "(" + string.Join(" OR ", membership.Values.Select(literal =>
                    GroupLiteralComparison(cell, membership.Path, literal, CosmicQueryComparisonOperator.Equal))) + ")";
            }
            if (predicate is CosmicQueryStringMatch match)
            {
                var cell = GroupCell(query, match.Path); var literal = Bind(match.Value);
                var raw = match.Operator == CosmicQueryStringOperator.Contains ? "instr(" + cell.Value + "," + literal + ")>0"
                    : match.Operator == CosmicQueryStringOperator.StartsWith ? "substr(" + cell.Value + ",1,length(" + literal + "))=" + literal + " COLLATE BINARY"
                    : match.Operator == CosmicQueryStringOperator.EndsWith ? "(length(" + literal + ")=0 OR substr(" + cell.Value + ",-length(" + literal + "))=" + literal + " COLLATE BINARY)"
                    : throw new NotSupportedException("Unknown grouped string operation.");
                return "(CASE WHEN " + cell.IsNull + " THEN 0 ELSE COALESCE((" + raw + "),0) END)";
            }
            throw new NotSupportedException("Unsupported grouped predicate.");
        }

        // One exact boundary owner for source Int32 and grouped integral domains.
        // Decimal/double values never reach SQLite as approximate numeric parameters.
        private static (bool? Constant, long Boundary) IntegerBoundary(CosmicQueryLiteral literal,
            CosmicQueryComparisonOperator operation, long minimum, long maximum)
        {
            long floor = 0, ceiling = 0; var outside = 0;
            if (literal.Kind == CosmicQueryScalarKind.Decimal)
            {
                var number = (decimal)literal.Value;
                if (number < minimum) outside = -1;
                else if (number > maximum) outside = 1;
                else { floor = (long)decimal.Floor(number); ceiling = (long)decimal.Ceiling(number); }
            }
            else if (literal.Kind == CosmicQueryScalarKind.Double)
            {
                var number = (double)literal.Value;
                if (double.IsNaN(number) || double.IsInfinity(number)) throw new NotSupportedException("Mixed literals must be finite.");
                if (number < minimum) outside = -1;
                else if (maximum == long.MaxValue ? number >= 9223372036854775808d : number > maximum) outside = 1;
                else { floor = (long)Math.Floor(number); ceiling = (long)Math.Ceiling(number); }
            }
            else throw new NotSupportedException("Mixed integral literals require Decimal or binary floating point.");
            if (outside != 0)
            {
                var always = operation == CosmicQueryComparisonOperator.NotEqual || (outside < 0
                    ? operation is CosmicQueryComparisonOperator.GreaterThan or CosmicQueryComparisonOperator.GreaterThanOrEqual
                    : operation is CosmicQueryComparisonOperator.LessThan or CosmicQueryComparisonOperator.LessThanOrEqual);
                return (always, 0);
            }
            if (floor != ceiling && operation is CosmicQueryComparisonOperator.Equal or CosmicQueryComparisonOperator.NotEqual)
                return (operation == CosmicQueryComparisonOperator.NotEqual, 0);
            return (null, operation is CosmicQueryComparisonOperator.GreaterThanOrEqual or CosmicQueryComparisonOperator.LessThan ? ceiling : floor);
        }
    }
}
