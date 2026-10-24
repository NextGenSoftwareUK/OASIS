using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    internal sealed class NeoCqlNumericValue
    {
        internal long High { get; }
        internal long Low { get; }
        internal long Count { get; }

        internal NeoCqlNumericValue(long high, long low, long count)
        {
            High = high;
            Low = low;
            Count = count;
        }
    }

    internal static NeoCqlNumericValue DecodeCqlNumeric(IReadOnlyDictionary<string, object> row)
    {
        if (row.Count != 7 || !row.TryGetValue("high", out var high) || high is not long highInteger
            || !row.TryGetValue("low", out var low) || low is not long lowInteger
            || !row.TryGetValue("present", out var present) || present is not long count
            || !row.TryGetValue("inputs", out var inputs) || inputs is not long work
            || !row.TryGetValue("valid", out var valid) || valid is not bool
            || !row.TryGetValue("traversalCycle", out var cycle) || cycle is not bool
            || !row.TryGetValue("traversalLimit", out var limit) || limit is not bool
            || count < 0 || work < 0 || count > work)
            throw new InvalidDataException("Neo4j numeric reduction requires exact integer limbs and consistent counts.");
        if (work > CosmicNumericScalarResults.MaximumParticipatingValues)
            throw new NeoCqlLimitException("Neo4j numeric reduction exceeds its input-work budget.");
        return new NeoCqlNumericValue(highInteger, lowInteger, count);
    }

    private static string CompileCqlNumeric<T, TResult>(NeoCqlSource compiled, CosmicScalarQuery<T, TResult> query) where T : IHolon
    {
        if (query.Selector == null || (query.Operation != CosmicScalarOperation.Sum && query.Operation != CosmicScalarOperation.Average))
            throw new NotSupportedException("An admitted integer numeric aggregate is required.");
        var translator = compiled.Translator;
        var cell = translator.TranslateProjectionPath(query.Selector);
        var keys = translator.TranslateOrdering(query.Source.Orderings);
        var aliases = keys.Select((_, index) => "cqlNumericSort" + index).ToArray();
        var keyValues = string.Concat(keys.Select((key, index) => "," + key.Expression + " AS " + aliases[index]));
        var order = string.Join(",", keys.Select((key, index) => aliases[index] + (key.Descending ? " DESC" : " ASC"))
            .Concat(new[] { "p.f_Id ASC" }));
        translator.Parameters.Add("numericOffset", (long)query.Source.Offset);
        // Include one detection row. It is never published as a truncated sum.
        translator.Parameters.Add("numericTake", Math.Min(query.Source.Limit.HasValue ? (long)query.Source.Limit.Value : long.MaxValue,
            (long)CosmicNumericScalarResults.MaximumParticipatingValues + 1));
        var invalid = Nullable.GetUnderlyingType(query.Selector.ValueType) == null ? "selectedState <> 2" : "selectedState = 3";
        const string zeros = "0 AS high,0 AS low,0 AS present,0 AS inputs";
        var traversal = translator.TraversalClauses.Count == 0 ? "" : " UNION ALL " + compiled.EligibleSource
            + "WHERE " + compiled.Cycle + " OR " + compiled.TraversalLimit
            + " RETURN " + zeros + ",false AS valid," + compiled.Cycle + " AS traversalCycle,"
            + compiled.TraversalLimit + " AS traversalLimit LIMIT 1";
        // Integer division and remainder keep both limbs integral. At the work
        // sentinel each limb sum fits Int64 even when the full total does not.
        // Ownership/traversal errors remain outside pagination and reduction.
        return "CALL { " + compiled.Source + "WHERE NOT valid RETURN " + zeros
            + ",false AS valid,false AS traversalCycle,false AS traversalLimit LIMIT 1" + traversal
            + " UNION ALL " + compiled.EligibleSource + "WHERE (" + compiled.DescendantPredicate
            + ") AND NOT(" + compiled.Cycle + " OR " + compiled.TraversalLimit + ") "
            + "WITH h,p" + keyValues + " ORDER BY " + order + " SKIP $numericOffset LIMIT $numericTake "
            + "WITH " + cell.State + " AS selectedState," + cell.Value + " AS selectedValue "
            + "WITH selectedState,CASE WHEN selectedState=2 THEN selectedValue ELSE 0 END AS number "
            + "WITH count(*) AS inputs,sum(CASE WHEN " + invalid + " THEN 1 ELSE 0 END) AS invalid,"
            + "sum(CASE WHEN selectedState=2 THEN 1 ELSE 0 END) AS present,"
            + "sum(number / 4294967296) AS high,sum(number % 4294967296) AS low "
            + "RETURN high,low,present,inputs,invalid=0 AS valid,false AS traversalCycle,false AS traversalLimit } "
            + "RETURN high,low,present,inputs,valid,traversalCycle,traversalLimit";
    }
}
