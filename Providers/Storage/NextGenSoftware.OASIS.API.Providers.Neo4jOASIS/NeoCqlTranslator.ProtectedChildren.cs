using System;
using System.Globalization;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Queries;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

internal sealed partial class NeoCqlTranslator
{
    private string TranslateProtectedChild(CosmicQueryAnyChild child)
    {
        var suffix = _relationshipDepth.ToString(CultureInfo.InvariantCulture) + "_" + Parameters.Count.ToString(CultureInfo.InvariantCulture);
        var node = "protectedChild" + suffix;
        var projection = "protectedProjection" + suffix;
        var state = "protectedState" + suffix;
        var inner = new NeoCqlTranslator(node, projection, Parameters, _access, _relationshipDepth + 1);
        var visibility = "true";
        if (_access != null)
        {
            var secured = CosmicQuery<Core.Holons.Holon>.Create().WithReadAccess(_access);
            if (secured.IsError) throw new NotSupportedException(secured.Message);
            visibility = inner.Translate(secured.Result.Predicates.Single());
        }
        var matched = inner.Translate(child.Predicate);
        var cycle = inner.TraversalCycles.Count == 0 ? "false" : "(" + string.Join(" OR ", inner.TraversalCycles) + ")";
        var limited = inner.TraversalLimits.Count == 0 ? "false" : "(" + string.Join(" OR ", inner.TraversalLimits) + ")";
        // Aggregate all visible children, independently of match/cardinality.
        // A first matching child cannot conceal another child's traversal error.
        TraversalClauses.Add("CALL { WITH " + _node + "," + _projection
            + " MATCH (" + _node + ")-[:HAS_CHILD]->(" + node + ":Holon)-[" + projection + ":HAS_CQL_PROJECTION]->(" + node + ")"
            + " WHERE " + ChildScopeCondition(node, projection, visibility)
            + " WITH " + node + "," + projection + " " + string.Concat(inner.TraversalClauses)
            + " WITH coalesce((" + matched + "),false) AS childMatched," + cycle + " AS childCycle," + limited + " AS childLimited"
            + " WITH max(CASE WHEN childMatched THEN 1 ELSE 0 END) AS m,max(CASE WHEN childCycle THEN 1 ELSE 0 END) AS c,"
            + "max(CASE WHEN childLimited THEN 1 ELSE 0 END) AS l"
            + " RETURN {matched:coalesce(m,0)>0,cycle:coalesce(c,0)>0,overBudget:coalesce(l,0)>0} AS " + state + " } ");
        TraversalStates.Add(state);
        TraversalCycles.Add(state + ".cycle");
        TraversalLimits.Add(state + ".overBudget");
        return state + ".matched";
    }
}
