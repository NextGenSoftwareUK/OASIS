using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories;

public partial class HolonRepository
{
    private sealed partial class CqlSqlTranslator
    {
        internal string TranslateGroups<T, TKey, TResult>(CosmicGroupedSelection<T, TKey, TResult> query,
            string predicate, string ordering) where T : IHolon
        {
            var input = new List<string>(); var shaped = new List<string>(); var bad = new List<string>();
            var keys = new List<string>(); var reductions = new List<string>(); var output = new List<string>();
            for (var index = 0; index < query.Grouping.Key.Columns.Count; index++)
            {
                var path = query.Grouping.Key.Columns[index].Path;
                var cell = ProjectionOperand(path, "groupKey" + index, comparable: true);
                input.Add(cell.State + " AS k" + index + "s"); input.Add(cell.Value + " AS k" + index + "v");
                shaped.Add("k" + index + "s");
                var type = Nullable.GetUnderlyingType(path.ValueType) ?? path.ValueType;
                var collation = type == typeof(Guid) ? " COLLATE NOCASE" : type == typeof(string) || type == typeof(DateTime) ? " COLLATE BINARY" : "";
                var key = "k" + index + "v";
                shaped.Add("(CASE WHEN k" + index + "s=2 THEN " + key + " ELSE NULL END)" + collation + " AS " + key);
                keys.Add(key);
                var optional = !path.ValueType.IsValueType || Nullable.GetUnderlyingType(path.ValueType) != null;
                bad.Add(optional ? "k" + index + "s=3" : "k" + index + "s<>2");
            }
            for (var index = 0; index < query.Columns.Count; index++)
            {
                var column = query.Columns[index];
                if (column.KeyIndex >= 0 || column.Selector == null) continue;
                var cell = ProjectionOperand(column.Selector, "groupAggregate" + index, comparable: true);
                var state = "a" + index + "s"; var value = "a" + index + "v";
                input.Add(cell.State + " AS " + state); input.Add(cell.Value + " AS " + value);
                shaped.Add(state); shaped.Add(value);
                var optional = !column.Selector.ValueType.IsValueType || Nullable.GetUnderlyingType(column.Selector.ValueType) != null;
                bad.Add(optional ? state + "=3" : state + "<>2");
                var present = "sum(CASE WHEN " + state + "=2 THEN 1 ELSE 0 END)";
                if (column.Operation is CosmicScalarOperation.Sum or CosmicScalarOperation.Average)
                {
                    var high = "a" + index + "h"; var low = "a" + index + "l"; var count = "a" + index + "n";
                    reductions.Add("sum(CASE WHEN " + state + "=2 THEN " + value + "/4294967296 ELSE 0 END) AS " + high);
                    reductions.Add("sum(CASE WHEN " + state + "=2 THEN " + value + "%4294967296 ELSE 0 END) AS " + low);
                    reductions.Add(present + " AS " + count);
                    output.Add(high); output.Add(low); output.Add(count);
                }
                else if (column.Operation is CosmicScalarOperation.Min or CosmicScalarOperation.Max)
                {
                    var aggregate = column.Operation == CosmicScalarOperation.Min ? "min" : "max";
                    var scalarType = Nullable.GetUnderlyingType(column.Selector.ValueType) ?? column.Selector.ValueType;
                    var comparison = scalarType == typeof(Guid) ? " COLLATE NOCASE" : " COLLATE BINARY";
                    reductions.Add(aggregate + "(CASE WHEN " + state + "=2 THEN " + value + " ELSE NULL END" + comparison + ") AS " + value);
                    output.Add(value);
                }
                else throw new NotSupportedException("Unknown grouped native aggregate.");
            }
            // The selected window is bounded by an extra work detection row.
            // It cannot become a successful truncated grouping or limb sum.
            // Compare exact integral sums as floor quotient/remainder pairs. Rebuilding
            // the total here could overflow Int64; floating point would lose adjacent values.
            var resultOrder = new List<string>();
            foreach (var selectedOrder in query.Orderings)
            {
                var column = query.Columns[selectedOrder.ColumnIndex];
                var direction = selectedOrder.Descending ? " DESC" : " ASC";
                if (column.KeyIndex >= 0)
                    resultOrder.Add("k" + column.KeyIndex + "v" + direction);
                else if (column.Operation is CosmicScalarOperation.Count or CosmicScalarOperation.LongCount)
                    resultOrder.Add("inputs" + direction);
                else if (column.Operation is CosmicScalarOperation.Min or CosmicScalarOperation.Max)
                {
                    var type = Nullable.GetUnderlyingType(column.Selector.ValueType) ?? column.Selector.ValueType;
                    resultOrder.Add("a" + selectedOrder.ColumnIndex + "v" +
                        (type == typeof(Guid) ? " COLLATE NOCASE" : " COLLATE BINARY") + direction);
                }
                else if (column.Operation == CosmicScalarOperation.Sum)
                {
                    var parts = GroupedIntegralParts("a" + selectedOrder.ColumnIndex + "h", "a" + selectedOrder.ColumnIndex + "l");
                    resultOrder.Add(parts.High + direction); resultOrder.Add(parts.Low + direction);
                }
                else throw new NotSupportedException("Grouped aggregate ordering is not enrolled for this operation.");
            }
            // A complete identity tie-break makes limits deterministic across providers.
            resultOrder.AddRange(keys.Select(key => key + " ASC"));
            var tail = output.Count == 0 ? "" : "," + string.Join(",", output);
            var filter = query.ResultPredicates.Count == 0 ? "1" : string.Join(" AND ",
                query.ResultPredicates.Select(value => TranslateGroupPredicate(query, value)));
            return "/* COSMIC grouped */ WITH selected AS (SELECT " + string.Join(",", input)
                + " FROM Holon WHERE " + predicate + " ORDER BY " + ordering + " LIMIT $groupTake OFFSET $groupOffset),"
                + " shaped AS (SELECT " + string.Join(",", shaped) + " FROM selected),"
                + " reduced AS (SELECT " + string.Join(",", keys) + ",count(*) AS inputs,"
                + "sum(CASE WHEN " + string.Join(" OR ", bad.Select(condition => "(" + condition + ")")) + " THEN 1 ELSE 0 END) AS invalid"
                + (reductions.Count == 0 ? "" : "," + string.Join(",", reductions)) + " FROM shaped GROUP BY " + string.Join(",", keys) + "), filtered AS (SELECT *,CASE WHEN " + filter + " THEN 1 ELSE 0 END AS filterMember FROM reduced), ranked AS (SELECT *,row_number() OVER (ORDER BY "
                + string.Join(",", resultOrder) + ") AS groupPosition,sum(filterMember) OVER (ORDER BY " + string.Join(",", resultOrder)
                + " ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS matchedPosition FROM filtered) "
                + "SELECT invalid,(SELECT count(*) FROM selected) AS work,inputs," + string.Join(",", keys) + tail
                + ",CASE WHEN filterMember=1 AND matchedPosition>$resultOffset AND ($resultTake IS NULL OR matchedPosition<=$resultOffset+$resultTake) THEN 1 ELSE 0 END AS resultMember"
                + " FROM ranked ORDER BY groupPosition LIMIT $groupLimit";
        }
    }
}
