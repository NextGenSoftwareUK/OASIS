using System;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    private static string CompileCqlExtremum<T, TResult>(NeoCqlSource compiled, CosmicScalarQuery<T, TResult> query) where T : IHolon
    {
        var translator = compiled.Translator;
        var cell = translator.TranslateProjectionPath(query.Selector);
        var keys = translator.TranslateOrdering(query.Source.Orderings);
        var aliases = keys.Select((_, index) => "cqlScalarSort" + index).ToArray();
        var keyValues = string.Concat(keys.Select((key, index) => "," + key.Expression + " AS " + aliases[index]));
        var order = string.Join(",", keys.Select((key, index) => aliases[index] + (key.Descending ? " DESC" : " ASC"))
            .Concat(new[] { "p.f_Id ASC" }));
        translator.Parameters.Add("scalarOffset", (long)query.Source.Offset);
        translator.Parameters.Add("scalarTake", query.Source.Limit.HasValue ? (long)query.Source.Limit.Value : long.MaxValue);
        translator.Parameters.Add("scalarBytes", CosmicProjectionResults.MaximumPayloadLimit);
        var required = query.Selector.ValueType.IsValueType && Nullable.GetUnderlyingType(query.Selector.ValueType) == null;
        var invalid = required ? "selectedState <> 2" : "selectedState = 3";
        var reduction = query.Operation == CosmicScalarOperation.Min ? "min" : "max";
        var payloadLimit = query.Selector.ValueType == typeof(string)
            ? "CASE WHEN extremum IS NULL THEN false ELSE size(extremum) > $scalarBytes END" : "false";
        const string empty = "null AS value,false AS valid,false AS traversalCycle,false AS traversalLimit,0 AS present,false AS payloadLimit";
        var traversal = translator.TraversalClauses.Count == 0 ? "" : " UNION ALL " + compiled.EligibleSource
            + "WHERE " + compiled.Cycle + " OR " + compiled.TraversalLimit
            + " RETURN null AS value,false AS valid," + compiled.Cycle + " AS traversalCycle,"
            + compiled.TraversalLimit + " AS traversalLimit,0 AS present,false AS payloadLimit LIMIT 1";
        // Ownership and traversal errors remain outside both the source window
        // and aggregate reduction. Only selected-domain validation follows paging.
        return "CALL { " + compiled.Source + "WHERE NOT valid RETURN " + empty + " LIMIT 1" + traversal
            + " UNION ALL " + compiled.EligibleSource + "WHERE (" + compiled.DescendantPredicate
            + ") AND NOT(" + compiled.Cycle + " OR " + compiled.TraversalLimit + ") "
            + "WITH h,p" + keyValues + " ORDER BY " + order + " SKIP $scalarOffset LIMIT $scalarTake "
            + "WITH " + cell.State + " AS selectedState," + cell.Value + " AS selectedValue "
            + "WITH sum(CASE WHEN " + invalid + " THEN 1 ELSE 0 END) AS invalid,count(selectedValue) AS present,"
            + reduction + "(selectedValue) AS extremum "
            + "RETURN CASE WHEN " + payloadLimit + " THEN null ELSE extremum END AS value,invalid=0 AS valid,"
            + "false AS traversalCycle,false AS traversalLimit,present," + payloadLimit + " AS payloadLimit } "
            + "RETURN value,valid,traversalCycle,traversalLimit,present,payloadLimit";
    }
}
