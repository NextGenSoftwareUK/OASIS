using System;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    private static (string High, string Low) GroupedIntegralParts(string high, string low)
    {
        var remainder = "(" + low + " % 4294967296)";
        var quotient = "(" + high + " + " + low + " / 4294967296)";
        return ("CASE WHEN " + remainder + " < 0 THEN " + quotient + " - 1 ELSE " + quotient + " END",
            "CASE WHEN " + remainder + " < 0 THEN " + remainder + " + 4294967296 ELSE " + remainder + " END");
    }
    private sealed class GroupFilterCompiler<T, TKey, TResult> where T : IHolon
    {
        private readonly CosmicGroupedSelection<T, TKey, TResult> query;
        private readonly NeoCqlTranslator translator;
        internal GroupFilterCompiler(CosmicGroupedSelection<T, TKey, TResult> query, NeoCqlTranslator translator)
        { this.query = query; this.translator = translator; }
        private sealed class Cell
        {
            internal string Value, High, Low;
            internal bool IsSum => High != null;
            internal string IsNull => IsSum ? "false" : "(" + Value + " IS NULL)";
        }
        private Cell Value(CosmicQueryPath path)
        {
            var index = !path.IsMetadata && path.Segments.Count == 1
                ? query.Columns.ToList().FindIndex(column => column.Alias == path.Segments[0] && column.ValueType == path.ValueType) : -1;
            if (index < 0) throw new NotSupportedException("Grouped filter requires an exact selected scalar.");
            var column = query.Columns[index];
            if (column.KeyIndex >= 0) return new Cell { Value = "keys[" + column.KeyIndex + "].v" };
            if (column.Operation is CosmicScalarOperation.Count or CosmicScalarOperation.LongCount) return new Cell { Value = "inputs" };
            var slot = query.Columns.Take(index).Count(item => item.KeyIndex < 0 && item.Selector != null);
            var packet = "aggregates[" + slot + "]";
            if (column.Operation == CosmicScalarOperation.Sum) return new Cell { High = packet + ".high", Low = packet + ".low" };
            if (column.Operation is CosmicScalarOperation.Min or CosmicScalarOperation.Max) return new Cell { Value = packet + ".value" };
            throw new NotSupportedException("Exact normalized Average filtering is not enrolled.");
        }
        private static (string High, string Low) Parts(Cell cell) => cell.IsSum ? GroupedIntegralParts(cell.High, cell.Low)
            : GroupedIntegralParts("(" + cell.Value + "/4294967296)", "(" + cell.Value + "%4294967296)");
        private static string NullAware(Cell left, Cell right, CosmicQueryComparisonOperator operation, string raw)
        {
            if (operation == CosmicQueryComparisonOperator.Equal)
                return "(CASE WHEN " + left.IsNull + " THEN " + right.IsNull + " WHEN " + right.IsNull + " THEN false ELSE coalesce((" + raw + "),false) END)";
            if (operation == CosmicQueryComparisonOperator.NotEqual)
                return "(CASE WHEN " + left.IsNull + " THEN NOT " + right.IsNull + " WHEN " + right.IsNull + " THEN true ELSE coalesce((" + raw + "),false) END)";
            return "(CASE WHEN " + left.IsNull + " OR " + right.IsNull + " THEN false ELSE coalesce((" + raw + "),false) END)";
        }
        private static string Compare(Cell left, Cell right, CosmicQueryComparisonOperator operation)
        {
            var symbol = NeoCqlTranslator.Symbol(operation);
            string raw;
            if (left.IsSum || right.IsSum)
            {
                var first = Parts(left); var second = Parts(right);
                var equal = "(" + first.High + "=" + second.High + " AND " + first.Low + "=" + second.Low + ")";
                raw = operation == CosmicQueryComparisonOperator.Equal ? equal
                    : operation == CosmicQueryComparisonOperator.NotEqual ? "NOT " + equal
                    : "(" + first.High + (operation is CosmicQueryComparisonOperator.GreaterThan or CosmicQueryComparisonOperator.GreaterThanOrEqual ? ">" : "<")
                        + second.High + " OR (" + first.High + "=" + second.High + " AND " + first.Low + symbol + second.Low + "))";
            }
            else raw = left.Value + symbol + right.Value;
            return NullAware(left, right, operation, raw);
        }
        private string LiteralComparison(CosmicQueryPath path, CosmicQueryLiteral literal, CosmicQueryComparisonOperator operation)
        {
            var left = Value(path);
            if (literal.Kind != CosmicQueryScalarKind.Null && literal.Kind != path.ScalarKind)
            {
                if (path.ScalarKind != CosmicQueryScalarKind.Int64) throw new NotSupportedException("Unsupported mixed grouped scalar.");
                var boundary = IntegerBoundary(literal, operation, long.MinValue, long.MaxValue);
                if (boundary.Constant.HasValue) return NullAware(left, new Cell { Value = "0" }, operation, boundary.Constant.Value ? "true" : "false");
                return Compare(left, new Cell { Value = translator.Bind(boundary.Boundary) }, operation);
            }
            return Compare(left, new Cell { Value = literal.Kind == CosmicQueryScalarKind.Null ? "null" : translator.Bind(NeoCqlTranslator.Literal(literal)) }, operation);
        }
        internal string Translate(CosmicQueryPredicate predicate)
        {
            if (predicate is CosmicQueryBoolean boolean) return boolean.Value ? "true" : "false";
            if (predicate is CosmicQueryLogical logical) return "(" + Translate(logical.Left) + (logical.IsAnd ? " AND " : " OR ") + Translate(logical.Right) + ")";
            if (predicate is CosmicQueryNot not) return "(NOT " + Translate(not.Operand) + ")";
            if (predicate is CosmicQueryComparison comparison) return LiteralComparison(comparison.Path, comparison.Literal, comparison.Operator);
            if (predicate is CosmicQueryFieldComparison fields) return Compare(Value(fields.Left), Value(fields.Right), fields.Operator);
            if (predicate is CosmicQueryMembership membership)
                return membership.Values.Count == 0 ? "false" : "(" + string.Join(" OR ", membership.Values.Select(literal => LiteralComparison(membership.Path, literal, CosmicQueryComparisonOperator.Equal))) + ")";
            if (predicate is CosmicQueryStringMatch match)
            {
                var operation = match.Operator == CosmicQueryStringOperator.Contains ? " CONTAINS "
                    : match.Operator == CosmicQueryStringOperator.StartsWith ? " STARTS WITH "
                    : match.Operator == CosmicQueryStringOperator.EndsWith ? " ENDS WITH " : throw new NotSupportedException("Unknown grouped string operation.");
                return "coalesce((" + Value(match.Path).Value + operation + translator.Bind(match.Value) + "),false)";
            }
            throw new NotSupportedException("Unsupported grouped predicate.");
        }
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
