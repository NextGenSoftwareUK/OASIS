using System;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

public partial class Neo4jOASIS
{
    // The source owner must first admit/normalize the declared finite value.
    // Extract each binary word before scaling, avoiding whole-value overflow
    // and any conversion through decimal text or sequential floating sums.
    internal static string FloatingAggregateParts(string value, bool single, Func<object, string> bind)
    {
        var width = single ? CosmicFloatingAggregateResults.SingleLimbCount : CosmicFloatingAggregateResults.DoubleLimbCount;
        var minimumExponent = single ? -149 : -1074;
        var lower = bind(Enumerable.Range(0, width).Select(index => Math.Pow(2, minimumExponent + 30 * index)).ToArray());
        var upper = bind(Enumerable.Range(0, width).Select(index => !single && index == width - 1
            ? 0d : Math.Pow(2, minimumExponent + 30 * index + 30)).ToArray());
        var remainder = !single
            ? "CASE WHEN cfWord=" + (width - 1) + " THEN cfMagnitude ELSE cfMagnitude%" + upper + "[cfWord] END"
            : "cfMagnitude%" + upper + "[cfWord]";
        return "head([cfInput IN [" + value + "] | head([cfMagnitude IN [abs(cfInput)] | "
            + "[cfWord IN range(" + (width - 1) + ",0,-1) | toInteger((" + remainder + ")/" + lower
            + "[cfWord])*(CASE WHEN cfInput<0 THEN -1 ELSE 1 END)]])])";
    }
}
