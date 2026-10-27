using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    private static string CompileCqlAll<T, TResult>(NeoCqlSource compiled, CosmicScalarQuery<T, TResult> query) where T : IHolon
    {
        var translator = compiled.Translator;
        var keys = translator.TranslateOrdering(query.Source.Orderings);
        var order = string.Join(",", keys.Select(key => key.Expression + (key.Descending ? " DESC" : " ASC"))
            .Concat(new[] { "p.f_Id ASC" }));
        var predicate = string.Join(" AND ", query.AllPredicate.Predicates.Select(translator.Translate));
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
            // Aggregate alone first: grouping by the validity constants in the
            // same projection suppresses Neo4j's aggregate row on empty input.
            + "WITH sum(CASE WHEN coalesce((" + predicate + "),false) THEN 0 ELSE 1 END) AS total "
            + "RETURN total AS value,true AS valid,false AS traversalCycle,false AS traversalLimit } RETURN value,valid,traversalCycle,traversalLimit";
    }
}
