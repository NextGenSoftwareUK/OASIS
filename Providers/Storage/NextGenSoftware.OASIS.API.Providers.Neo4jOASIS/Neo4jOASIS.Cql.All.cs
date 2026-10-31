using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    private static string CompileCqlAll<T, TResult>(NeoCqlSource compiled, CosmicScalarQuery<T, TResult> query) where T : IHolon
        => CompileCqlTerminalPredicate(compiled, query, false);

    private static string CompileCqlCardinalityPredicate<T, TResult>(NeoCqlSource compiled, CosmicScalarQuery<T, TResult> query) where T : IHolon
        => CompileCqlTerminalPredicate(compiled, query, true);

    private static string CompileCqlTerminalPredicate<T, TResult>(NeoCqlSource compiled, CosmicScalarQuery<T, TResult> query, bool countMatches) where T : IHolon
    {
        var translator = compiled.Translator;
        var keys = translator.TranslateOrdering(query.Source.Orderings);
        var order = string.Join(",", keys.Select(key => key.Expression + (key.Descending ? " DESC" : " ASC"))
            .Concat(new[] { "p.f_Id ASC" }));
        // Terminal state belongs after the source window. Never append it to
        // the source traversal clauses, whose protected scope is different.
        var terminal = new NeoCqlTranslator("h", "p", translator.Parameters, query.Source.ReadAccess);
        var predicate = string.Join(" AND ", query.TerminalPredicate.Predicates.Select(terminal.Translate));
        var terminalCycle = terminal.TraversalCycles.Count == 0 ? "false" : "(" + string.Join(" OR ", terminal.TraversalCycles) + ")";
        var terminalLimit = terminal.TraversalLimits.Count == 0 ? "false" : "(" + string.Join(" OR ", terminal.TraversalLimits) + ")";
        translator.Parameters.Add("allOffset", (long)query.Source.Offset);
        translator.Parameters.Add("allTake", query.Source.Limit.HasValue ? (long)query.Source.Limit.Value : long.MaxValue);
        var traversalError = translator.TraversalClauses.Count == 0 ? "" :
            " UNION ALL " + compiled.EligibleSource + "WHERE " + compiled.Cycle + " OR " + compiled.TraversalLimit
            + " RETURN 0 AS value,false AS valid," + compiled.Cycle + " AS traversalCycle,"
            + compiled.TraversalLimit + " AS traversalLimit LIMIT 1";
        return "CALL { " + compiled.Source
            + "WHERE NOT valid RETURN 0 AS value,false AS valid,false AS traversalCycle,false AS traversalLimit LIMIT 1"
            + traversalError + " UNION ALL " + compiled.EligibleSource + "WHERE (" + compiled.DescendantPredicate
            + ") AND NOT(" + compiled.Cycle + " OR " + compiled.TraversalLimit + ") "
            + "WITH h,p ORDER BY " + order + " SKIP $allOffset LIMIT $allTake "
            + string.Concat(terminal.TraversalClauses)
            // Aggregate alone first: grouping by the validity constants in the
            // same projection suppresses Neo4j's aggregate row on empty input.
            + "WITH sum(CASE WHEN " + terminalCycle + " OR " + terminalLimit + " THEN 0 WHEN coalesce((" + predicate + "),false) THEN " + (countMatches ? "1 ELSE 0" : "0 ELSE 1") + " END) AS total,"
            + "max(CASE WHEN " + terminalCycle + " THEN 1 ELSE 0 END) AS cycle,max(CASE WHEN " + terminalLimit + " THEN 1 ELSE 0 END) AS limited "
            + "RETURN total AS value,true AS valid,coalesce(cycle,0)>0 AS traversalCycle,coalesce(limited,0)>0 AS traversalLimit } RETURN value,valid,traversalCycle,traversalLimit";
    }
}
