using System;
using System.Collections.Generic;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

internal sealed class NeoCqlGroupedCommand
{
    internal string Query { get; }
    internal IReadOnlyDictionary<string, object> Parameters { get; }
    internal NeoCqlGroupedCommand(string query, Dictionary<string, object> parameters)
    {
        Query = query;
        Parameters = new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(parameters);
    }
}

public partial class Neo4jOASIS
{
    // This internal compiler grants no native capability. A grouped adapter must
    // admit the intent, bound the complete packet buffer and validate every row.
    internal static NeoCqlGroupedCommand CompileCqlGrouped<T, TKey, TResult>(
        CosmicGroupedSelection<T, TKey, TResult> query, int maximumGroups) where T : IHolon
    {
        if (query == null || maximumGroups < 0 || maximumGroups > query.Grouping.Options.MaximumGroups)
            throw new ArgumentOutOfRangeException(nameof(maximumGroups));
        var compiled = CompileCqlSource(query.Source);
        var translator = compiled.Translator;
        var ordering = translator.TranslateOrdering(query.Source.Orderings);
        var sortValues = string.Concat(ordering.Select((key, index) => "," + key.Expression + " AS sourceSort" + index));
        var sourceOrder = string.Join(",", ordering.Select((key, index) => "sourceSort" + index + (key.Descending ? " DESC" : " ASC"))
            .Concat(new[] { "p.f_Id ASC" }));
        var input = new List<string>();
        var bad = new List<string>();
        var identities = new List<string>();
        var reductions = new List<string>();
        var aggregatePackets = new List<string>();
        string Invalid(CosmicQueryPath path, string state)
            => path.ValueType.IsValueType && Nullable.GetUnderlyingType(path.ValueType) == null
                ? state + "<>2" : state + "=3";

        for (var index = 0; index < query.Grouping.Key.Columns.Count; index++)
        {
            var path = query.Grouping.Key.Columns[index].Path;
            var cell = translator.TranslateProjectionPath(path);
            var name = "k" + index;
            input.Add(cell.State + " AS " + name + "s");
            input.Add(cell.Value + " AS " + name + "v");
            identities.Add("CASE WHEN " + name + "s=2 THEN " + name + "v ELSE null END AS " + name);
            bad.Add(Invalid(path, name + "s"));
        }
        for (var index = 0; index < query.Columns.Count; index++)
        {
            var column = query.Columns[index];
            if (column.KeyIndex >= 0 || column.Selector == null) continue;
            var cell = translator.TranslateProjectionPath(column.Selector);
            var name = "a" + index;
            input.Add(cell.State + " AS " + name + "s");
            input.Add(cell.Value + " AS " + name + "v");
            bad.Add(Invalid(column.Selector, name + "s"));
            reductions.Add("sum(CASE WHEN " + name + "s=2 THEN 1 ELSE 0 END) AS " + name + "n");
            var value = "CASE WHEN " + name + "s=2 THEN " + name + "v ELSE 0 END";
            if (column.Operation is CosmicScalarOperation.Sum or CosmicScalarOperation.Average)
            {
                reductions.Add("sum((" + value + ") / 4294967296) AS " + name + "h");
                reductions.Add("sum((" + value + ") % 4294967296) AS " + name + "l");
                aggregatePackets.Add("{present:" + name + "n,high:" + name + "h,low:" + name + "l}");
            }
            else if (column.Operation is CosmicScalarOperation.Min or CosmicScalarOperation.Max)
            {
                var op = column.Operation == CosmicScalarOperation.Min ? "min" : "max";
                reductions.Add(op + "(CASE WHEN " + name + "s=2 THEN " + name + "v ELSE null END) AS " + name + "x");
                aggregatePackets.Add("{present:" + name + "n,value:" + name + "x}");
            }
            else throw new NotSupportedException("Unknown native grouped aggregate.");
        }
        var keyNames = Enumerable.Range(0, query.Grouping.Key.Columns.Count).Select(i => "k" + i).ToArray();
        var keyCells = "[" + string.Join(",", keyNames.Select(k => "{s:CASE WHEN " + k + " IS NULL THEN 1 ELSE 2 END,v:" + k + "}")) + "]";
        var packets = "[" + string.Join(",", aggregatePackets) + "]";
        translator.Parameters.Add("groupOffset", (long)query.Source.Offset);
        translator.Parameters.Add("groupTake", Math.Min(query.Source.Limit.HasValue ? (long)query.Source.Limit.Value : long.MaxValue,
            (long)CosmicNumericScalarResults.MaximumParticipatingValues + 1));
        translator.Parameters.Add("groupLimit", maximumGroups + 1);
        const string failure = "0 AS inputs,[] AS keys,[] AS aggregates,false AS valid";
        var traversal = translator.TraversalClauses.Count == 0 ? "" : " UNION ALL " + compiled.EligibleSource
            + "WHERE " + compiled.Cycle + " OR " + compiled.TraversalLimit + " RETURN " + failure + ","
            + compiled.Cycle + " AS traversalCycle," + compiled.TraversalLimit + " AS traversalLimit LIMIT 1";
        // Error branches precede pagination. The input sentinel bounds integral
        // limb sums; the reader must sum input proofs before publishing groups.
        var cypher = "CALL { " + compiled.Source + "WHERE NOT valid RETURN " + failure
            + ",false AS traversalCycle,false AS traversalLimit LIMIT 1" + traversal
            + " UNION ALL " + compiled.EligibleSource + "WHERE (" + compiled.DescendantPredicate
            + ") AND NOT(" + compiled.Cycle + " OR " + compiled.TraversalLimit + ") "
            + "WITH h,p" + sortValues + " ORDER BY " + sourceOrder + " SKIP $groupOffset LIMIT $groupTake "
            + "WITH " + string.Join(",", input) + " WITH *,CASE WHEN " + string.Join(" OR ", bad.Select(x => "(" + x + ")"))
            + " THEN 1 ELSE 0 END AS bad "
            + "WITH " + string.Join(",", identities) + ",count(*) AS inputs,sum(bad) AS invalid"
            + (reductions.Count == 0 ? "" : "," + string.Join(",", reductions))
            + " RETURN inputs," + keyCells + " AS keys," + packets + " AS aggregates,invalid=0 AS valid,"
            + "false AS traversalCycle,false AS traversalLimit } RETURN inputs,keys,aggregates,valid,traversalCycle,traversalLimit "
            + "ORDER BY valid ASC," + string.Join(",", keyNames.Select((_, i) => "CASE WHEN keys[" + i + "].v IS NULL THEN 0 ELSE 1 END ASC,keys[" + i + "].v ASC")) + " LIMIT $groupLimit";
        return new NeoCqlGroupedCommand(cypher, translator.Parameters);
    }
}
